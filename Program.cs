using System.Net;

namespace MantoProxy
{
    class MantoProxy
    {
        private const int ListenPort = 8080;

        private static readonly Application APP = new(IPAddress.Any, ListenPort);

        private const string DebugFlag = "--debug";

        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    switch (arg)
                    {
                        case DebugFlag:
                            Application.SetDebugMode(true);
                            Console.WriteLine("Inicializando com o modo de debug!");
                            break;
                        default:
                            break;
                    }
                }
            }

            APP.Start();
        }

    }
}
