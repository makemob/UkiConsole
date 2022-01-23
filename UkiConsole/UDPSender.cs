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
    class UDPSender
    {

        private Socket _udpsock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private ConcurrentQueue<RawMove> _moveOut = new();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;
        private moveLoader _main;
        IPEndPoint _endpoint;
        public UDPSender()
        {
           

        }
        public ConcurrentQueue<RawMove> MoveOut { get => _moveOut; }

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }
        public void Server(String addr, int port)
        {
            IPAddress servaddr = IPAddress.Parse(addr);

            _endpoint = new IPEndPoint(servaddr, 10001);
            
        }

        public void ShutDown()
        {
            _udpsock.Close();
        }
        public void sendStatus(RawMove _mv)
        {

           
            // This should be a static singleton....
            byte[] data = new byte[6];
            byte[] _add = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(short.Parse(_mv.Addr)));
            byte[] _reg = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(_mv.Reg));
            byte[] _val = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(_mv.Val));
           // System.Diagnostics.Debug.WriteLine("UDP AXIS: " + _mv.Addr);
          //  System.Diagnostics.Debug.WriteLine("UDP REG: " + _mv.Reg.ToString());
          //  System.Diagnostics.Debug.WriteLine("UDP VAL: " + _mv.Val.ToString());
            data[0] = _add[1];
            data[1] = _add[0];


            // XXX Somewhere we are flipping network to host once too often.
            // This fixes that.
            data[2] = _reg[1];
            data[3] = _reg[0];
            data[4] = _val[1];
            data[5] = _val[0];

            Send(data);
            
        }
        public void Send(byte[] message)
        {
           

            _udpsock.SendTo(message, _endpoint);
           // System.Diagnostics.Debug.Write("Sent ");
            //System.Diagnostics.Debug.WriteLine( message.ToString());

        }
    }
}
