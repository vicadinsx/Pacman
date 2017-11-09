using Proxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Timers;

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

        PlayerGameObject[] playerObjects;
        UnmovableGameObject[] unmovableGameObjects;
        EnemyGameObject[] enemyGameObjects;

        private const int MAX_NUMBER = 1;
        private const int MS_TIMER = 30;
        System.Timers.Timer movementTimer;

        GameServerServices()
        {
            clients = new List<IClient>(MAX_NUMBER);
            playerObjects = new PlayerGameObject[MAX_NUMBER];
            createEnemies();
            createUnmovableObjects();
        }

        private void createEnemies()
        {
            enemyGameObjects = new EnemyGameObject[3];

            //RedGhost
            enemyGameObjects[0] = new EnemyGameObject(5, 0, 240, 90, 40, 37, EnemyType.RED);
            //YellowGhost
            enemyGameObjects[1] = new EnemyGameObject(5, 0, 290, 336, 40, 37, EnemyType.YELLOW);
            //PinkGhost
            enemyGameObjects[2] = new EnemyGameObject(5, 5, 370, 89, 40, 37, EnemyType.PINK);
        }

        private void createUnmovableObjects()
        {
            unmovableGameObjects = new UnmovableGameObject[4];

            //Wall up-left
            unmovableGameObjects[0] = new UnmovableGameObject(85, 35, 20, 117,true, UnmovableType.WALL, System.Drawing.Color.MidnightBlue);

            //Wall down-left
            unmovableGameObjects[1] = new UnmovableGameObject(125, 230, 20, 117, true, UnmovableType.WALL, System.Drawing.Color.MidnightBlue);

            //Wall up-right
            unmovableGameObjects[2] = new UnmovableGameObject(245, 35, 20, 117, true, UnmovableType.WALL, System.Drawing.Color.MidnightBlue);

            //Wall down-right
            unmovableGameObjects[3] = new UnmovableGameObject(290, 230, 20, 117, true, UnmovableType.WALL, System.Drawing.Color.MidnightBlue);
        }

        public void RegisterClient(string NewClientName)
        {
            Console.WriteLine("New client listening at " + "tcp://localhost:" + NewClientName + "/GameClient");
            IClient newClient =
                (IClient)Activator.GetObject(
                       typeof(IClient), "tcp://localhost:" + NewClientName + "/GameClient");

            lock (clients)
            {
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

            for (int i = 0; i < MAX_NUMBER; i++)
            {
                playerObjects[i] = new PlayerGameObject(i);
            }

            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    ((IClient)clients[i]).StartGame(i, playerObjects, enemyGameObjects, unmovableGameObjects);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed sending message to client. Removing client. " + e.Message);
                    clients.RemoveAt(i);
                }
            }

            movementTimer = new System.Timers.Timer(MS_TIMER);

            movementTimer.Elapsed += RoundTimer;
            movementTimer.AutoReset = true;
            movementTimer.Enabled = true;
        }

        public void RegisterMovement(int playerNumber, Movement movement)
        {
            lock (playerObjects)
            {
                if(movement == Movement.UP)
                {
                    playerObjects[playerNumber].goup = true;
                }
                if (movement == Movement.DOWN)
                {
                    playerObjects[playerNumber].godown = true;
                }
                if (movement == Movement.LEFT)
                {
                    playerObjects[playerNumber].goleft = true;
                }
                if (movement == Movement.RIGHT)
                {
                    playerObjects[playerNumber].goright = true;
                }

            }
        }

        public void UnRegisterMovement(int playerNumber, Movement movement)
        {
            lock (playerObjects)
            {
                if (movement == Movement.UP)
                {
                    playerObjects[playerNumber].goup = false;
                }
                if (movement == Movement.DOWN)
                {
                    playerObjects[playerNumber].godown = false;
                }
                if (movement == Movement.LEFT)
                {
                    playerObjects[playerNumber].goleft = false;
                }
                if (movement == Movement.RIGHT)
                {
                    playerObjects[playerNumber].goright = false;
                }

            }
        }

        public void RoundTimer(Object source, ElapsedEventArgs e)
        {
            for (int i = 0; i < playerObjects.Length; i++)
            {
                playerObjects[i].updatePosition();
            }

            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    ((IClient)clients[i]).UpdateGame(playerObjects, enemyGameObjects);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed sending message to client. Removing client. " + ex.Message);
                    clients.RemoveAt(i);
                }
            }
        }
    }
}
