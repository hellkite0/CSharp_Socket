using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CSharp_Socket
{
    class Program
    {
        static ServerCore cServerCore_;

        static void Main(string[] args)
        {
            var domain = AppDomain.CurrentDomain;
            domain.ProcessExit += new EventHandler(OnExit);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            Form1 cMainHandler_ = new Form1();

            cServerCore_ = new ServerCore(ref cMainHandler_);
            cServerCore_.Initialize();
            cServerCore_.StartServer();

            //Console.Read();

            Application.Run(cMainHandler_);

            
        }

        static void OnExit(object sender, EventArgs e)
        {
            Console.WriteLine("On Exit.");
            cServerCore_.Finalize();
        }
    }
}
