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
using System.IO;

namespace pacman {

    public delegate void SetBoxText(string Message);
    public partial class FormClient : Form {

        List<IClient> ActivePlayers; //lista de peers para o chat
        List<string> listOfMoves; //player moves if it exists file
        List<PictureBox> pacmans;
        List<PictureBox> gameEnemies;
        List<PictureBox> unmovableObjects;
        IServer obj;
        BinaryClientFormatterSinkProvider clientProv;
        BinaryServerFormatterSinkProvider serverProv;
        string lol = string.Empty;
        private int clientPlayerNumber;
        bool gameRunning;
        bool isFinished = false;

        Movement currentMovement;
        int score = 0;
        int counter = 0;

        public FormClient() {
            
            InitializeComponent();
            label2.Visible = false;
			gameRunning = false;
            ActivePlayers = new List<IClient>();
            listOfMoves = new List<string>();

            label3.Text = "Press Join Game to join";
            label3.Visible = true;
            label1.Visible = false;
        }

        private void ReadMoves(string path)
        {
            using (var reader = new StreamReader(path))
            {
                listOfMoves = new List<string>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    listOfMoves.Add(values[1]);
                }
            }
        }

        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && !tbMsg.Enabled)
            {
                tbMsg.Text = string.Empty;
                tbMsg.Enabled = true;
                tbMsg.Focus();
                return true;
            }
            if (keyData == Keys.Enter && tbMsg.Enabled)
            {
                ThreadStart ts = new ThreadStart(this.SendMessageToAll);
                Thread t = new Thread(ts);
                t.Start();

                tbMsg.Enabled = false;
                this.Focus();
                return true;
            }
            return false;
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

            obj.RegisterMovement(clientPlayerNumber, currentMovement);
        }

        private Movement getMovement(string movement)
        {
            switch(movement)
            {
                case "RIGHT":
                    return Movement.RIGHT;
                case "LEFT":
                    return Movement.LEFT;
                case "DOWN":
                    return Movement.DOWN;
                case "UP":
                    return Movement.UP;
            }
            return Movement.UNDEFINED;
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
                    if (isFinished)
                    {
                        obj.UnRegisterMovement(clientPlayerNumber, movement.GetMovement());
                        isFinished = false;
                    }

                    score = movement.getScore();
                    if(counter < listOfMoves.Count)
                    {
                        obj.UnRegisterMovement(clientPlayerNumber, movement.GetMovement());
                        obj.RegisterMovement(clientPlayerNumber, getMovement(listOfMoves[counter]));
                        counter++;
                        if (counter == listOfMoves.Count)
                            isFinished = true;
                    }
                }

                label1.Text = "Score: " + score;
            }
            catch (Exception ex)
            {
                throw new Exception("Error on doMovement : " + ex.Message);
            }
        }

        private void SendMessageToAll()
        {
            for (int i = 0; i < ActivePlayers.Count; i++)
            {
                ActivePlayers[i].Message("MESSAGE", this.clientPlayerNumber.ToString(), tbMsg.Text);
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
                    label3.Text = auxMessage;
                    label3.Visible = true;

                    label2.Visible = false;
                    gameRunning = false;
                    JoinGame.Enabled = true;
                    label1.Text = "Press Join Game to start a new game";
                    label1.Visible = true;
                    break;
                default:
                    return;
            }
        }

        public void Message(string Message, string sender,string auxMessage)
        {
            switch (Message)
            {
                case "MESSAGE":
                    SetTextBox("Player " + sender + ": "+auxMessage);
                    break;
                default:
                    return;
            }
        }

        public void StartGame(string filePath, int playerNumber, IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovableGameObjects)
        {
            if(!string.IsNullOrEmpty(filePath))
                ReadMoves(filePath);

            this.clientPlayerNumber = playerNumber;
            if (playerNumber != -1)
            { 
                gameRunning = true;
                SetTextBox("Session full, game is starting!");
            }
            else label1.Text = "Viewing game...";

            label3.Visible = false;
            label1.Visible = true;

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
            label3.Visible = false;
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

                channelData = (ChannelDataStore)channel.ChannelData;
                port = new Uri(channelData.ChannelUris[0]).Port;

                ClientServices servicos = new ClientServices(port.ToString());
                RemotingServices.Marshal(servicos, "GameClient",
                    typeof(ClientServices));

                //Activate class and get object.
                obj = (IServer)Activator.GetObject(typeof(IServer),
                    string.Format("tcp://localhost:{0}/GameManagement", "8086"));

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

    delegate void Message(string mensagem, string sender, string auxMessage);
    delegate void ActivePlayers(List<IClient> players);
    delegate void DelGameEvent(string mensagem, string auxMessage);
    delegate void StartGameEvent(string filePath, int playerNumber, IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovables);
    delegate void PlayerMovement(IPlayer movement, int playerNumber);
    delegate void EnemyMovement(IEnemy movement, int playerNumber);
    delegate void UnmovableMovement(IUnmovable movement, int playerNumber);

    public class ClientServices : MarshalByRefObject, IClient
    {
        public static FormClient form;
        public string name;

        public ClientServices(string name)
        {
            this.name = name;
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

        public void StartGame(string filePath, int playerNumber, IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovableObjects)
        {
            form.Invoke(new StartGameEvent(form.StartGame), filePath, playerNumber, players, enemies, unmovableObjects);
        }

 		public void Message(String type , String sender, String message)
        {
            form.Invoke(new Message(form.Message), type, sender, message);
        }

        public void StartViewingGame(IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovableObjects)
        {
            form.Invoke(new StartGameEvent(form.StartGame), null , - 1, players, enemies, unmovableObjects);
        }

        public override bool Equals(object obj)
        {
            return this.name.Equals(((ClientServices)obj).name);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }
}
