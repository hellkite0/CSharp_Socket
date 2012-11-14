using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace CSharp_Socket
{
    class UserConnection
    {
        public Byte[] cBuffers_;
        public Socket cSocket_;

        public UserConnection()
        {
            Console.WriteLine("Connection constructed.");
            cBuffers_ = new Byte[2048];
        }

        public void ClearBuffer()
        {
            Array.Clear(cBuffers_, 0, cBuffers_.Length);
        }
    }

}
