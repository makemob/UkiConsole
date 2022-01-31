using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace UkiConsole
{
    class SendWrapper 
    {
        private ConcurrentQueue<RawMove> _commsin = new ConcurrentQueue<RawMove>();
        private ConcurrentQueue<RawMove> _commsout = new ConcurrentQueue<RawMove>();
        bool _run = true;
        private Sender _networkSender;
        private AxisManager _axes;

        internal Sender NetworkSender { get => _networkSender; }

        public SendWrapper(Sender netsender, AxisManager axes)
        {
            _networkSender = netsender;
            _axes = axes;
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
