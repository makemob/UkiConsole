using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
namespace UkiConsole
{
    class DummySender : Sender
    {
        private TcpClient _tcpClient;
        // private State state = new State();
        private ConcurrentQueue<RawMove> _moveOut = new();
        private NetworkStream _networkStream;
        private bool _run = true;
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _connected = true;
       
        public ConcurrentQueue<RawMove> MoveOut { get => _moveOut; }
        public bool senderConnected { get => _connected; set => _connected = value; }

        public DummySender()
        {

        }



        public void Run()
        {
            

                _run = true;
               // while (_run == true)
               // { }
            
        }
        public void ShutDown()
        {
            _run = false;
        }

        public void Enqueue(RawMove mv)
        {

            MoveOut.Enqueue(mv);
        }
        
        
    }
}

