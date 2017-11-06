using Proxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;

namespace Server
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            Hashtable props = new Hashtable();
            props["port"] = 8086;
            props["name"] = "GameServer";

            //Set up for remoting events properly
            BinaryServerFormatterSinkProvider serverProv =
                  new BinaryServerFormatterSinkProvider();
            serverProv.TypeFilterLevel =
                  System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

            TcpServerChannel channel = new TcpServerChannel(props, serverProv);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(GameServerServices),
                "GameManagement",
                WellKnownObjectMode.Singleton);


            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();
        }
    }

    class GameServerServices : MarshalByRefObject, IServer
    {
        List<IClient> clients;
        private const int MAX_NUMBER = 2;
        GameServerServices()
        {
            clients = new List<IClient>();
        }


        public void RegisterClient(string NewClientName)
        {
            Console.WriteLine("New client listening at " + "tcp://localhost:" + NewClientName + "/GameClient");
            IClient newClient =
                (IClient)Activator.GetObject(
                       typeof(IClient), "tcp://localhost:" + NewClientName + "/GameClient");
            clients.Add(newClient);

            ThreadStart tsNew = new ThreadStart(this.PublishNewPlayer);
            Thread tNew = new Thread(tsNew);
            tNew.Start();

            if (clients.Count == MAX_NUMBER)
            {
                ThreadStart ts = new ThreadStart(this.StartGame);
                Thread t = new Thread(ts);
                t.Start();
            }
        }
        
        private void PublishGameEvent(string message, string auxMessage)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    ((IClient)clients[i]).GameEvent(message, auxMessage);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed sending message to client. Removing client. " + e.Message);
                    clients.RemoveAt(i);
                }
            }
        }
        private void PublishNewPlayer()
        {
            PublishGameEvent("NEWPLAYER", clients.Count.ToString());
        }

        public void StartGame()
        {
            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    ((IClient)clients[i]).StartGame(i, clients.Count);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed sending message to client. Removing client. " + e.Message);
                    clients.RemoveAt(i);
                }
            }
        }
    }
}
