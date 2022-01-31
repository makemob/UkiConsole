using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UkiConsole
{
    interface Listener
    {
        public void Receive();
        public void ShutDown();
    }
}
