using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using Proxy;
using Server;
using System.Threading;
using pacman;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Puppetmaster
{
    public class PuppetMaster
    {
        private const int F = 1;//numero de falhas a tolerar
        static Dictionary<string,IServer> servers = new Dictionary<string, IServer>((2 * F) + 1);
        static Dictionary<string, FormClient> clients = new Dictionary<string, FormClient>(2);

        public static void Main(string[] args)
        {
            int[] serverIds = new int[(2 * F) + 1];

            for (int i = 0; i < (2 * F) + 1; i++)
            {
                Hashtable props = new Hashtable();
                props["port"] = (8085 + i);
                props["name"] = "GameServer"+i;
                serverIds[i] = i;

                //Set up for remoting events properly
                BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
                serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

                TcpServerChannel channel = new TcpServerChannel(props, serverProv);
                ChannelServices.RegisterChannel(channel, false);

                RemotingConfiguration.RegisterWellKnownServiceType(typeof(GameServerServices), "GameManagement"+i, WellKnownObjectMode.Singleton);
                IServer server = (IServer)Activator.GetObject(typeof(IServer), "tcp://localhost:" + (8085 + i) + "/GameManagement"+i);

                servers[i] = server;
            }

            for (int i = 0; i < serverIds.Length; i++)
            {
                servers[i].SetReplicationData(i, serverIds, servers);
            }

            testing(serverIds);

            while(true)
            {
                string commandLine = Console.ReadLine();

                string[] command = commandLine.Split(' ');

                try
                {
                    HandleCommand(command);
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("Invalid Arguments");
                }

            }
            System.Console.WriteLine("<enter> para sair...");
            System.Console.ReadLine();
        }

        private static void HandleCommand(string[] command)
        {

            switch(command[0])
            {
                case "StartClient":
                    StartClientCommand(command[1], command[2], command[3], command[4], command[5]);
                    break;
                case "StartServer":
                    StartServerCommand(command[1], command[2], command[3], command[4], command[5]);
                    break;
                case "GlobalStatus":
                    GlobalStatusCommand();
                    break;
                case "Crash":
                    CrashCommand(command[1]);
                    break;
                case "Freeze":
                    FreezeCommand(command[1]);
                    break;
                case "UnFreeze":
                    UnFreezeCommand(command[1]);
                    break;
                case "InjectDelay":
                    InjectDelayCommand(command[1], command[2]);
                    break;
                case "LocalState":
                    LocalStateCommand(command[1], command[2]);
                    break;
                case "Wait":
                    WaitCommand(command[1]);
                    break;
                default:
                    return;
            }
        }

        private static void StartClientCommand(string PID, string PCSUrl, string ClientURL, string timePerRound, string numPlayers)
        {
            ParameterizedThreadStart tsClient = new ParameterizedThreadStart(StartClient);
            Thread tClient = new Thread(tsClient);
            //tClient.Start(i);

            Thread.Sleep(2000);
        }
        private static void StartServerCommand(string PID, string PCSUrl, string ServerURL, string timePerRound, string numPlayers)
        {
        }

        private static void GlobalStatusCommand()
        {

        }

        private static void CrashCommand(string PID)
        {
            if (servers.ContainsKey(PID))
                servers[PID].crash();
            else if (clients.ContainsKey(PID))
                clients[PID].crash();
        }
        private static void FreezeCommand(string PID)
        {
            if (servers.ContainsKey(PID))
                servers[PID].freeze();
            else if (clients.ContainsKey(PID))
                clients[PID].freeze();
        }
        private static void UnFreezeCommand(string PID)
        {
            if (servers.ContainsKey(PID))
                servers[PID].unfreeze();
            else if (clients.ContainsKey(PID))
                clients[PID].unfreeze();
        }

        private static void InjectDelayCommand(string PIDSource, string PIDDestiny)
        {

        }

        private static void LocalStateCommand(string PID, string RoundId)
        {

        }

        private static void WaitCommand(string time)
        {
            Thread.Sleep(int.Parse(time));
        }
        private static void StartClient(object i)
        {
            FormClient client = new FormClient();
            int port = (8090 + (int)i);
            client.JoinGameByPuppet(servers[0], port.ToString(), i.ToString());
            clients[(int)i] = client;
            Application.Run((FormClient)client);
        }

        static void testing(int[] serverIds)
        {
            Thread.Sleep(2000);

            clients[1].crash();
            servers[2].crash();

            Thread.Sleep(2000);

            for (int i = 0; i < serverIds.Length; i++)
            {
                Console.WriteLine(servers[i].getId());
            }

            servers[1].crash();

            Thread.Sleep(2000);

            for (int i = 0; i < serverIds.Length; i++)
            {
                Console.WriteLine(servers[i].getId());
            }
        }
    }
}
