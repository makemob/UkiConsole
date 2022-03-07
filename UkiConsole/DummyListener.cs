using System;
using System.Collections.Concurrent;
using System.ComponentModel;

using System.Net;      //required
using System.Net.Sockets;    //required

namespace UkiConsole
{
    class DummyListener : Listener
    {


        private System.Net.Sockets.TcpListener _server;
        private ConcurrentQueue<RawMove> _moveOut = new ConcurrentQueue<RawMove>();
        private ConcurrentQueue<RawMove> _commandOut = new ConcurrentQueue<RawMove>();
        private bool _run = false;
        private bool _connected = true;
        public bool listenerConnected { get => _connected; }


        public event PropertyChangedEventHandler PropertyChanged;




        private void checkConnection(bool state)
        {
            if (_connected != state)
            {
                _connected = state;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Connected"));
            }
        }
        public DummyListener(ConcurrentQueue<RawMove> Moves, ConcurrentQueue<RawMove> Control)
        {
           
           

            _moveOut = Moves;
            _commandOut = Control;
           
            }

        public ConcurrentQueue<RawMove> MoveOut { get => _moveOut; }
        public ConcurrentQueue<RawMove> CommandOut { get => _commandOut; }
        
        public void Receive()
        {
            _run = true;
            // this will start the server


          //  while (_run == true)   //we wait for a connection
          //  {
               
                   
          //  }
           

        }
        public void ShutDown()
        {
            _run = false;
           // _server.Stop();

        }
    }
}