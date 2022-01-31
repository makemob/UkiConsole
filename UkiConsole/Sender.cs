using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace UkiConsole
{
    interface Sender
    {
        public void Run();
        public void ShutDown();
        public void Enqueue(RawMove _mv);
    }
}
