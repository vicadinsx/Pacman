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
using System.IO;

namespace Puppetmaster
{
    public class PuppetMaster
    {
        private const int F = 1;//numero de falhas a tolerar
        static Dictionary<string,IServer> servers = new Dictionary<string, IServer>((2 * F) + 1);
        static Dictionary<string, FormClient> clients = new Dictionary<string, FormClient>(2);
        static int[] serverIds = new int[(2 * F) + 1];
        static IServer[] serversRep = new IServer[(2 * F) + 1];

        public static void Main(string[] args)
        {

            testing(serverIds);

            while(true)
            {
                string commandLine = Console.ReadLine();

                string[] command = commandLine.Split(' ');

                try
                {
                    if(!command[0].Equals("Read"))
                        HandleCommand(command);
                    else
                    {
                        using (var reader = new StreamReader(command[1]))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                command = line.Split(' ');

                                HandleCommand(command);
                            }
                        }
                    }
                        
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
            FormClient client = new FormClient();
            string port = ClientURL.Split(':')[2].Split('/')[0];
            string name = ClientURL.Split(':')[2].Split('/')[1];

            client.JoinGameByPuppet(serversRep[0], port, PID);
            clients.Add(PID,client);

            ParameterizedThreadStart tsClient = new ParameterizedThreadStart(StartClient);
            Thread tClient = new Thread(tsClient);
            tClient.Start(client);
        }

        private static void StartClient(object client)
        {
            Application.Run((FormClient)client);
        }

        private static void StartServerCommand(string PID, string PCSUrl, string ServerURL, string timePerRound, string numPlayers)
        {
            string port = ServerURL.Split(':')[2].Split('/')[0];
            string name = ServerURL.Split(':')[2].Split('/')[1];
            Hashtable props = new Hashtable();
            props["port"] = port;
            props["name"] = name;

            //Set up for remoting events properly
            BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
            serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

            TcpServerChannel channel = new TcpServerChannel(props, serverProv);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(GameServerServices), name, WellKnownObjectMode.Singleton);
            IServer server = (IServer)Activator.GetObject(typeof(IServer), ServerURL);
            server.DefineVariables(int.Parse(numPlayers), int.Parse(timePerRound));

            servers.Add(PID, server);
            serverIds[0] = 0;
            serversRep[0] = server;
            AddReplicationServers(int.Parse(port), name, numPlayers, timePerRound);
        }

        private static void AddReplicationServers(int port, string name, string numPlayers, string timePerRound)
        {
            for (int i = 1; i < (2 * F)+1; i++)
            {
                Hashtable props = new Hashtable();
                props["port"] = (port + i).ToString();
                props["name"] = name + i;

                serverIds[i] = i;

                //Set up for remoting events properly
                BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
                serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

                TcpServerChannel channel = new TcpServerChannel(props, serverProv);
                ChannelServices.RegisterChannel(channel, false);

                RemotingConfiguration.RegisterWellKnownServiceType(typeof(GameServerServices), name + i, WellKnownObjectMode.Singleton);
                IServer server = (IServer)Activator.GetObject(typeof(IServer), "tcp://localhost:" + (port + i) + "/" + name+i);

                serversRep[i] = server;
                server.DefineVariables(int.Parse(numPlayers), int.Parse(timePerRound));
            }

            for (int i = 0; i < serverIds.Length; i++)
            {
                serversRep[i].SetReplicationData(i, serverIds, serversRep);
            }

            for(int i=0; i<serverIds.Length;i++)
            {
                Console.WriteLine("Server Id:" + serversRep[i].getId());
            }
        }

        private static void GlobalStatusCommand()
        {
            string result = string.Empty;
            for(int i=0; i<serversRep.Length; i++)
            {
                result += "Server" +i +" : "+ serversRep[i].Status()+"\n";
            }

            foreach(KeyValuePair<string, FormClient> client in clients)
            {
                result += client.Key + " : " + client.Value.Status() + '\n';
            }
            Console.WriteLine(result);
        }

        private static void CrashCommand(string PID)
        {
            if (servers.ContainsKey(PID))
            {
                for (int i = 0; i < serversRep.Length; i++)
                {
                    if (serversRep[i].IsServerCoord())
                    {
                        serversRep[i].crash();
                        break;
                    }
                }
            }
            else if (clients.ContainsKey(PID))
                clients[PID].crash();
        }
        private static void FreezeCommand(string PID)
        {
            if (servers.ContainsKey(PID))
            {
                for (int i = 0; i < serversRep.Length; i++)
                {
                    if (serversRep[i].IsServerCoord())
                    {
                        serversRep[i].Freeze();
                        break;
                    }
                }
            }
            else if (clients.ContainsKey(PID))
                clients[PID].Freeze();
        }
        private static void UnFreezeCommand(string PID)
        {
            if (servers.ContainsKey(PID))
            {
                for(int i=0; i<serversRep.Length; i++)
                    serversRep[i].UnFreeze();
            }
            else if (clients.ContainsKey(PID))
                clients[PID].UnFreeze();
        }

        private static void InjectDelayCommand(string PIDSource, string PIDDestiny)
        {
            //if (servers.ContainsKey(PIDSource))
            //    servers[PIDSource].delay(PIDDestiny);
            //else if (clients.ContainsKey(PIDSource))
            //    clients[PIDSource].delay(PIDDestiny);
            //if (servers.ContainsKey(PIDDestiny))
            //    servers[PIDDestiny].delay(PIDSource);
            //else if (clients.ContainsKey(PIDDestiny))
            //    clients[PIDDestiny].delay(PIDSource);
        }

        private static void LocalStateCommand(string PID, string RoundId)
        {
            if (servers.ContainsKey(PID))
                Console.WriteLine(servers[PID].LocalState(int.Parse(RoundId)));
            else if(clients.ContainsKey(PID))
                Console.WriteLine(clients[PID].LocalState(int.Parse(RoundId)));
        }

        private static void WaitCommand(string time)
        {
            Thread.Sleep(int.Parse(time));
        }

        static void testing(int[] serverIds)
        {
            //Thread.Sleep(2000);

            //clients[1].crash();
            //servers[2].crash();

            //Thread.Sleep(2000);

            //for (int i = 0; i < serverIds.Length; i++)
            //{
            //    Console.WriteLine(servers[i].getId());
            //}

            //servers[1].crash();

            //Thread.Sleep(2000);

            //for (int i = 0; i < serverIds.Length; i++)
            //{
            //    Console.WriteLine(servers[i].getId());
            //}
        }
    }
}
