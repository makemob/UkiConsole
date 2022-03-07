using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Ports;
namespace UkiConsole
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int axbuttonheight = 30;
        private int axbuttonwidth = 50;
        
        private AxisManager _axes;
        private Dictionary<String, Button> _axisButtons = new Dictionary<string, Button>();
        private Dictionary<Button, axisWindow> popouts = new();
        private ShowRunner _showrunner;
        private String consoleConf = @"consoleConfig.json";
        private Dictionary<String, String> _config = new Dictionary<string, string>();
        // move this to config
        private int _duration = 1000;
        private Timer _cueTimer ;
        AutoResetEvent _cueNotice = new AutoResetEvent(false);
        private CueFile _showmoves = new();
        private CueWindow _cueWindow;
        private bool _running = false;
        private bool _estopped = true;
        private Dictionary<String,AxisMove> _nextMove;
        private String _inputType = "CSV";
       // private UDPListener _udpListener ;
        
        // Portmap is "left => Com3"
        private Dictionary<String, String> _portmap = new();
        private bool _udp_armed = false;
        private DateTime _last_UDP = DateTime.Now;
        AutoResetEvent _heart = new AutoResetEvent(false);
        private Timer _heartbeat;
        private List<String> _comports = new() { "None" };
        private Dictionary<string, string> _revPortMap = new();
        private Dictionary<string, Dictionary<String, Object>> _usbButtonMap = new();
        private Dictionary<int, String> _essentials = new Dictionary<int, String>()
            {
             { (int)ModMap.RegMap.MB_ESTOP_STATE , "Estate" },
            {(int)ModMap.RegMap.MB_EXTENSION , "Pos" },
          //  { (int)ModMap.RegMap.MB_GOTO_POSITION , "Target" },
            {(int)ModMap.RegMap.MB_MOTOR_SPEED , "Speed" },
          //  { (int)ModMap.RegMap.MB_CURRENT_LIMIT_INWARD , "Current (I)" },
          //  {(int)ModMap.RegMap.MB_MOTOR_ACCEL , "Accel" },
            {(int)ModMap.RegMap.MB_INWARD_ENDSTOP_STATE, "Micro" },
            };

        public List<string> Comports { get => _comports;  }
        public String LeftComPort { get => _portmap["left"]; set => _portmap["left"] = value; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public String RightComPort { get => _portmap["right"]; set => _portmap["right"] = value; }
        public MainWindow()
        {
            Comports.AddRange(SerialPort.GetPortNames());

            LoadConfig();
            _axes = new AxisManager(_config["axisConfig"]);
            _showrunner = new ShowRunner(_portmap, _axes, _essentials.Keys.ToList<int>());
            SetMap();
            InitializeComponent();
            AddAxisButtons();
            _cueWindow = new CueWindow(this, _essentials.Keys.ToList<int>());
            _cueWindow.Show();
            DataContext = this;
            foreach (KeyValuePair<String, String> kvp in _portmap) {
                _revPortMap[kvp.Value] = kvp.Key;
                if (!Comports.Contains(kvp.Value))
                {
                    Comports.Add(kvp.Value);
                }
                    // Eh. Fix later
                _usbButtonMap[_portmap["left"]] = new Dictionary<String, Object> {
                    { "Label", LeftPortLabel },
                    {"Button", LeftUSBButton }
                    
                };
             }
            _showrunner.PropertyChanged += new PropertyChangedEventHandler(maintoggle);
            Thread showThread = new Thread(_showrunner.Listen);
            showThread.Start();
            _axes.PropertyChanged += new PropertyChangedEventHandler(Estopped);
           
            // _showrunner.PropertyChanged += new PropertyChangedEventHandler(listenConn);
        }

       
        private void UpdateLabel(Label myLabel, String myContent, Brush myColor)
        {
            myLabel.Background = myColor;
            myLabel.Content = myContent;
        }

        private void Estopped(object sender, EventArgs e)
        {

            _estopped = true;
            StopShow();

        }
        private void maintoggle(object sender, PropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.PropertyName.ToString());
            if (e.PropertyName.ToString().Equals("Toggle")) {
                // 
                List<String> changed;
                _showrunner.QueryOut.TryDequeue(out changed);
                if (changed is not null)
                {
                    foreach (String ax in changed)
                    {
                        refreshButton(_axes.LabelFromAddress(ax));
                        //  System.Diagnostics.Debug.WriteLine("Main: {0}",ax);
                    }
                }
            }else if (e.PropertyName.ToString().Equals("listenerConnected") )
            {
               // System.Diagnostics.Debug.WriteLine(String.Format("Listener changed conn status in Main: {0}", _showrunner.ListenerConnected));
               
                Brush bg = Brushes.Yellow;
                String labelContent = "No Listener";
                if (_showrunner.ListenerConnected)
                {
                    bg = Brushes.Green;
                    labelContent = "Listener connected";
                }

                Dispatcher.BeginInvoke(new Action<Label, String, Brush>(UpdateLabel), DispatcherPriority.Normal, listenStatus, labelContent,bg);
            }else if (e.PropertyName.ToString().Equals("senderConnected"))
            {
                Brush bg = Brushes.Yellow;
                String labelContent = "No Sender";
                if (_showrunner.SenderConnected)
                {
                    bg = Brushes.Green;
                    labelContent = "Sender connected";

                }

                Dispatcher.BeginInvoke(new Action<Label, String, Brush>(UpdateLabel), DispatcherPriority.Normal, sendStatus, labelContent, bg);
            }
            else if (_portmap.Values.Contains(e.PropertyName.ToString()))
            {
                System.Diagnostics.Debug.WriteLine(String.Format("Main says change in connection for serial {0}", e.PropertyName));
               // String labelContent = "Disconn";
                Brush bg = Brushes.Red;
                if (_showrunner.portStatus( _revPortMap[e.PropertyName]))
                {
                    bg = Brushes.Green;
                    //labelContent = e.PropertyName;
                }
                
                Dispatcher.BeginInvoke(new Action<Button, Brush>(UpdateButtonBG), DispatcherPriority.Normal, _usbButtonMap[e.PropertyName]["Button"],  bg);

            }
        }
        private void LoadConfig()
        {
            
            // First thing to load is our own config
            JObject o1 = JObject.Parse(File.ReadAllText(consoleConf));


            foreach (JProperty mySetting in o1["appSettings"].Children())
            {
                
                // Console.WriteLine(myAxis["name"]);
                if (mySetting.Name.Equals("defaultComPorts")){
                    //Dictionary<String, String> _portmap = new();
                    foreach (JProperty conf in mySetting.Children().Children())
                    {
                        
                        //This is ugly! Must find a better way of doing this.
                        _portmap[conf.Name.ToLower()] = conf.Value.ToString();

                    }
                }
                else
                {
                    _config[(String)mySetting.Name] = (String)mySetting.Value;
                }
            }


        }
        public String GetConf(String confKey)
        {

            if (_config.ContainsKey(confKey))
            {
                return _config[confKey];
            }
            else
            {
                return "";
            }
        }
        private void AddAxisButtons(int baserow = 0, int basecol = 0, int collimit = 10)
        {

            foreach (Axis ax in _axes.ListAxes())
            {
                int colmod = basecol;
                int rowmod = baserow;

                if (ax.Colnum > 0)
                {
                    colmod = ax.Colnum - 1;
                }
                else
                {
                    basecol = (basecol++ % collimit);
                    if (basecol == 0)
                    {
                        baserow++;
                    }
                }

                if (ax.Rownum > 0)
                {
                    rowmod = ax.Rownum -1;
                }

                Button newButton = new Button();
                // newButton.Margin = new Thickness(colmod * (axbuttonheight + 2),rowmod * (axbuttonwidth + 2),0,0);
                //newButton.Width = axbuttonwidth;
                //newButton.Height = axbuttonheight;
                Grid.SetRow(newButton, rowmod);
                Grid.SetColumn(newButton, colmod);
                newButton.Background = Brushes.Red;
                
                newButton.Content = String.Format("{0} ",ax.DisplayName);
                newButton.Click += (sender, EventArgs) => { clickButton(sender, EventArgs, ax.DisplayName); };
                //ax.PropertyChanged += (sender, EventArgs) => { clickButton(sender, EventArgs, ax.DisplayName);  };
                
                axGrid.Children.Add(newButton);
                _axisButtons[ax.DisplayName] = newButton;

            }




        }
        private void clickButton(object sender, EventArgs e, String name)
        {
            Button butt = sender as Button;

            axisWindow popWin = new axisWindow(this, butt);
            popouts[butt] = popWin;
            refreshButton(name);
            popWin.Show();

            
        }

        public void refreshButton(String name)
        {
            try
            {
                Button myButton = _axisButtons[name];
                string addr = _axes.AddressFromLabel(name);
                string desc = name;
                Brush bgColor = Brushes.Red ;
                int eState = _axes.GetAxisStatus(addr, ((int)ModMap.RegMap.MB_ESTOP_STATE).ToString());
                if ( eState == 0)
                {
                    bgColor = Brushes.Yellow;
                    if (_axes.GetAxisStatus(addr, ((int)ModMap.RegMap.MB_MOTOR_SPEED).ToString()) == 0)
                    {
                        bgColor = Brushes.Green;
                    }
                
                // REplace with template
                 foreach (int essential in _essentials.Keys)
                {
                        if (!_essentials[essential].Equals("Estate")){
                            try
                            {
                                desc += String.Format("\n{0} : {1}", _essentials[essential], _axes.GetAxisStatus(addr, essential.ToString()).ToString());
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex.Message);
                            }
                        }

                }
                }
                else
                {
                    desc += String.Format(" \n {0}", ModMap.EstopLabel[eState]);
                }
                
                Dispatcher.BeginInvoke(new Action<Button,string, Brush>(UpdateButton), DispatcherPriority.Normal, myButton,desc, bgColor);
                
            } catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);

            }
        }

        public void UpdatePopout(axisWindow mywin, String content, Brush bgColor)
        {
            mywin.label.Content = content;
            mywin.label.Background = bgColor;
        }
        public void UpdateButton(Button butt,String content, Brush bgColor)
        {
            UpdateButtonText(butt, content);
            butt.Background = bgColor;
            if (popouts.ContainsKey(butt))
            {
                Dispatcher.BeginInvoke(new Action<axisWindow, string, Brush>(UpdatePopout), DispatcherPriority.Normal, popouts[butt], content, bgColor);

            }
        }
        public void UpdateButtonText(Button butt, String content)
        {
            butt.Content = content;
        }
        public void UpdateButtonBG(Button butt, Brush bg)
        {
            butt.Background = bg;
        }
        private void SetMap()
        {
            Dictionary<String, Point> axmap = getAxMap();

            foreach (Axis ax in _axes.ListAxes())
            {
                //Console.WriteLine("Found {0}: {1}", ax.DisplayName, ax.Address);
                if (axmap.Keys.Contains(ax.DisplayName))
                {
                    ax.Rownum = (int)axmap[ax.DisplayName].X;
                    ax.Colnum = (int)axmap[ax.DisplayName].Y;


                }
            }

        }
       /* public void LoadUDPMove()
        {

            if (_inputType == "UDP")
            {

               
                RawMove _udpMove;
                
                while (_udpListener.MoveOut.TryDequeue(out _udpMove))
                {
                    if (_udpMove is not null)
                    {
                        if (_udpMove.Addr.Equals("240"))
                        {
                            // should just "foreach axis..."
                            return;
                        }
                        _showrunner.RawIn.Enqueue(_udpMove);
                    }
                     
                     
                }
                NextMove();

            }

        }
        */
        public void LoadShowfile(CueFile cuefile)
        {
            if (_inputType == "CSV")
            {
                _showmoves = cuefile;
                // Pop here. Add time for each move - default time should be set in CueWindow,
                // *not* here

                NextMove();
                button.Content = "Show Loaded";
            }
            
        }
        public void NextMove()
        {
            if (_showmoves.Moves.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("Cues left: {0}", _showmoves.Moves.Count);
                Dictionary<String, AxisMove> axmove = _showmoves.Moves[0];
                _showmoves.Moves.RemoveAt(0);
                // We want the axis to know that it will be moving, partly for GUI stuff.
                // But we also want to keep and send the move as a move rather than rebuilding from the axis.
                // It might actually be easier in the long run to just keep a list of axes moving, and send the targets from the axis in FireCue.
                
                _axes.LoadTargets(axmove);
                _nextMove = axmove;

                
            }
            else
            {
                PauseShow();
                Dispatcher.BeginInvoke(new Action<Button, string>(UpdateButtonText), DispatcherPriority.Normal, button, "Show Finished" );

                
            }
        }

        private void PauseShow()
        {
            _running = false;
            _cueTimer.Change(Timeout.Infinite, Timeout.Infinite);
           // button.Content = "Paused";
        }
        private Dictionary<String, Point> getAxMap()
        {
            Dictionary<String, Point> _result = new Dictionary<String, Point>();
            String axisfile = _config["axisMap"];
            JObject o1 = JObject.Parse(File.ReadAllText(axisfile));

            JArray axes = (JArray)o1["axes"];
            foreach (JObject myAxis in axes)
            {
                // Console.WriteLine(myAxis["name"]);

                _result[(String)myAxis["name"]] = new Point((int)myAxis["row"], (int)myAxis["column"]);

            }
            return _result;

        }
        private void RunShow()
        {
            ClearAll();
            if (_nextMove is not null)
            {
                _running = true;
                _cueTimer = new Timer(FireCue, _cueNotice, 0, _duration);
            }
            


        }
        private void FireCue(Object stateinfo)
        {
            // AutoResetEvent autoEvent = (AutoResetEvent)stateinfo;
            if (_running)
            {
                try
                {

                    if (_nextMove is not null)
                    {
                        foreach (AxisMove mv in _nextMove.Values)
                        {
                            _showrunner.MoveIn.Enqueue(mv);
                        }
                        NextMove();
                    }
                }catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("No show loaded? {0}", e.Message);
                }
            }
            else
            {
                _cueTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
        
        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (_running)
            {
                StopShow();
                System.Diagnostics.Debug.WriteLine("STOPPING");

            }
            else
            {
                RunShow();
                button.Content = "MOVING!";
            }
        }
        private void StopShow()
        {
           
            _showrunner.Control.Enqueue("ESTOP");
            if (_running)
            {


                PauseShow();
            }
            //button.Content = "Stopped";
        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();
           
        }
        public void ClearAll()
        {
            _showrunner.Control.Enqueue("CLEAR_ESTOP");
            int tries = 3;
            while (tries > 0)
            {
                tries--;
                if (_estopped)
                {
                    _estopped = false;
                    _showrunner.Control.Enqueue("CLEAR_ESTOP");
                }
                else
                {
                    tries = 0;
                }
            }
        }

        private void mode_Changed(object sender, RoutedEventArgs e)
        {
            // Enable/Disable relevant listener
            RadioButton source = (RadioButton)sender;
            _inputType = source.Content.ToString();
            _showrunner.setMode(_inputType);
           
            System.Diagnostics.Debug.WriteLine(_inputType);

        }

        private void stopbutton_Click(object sender, RoutedEventArgs e)
        {
                StopShow();
            
        }
        private void buttonConfig_Click(object sender, RoutedEventArgs e)
        {
            ConfigAll();
        }

        private void ConfigAll()
        {

            _showrunner.Control.Enqueue("CONFIG");
        }

        private void buttonHome_Click(object sender, RoutedEventArgs e)
        {
            HomeAll();
        }

        private void HomeAll()
        {

            _showrunner.Control.Enqueue("HOME");
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            CalibrateAll();
        }

        private void CalibrateAll()
        {
            _showrunner.Control.Enqueue("CALIBRATE");
        }
        public void startSender(object sender, RoutedEventArgs e)
        {
            _showrunner.startSender();
        }
        public void startListener(object sender, RoutedEventArgs e)
        {
            _showrunner.startListener();
        }
        public void deletePopout(Button butt)
        {
            popouts.Remove(butt);
        }
        private void ConnectComPort(object sender, RoutedEventArgs e)
        {
            string portside = (((Button)sender).Tag.ToString()).ToLower();
            string port = _portmap[portside] ;
            System.Diagnostics.Debug.WriteLine(port);
            _showrunner.USBConnect(portside, port);

        }
        protected override void OnClosing(CancelEventArgs e)
        {
            StopShow();
            
            //_udpListener.ShutDown();
            _showrunner.Control.Enqueue("SHUTDOWN");
            
            _cueWindow.Close();
            base.OnClosing(e);
        }
    }
}
