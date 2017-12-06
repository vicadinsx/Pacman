using Proxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Timers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels.Tcp;

namespace Puppetmaster
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            int f = 1;//numero de falhas a tolerar
            for (int i = 1; i < (2 * f) + 1; i++)
            {
                Hashtable props = new Hashtable();
                props["port"] = 8085 + i;
                props["name"] = "GameServer";

                //Set up for remoting events properly
                BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
                serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

                TcpServerChannel channel = new TcpServerChannel(props, serverProv);
                ChannelServices.RegisterChannel(channel, false);

                RemotingConfiguration.RegisterWellKnownServiceType( typeof(IServer), "GameManagement", WellKnownObjectMode.Singleton);
            }
            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();
        }
    }
    class Puppetmaster
    {

    }
}
