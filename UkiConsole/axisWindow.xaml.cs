using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UkiConsole
{
    /// <summary>
    /// Interaction logic for axisWindow.xaml
    /// </summary>
    public partial class axisWindow : Window
    {
        MainWindow _parent;
        Button _butt;
        
        public axisWindow(MainWindow parent, Button butt )
        {
            _parent = parent;
            _butt = butt;
            InitializeComponent();
            
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            _parent.deletePopout(_butt);
        }
    }
}
