using System;
using System.Collections.Concurrent;
using System.ComponentModel;

using System.Net;      //required
using System.Net.Sockets;    //required

namespace UkiConsole
{
    class TCPListener : Listener, INotifyPropertyChanged
    {


        private System.Net.Sockets.TcpListener _server;
        private ConcurrentQueue<RawMove> _moveOut = new ConcurrentQueue<RawMove>();
        private ConcurrentQueue<RawMove> _commandOut = new ConcurrentQueue<RawMove>();
        private bool _run = false;
        private bool _connected = false;
        private String _addr;
        private int _port;
        public event PropertyChangedEventHandler PropertyChanged;

        public ConcurrentQueue<RawMove> MoveOut { get => _moveOut; }
        public ConcurrentQueue<RawMove> CommandOut { get => _commandOut; }
        public bool listenerConnected { get => _connected; }


        protected void OnPropertyChanged(string propertyname)
        {
            PropertyChangedEventHandler eh = PropertyChanged;
            if (eh != null)
            {
                var en = new PropertyChangedEventArgs(propertyname);
                eh(this, en);
            }
        }
        private void checkConnection(bool state)
        {
            if (_connected != state)
            {
               // System.Diagnostics.Debug.WriteLine(String.Format("Changed connection: {0}", state));
                _connected = state;
                OnPropertyChanged("listenerConnected");
                
            }
        }
        public TCPListener(String addr, int port,ConcurrentQueue<RawMove> Moves, ConcurrentQueue<RawMove> Control)
        {
            _addr = addr;
            _port = port;
            // Config this, and add "local" vs "remote"
            if (_server is null)
            {
                _server = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse(_addr), _port);
                _server.Start();

            }
            else
            {
                _server.Stop();
                _server.Start();
            }
           
            


            _moveOut = Moves;
            _commandOut = Control;
           
            }


        public void Receive()
        {
            _run = true;
           
            System.Diagnostics.Debug.WriteLine("Listening...");

            while (_run == true)   //we wait for a connection
            {
                try
                {
                    TcpClient _tcpclient = _server.AcceptTcpClient();  //if a connection exists, the server will accept it

                    NetworkStream ns = _tcpclient.GetStream();



                    while (_tcpclient.Connected is true)  //while the client is connected, we look for incoming messages
                    {

                        checkConnection(true);
                        while (ns.DataAvailable)
                        {


                            try
                            {
                                //networkstream is used to send/receive messages

                                byte[] msg = new byte[6];     //the messages arrive as byte array
                                ns.Read(msg, 0, msg.Length);   //the same networkstream reads the message sent by the client

                                UInt16 _addr = BitConverter.ToUInt16(msg, 0);

                                UInt16 reg = BitConverter.ToUInt16(msg, 2);
                                UInt16 val = BitConverter.ToUInt16(msg, 4);
                                System.Diagnostics.Debug.WriteLine("TCP MOVE: {0} : {1}, {2} ({3})", _addr.ToString(), reg, val, msg.Length);


                                RawMove _mv = new RawMove(_addr.ToString(), reg, val);



                                if (ModMap.ControlRegisters.Contains(reg) || ModMap.ControlAddresses.Contains(_addr))
                                {
                                    CommandOut.Enqueue(_mv);

                                }
                                else
                                {
                                    // System.Diagnostics.Debug.WriteLine("RECV MOVE: {0} : {1}, {2}", _addr.ToString(), reg, val);


                                    MoveOut.Enqueue(_mv);

                                }

                            }

                            catch (Exception ex)
                            {
                                _run = false;
                                System.Diagnostics.Debug.WriteLine("No TCP client connected");
                            }
                        }
                        ns.Flush();
                    }
                }catch(Exception ex){
                    System.Diagnostics.Debug.WriteLine("Also messy...");
                }
                checkConnection(false);
               // _tcpclient.Close();
            }
            _server.Stop();

        }
        public void ShutDown()
        {
            _run = false;
            _server.Stop();

        }
    }
}