using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UkiConsole
{
    class UDPListener
    {
        private Socket _udpsock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private ConcurrentQueue<RawMove> _moveOut = new();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;
        private moveLoader _main; 
        private bool heartbeat_armed = false;
        public UDPListener(moveLoader main)
        {
            _main = main;

        }
        public ConcurrentQueue<RawMove> MoveOut { get => _moveOut;  }

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }
        public void Server( int port)
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            String _myaddr = "";
            foreach (IPAddress addr in localIPs)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (_myaddr.Equals(""))
                    {
                        // really, any 
                        _myaddr = addr.ToString();
                    }
                }
            }
            try
            {
                _myaddr = "127.0.0.1";
                _udpsock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
                _udpsock.Bind(new IPEndPoint(IPAddress.Parse(_myaddr), port));
                Receive();
            }catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("No udp");
            }

        }

        public void ShutDown()
        {
            _udpsock.Close();
        }
        private void Receive()
        {
            _udpsock.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                List<String> showme = new List<String>() { "218","219", "300" };
                State so = (State)ar.AsyncState;
                int bytes = _udpsock.EndReceiveFrom(ar, ref epFrom);
                _udpsock.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);

                int _addr = BitConverter.ToUInt16(so.buffer, 0);
                int reg = 0;
                int val = 0;
                 for (int _base = 2; _base < bytes; _base = _base + 4)
                {
                    reg = BitConverter.ToUInt16(so.buffer, _base);
                    val = BitConverter.ToUInt16(so.buffer, _base+2);

                    // _mv.Targets[BitConverter.ToUInt16(so.buffer, _base).ToString()] = BitConverter.ToUInt16(so.buffer, _base+2);
                    // System.Diagnostics.Debug.WriteLine("UDP IN Ax: "+  _addr.ToString() + " : " + reg.ToString() +" : " + val.ToString());

                }
               
                    
                RawMove _mv = new RawMove(_addr.ToString(), reg, val);
                if (showme.Contains(reg.ToString()))
                {
                    System.Diagnostics.Debug.WriteLine("RECV: {0} : {1}, {2}", _addr.ToString(), reg, val);
                }
           
                MoveOut.Enqueue(_mv);
                _main.LoadUDPMove();
            }, state);


        }
    }
}
