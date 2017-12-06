using Proxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Timers;
using System.Linq;

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
        private const int NUM_COINS = 60;
        private bool gameRunning = false;

        int playerNumber = 0;
        int score = 0;

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
            enemyGameObjects[0] = new EnemyGameObject(5, 0, 220, 110, 40, 37, EnemyType.RED);
            //YellowGhost
            enemyGameObjects[1] = new EnemyGameObject(5, 0, 270, 336, 40, 37, EnemyType.YELLOW);
            //PinkGhost
            enemyGameObjects[2] = new EnemyGameObject(5, 5, 370, 89, 40, 37, EnemyType.PINK);
        }

        private void updateGame()
        {
            if (enemyGameObjects[0].IntersectsWith(unmovableGameObjects[0].getRectangle()) ||
                enemyGameObjects[0].IntersectsWith(unmovableGameObjects[2].getRectangle()))
                enemyGameObjects[0].enemyXSpeed = -enemyGameObjects[0].enemyXSpeed;

            if (enemyGameObjects[1].IntersectsWith(unmovableGameObjects[1].getRectangle()) ||
                enemyGameObjects[1].IntersectsWith(unmovableGameObjects[3].getRectangle()))
                    enemyGameObjects[1].enemyXSpeed = -enemyGameObjects[1].enemyXSpeed;

            if (enemyGameObjects[2].IntersectsWith(unmovableGameObjects[0].getRectangle()) ||
                enemyGameObjects[2].IntersectsWith(unmovableGameObjects[1].getRectangle()) ||
                enemyGameObjects[2].IntersectsWith(unmovableGameObjects[2].getRectangle()) ||
                enemyGameObjects[2].IntersectsWith(unmovableGameObjects[3].getRectangle()) ||
                enemyGameObjects[2].x > 370 || enemyGameObjects[2].x < 5)
                    enemyGameObjects[2].enemyXSpeed = -enemyGameObjects[2].enemyXSpeed;

            if(enemyGameObjects[2].y < 60 || enemyGameObjects[2].y > 360)
            {
                enemyGameObjects[2].enemyYSpeed = -enemyGameObjects[2].enemyYSpeed;
            }

            for (int i = 0; i < enemyGameObjects.Length; i++)
            {
                enemyGameObjects[i].UpdateObject();
            }
        }

        public void PlayerKilled(int playerNumber)
        {
            playerObjects[playerNumber].isDead = true;
            if(playerObjects.Where(u => u.isPlayerDead()).Count() == MAX_NUMBER)
            {
                gameRunning = false;
                GameOver();
            }
        }

        private void createUnmovableObjects()
        {
            unmovableGameObjects = new UnmovableGameObject[77];

            //Wall up-left
            unmovableGameObjects[0] = new UnmovableGameObject(85, 25, 20, 117,true, UnmovableType.WALL, System.Drawing.Color.MidnightBlue);

            //Wall down-left
            unmovableGameObjects[1] = new UnmovableGameObject(125, 240, 20, 117, true, UnmovableType.WALL, System.Drawing.Color.MidnightBlue);

            //Wall up-right
            unmovableGameObjects[2] = new UnmovableGameObject(245, 25, 20, 117, true, UnmovableType.WALL, System.Drawing.Color.MidnightBlue);

            //Wall down-right
            unmovableGameObjects[3] = new UnmovableGameObject(290, 240, 20, 117, true, UnmovableType.WALL, System.Drawing.Color.MidnightBlue);

            int line = 1;
            for(int i=0; i<73; i++)
            {
                unmovableGameObjects[i+4] = new UnmovableGameObject((42*i) % 378, line*40, 20, 20, true, UnmovableType.COIN, System.Drawing.Color.Yellow);
                if (i!= 0 && i % 9 == 0)
                    line++;
            }

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
                    createEnemies();
                    createUnmovableObjects();

                    //enviar lista para os clientes 
                    ThreadStart tsp = new ThreadStart(this.UpdatePlayers);
                    Thread tp = new Thread(tsp);
                    tp.Start();

                    ThreadStart ts = new ThreadStart(this.StartGame);
                    Thread t = new Thread(ts);
                    t.Start();
                }
            }
        }
        
        private void UpdatePlayers()
        {
            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    ((IClient)clients[i]).UpdatePlayers(clients);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed sending message to client. Removing client. " + e.Message);
                    clients.RemoveAt(i);
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

        private void PublishGameOver()
        {
            PublishGameEvent("GAMEOVER", "The player " + (playerNumber + 1) + " won the game with " + score + " coins");
            clients.Clear();
        }

        public void StartGame()
        {
            gameRunning = true;

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
            if (playerObjects[playerNumber].isDead)
                return;

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
            if (!gameRunning) return;

            for (int i = 0; i < playerObjects.Length; i++)
            {
                if (!playerObjects[i].isPlayerDead())
                    playerObjects[i].updatePosition();
            }

            updateGame();

            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    ((IClient)clients[i]).UpdateGame(playerObjects, enemyGameObjects, unmovableGameObjects);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed sending message to client. Removing client. " + ex.Message);
                    clients.RemoveAt(i);
                }
            }
        }

        public void GameOver()
        {
            movementTimer.Stop();
            playerNumber = 0;
            score = 0;
            for (int i = 0; i < playerObjects.Length; i++)
            {
                if (playerObjects[i].getScore() > score)
                {
                    score = playerObjects[i].getScore();
                    playerNumber = i;
                }
            }

            ThreadStart ts = new ThreadStart(this.PublishGameOver);
            Thread t = new Thread(ts);
            t.Start();
        }
        public void GatheredCoin(int playerNumber, int coinNumber)
        {
            if (unmovableGameObjects[coinNumber].isVisible && unmovableGameObjects[coinNumber].GetEnemyType() == UnmovableType.COIN)
            {
                playerObjects[playerNumber].score++;
                unmovableGameObjects[coinNumber].isVisible = false;
            }

            if (unmovableGameObjects.Where(u => u.GetEnemyType() == UnmovableType.COIN && !u.isVisible).Count() == NUM_COINS)
            {
                gameRunning = false;
                GameOver();
            }
        }
    }
}
