using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.ComponentModel;


namespace UkiConsole
{
    class SendWrapper 
    {
        private ConcurrentQueue<RawMove> _commsin = new ConcurrentQueue<RawMove>();
        private ConcurrentQueue<RawMove> _commsout = new ConcurrentQueue<RawMove>();
        bool _run = true;
        private Sender _networkSender;
        private AxisManager _axes;
        private bool _connected = false;
        public event PropertyChangedEventHandler PropertyChanged;

        internal Sender NetworkSender { get => _networkSender; }
        public bool senderConnected { get => _connected;  }

        public SendWrapper(Sender netsender, AxisManager axes)
        {
            _networkSender = netsender;
            _networkSender.PropertyChanged += new PropertyChangedEventHandler(connChange);
            _axes = axes;
        } 

        private void connChange(object sender, PropertyChangedEventArgs e)
        {
            _connected = _networkSender.senderConnected;
            PropertyChanged?.Invoke(this, e);
        }
        public void Run()
        {
            Thread sendThread = new Thread(NetworkSender.Run);
            sendThread.Start();
        }
        public void Enqueue(RawMove mv)
        {
            RawMove valmv = _axes.AdjustOutgoingMove(mv);
         //   System.Diagnostics.Debug.WriteLine("Queueing in wrapper");

            NetworkSender.Enqueue(valmv);
           
        }
        public void ShutDown()
        {
            NetworkSender.ShutDown();
        }
    }

}
