using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CsvHelper;
using System.Globalization;

namespace UkiConsole
{
    
    public class AxisMove
    {
        String _name;
        String _type = "Speed";
        Dictionary<String, int> _targets = new Dictionary<string, int>();
        
        public AxisMove(String name)
        {
            _name = name;
            
        }
        public AxisMove(String name, Dictionary<String, int> targets):this(name)
        {
            _targets = targets;

        }
        public Dictionary<string, int> Targets { get => _targets;  }
        public string Name { get => _name;  }
        public string Type { get => _type; set => _type = value; }
       
        public void setTarget(String label, int target)
        {
            Targets[label] = target;
        }
    }
    public class CueFile
    {

        int MOVE_MULTIPLIER = 10;
        List<Dictionary<String,AxisMove>> _moves = new List<Dictionary<String,AxisMove>>();
        
        public CueFile(String filepath)
        {
             BuildShow(filepath);
        }
        public CueFile() { }

        public List<Dictionary<String,AxisMove>> Moves { get => _moves;  }
       

        public void BuildShow(string filepath)
        {
            
            using var myReader =new StreamReader(filepath);
            {
                
                using (var myCSV = new CsvReader(myReader, CultureInfo.InvariantCulture))
                {
                    myCSV.Read();
                     myCSV.ReadHeader();

                    while (myCSV.Read())
                    {
                        Dictionary<String,AxisMove> move = new ();

                        foreach (String header in myCSV.HeaderRecord)
                        {
                            String[] res = header.Split("_");
                            String axname = res[0];
                            if (!move.ContainsKey(axname))
                            {
                                move[axname] = new AxisMove(axname);
                            }
                            
                            
                            if (res[1].Equals("Position"))
                            {
                                move[axname].Type = "Position";
                            }
                           


                            //int target;

                            String targstring = myCSV.GetField(header).ToString();
                            if (!targstring.Equals(""))
                            {
                                move[axname].setTarget(res[1], int.Parse(targstring));
                                if (res[1].Equals("Position"))
                                {
                                    move[axname].setTarget(res[1], int.Parse(targstring) * MOVE_MULTIPLIER);
                                }
                            }

                        }
                        _moves.Add(move);
                    }
                }
            }

                
        }
    }
    /// <summary>
    /// Interaction logic for CueWindow.xaml
    /// </summary>
    public partial class CueWindow : Window
    {
        private String _cueDirectory;
        private MainWindow _parent;
        private Dictionary<String, String> _files = new();
        private String _cuefile;
        private CueFile _show;
        private  List<Dictionary<string, AxisMove>> _moves = new List<Dictionary<string, AxisMove>>();
        private List<int> _essentials;
        public List<List<int>> windata = new();
        private List<Dictionary<String, String>> testrows = new();

        public CueWindow(MainWindow myMain, List<int> essentials)
        {
            InitializeComponent();
            _parent = myMain;
            _essentials = essentials;
            _cueDirectory = _parent.GetConf("cueDirectory");
            String[] _tfiles = System.IO.Directory.GetFiles(_cueDirectory);
            foreach(String p in _tfiles)
            {
                _files[System.IO.Path.GetFileName(p)] = p;
            }
            listBox.SelectedItem = Files[0];
            
            DataContext = this;
        }

        public List<String> Files { get { return _files.Keys.ToList<String>(); } }
                        

            
        public List<Dictionary<string, AxisMove>> Moves { get => _moves;  }
        public List<Dictionary<string, string>> Testrows { get => testrows; 
        
        
        
        }

        //public CueFile Showcues { get => _show; }


        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ls = sender as ComboBox;
            _cuefile = _files[(String)ls.SelectedItem];

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                _cuefile = ofd.FileName; 
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<String, Dictionary<String, List<String>>> axtargval = new();
            int _rownum = 0;
            List<String> _subheaders = new();
            
            // "(
            // {{ LeftMidAnkle_Speed,30}, {LeftMidAnkle_Position, 25}, {....}}
            
            dataGrid.Columns.Clear();
            List<String> headers = new();
            if (_cuefile is null)
            {
                _cuefile = listBox.Items[0].ToString();
            }

            _show = new CueFile(_cuefile);
            
            foreach(Dictionary<String, AxisMove> mv in _show.Moves)
            {
                _moves.Add(mv);

            }
            

            _parent.LoadShowfile(_show);
            //dataGrid.ItemsSource = Moves;

            foreach (Dictionary<String, AxisMove> mv in _moves)
            {
                //_rows[_rownum.ToString()] = new();
                //  DataGridRow dgrow = new DataGridRow();
                List<int> targets = new();


                foreach (KeyValuePair<String, AxisMove> nameMove in mv)
                {

                    if (!axtargval.ContainsKey(nameMove.Key))
                    {
                        axtargval[nameMove.Key] = new();

                    }
                    // Fucking wpf.
                    // I think I need a dictioneru for each row, with the Bindings.
                    // Let's aim for Titles of the full move type (LeftMidAnkle_Speed)
                    // and then each row gets a dictionary with the Binding for that. 
                    // Which I think is what axtargval has? Not sure how to do double level bindings,
                    // but oh well. Also need to account for null Targets - at the moment we correctly
                    // have moves that have the axis but without a target - not sure what wpf will do with that. Can I add a row that doesn't have the right binding?
                    foreach (KeyValuePair<String, int> targVal in nameMove.Value.Targets)
                    {
                        if (!axtargval[nameMove.Key].ContainsKey(targVal.Key))
                        {
                            axtargval[nameMove.Key][targVal.Key] = new();
                        }
                        
                        
                            axtargval[nameMove.Key][targVal.Key].Add(targVal.Value.ToString());
                        
                    }



                }
            }
                foreach (KeyValuePair<String, Dictionary<String, List<String>>> _ax in axtargval){
                   
                    foreach (KeyValuePair<String, List<String>> targlist in _ax.Value)
                    {
                    

                    DataGridTextColumn newcol = new DataGridTextColumn();
                    newcol.Header = String.Format("{0}_{1}",_ax.Key,targlist.Key);
                    
                    

                    newcol.Binding = new Binding(String.Format("{0}_{1}", _ax.Key, targlist.Key));
                    dataGrid.Columns.Add(newcol);
                    // Add the axis as a header (twice?

                    _subheaders.Add(targlist.Key);
                        
                        
                        //_rows[_rownum.ToString()] = targlist.Value;

                    _rownum++;

                    }
                
               
                }

            testrows.Add(new() { { "LeftMidAnkle_Position", "20" }, { "LeftMidAnkle_Speed", "25" }, { "LeftRearAnkle_Speed", "30" }, { "LeftRearAnkle_Accel", "35" } });
            testrows.Add(new() { { "LeftMidAnkle_Position", "" }, { "LeftMidAnkle_Speed", "" }, { "LeftRearAnkle_Speed", "" }, { "LeftRearAnkle_Accel", "" } });
            testrows.Add(new() { { "LeftMidAnkle_Position", "" }, { "LeftMidAnkle_Speed", "" }, { "LeftRearAnkle_Speed", "" }, { "LeftRearAnkle_Accel", "" } });
            testrows.Add(new() { { "LeftMidAnkle_Position", "" }, { "LeftMidAnkle_Speed", "" }, { "LeftRearAnkle_Speed", "" }, { "LeftRearAnkle_Accel", "" } });
            testrows.Add(new() { { "LeftMidAnkle_Position", "" }, { "LeftMidAnkle_Speed", "" }, { "LeftRearAnkle_Speed", "" }, { "LeftRearAnkle_Accel", "" } });
            testrows.Add(new() { { "LeftMidAnkle_Position", "" }, { "LeftMidAnkle_Speed", "" }, { "LeftRearAnkle_Speed", "" }, { "LeftRearAnkle_Accel", "" } });
            testrows.Add(new() { { "LeftMidAnkle_Position", "" }, { "LeftMidAnkle_Speed", "" }, { "LeftRearAnkle_Speed", "" }, { "LeftRearAnkle_Accel", "" } });
            testrows.Add(new() { { "LeftMidAnkle_Position", "" }, { "LeftMidAnkle_Speed", "" }, { "LeftRearAnkle_Speed", "" }, { "LeftRearAnkle_Accel", "" } });
            testrows.Add(new() { { "LeftMidAnkle_Position", "20" }, { "LeftMidAnkle_Speed", "" }, { "LeftRearAnkle_Speed", "0" }, { "LeftRearAnkle_Accel", "" } });


            //foreach (Dictionary<String, String> newrow in _testrows)
            //{
            //     dataGrid.Items.Add(newrow);
            // }
            // windata.Add(targets);
        }





    }
}
