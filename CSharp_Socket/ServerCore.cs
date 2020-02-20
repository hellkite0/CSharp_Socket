using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CSharp_Socket
{
    class ServerCore
    {
        private Queue<UserConnection> cUserQueue_;
        private Mutex cUserListMutex_;
        private List<UserConnection> cUserList_;
        private Socket cServer_;

        private Thread cListenThread_;
        private Thread cProcThread_;
        private Form1 cHandler_;

        public ServerCore(ref Form1 cHandler)
        {
            cHandler_ = cHandler;
        }

        public void Initialize()
        {
            try
            {
                cUserQueue_ = new Queue<UserConnection>();
                cUserListMutex_ = new Mutex();
                cUserList_ = new List<UserConnection>();

                cServer_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IPHostEntry host = Dns.Resolve(Dns.GetHostName());
                string sIP = host.AddressList[0].ToString();
                IPAddress hostIP = IPAddress.Parse(sIP);

                IPEndPoint ep = new IPEndPoint(hostIP, 13000);
                cServer_.Bind(ep);
                cServer_.Blocking = true;
                cServer_.Listen(5);

                Console.WriteLine("Server Initialize Complete.");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Server Core Exception : {0}", ex.ToString());
            }
        }

        public void StartServer()
        {
            cListenThread_ = new Thread(new ThreadStart(ServerListen));
            cListenThread_.Start();

            cProcThread_ = new Thread(new ThreadStart(ServerProc2));
            cProcThread_.Start();
        }

        public void Finalize()
        {
            cListenThread_.Abort();
            cProcThread_.Abort();
        }

        public void ServerListen()
        {
            Console.WriteLine("ServerListen Start.");

            Socket cClient = null;
            while (true)
            {
                cClient = cServer_.Accept();

                Console.WriteLine("New Connection Discovered.");

                UserConnection cNewConnection = new UserConnection();
                cNewConnection.cSocket_ = cClient;
                cNewConnection.cSocket_.Blocking = false;

                cUserListMutex_.WaitOne();
                //cUserList_.Add(cNewConnection);
                cUserQueue_.Enqueue(cNewConnection);
                cHandler_.InsertText("test");
                cUserListMutex_.ReleaseMutex();
            }
        }

        public void ServerProc2()
        {
            Console.WriteLine("Server Proc 2 Start.");

            int len;
            SocketError err;

            while (true)
            {
                cUserListMutex_.WaitOne();

                if (cUserQueue_.Count == 0)
                {
                    cUserListMutex_.ReleaseMutex();
                    continue;
                }

                UserConnection i = cUserQueue_.Dequeue();

                len = i.cSocket_.Receive(i.cBuffers_, 0, i.cBuffers_.Length, SocketFlags.None, out err);
                if (SocketError.Success == err)
                {
                    if (len > 0)
                    {
                        string sMsg = Encoding.ASCII.GetString(i.cBuffers_, 0, len);

                        Console.WriteLine("Msg : {0} , Length : {1}", sMsg, len);
                        i.cSocket_.Send(i.cBuffers_);
                        i.ClearBuffer();
                        cUserQueue_.Enqueue(i);
                    }
                    else
                    {
                        Console.WriteLine("Something wrong with socket.");
                        i.cSocket_.Shutdown(SocketShutdown.Both);
                    }
                }
                else if (SocketError.WouldBlock == err)
                {
                    //Console.WriteLine("WouldBlock.");
                    cUserQueue_.Enqueue(i);
                }
                else if (SocketError.WouldBlock != err)
                {
                    Console.WriteLine("Connection Closed.");
                    i.cSocket_.Shutdown(SocketShutdown.Both);
                }

                //foreach (UserConnection i in cUserList_)
                //{
                //    len = i.cSocket_.Receive(i.cBuffers_, 0, i.cBuffers_.Length, SocketFlags.None, out err);
                //    if (SocketError.Success == err)
                //    {
                //        if (len > 0)
                //        {
                //            string sMsg = Encoding.ASCII.GetString(i.cBuffers_, 0, len);

                //            Console.WriteLine("Msg : {0} , Length : {1}", sMsg, len);
                //            i.cSocket_.Send(i.cBuffers_);
                //            i.ClearBuffer();
                //        }
                //        else
                //        {
                //            Console.WriteLine("Something wrong with socket.");
                //            i.cSocket_.Shutdown(SocketShutdown.Both);
                //            cUserList_.Remove(i);
                //        }
                //    }
                //    else if (SocketError.WouldBlock == err)
                //    {
                //        //Console.WriteLine("WouldBlock.");
                //    }
                //    else if (SocketError.WouldBlock != err)
                //    {
                //        Console.WriteLine("Connection Closed.");
                //        i.cSocket_.Shutdown(SocketShutdown.Both);
                //        cUserList_.Remove(i);
                //    }
                //}
                cUserListMutex_.ReleaseMutex();
            }
        }

        public void ServerProc()
        {
            Console.WriteLine("Server Proc Start.");

            bool bConnect = false;
            Socket cClient = null;
            UserConnection cConnection = new UserConnection();
            while (true)
            {
                //Console.WriteLine("Listening.");
                if (!bConnect)
                {
                    //cClient = cServer_.Accept();
                    //cClient.Blocking = false;

                    cConnection.cSocket_ = cServer_.Accept();
                    cConnection.cSocket_.Blocking = false;

                    bConnect = true;
                    Console.WriteLine("Client Connected.");

                }
                else
                {

                    int len;

                    try
                    {
                        List<ArraySegment<byte>> cBuffers = new List<ArraySegment<byte>>();

                        Byte[] bytes = new Byte[1024];

                        SocketError err;
                        len = cConnection.cSocket_.Receive(cConnection.cBuffers_, 0, cConnection.cBuffers_.Length, SocketFlags.None, out err);
                        //Console.WriteLine("Receive.");
                        if (SocketError.Success == err)
                        {
                            if (len > 0)
                            {
                                string sMsg = Encoding.ASCII.GetString(cConnection.cBuffers_, 0, len);

                                Console.WriteLine("Msg : {0} , Length : {1}", sMsg, len);
                                cConnection.cSocket_.Send(cConnection.cBuffers_);
                                cConnection.ClearBuffer();
                            }
                            else
                            {
                                Console.WriteLine("Something wrong with socket.");
                                cConnection.cSocket_.Shutdown(SocketShutdown.Both);
                                bConnect = false;
                            }
                        }
                        else if (SocketError.WouldBlock == err)
                        {
                            //Console.WriteLine("WouldBlock.");
                        }
                        else if (SocketError.WouldBlock != err)
                        {
                            Console.WriteLine("Connection Closed.");
                            cConnection.cSocket_.Shutdown(SocketShutdown.Both);
                            bConnect = false;
                        }

                        //len = cClient.Receive(bytes);
                        //cConnection.cSocket_.BeginReceive(cConnection.cBuffers_, 0, cConnection.cBuffers_.Length, SocketFlags.None, new AsyncCallback(ReceieveComplete), cConnection);
                        //if (len == 0)
                        //{
                        //    bConnect = false;
                        //    cClient.Disconnect(false);
                        //    Console.WriteLine("Client Disconnected.");
                        //}
                        //else
                        //{
                        //    string sMsg = Encoding.ASCII.GetString(bytes);
                        //    Console.WriteLine("{0}", sMsg);
                        //}
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine("Server Proc Exception : {0}", ex.ToString());
                        bConnect = false;
                    }
                }
            }
        }

        public void ReceieveComplete(IAsyncResult result)
        {
            UserConnection cConnection = result.AsyncState as UserConnection;
            //Socket cSocket = result.AsyncState as Socket;
            var bytesReceived = cConnection.cSocket_.EndReceive(result);

            if (bytesReceived == 0)
            {
                cConnection.cSocket_.Shutdown(SocketShutdown.Both);
                return;
            }

            string sStr = Encoding.ASCII.GetString(cConnection.cBuffers_);

            cConnection.cSocket_.Send(cConnection.cBuffers_);

            cConnection.ClearBuffer();

            //Byte[] bRet = Encoding.UTF8.GetBytes(sStr);

            //cSocket.Send(bRet);
        }
    }
}
