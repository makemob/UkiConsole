using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net;


using System.Threading.Tasks;

namespace UkiConsole
{
    class ShowRunner : INotifyPropertyChanged
    {
        
        // Comport -> manager instance
        private  Dictionary <String, ModbusManager> _myManagers = new();
        private AxisManager _axes;
        // This is really currently a shim from GUI to Modbus
        // At some point the comms between GUI and Showrunner will become more something.
        // Probably actual axes - GUI sends the changes, Showrunner updates axes directly.
        // How do we do cues?
        // Main question about cues - where is timing done?

        // Also, the UDP listener and sender really should be here so we can decouple from the main GUI
        private ConcurrentQueue<AxisMove> _moveIn = new ConcurrentQueue<AxisMove>();
        private ConcurrentQueue<RawMove> _rawIn = new ConcurrentQueue<RawMove>();

        private ConcurrentQueue<List<String>> _queryOut = new();
        private ConcurrentQueue<String> _control = new ConcurrentQueue<String>();
        private ConcurrentQueue<String> _message = new ConcurrentQueue<String>();
        private List<int> _essentials ;
        private bool _estopped = true;
        private bool _toggled = false;
        private UDPSender _udpSender = new UDPSender();
        
        private Dictionary<String, String> _comport = new();
        public event PropertyChangedEventHandler PropertyChanged;
        public ConcurrentQueue<string> Control { get => _control;  }
        public bool Estopped { get => _estopped; }
        
        public ConcurrentQueue<AxisMove> MoveIn { get => _moveIn;  }
        public ConcurrentQueue<RawMove> RawIn { get => _rawIn; }

        public ConcurrentQueue<List<String>> QueryOut { get => _queryOut; }




        public ShowRunner(Dictionary<String,String> comport, AxisManager axes , List<int> essentials)
        {
            _axes = axes;
            _essentials = essentials;
            _comport = comport;
            // Move this, etc....
            _udpSender.Server("127.0.0.1", 10001);
            // We build address lists here so ModbusManager can spin up what it
            // needs, but then we never need to worry about them again
            Dictionary<String, List<String>> _portmap = _axes.ListAxesByPort();
            Dictionary<String,List<String>> _portlists = new();




            foreach(KeyValuePair<String, List<String>> plist in _portmap)
            {
                _portlists[comport[plist.Key]] = new();
                
                foreach (String ax in plist.Value)
                {
                    _portlists[comport[plist.Key]].Add(ax);
                }
            }

            foreach (KeyValuePair<String, String> kvp in comport)
            {
                if (_portlists.ContainsKey(kvp.Value))
                {
                    // So after all that, here we have "left => Com3"
                    _myManagers[kvp.Key] = new ModbusManager(kvp.Value, _portlists[kvp.Value], _essentials);

                    _myManagers[kvp.Key].Connect();
                    Thread manThread = new Thread(_myManagers[kvp.Key].Listen);
                    manThread.Start();
                }
            }
            
        }
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler eh = PropertyChanged;
            if (eh != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                eh(this, e);
            }
        }

    
    public void Listen()
        {
             
            bool _run = true;
            while (_run)
            {
                string command;

                while (_control.Count > 0)
                {

                    Control.TryDequeue(out command);

                    foreach (ModbusManager _myManager in _myManagers.Values)
                    {
                        // Delegates, Jai, delegates....
                        _myManager.Control.Enqueue(command);
                        if (command.Equals("SHUTDOWN"))
                        {

                            _run = false;
                        }
                        else if (command.Equals("CALIBRATE"))
                        {
                            foreach (String ax in _axes.AddressList)
                            {
                                if (_axes.IsEnabled(ax))
                                {
                                    ModbusManager.command cmd = new ModbusManager.command() { address = int.Parse(ax), register = (int)ModMap.RegMap.MB_FORCE_CALIBRATE_ENCODER, value = 0xA0A0 };


                                    _myManager.Command.Enqueue(cmd);

                                }
                            }
                        }else if (command.Equals("CONFIG"))
                        {
                            ConfigAll();
                        }

                    }
                }
                foreach (ModbusManager _myManager in _myManagers.Values)
                {

                    while (! _myManager.MessageOut.IsEmpty)
                    {
                        String message;
                        _myManager.MessageOut.TryDequeue(out message);
                        if (message.StartsWith("TIMEOUT"))
                        {
                            String[] res = message.Split(":");
                            _axes.SetAxisConfig(res[1].Trim(), "enabled", "false");
                            _axes.SetAxisAttribute(res[1].Trim(), "estop", 8);
                            System.Diagnostics.Debug.WriteLine("Set {0} timed out", res[1].Trim());
                            _queryOut.Enqueue(new List<string>() { res[1].Trim() });
                            OnPropertyChanged("Toggle");

                        }

                    }
                }
                // rawMove and moveIn should really be the same queue - we don't care where a move comes from.
                while (_moveIn.Count > 0)
                {
                    AxisMove mv;
                    MoveIn.TryDequeue(out mv);
                    if (mv is not null)
                    {
                        try
                        {
                            String addr = _axes.AddressFromLabel(mv.Name);
                            String port = _axes.GetAxisConfig(addr, "port");
                            if (_axes.IsEnabled(addr)) { 
                                foreach (KeyValuePair<string, int> kvp in mv.Targets)
                                {
                                    // reverse lookup for register names to target
                                    ModMap.RegMap reg = ModMap.regFromTarget(mv.Type,kvp.Key);
                                    ModbusManager.command cmd = new ModbusManager.command { address = int.Parse(addr), register = (int)reg, value = kvp.Value };
                                    System.Diagnostics.Debug.WriteLine(String.Format("Command: {0} : {1} : {2} ({3},{4})",mv.Name, reg, kvp.Value, addr, port));

                                    _myManagers[port].Command.Enqueue(cmd);
                                }
                            }
                        }catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine(e.Message);
                        }

                    }
                }
                while (! _rawIn.IsEmpty)
                {
                    RawMove _mv;
                    _rawIn.TryDequeue(out _mv);
                    if (_mv is not null)
                    {
                       // System.Diagnostics.Debug.WriteLine("Got raw");

                        if (_axes.IsEnabled(_mv.Addr))
                        {
                            // XXX Check max values and convert (max motor speed, etc)
                            _mv = _axes.Validate(_mv);
                            System.Diagnostics.Debug.WriteLine("Showrunner - {0} :{1} :{2}", _mv.Addr, _mv.Reg, _mv.Val);

                            ModbusManager.command cmd = new ModbusManager.command() { address = int.Parse(_mv.Addr), register = _mv.Reg, value = _mv.Val };

                            String port = _axes.GetAxisConfig(_mv.Addr, "port");
                            _myManagers[port].Command.Enqueue(cmd);
                            // We then want to send actual position status as read from the Essential read.

                            if (_axes.HasStatus(_mv.Addr.ToString(), _mv.Reg.ToString()))
                            {
                                int val = _axes.GetAxisStatus(_mv.Addr.ToString(), _mv.Reg.ToString());
                                

                               // RawMove udpResponse = new RawMove(_mv.Addr, _mv.Reg, val);
                              //  _udpSender.sendStatus(udpResponse);
                            }
                        }
                    }

                  //  System.Diagnostics.Debug.WriteLine("Done raw");
                }
                checkStatus();
                
            }
        }
       
        private void checkStatus()
        {
            Dictionary<String, int[]> _results = new Dictionary<String, int[]>();
            foreach (ModbusManager _myManager in _myManagers.Values)
            {
                while (!_myManager.Results.IsEmpty)
                {
                    _myManager.Results.TryDequeue(out _results);
                    if (_results is not null)
                    {


                        foreach (KeyValuePair<String, int[]> res in _results)
                        {
                            // Christ, this should all just be rawmoves internally for axis, reg, value.
                            int _reg = res.Value[0];
                            int _val = res.Value[1];
                            // Don't need the out queue, just add to the Changed property.
                            if ((!_axes.HasStatus(res.Key, _reg.ToString())) || _axes.GetAxisStatus(res.Key, _reg.ToString()) != _val)
                            {
                                // System.Diagnostics.Debug.WriteLine("Addr: {0},  {1} : {2}", res.Key, ModMap.RevMap(res.Value[0]), res.Value[1]);
                                _axes.SetAxisAttribute(res.Key, _reg.ToString(), _val);
                                if (_axes.HasAxisStatus(res.Key, _reg.ToString()))
                                {
                                    _val = _axes.GetAxisDisplayStatus(res.Key, _reg.ToString());
                                    RawMove udpResponse = new RawMove(res.Key, _reg, _val);
                                    _udpSender.sendStatus(udpResponse);
                                }


                                _queryOut.Enqueue(_results.Keys.ToList<String>());
                                OnPropertyChanged("Toggle");


                            }





                        }
                    }
                }
            }

        }
        private void ConfigAll()
        {
            foreach (Axis ax in _axes.ListAxes())
            {
                String port = ax.GetConfig("port");
              

                foreach (KeyValuePair<String, ModMap.RegMap> kvp in ModMap.confMap)
                {
                    try
                    {
                        String conf = kvp.Key.ToLower();
                        String strval = ax.GetConfig(conf);
                        if (strval is not null)
                        {
                            System.Diagnostics.Debug.WriteLine(String.Format("{0} : {1}", kvp.Key, strval));
                            int val = int.Parse(strval);
                            ModbusManager.command cmd = new ModbusManager.command { address = int.Parse(ax.Address), register = (int)kvp.Value, value = val };
                            _myManagers[port].Command.Enqueue(cmd);

                        }


                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("No conf");
                    }


                   
                }
            }
        }
    }
}
