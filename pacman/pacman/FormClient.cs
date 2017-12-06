using System;
using System.Windows.Forms;
using Proxy;
using System.Runtime.Remoting.Channels;
using System.Collections;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels.Tcp;
using System.Drawing;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace pacman {

    public delegate void SetBoxText(string Message);
    public partial class FormClient : Form {

        List<IClient> ActivePlayers; //lista de peers para o chat
        List<PictureBox> pacmans;
        List<PictureBox> gameEnemies;
        List<PictureBox> unmovableObjects;
        IServer obj;
        BinaryClientFormatterSinkProvider clientProv;
        BinaryServerFormatterSinkProvider serverProv;
        string lol = string.Empty;
        private int clientPlayerNumber;
        bool gameRunning;

        Movement currentMovement;
        int score = 0;      

        public FormClient() {
            
            InitializeComponent();
            label2.Visible = false;
			gameRunning = false;
            ActivePlayers = new List<IClient>();
        }

        private void keyisdown(object sender, KeyEventArgs e) {
            if (!gameRunning) return;

            if (e.KeyCode == Keys.Left) {
                pacmans[clientPlayerNumber].Image = Properties.Resources.Left;
                currentMovement = Movement.LEFT;
            }
            if (e.KeyCode == Keys.Right) {
                pacmans[clientPlayerNumber].Image = Properties.Resources.Right;
                currentMovement = Movement.RIGHT;
            }
            if (e.KeyCode == Keys.Up) {
                pacmans[clientPlayerNumber].Image = Properties.Resources.Up;
                currentMovement = Movement.UP;
            }
            if (e.KeyCode == Keys.Down) {
                pacmans[clientPlayerNumber].Image = Properties.Resources.down;
                currentMovement = Movement.DOWN;
            }
            if (e.KeyCode == Keys.Enter) {
                    tbMsg.Enabled = true; tbMsg.Focus();
               }

            obj.RegisterMovement(clientPlayerNumber, currentMovement);
        }

        private void keyisup(object sender, KeyEventArgs e)
        {
            if (!gameRunning) return;


            if (e.KeyCode == Keys.Left)
            {
                currentMovement = Movement.LEFT;
            }
            if (e.KeyCode == Keys.Right)
            {
                currentMovement = Movement.RIGHT;
            }
            if (e.KeyCode == Keys.Up)
            {
                currentMovement = Movement.UP;
            }
            if (e.KeyCode == Keys.Down)
            {
                currentMovement = Movement.DOWN;
            }
            if (e.KeyCode == Keys.Enter)
            {
                tbMsg.Enabled = true; tbMsg.Focus();
            }

            obj.UnRegisterMovement(clientPlayerNumber, currentMovement);
        }

        private void defineMovementImage(Movement direction, int playerNumber)
        {
            switch (direction)
            {
                case Movement.DOWN:
                    pacmans[playerNumber].Image = Properties.Resources.down;
                    break;
                case Movement.UP:
                    pacmans[playerNumber].Image = Properties.Resources.Up;
                    break;
                case Movement.LEFT:
                    pacmans[playerNumber].Image = Properties.Resources.Left;
                    break;
                case Movement.RIGHT:
                    pacmans[playerNumber].Image = Properties.Resources.Right;
                    break;
            }
        }

        public void doEnemyMovement(IEnemy enemy, int enemyNumber)
        {
            try
            {
                gameEnemies[enemyNumber].Top = enemy.GetY() - enemy.GetSizeY();
                gameEnemies[enemyNumber].Left = enemy.GetX() - enemy.GetSizeX();

                if (!gameRunning) return;

                if (pacmans[clientPlayerNumber].Bounds.IntersectsWith(gameEnemies[enemyNumber].Bounds))
                {
                    obj.PlayerKilled(clientPlayerNumber);
                }
            }
            catch(Exception ex)
            {
                throw new Exception("Error on doEnemyMovement : " + ex.Message);
            }
        }
        public void doUnmovableMovement(IUnmovable unmovable, int unmovableNumber)
        {
            try
            {
                unmovableObjects[unmovableNumber].Visible = unmovable.isVisible();
                unmovableObjects[unmovableNumber].Top = unmovable.GetY();
                unmovableObjects[unmovableNumber].Left = unmovable.GetX();

                if (!gameRunning) return;

                if (pacmans[clientPlayerNumber].Bounds.IntersectsWith(unmovableObjects[unmovableNumber].Bounds) && unmovable.GetEnemyType() == UnmovableType.WALL)
                {
                    obj.PlayerKilled(clientPlayerNumber);
                }

                if (pacmans[clientPlayerNumber].Bounds.IntersectsWith(unmovableObjects[unmovableNumber].Bounds) && unmovable.GetEnemyType() == UnmovableType.COIN)
                {
                    obj.GatheredCoin(clientPlayerNumber, unmovableNumber);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error on doUnmovableMovement : " + ex.Message);
            }
        }

        public void doMovement(IPlayer movement, int playerNumber)
        {
            try
            {
                pacmans[playerNumber].Top = movement.GetY();
                pacmans[playerNumber].Left = movement.GetX();

                if (movement.isMovementChanged())
                    defineMovementImage(movement.GetMovement(), playerNumber);

                if (!gameRunning) return;

                if (playerNumber == this.clientPlayerNumber && movement.isPlayerDead())
                {
                    label2.Text = "You are dead";
                    label2.Visible = true;
                }

                if (playerNumber == this.clientPlayerNumber)
                {
                    score = movement.getScore();
                }

                label1.Text = "Score: " + score;
            }
            catch (Exception ex)
            {
                throw new Exception("Error on doMovement : " + ex.Message);
            }
        }

        private void TbMsg_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                tbMsg.Enabled = false; this.Focus();
                //tbMsg.Text = string.Empty;
                ThreadStart ts = new ThreadStart(this.SendMessageToAll);
                Thread t = new Thread(ts);
                t.Start();
            }
        }

        private void SendMessageToAll()
        {
            for (int i = 0; i < ActivePlayers.Count; i++)
            {
                ActivePlayers[i].Message("MESSAGE", tbMsg.Text);
            }
        }

        public void GameEvent(string Message, string auxMessage)
        {
            switch(Message)
            {
                case "NEWPLAYER":
                    SetTextBox("Player "+auxMessage+" joined the game.");
                    break;
                case "GAMEOVER":
                    label1.Text = auxMessage;
                    label2.Visible = false;
                    gameRunning = false;
                    JoinGame.Enabled = true;
                    SetTextBox("System : Press Join Game to start a new game");
                    break;
                default:
                    return;
            }
        }

        public void Message(string Message, string auxMessage)
        {
            switch (Message)
            {
                case "MESSAGE":
                    SetTextBox("Player " + this.clientPlayerNumber + ": "+auxMessage);
                    break;
                default:
                    return;
            }
        }

        public void StartGame(int playerNumber, IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovableGameObjects)
        {
            this.clientPlayerNumber = playerNumber; 
            if (playerNumber != -1)
            { 
                gameRunning = true;
                SetTextBox("Session full, game is starting!");
            }
            else label1.Text = "Viewing game...";

            addNewPlayers(players);
            addNewEnemies(enemies);
            addUnmovableObjects(unmovableGameObjects);

        }

        private void addUnmovableObjects(IUnmovable[] unmovableGameObjects)
        {
            unmovableObjects = new List<PictureBox>();
            for (int i = 0; i < unmovableGameObjects.Length; i++)
            {
                PictureBox picture = null;
                if (unmovableGameObjects[i].GetEnemyType() == UnmovableType.WALL)
                {
                    picture = new PictureBox
                    {
                        Name = "unmovableObject" + i,
                        Size = new Size(unmovableGameObjects[i].GetSizeX(), unmovableGameObjects[i].GetSizeY()),
                        Location = new Point(unmovableGameObjects[i].GetX(), unmovableGameObjects[i].GetY()),
                        BackColor = unmovableGameObjects[i].getColor(),
                        Visible = true,
                        SizeMode = PictureBoxSizeMode.Normal
                    };
                }
                else
                {
                    picture = new PictureBox
                    {
                        Name = "unmovableObject" + i,
                        Size = new Size(unmovableGameObjects[i].GetSizeX(), unmovableGameObjects[i].GetSizeY()),
                        Location = new Point(unmovableGameObjects[i].GetX(), unmovableGameObjects[i].GetY()),
                        Image = getUnmovableImage(unmovableGameObjects[i].GetEnemyType()),
                        Visible = true,
                        SizeMode = PictureBoxSizeMode.StretchImage
                    };
                }
                this.Controls.RemoveByKey("unmovableObject" + i);
                unmovableObjects.Add(picture);
                this.Controls.Add(picture);
            }
        }

        private void addNewEnemies(IEnemy[] enemies)
        {
            gameEnemies = new List<PictureBox>();
            for (int i = 0; i < enemies.Length; i++)
            {
                PictureBox picture = new PictureBox
                {
                    Name = "enemy" + i,
                    Size = new Size(enemies[i].GetSizeX(), enemies[i].GetSizeY()),
                    Location = new Point(enemies[i].GetX(), enemies[i].GetY()),
                    Image = getEnemyImage(enemies[i].GetEnemyType()),
                    Visible = true,
                    SizeMode = PictureBoxSizeMode.Zoom
                };
                this.Controls.RemoveByKey("enemy" + i);
                gameEnemies.Add(picture);
                this.Controls.Add(picture);
            }
        }

        private Image getUnmovableImage(UnmovableType type)
        {
            switch (type)
            {
                case UnmovableType.COIN:
                    return Properties.Resources.atc_coin;
                case UnmovableType.WALL:
                    return null;
            }

            return null;
        }

        private Image getEnemyImage(EnemyType type)
        {
            switch(type)
            {
                case EnemyType.PINK:
                    return Properties.Resources.pink_guy;
                case EnemyType.RED:
                    return Properties.Resources.red_guy;
                case EnemyType.YELLOW:
                    return Properties.Resources.yellow_guy;
            }

            return null;
        }

        private void addNewPlayers(IPlayer[] players)
        {
            pacmans = new List<PictureBox>();
            for (int i = 0; i < players.Length; i++)
            {
                PictureBox picture = new PictureBox
                {
                    Name = "pacman"+i,
                    Size = new Size(players[i].GetSizeX(), players[i].GetSizeY()),
                    Location = new Point(players[i].GetX(), players[i].GetY()),
                    Image = Properties.Resources.Left,
                    Visible = true,
                    SizeMode = PictureBoxSizeMode.StretchImage
                };
                this.Controls.RemoveByKey("pacman" + i);
                pacmans.Add(picture);
                this.Controls.Add(picture);
            }
        }

        private void SetTextBox(string Message)
        {
            if (tbChat.InvokeRequired)
            {
                this.BeginInvoke(new SetBoxText(SetTextBox), new object[] { Message });
                return;
            }
            else
                tbChat.AppendText(Message + "\r\n");
        }

		internal void ActivePlayersUpdate(List<IClient> players)
		{
		    ActivePlayers.Clear();
		    ActivePlayers.AddRange(players);
		}

        TcpChannel channel;
        ChannelDataStore channelData;
        int port;

        private void JoinGame_Click(object sender, EventArgs e)
        {
            JoinGame.Enabled = false;
            gameRunning = false;

            if (obj == null)
            {
                ClientServices.form = this;

                //Define client and server providers (full filter to be able to use events).  
                clientProv = new BinaryClientFormatterSinkProvider();
                serverProv = new BinaryServerFormatterSinkProvider();
                serverProv.TypeFilterLevel =
                  System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;

                //Dummy props.
                Hashtable props = new Hashtable();
                props["name"] = "GameClient";
                props["port"] = 0;

                //Connect tcp channel with server and client provider settings.
                channel = new TcpChannel(props, clientProv, serverProv);
                ChannelServices.RegisterChannel(channel, false);

                ClientServices servicos = new ClientServices();
                RemotingServices.Marshal(servicos, "GameClient",
                    typeof(ClientServices));

                //Activate class and get object.
                obj = (IServer)Activator.GetObject(typeof(IServer),
                    string.Format("tcp://localhost:{0}/GameManagement", "8086"));

                channelData = (ChannelDataStore)channel.ChannelData;
                port = new Uri(channelData.ChannelUris[0]).Port;
            }

            try
            {
                //Register event.
                obj.RegisterClient(port.ToString());
            }
            catch (SocketException)
            {
                tbChat.Text = "Could not locate server";
                ChannelServices.UnregisterChannel(channel);
                return;
            }

            tbChat.Text = "Connected! \r\n";
            label1.Text = "Waiting for players...";
        }
    }

    delegate void Message(string mensagem, string auxMessage);
    delegate void ActivePlayers(List<IClient> players);
    delegate void DelGameEvent(string mensagem, string auxMessage);
    delegate void StartGameEvent(int playerNumber, IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovables);
    delegate void PlayerMovement(IPlayer movement, int playerNumber);
    delegate void EnemyMovement(IEnemy movement, int playerNumber);
    delegate void UnmovableMovement(IUnmovable movement, int playerNumber);

    public class ClientServices : MarshalByRefObject, IClient
    {
        public static FormClient form;

        public ClientServices()
        {
        }

        public void UpdatePlayers(List<IClient> players) //fazer update da lista de ligações
        {
            form.Invoke(new ActivePlayers(form.ActivePlayersUpdate), players);
        }

        public void UpdateGame(IPlayer[] movements, IEnemy[] enemies, IUnmovable[] unmovableObjects)
        {
            //TODO fazer isto tudo sem fors (fica mais sincrono)
            for (int i = 0; i < movements.Length; i++)
            {
                form.Invoke(new PlayerMovement(form.doMovement), movements[i], i);
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                form.Invoke(new EnemyMovement(form.doEnemyMovement), enemies[i], i);
            }

            for(int i=0; i < unmovableObjects.Length; i++)
            {
                form.Invoke(new UnmovableMovement(form.doUnmovableMovement), unmovableObjects[i], i);
            }
        }

        public void GameEvent(string message, string auxMessage)
        {
            form.Invoke(new DelGameEvent(form.GameEvent), message, auxMessage);
        }

        public void StartGame(int playerNumber, IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovableObjects)
        {
            form.Invoke(new StartGameEvent(form.StartGame), playerNumber, players, enemies, unmovableObjects);
        }

        public void Message(String type , String message)
        {
            form.Invoke(new Message(form.Message), type, message);
        }

        public void StartViewingGame(IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovableObjects)
        {
            form.Invoke(new StartGameEvent(form.StartGame), -1, players, enemies, unmovableObjects);
        }
    }
}
