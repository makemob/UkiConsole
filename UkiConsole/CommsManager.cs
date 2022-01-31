using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net;

using System.Net;      //required
using System.Net.Sockets;    //required


namespace UkiConsole
{
    class CommsManager
    {
        private Listener _listener;
        private SendWrapper _sender;
        private Dictionary<String, ModbusManager> _myManagers = new();
        private string _mode ;
        private ConcurrentQueue<RawMove> _control = new ConcurrentQueue<RawMove>();
        private ConcurrentQueue<RawMove> _rawIn = new ConcurrentQueue<RawMove>();
        private AxisManager _axes;
        private bool _run = false;
        private ShowRunner _showrunner;
        private int port = 9000;
        private String addr = "192.168.1.110";
       
        public CommsManager(string Mode,  ShowRunner Runner, Dictionary<String, ModbusManager> Managers )
        {
            _mode = Mode;
            _showrunner = Runner;
            _axes = _showrunner.Axes;
            _myManagers = Managers;

            spawn_comms();

            foreach(ModbusManager mm in _myManagers.Values)
            {
                mm.commsSender = _sender;
            }

            Thread sendThread = new Thread(_sender.Run);
            sendThread.Start();
            Thread listenThread = new Thread(_listener.Receive);
            listenThread.Start();
            _run = true;
        }

        public ConcurrentQueue<RawMove> Control { get => _control; set => _control = value; }
        public ConcurrentQueue<RawMove> Moves { get => _rawIn; set => _rawIn = value; }

        public void Run()
        {
            while (_run == true)
            {


                if (Control.IsEmpty)
                {
                   // System.Diagnostics.Debug.Write("Control is empty");

                    if (!Moves.IsEmpty)
                    {

                      // System.Diagnostics.Debug.Write("Parsing move");
                        RawMove _mv;
                        Moves.TryDequeue(out _mv);
                        if (_mv is not null)
                        {
                            if ( _axes.IsEnabled(_mv.Addr))
                            {
                                // XXX Check max values and convert (max motor speed, etc)
                                _mv = _axes.Validate(_mv);
                               // System.Diagnostics.Debug.WriteLine("Commsman - {0} :{1} :{2}", _mv.Addr, _mv.Reg, _mv.Val);

                                ModbusManager.command cmd = new ModbusManager.command() { address = int.Parse(_mv.Addr), register = _mv.Reg, value = _mv.Val };

                                String port = _axes.GetAxisConfig(_mv.Addr, "port");
                                _myManagers[port].Command.Enqueue(cmd);


                            }
                        }

                    }
                }
                else
                {

                    
                    RawMove _mv;
                    Control.TryDequeue(out _mv);
                    if (_mv.Addr == "240")
                    {

                       // return;
                        //heartbeat stuff
                    }
                    else
                    {
                        // Control messages can be for all axes - send to both ports.
                        ModbusManager.command cmd = new ModbusManager.command() { address = int.Parse(_mv.Addr), register = _mv.Reg, value = _mv.Val };
                        if (_mv.Reg == 208)
                        {
                            //Setting Estop
                            if (_mv.Addr == "0")
                            {
                                foreach (ModbusManager mm in _myManagers.Values)
                                {
                                    mm.SendStopToAll();
                                }
                            }
                            else
                            {
                                String port = _axes.GetAxisConfig(_mv.Addr, "port");
                                _myManagers[port].Command.Enqueue(cmd);
                            }
                        }
                        if ( _mv.Reg == 209)
                        {
                            if (_mv.Addr == "0")
                            {
                                foreach (ModbusManager mm in _myManagers.Values)
                                {
                                    mm.SendClearToAll();
                                }
                            }
                            else
                            {
                                String port = _axes.GetAxisConfig(_mv.Addr, "port");
                                _myManagers[port].Command.Enqueue(cmd);
                            }
                        }
                        foreach (ModbusManager mm in _myManagers.Values)
                        {
                           // System.Diagnostics.Debug.Write("Parsing control");

                            mm.Command.Enqueue(cmd);
                        }
                    }
                }

            }

        }
    
        private void spawn_comms()
        {
            if (_mode == "TCP")
            {
                TcpClient _tcplisten = new TcpClient();
                try
                {
                    _tcplisten.Connect("192.168.1.107", 9000);
                }catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("no connection");
                }
                TcpClient _tcpSend = new TcpClient();
               
               

                spawn_tcplistener(_tcplisten);
                spawn_tcpsender(_tcpSend);
            }
            else
            {
                spawn_listener();
                spawn_sender();
            }

        }
       
        private void spawn_listener()
        {
            
            // delegates and dictionaries...
            if (_mode == "UDP") { 
            _listener =  new UDPListener(9000, Moves, Control) as Listener;
            
            }

        }

        private void spawn_tcpsender(TcpClient tcpclient)
        {
            Sender _netsender = new TCPSender(tcpclient) as Sender;
            _sender = new SendWrapper(_netsender, _axes);

        }
        private void spawn_tcplistener(TcpClient tcpclient)
        {
            _listener = new TCPListener(tcpclient, Moves, Control) as Listener;
            
        }
        public void ShutDown()
        {
            _sender.ShutDown();
            _listener.ShutDown();
        }
        private void spawn_sender()
        {
            if (_mode == "UDP")
            {
                try
                {
                    Sender _netsender = new UDPSender("192.168.1.110", 10001) as Sender;
                    _sender = new SendWrapper(_netsender, _axes);


                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

            }
        
        }

    }
}
