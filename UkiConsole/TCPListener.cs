using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Linq;
using System.Text;
using System.Threading.Tasks;



using System.Net;      //required
using System.Net.Sockets;    //required

namespace UkiConsole
{
    class TCPListener : Listener
    {


        private System.Net.Sockets.TcpListener _server;
        private ConcurrentQueue<RawMove> _moveOut = new ConcurrentQueue<RawMove>();
        private ConcurrentQueue<RawMove> _commandOut = new ConcurrentQueue<RawMove>();
        private bool _run = false;


        public TCPListener(TcpClient tcpclient, ConcurrentQueue<RawMove> Moves, ConcurrentQueue<RawMove> Control)
        {
            _server = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse("192.168.1.107"), 9000);
            // we set our IP address as server's address, and we also set the port: 9999

           

            _moveOut = Moves;
            _commandOut = Control;
           
            }

        public ConcurrentQueue<RawMove> MoveOut { get => _moveOut; }
        public ConcurrentQueue<RawMove> CommandOut { get => _commandOut; }

        public void Receive()
        {
            _run = true;
            _server.Start();  // this will start the server


            while (_run == true)   //we wait for a connection
            {
                TcpClient _tcpclient = _server.AcceptTcpClient();  //if a connection exists, the server will accept it

                NetworkStream ns = _tcpclient.GetStream();
                //sending the message

                while (_tcpclient.Connected is true)  //while the client is connected, we look for incoming messages
                {
                   

                    try
                    {
                        //networkstream is used to send/receive messages

                        byte[] msg = new byte[6];     //the messages arrive as byte array
                        ns.Read(msg, 0, msg.Length);   //the same networkstream reads the message sent by the client

                        UInt16 _addr = BitConverter.ToUInt16(msg, 0);
                       
                        UInt16 reg = BitConverter.ToUInt16(msg, 2);
                        UInt16 val = BitConverter.ToUInt16(msg, 4);
                        //System.Diagnostics.Debug.WriteLine("RECV MOVE: {0} : {1}, {2} ({3})", _addr.ToString(), reg, val , msg.Length);


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
                    }catch(Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("No TCP client connected");
                    }
                }

                _tcpclient.Close();
            }
            

        }
        public void ShutDown()
        {
            _run = false;

        }
    }
}