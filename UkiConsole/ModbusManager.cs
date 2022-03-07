using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using NModbus;
using NModbus.Serial;
using System.ComponentModel;


namespace UkiConsole
{
    class ModbusManager
    {
        public struct command
        {
            public int address;
            public int register;
            public int value;
        };

        //store timeouts so we stop talking to them
        // Should have some way to remove them on a clear from above
        private List<int> _blacklist = new List<int>();
        private Dictionary<int, int> _registers = new Dictionary<int, int>();
        // _query is a list of addresses and registers to ask about
        // _command is a move command to send (the main difference is whether we want a response or not
        // _control is meta commands for the listener
        private ConcurrentQueue<Dictionary<String, int>> _query = new ConcurrentQueue<Dictionary<String, int>>();
        private ConcurrentQueue<command> _command = new ConcurrentQueue<command>();
        private ConcurrentQueue<String> _controlIn = new ConcurrentQueue<String>();
        // _result is address, register and value
        // _messageOut is meta again.
        private ConcurrentQueue<Dictionary<String, int[]>> _results = new ConcurrentQueue<Dictionary<String, int[]>>();
        private ConcurrentQueue<String> _messageOut = new ConcurrentQueue<String>();
        private List<int> _axes;
        private Timer miniPoll;
        private IModbusMaster _myStream;
        private bool _connected = false;
        private int _nextessential = 0;
        private List<int> _essential_reg;
        private SendWrapper _mysender ;
        private bool _run = true;
        public event PropertyChangedEventHandler PropertyChanged;
        public ConcurrentQueue<String> Control { get => _controlIn; }
        public ConcurrentQueue<Dictionary<String, int[]>> Results { get => _results; }
        public ConcurrentQueue<Dictionary<String, int>> Query { get => _query; }
        internal ConcurrentQueue<command> Command { get => _command; }
        public ConcurrentQueue<string> MessageOut { get => _messageOut; }
        public List<int> Axes { get => _axes; }
        public SendWrapper commsSender { get => _mysender; set => _mysender = value; }
        public bool Connected { get => _connected; }
        private SerialPort _serialPort;
        private String _comport;
        // axes is the comport map - it gives Serial ports and the axes attached
        public ModbusManager(String comport, List<String> axes, List<int> essentials)
        {
            var factory = new ModbusFactory();
            _axes = axes.Select(s => int.Parse(s)).ToList();
            _essential_reg = essentials;
            _comport = comport;
            try
            {

                Connect();
                try
                {
                    _serialPort.Open();
                    checkConnection(true);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Could not connect {0} : {1}", _comport, e.Message);
                    checkConnection(false);
                }
                 _myStream = factory.CreateRtuMaster(_serialPort);
                


            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("No such comport {0} : {1}", _comport, e.Message);
                checkConnection(false);
            }






        }
        protected void OnPropertyChanged(string propertyname)
        {
            PropertyChangedEventHandler eh = PropertyChanged;
            if (eh != null)
            {
                var en = new PropertyChangedEventArgs(_comport);
                eh(this, en);
            }
        }
        private void checkConnection(bool state)
        {
            if (_connected != state)
            {
                 System.Diagnostics.Debug.WriteLine(String.Format("Changed connection: {0} : {1}", _comport, state));
                _connected = state;
                if (! _connected )
                {
                    try
                    {
                        Connect();
                        
                        System.Diagnostics.Debug.WriteLine(String.Format(" {0} Connected", _comport));

                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Could not connect");
                    }
                }
                OnPropertyChanged("mmConnected");

            }
        }

        private void Connect()
        {
            _serialPort = new SerialPort();
            _serialPort.PortName = _comport;
            _serialPort.BaudRate = 19200;
            _serialPort.Parity = System.IO.Ports.Parity.None;
            _serialPort.StopBits = System.IO.Ports.StopBits.One;
            _serialPort.ReadTimeout = 100;
            _serialPort.WriteTimeout = 100;

        }

        public void Reconnect()
        {
            try
            {
                _serialPort.Close();
                _serialPort.Open();
                checkConnection(true);
            }
            catch (Exception e)
            {
                ShutDown();
                Connect();
                _serialPort.Open();
                checkConnection(true);
            }
        }
        public void Listen()
        {

            OnPropertyChanged("Connected");
            string myControl;

            while (_run)
            {
                
                if (!_serialPort.IsOpen)
                {
                    checkConnection(false);
                    return;
                }

                while (!Control.IsEmpty)
                {
                    
                    Control.TryDequeue(out myControl);
                    // Convert all this to delegates....
                    if (myControl.Equals("ESTOP"))
                    {
                        System.Diagnostics.Debug.WriteLine(myControl);

                        //miniPoll.Change(Timeout.Infinite, Timeout.Infinite);
                        // Send Estop to everything
                        SendStopToAll();
                        // miniPoll.Change(5, 500);
                        //_run = false;
                    }
                    else if (myControl.Equals("CLEAR_ESTOP"))
                    {
                        SendClearToAll();
                    }
                    else if (myControl.Equals("SHUTDOWN"))
                    {
                        SendStopToAll();
                        _run = false;
                    }





                }
                while (!Command.IsEmpty)
                {
                    

                    command cm;
                    Command.TryDequeue(out cm);
                    // System.Diagnostics.Debug.WriteLine(" MM Got {0} : {1}, {2}", cm.address, ModMap.RevMap(cm.register), cm.value);
                    if (Axes.Contains(cm.address))
                    {
                        sendRegister(cm.address, cm.register, cm.value);
                        System.Diagnostics.Debug.WriteLine(" MM Sent {0} : {1}, {2}", cm.address, ModMap.RevMap(cm.register), cm.value);

                        if (cm.register.Equals(ModMap.RegMap.MB_GOTO_POSITION))
                        {
                            confirmTarget(cm.address, cm.register, cm.value);

                        }
                    }
                }
                readEssential();

            }
            Control.Enqueue("STOPPED");
        }

        public void ShutDown()
        {
            System.Diagnostics.Debug.WriteLine("closing mm");

            SendStopToAll();
            _run = false;
            _serialPort.Close();
            _serialPort.Dispose();
            _connected = false;
            OnPropertyChanged("mmConnected");
        }
        private void readEssential()
        {
            Dictionary<String, int[]> _result = new Dictionary<string, int[]>();

            if (Axes.Count > 0)
            {

                // This should always be true, but just in case....
                if (_nextessential < Axes.Count)
                {
                    
               

                    byte addr = (byte) Axes[_nextessential];
                    if (!_blacklist.Contains(addr))
                    {
                        try
                        {
                            foreach (int reg in _essential_reg)
                            {
                                ushort[] resp;
                                resp = _myStream.ReadHoldingRegisters(addr, (ushort)reg, 1);

                                
                                //ushort newdata = resp[0];
                                // ushort nreg = resp[1];
                                ushort _val = resp[0];


                                RawMove _mv = new RawMove(addr.ToString(), reg, _val);

                                if (commsSender is not null) {
                                  
                                    commsSender.Enqueue(_mv);
                                }

                                _result[addr.ToString()] = new int[2] { reg, _val };
                                Results.Enqueue(_result);
                                //  System.Diagnostics.Debug.WriteLine("It worked! {0}: {1} ({2}) : {3}",addr, ModMap.RevMap(reg), reg, _val);
                            }
                        }
                        catch(Exception e)
                        {
                            // Should set to disabled so we don't get constant errors
                            MessageOut.Enqueue(String.Format("TIMEOUT:{0}", addr));
                            _blacklist.Add(addr);
                            System.Diagnostics.Debug.WriteLine("TIMEOUT, {0}", e.Message);

                        }

                        // Might be better to do it in one hit. We'll see.

                    }


                }

                _nextessential = (_nextessential + 1) % Axes.Count;
            }
            //  System.Diagnostics.Debug.WriteLine("Updoot 2 {0}", _nextessential);


        }
        public void SendStopToAll()
        {
            System.Diagnostics.Debug.WriteLine("Stopping");
            int ESTOP = (int)ModMap.RegMap.MB_ESTOP;
            foreach (int addr in Axes)
            {
                sendRegister(addr, ESTOP, 1);

            }
        }
        public void SendClearToAll()
        {
            _blacklist = new List<int>();
            // System.Diagnostics.Debug.WriteLine("Clearing");
            int CLEAR = (int)ModMap.RegMap.MB_RESET_ESTOP;
            foreach (int addr in Axes)
            {
                sendRegister(addr, CLEAR, 0x5050);

            }
            
        }
        private void confirmTarget(int addr, int register, int value)
        {
            int bufsize = 6;
            ushort[] resp = new ushort[bufsize];
            resp = _myStream.ReadInputRegisters((byte)addr, (ushort)register, 1);
            while (resp[0] != value)
            {
                sendRegister(addr, register, value);
                resp = _myStream.ReadInputRegisters((byte)addr, (ushort)register, 1);
                
            }
        }
        public void sendRegister(int addr, int register, int value)
        {

            if (!_serialPort.IsOpen)
            {
                checkConnection(false);
                return;
            }

            try
            {
                _myStream.WriteSingleRegister((byte)addr, (ushort)register, (ushort)value);
                System.Diagnostics.Debug.WriteLine(String.Format(" SENT IN REG {0}", addr));

            }
            catch (Exception e)
            {

                System.Diagnostics.Debug.WriteLine(String.Format(" Could not send register {0}: {1}", addr, e.Message));
            }
            

        }
        public void sendRegisters(int addr, int startAddr, List<int> values)
        {
            if (!_serialPort.IsOpen)
            {
                checkConnection(false);
                return;
            }
            List<ushort> _vals = new List<ushort>();

            foreach (int i in values)
            {
                byte[] bval = BitConverter.GetBytes(i);
                _vals.Add(bval[2]);
                _vals.Add(bval[3]);

            }
            //int axis = int.Parse(addr);
            _myStream.WriteMultipleRegistersAsync((byte)addr, (ushort)startAddr, _vals.ToArray());

        }


    }
}
