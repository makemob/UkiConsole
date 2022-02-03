using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.ComponentModel;


namespace UkiConsole
{
    interface Sender
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public bool senderConnected { get; }
        public void Run();
        public void ShutDown();
        public void Enqueue(RawMove _mv);
    }
}
