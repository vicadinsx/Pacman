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

    public class GameServerServices : MarshalByRefObject, IServer
    {
        List<IClient> clients;
        List<IClient> connectedClients;

        Dictionary<int, PlayerGameObject[]> playerByRound;
        Dictionary<int, UnmovableGameObject[]> unmovableByRound;
        Dictionary<int, EnemyGameObject[]> enemyByRound;

        PlayerGameObject[] playerObjects;
        UnmovableGameObject[] unmovableGameObjects;
        EnemyGameObject[] enemyGameObjects;

        bool isCoord = false;
        bool isCrashed = false;
        int CoordId;
        int id;
        int[] serverIds;
        private int MAX_NUMBER = 2;
        private int MS_TIMER = 30;
        private const int NUM_COINS = 60;
        private bool gameRunning = false;
        IServer[] servers;
        int playerNumber = 0;
        int score = 0;
        bool isFrozen = false;

        System.Timers.Timer movementTimer;
        System.Timers.Timer pingTimer;
        System.Timers.Timer answserTimer;

        int roundNumber = 0;
        public GameServerServices()
        {
            connectedClients = new List<IClient>();
            clients = new List<IClient>(MAX_NUMBER);
            playerObjects = new PlayerGameObject[MAX_NUMBER];

            playerByRound = new Dictionary<int, PlayerGameObject[]>();
            enemyByRound = new Dictionary<int, EnemyGameObject[]>();
            unmovableByRound = new Dictionary<int, UnmovableGameObject[]>();

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

        public void Freeze()
        {
            isFrozen = true;
        }

        public void UnFreeze()
        {
            isFrozen = false;
        }

        private void updateGame()
        {
            for (int i = 0; i < playerObjects.Length; i++)
            {
                if (!playerObjects[i].isPlayerDead())
                    playerObjects[i].updatePosition();
            }

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

        public void RegisterClient(string NewClientName, string name)
        {
            Console.WriteLine("New client listening at " + "tcp://localhost:" + NewClientName + "/GameClient"+name);
            IClient newClient =
                (IClient)Activator.GetObject(
                       typeof(IClient), "tcp://localhost:" + NewClientName + "/GameClient"+name);

            lock (clients)
            {
                if (!connectedClients.Contains(newClient))
                {
                    connectedClients.Add(newClient);

                    //enviar lista para os clientes 
                    ThreadStart tsp = new ThreadStart(this.UpdatePlayers);
                    Thread tp = new Thread(tsp);
                    tp.Start();
                }

                if (clients.Count < MAX_NUMBER)
                {
                    clients.Add(newClient);

                    ThreadStart tsNew = new ThreadStart(this.PublishNewPlayer);
                    Thread tNew = new Thread(tsNew);
                    tNew.Start();

                    if (clients.Count == MAX_NUMBER)
                    {
                        createEnemies();
                        createUnmovableObjects();
                        for (int i = 0; i < MAX_NUMBER; i++)
                        {
                            playerObjects[i] = new PlayerGameObject(i);
                        }

                        ThreadStart ts = new ThreadStart(this.StartGame);
                        Thread t = new Thread(ts);
                        t.Start();

                        unmovableByRound.Add(roundNumber, (UnmovableGameObject[])unmovableGameObjects.Clone());
                        enemyByRound.Add(roundNumber, (EnemyGameObject[])enemyGameObjects.Clone());
                        playerByRound.Add(roundNumber, (PlayerGameObject[])playerObjects.Clone());

                        roundNumber++;

                        movementTimer = new System.Timers.Timer(MS_TIMER);
                        movementTimer.Elapsed += RoundTimer;
                        movementTimer.AutoReset = true;
                        movementTimer.Enabled = true;
                    }
                }
                else
                {
                    ParameterizedThreadStart ts = new ParameterizedThreadStart(this.StartViewer);
                    Thread t = new Thread(ts);
                    t.Start(newClient);
                }
            }
        }
        
        private void UpdatePlayers()
        {
            for (int i = 0; i < connectedClients.Count; i++)
            {
                try
                {
                    ((IClient)connectedClients[i]).UpdatePlayers(connectedClients);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed sending message to client on UpdatePlayers. Removing client. " + e.Message);
                    //connectedClients.RemoveAt(i);
                }
            }
        }

        private void PublishGameEvent(string message, string auxMessage)
        {
            for (int i = 0; i < connectedClients.Count; i++)
            {
                try
                {
                    ((IClient)connectedClients[i]).GameEvent(message, auxMessage);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed sending message to client on PublishGameEvent. Removing client. " + e.Message);
                    //clients.RemoveAt(i);
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

            for (int i = 0; i < clients.Count; i++)
            {
                try
                {
                    ((IClient)clients[i]).StartGame(null, i, playerObjects, enemyGameObjects, unmovableGameObjects);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed sending message to client on StartGame. Removing client. " + e.Message);
                    clients.RemoveAt(i);
                }
            }

            List<IClient> viewers = connectedClients.Except(clients).ToList();

            for (int i = 0; i < viewers.Count; i++)
            {
                try
                {
                    ((IClient)viewers[i]).StartViewingGame(playerObjects, enemyGameObjects, unmovableGameObjects);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed sending message to client on StartGame. Removing client. " + e.Message);
                    viewers.RemoveAt(i);
                }
            }
        }

        public void StartViewer(object client)
        {
            try
            {
                ((IClient)client).StartViewingGame(playerObjects, enemyGameObjects, unmovableGameObjects);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed sending message to client on StartViewer. Removing client. " + e.Message);
            }
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
            unmovableByRound.Add(roundNumber, (UnmovableGameObject[])unmovableGameObjects.Clone());
            enemyByRound.Add(roundNumber, (EnemyGameObject[])enemyGameObjects.Clone());
            playerByRound.Add(roundNumber, (PlayerGameObject[])playerObjects.Clone());

            if (!gameRunning) return;

            ParameterizedThreadStart tsUpdate = new ParameterizedThreadStart(this.sendUpdates);
            Thread tUpdate = new Thread(tsUpdate);
            tUpdate.Start(roundNumber);

            roundNumber++;
        }

        public void sendUpdates(object round)
        {
            updateGame();
            List<IClient> removedClients = new List<IClient>();
            lock (connectedClients)
            {
                for (int i = 0; i < connectedClients.Count; i++)
                {
                    try
                    {
                        ((IClient)connectedClients[i]).UpdateGame((int)round, playerObjects, enemyGameObjects, unmovableGameObjects);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed sending message to client on RoundTimer. Removing client. " + ex.Message);
                        removedClients.Add(connectedClients[i]);
                        playerObjects[i].isDead = true;
                    }
                }
            }

            connectedClients = connectedClients.Except(removedClients).ToList();
            for(int i=0; i < servers.Length; i++)
            {
                servers[i].UpdateData(playerObjects, unmovableGameObjects, enemyGameObjects);
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

        public void election()//falta o T
        {
            if (isCrashed) return;
            isCoord = true;
            for (int i = 0; i < serverIds.Count(); i++)
            {
                if (this.id < serverIds[i])
                {
                    try
                    {
                        servers[i].message("ELECTION", this.id);
                    }
                    catch(Exception)
                    {
                        //Just to test
                    }
                }
            }

            if(answserTimer == null)
            {
                answserTimer = new System.Timers.Timer(MS_TIMER);
                answserTimer.Elapsed += CoordinatorTimer;
                answserTimer.AutoReset = true;
                answserTimer.Enabled = true;
            }
            else
            {
                answserTimer.Start();
            }

               
        }

        private void CoordinatorTimer(Object source, ElapsedEventArgs e)
        {
            if(isCoord) coordinator();
            answserTimer.Stop();
        }

        public void answer(int s)//corrido quando id maior que o this.id
        {
            if (isCrashed) return;
            servers[s].message("ANSWER", this.id);
        }

        public void coordinator()
        {
            Console.WriteLine("Coordinator is : " + id);
            this.CoordId = id;
            for (int i = 0; i < serverIds.Length; i++)
            {
                if (this.id > serverIds[i])
                {
                    servers[i].message("COORDINATOR", this.id);
                }
            }

            for(int i = 0; i < connectedClients.Count; i++)
            {
                connectedClients[i].UpdateServer(this);
            }
        }

        public void message(string type, int senderId)
        {
            switch (type)
            {
                case "ELECTION":
                    if (this.id > senderId)
                    {
                        this.answer(senderId);
                        this.election();
                        break;
                    }
                    else
                        break;
                case "COORDINATOR":
                    if (senderId >= id)
                    {
                        this.CoordId = senderId;
                        SetPing();
                    }
                    else coordinator();
                    break;
                case "ANSWER":
                    isCoord = false;
                    break;
                default:
                    break;
            }
        }

        private void SetPing()
        {
            if (pingTimer == null)
            {
                pingTimer = new System.Timers.Timer(MS_TIMER * 2);
                pingTimer.Elapsed += Ping;
                pingTimer.AutoReset = true;
                pingTimer.Enabled = true;
            }
            else pingTimer.Start();
        }

        private void Ping(Object source, ElapsedEventArgs e)
        {
            if (isCrashed) return;
            try
            {
                bool isCoordAlive = servers[CoordId].IsAlive();
            }
            catch(Exception)
            {
                election();
                pingTimer.Stop();
            }
        }

        public bool IsAlive()
        {
            if (isCrashed) throw new Exception("Crashed");
            return true;
        }

        public void SetReplicationData(int serverId, int[] serversId, IServer[] servers)
        {
            this.serverIds = serversId;
            this.id = serverId;
            this.servers = servers;

            if (serverId == serverIds.Length - 1)
                coordinator();
        }

        public void UpdateData(PlayerGameObject[] _playerObjects, UnmovableGameObject[] _unmovableObjects, EnemyGameObject[] _enemyObjects)
        {
            playerObjects = _playerObjects;
            unmovableGameObjects = _unmovableObjects;
            enemyGameObjects = _enemyObjects;
        }

        public void crash()
        {
            isCrashed = true;
        }

        public int getId()
        {
            return id;
        }

        public void DefineVariables(int maxPlayers, int roundTime)
        {
            MAX_NUMBER = maxPlayers;
            MS_TIMER = roundTime;
            clients = new List<IClient>(MAX_NUMBER);
            playerObjects = new PlayerGameObject[MAX_NUMBER];
        }

        public string Status()
        {
            string result = string.Empty;
            result += isCrashed ? "Crashed" : "Connected";
            result += isFrozen ? ", Frozen" : ", Not Frozen";
            result += id == CoordId ? ", Coordinator" : ", Not Coordinator";
            return result;
        }

        public string LocalState(int round)
        {
            if (!enemyByRound.ContainsKey(round) || !unmovableByRound.ContainsKey(round) || !playerByRound.ContainsKey(round))
                return "No Results";

            string result = string.Empty;
            EnemyGameObject[] enemies = enemyByRound[round - 1];
            UnmovableGameObject[] unmovables = unmovableByRound[round - 1];
            PlayerGameObject[] players = playerByRound[round - 1];

            for (int i = 0; i < enemies.Length; i++)
            {
                result += "M, " + enemies[i].x + ", " + enemies[i].y + '\n';
            }

            for (int i = 0; i < players.Length; i++)
            {
                result += "P" + i + ", " + (players[i].isDead ? "L, " : "P, ") + players[i].x + ", " + players[i].y + '\n';
            }

            for (int i = 0; i < unmovables.Length; i++)
            {
                if (unmovables[i].GetEnemyType() == UnmovableType.COIN && unmovables[i].isVisible)
                    result += "C, " + unmovables[i].x + ", " + unmovables[i].y + '\n';

                if (unmovables[i].GetEnemyType() == UnmovableType.WALL)
                    result += "W, " + unmovables[i].x + ", " + unmovables[i].y + '\n';
            }

            return result;
        }
    }
}
