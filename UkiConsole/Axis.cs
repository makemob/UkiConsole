using System;

using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UkiConsole

{
   
    class AxisManager : INotifyPropertyChanged
    {
        private Dictionary<String, Axis> _axes = new Dictionary<string, Axis>();
       // private Dictionary<String, Axis> _labelmap = new Dictionary<string, Axis>();
        //private bool _changed = false;
        public AxisManager(Dictionary<String, Axis> axes)
        {
            _axes = axes;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler eh = PropertyChanged;
            if (eh != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                eh(this, e);
            }
        }

        public AxisManager(String axisFile) 
        {

            JObject o1 = JObject.Parse(File.ReadAllText(@axisFile));

            JArray axes = (JArray)o1["actuators"];
            foreach (JObject myAxis in axes)
            {
                String addr = myAxis["address"].ToString();
                String name = myAxis["name"].ToString();
                myAxis.Remove("address");
                myAxis.Remove("name");
                _axes.Add(addr, new Axis(addr, name));

                foreach (JProperty conf in myAxis.Children()) {
                    
                    String _label = conf.Name.ToLower();
                    String _value = conf.Value.ToString().ToLower();
                    

                    SetAxisConfig(addr, _label, _value);

                }
                
            }



        }
        public Dictionary<String, List<String>> ListAxesByPort()
        {
            
            Dictionary<String, List<String>> _portlist = new();
            foreach(Axis ax in _axes.Values)
            {
                String port = ax.GetConfig("port");
                if (_portlist.ContainsKey(port)){
                    _portlist[port].Add(ax.Address);
                }
                else
                {
                    _portlist.Add(ax.GetConfig("port"), new List<string>(){ ax.Address});

                }
            }
            return _portlist;
        }
        public RawMove Validate(RawMove mv)
        {
            Axis ax = _axes[mv.Addr];
            RawMove Validated = new RawMove(mv.Addr, mv.Reg, ax.AdjustIncomingPosition( (int)mv.Reg,mv.Val));
            return Validated;
        }
        public RawMove AdjustOutgoingMove(RawMove mv)
        {
            Axis ax = _axes[mv.Addr];
            RawMove Validated = new RawMove(mv.Addr, mv.Reg, ax.AdjustOutgoingPosition((int)mv.Reg, mv.Val));
            return Validated;
            
        }
        public bool IsEnabled(String addr)
        {
            try
            {
                if (_axes[addr].GetConfig("enabled").Equals("true"))
                {
                    return true;
                }
            }catch (Exception e)
            {
                return false;
            }
            return false;
        }
        public void UpdateAxis(Axis axis)
        {
            _axes[axis.Address] = axis;
        }
        public void SetAxisAttribute(string addr, string attr, int value)
        {
            _axes[addr].Set(attr, value);
            if (attr.Equals("Estop"))
            {
                if (value != 0)
                {
                    OnPropertyChanged("Estop");
                }
            }
            //Changed = true;

        }
        public void SetAxisTarget(string addr, string attr, int value)
        {
            _axes[addr].SetTarget(attr, value);
           // Changed = true;
        }
        public void SetAxisConfig(string addr, string conf, string value)
        {
            _axes[addr].Config[conf] = value;
        }
        public void LoadTargets(Dictionary<String, AxisMove> targets)
        {
            try
            {
                
                
                    foreach (AxisMove _move in targets.Values)
                    {
                    _axes[AddressFromLabel(_move.Name)].ClearTargets();
                        foreach (KeyValuePair<String, int> kvp in _move.Targets)
                        {

                            SetAxisTarget(AddressFromLabel(_move.Name), kvp.Key, kvp.Value);

                        }
                    }
                
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }
        public String LabelFromAddress(string address)
        {
            try
            {
                return _axes[address].DisplayName;
            }catch(Exception e)
            {
                return "Wrong";
            }
        }
        public String AddressFromLabel(String Label)
        {
            foreach (KeyValuePair<String, Axis> kvp in _axes)
            {
                //srsly?
                if (kvp.Value.DisplayName.Equals(Label))
                {
                    return kvp.Value.Address;

                }
            }
            return "";
        }
        /*
        public String PortFromLabel(String Label)
        {
            foreach (KeyValuePair<String, Axis> kvp in _axes)
            {
                //srsly?
                if (kvp.Value.DisplayName.Equals(Label))
                {
                    return kvp.Value.getConfig("port");

                }
            }
            return "";
        }*/
        public List<Axis> ListAxes()
        {
            List<Axis> axes = new List<Axis>();
            foreach (KeyValuePair<String, Axis> kvp in _axes)
            {
                axes.Add(kvp.Value);

            }
            return axes;
        }
        public List<String> AddressList
        {
            get
            {
               
                return new List<string>(_axes.Keys);

            }
        }

       // public bool Changed { get => _changed;  }
       
        public bool HasStatus(String addr, String reg)
        {
            return _axes[addr].HasStatus(reg);   

        }

        public bool HasAxisStatus(String addr, String reg)
        {

            return _axes[addr].HasStatus(reg);

        }
        public  int GetAxisStatus(String addr, String reg)
        {
           
            return _axes[addr].GetStatus(reg);
            
        }
        public int GetAxisDisplayStatus(String addr, String reg)
        {

            return _axes[addr].GetDisplayStatus(reg);

        }
        public String GetAxisConfig(String addr, String reg)
        {

            return _axes[addr].GetConfig(reg);

        }
    }


    /// <summary>
    /// This is really a struct to hold raw Modbus Command data, which is what we get from UDP. 
    /// </summary>
    public class RawMove
    {
        private String _addr;
        private short _reg;
        private short _val;

        public String Addr { get { return _addr; } }

        public short Reg { get { return _reg; } }
        public short Val { get { return _val; } }

        public RawMove(String addr, int reg, int val)
        {
            _addr = addr;
            _reg = (short)reg;
            _val = (short)val;
        }
    }

    public class Axis
    {
        private String _address;
        private String _displayName;
        private int _colnum;
        private int _rownum;

        private int EncoderScale = 10;
        
       

        private bool _estopped = true;
        private Dictionary<String, int> _status = new();
        private Dictionary<String, String> _config = new ();
        private Dictionary<String, int> _targets = new();

        public string Address { get => _address; }
        public string DisplayName { get => _displayName;  }
        public int Colnum { get => _colnum; set => _colnum = value; }
        public int Rownum { get => _rownum; set => _rownum = value; }
        public Dictionary<string, int> Targets { get => _targets;  }
        public bool EStopped { get => _estopped; set {
                _estopped = value;
               

            }
        }

        public Dictionary<string, String> Config { get => _config;  }

        public Axis(String address, String displayname = "", int colnum=-1, int rownum = -1)
        {
            _address = address;
            _displayName = displayname;
            Colnum = colnum;
            Rownum = rownum;
            // Start assuming we are timed out
            Set("estop", 8);
        }

        public void Update(Dictionary<String, int> newReg)
        {
            // This may not be a complete Register
            foreach (KeyValuePair<String, int> kvp in newReg)
            {
                System.Diagnostics.Debug.WriteLine(kvp.Key);

                Set(kvp.Key, kvp.Value);
            }
        }
        public void Set(String label, int value)
        {

            _status[label] = value;
            
            

        }
        public void SetConfig(String label, String value)
        {
            Config[label] = value;
        }
        public bool HasConfig(String label)
        {
            return Config.ContainsKey(label);
        }
        public String GetConfig(String label)
        {
            if (HasConfig(label)){
                return Config[label];
            }
            else
            {
                return null;

            }
        }
        public void ClearTargets()
        {
            _targets = new();
        }

        public bool HasStatus(String reg)
        {
            return _status.ContainsKey(reg);
        }
        public int GetStatus(String reg)
        {
           
                return _status[reg];
            
            
            
        }
        public int GetDisplayStatus(String reg)
        {
            if (HasStatus(reg))
            {
                return AdjustOutgoingPosition(int.Parse(reg), _status[reg]);
            }
            else
            {
                return -1;
            }
        }
        public int AdjustIncomingPosition(int reg, int val)
        {
            if (ModMap.PositionRegisters.Contains(reg))
            {
                val = val * EncoderScale;

            }
            //SetTarget(ModMap.RevMap(reg),val);
            return val;
        }
        public int AdjustOutgoingPosition(int reg, int val)
        {
            if (ModMap.PositionRegisters.Contains(reg))
            {
                val = val / EncoderScale;

            }
            return val;
        }
        // public int AdjustPosition(string reg, int val)
        ///{
        // return AdjustPosition((int)reg, val);
        //}
        public void SetTarget(String label, int value)
        {
            // Replace this with the modbus version of AdjustPosition

            // value = AdjustIncomingPosition(int.Parse(label), value);
              String maxvalue = value.ToString();
            Config.TryGetValue( label.ToLower(), out maxvalue);
            if (int.Parse(maxvalue) < value)
            {
                value = int.Parse(maxvalue);
            } 
            _targets[label] = value;
            


        }
       /*
        * This is a better way to do it than above, but I don't have time.... 
         private List<int> getLimits(string axis, int reg)
        {
            List<int> Limits = new List<int>();
            List<ModMap.RegMap> axLimitMap = ModMap.LimitMap[reg];
            
            
            // For the moment, assume a maximum limit for speed, accel, etc
            if (axLimitMap.Count == 1)
            {
                    

            }
            
            

        }
        public RawMove Validate( RawMove _mv)
        {
            
        }
       */
        
    }
}
