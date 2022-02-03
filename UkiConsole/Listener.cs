using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace UkiConsole
{
    interface Listener
    {
        public bool listenerConnected { get; }
        public event PropertyChangedEventHandler PropertyChanged;

        public void Receive();
        public void ShutDown();
    }
}
