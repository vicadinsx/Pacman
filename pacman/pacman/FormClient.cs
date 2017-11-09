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

namespace pacman {

    public delegate void SetBoxText(string Message);
    public partial class FormClient : Form {

        List<PictureBox> pacmans;
        List<PictureBox> gameEnemies;
        List<PictureBox> unmovableObjects;
        IServer obj;
        BinaryClientFormatterSinkProvider clientProv;
        BinaryServerFormatterSinkProvider serverProv;
        string lol = string.Empty;
        private int playerNumber;
        bool gameRunning;

        Movement currentMovement;
        int score = 0;      

        public FormClient() {
            
            InitializeComponent();
            label2.Visible = false;
            gameRunning = false;

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
            TcpChannel channel = new TcpChannel(props, clientProv, serverProv);
            ChannelServices.RegisterChannel(channel, false);

            ClientServices servicos = new ClientServices();
            RemotingServices.Marshal(servicos, "GameClient",
                typeof(ClientServices));

            //Activate class and get object.
            obj = (IServer)Activator.GetObject(typeof(IServer),
                string.Format("tcp://localhost:{0}/GameManagement", "8086"));

            var channelData = (ChannelDataStore)channel.ChannelData;
            var port = new Uri(channelData.ChannelUris[0]).Port;

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
        }

        private void keyisdown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Left) {
                pacmans[playerNumber].Image = Properties.Resources.Left;
                currentMovement = Movement.LEFT;
            }
            if (e.KeyCode == Keys.Right) {
                pacmans[playerNumber].Image = Properties.Resources.Right;
                currentMovement = Movement.RIGHT;
            }
            if (e.KeyCode == Keys.Up) {
                pacmans[playerNumber].Image = Properties.Resources.Up;
                currentMovement = Movement.UP;
            }
            if (e.KeyCode == Keys.Down) {
                pacmans[playerNumber].Image = Properties.Resources.down;
                currentMovement = Movement.DOWN;
            }
            if (e.KeyCode == Keys.Enter) {
                    tbMsg.Enabled = true; tbMsg.Focus();
               }

            obj.RegisterMovement(playerNumber, currentMovement);
        }

        private void keyisup(object sender, KeyEventArgs e)
        {
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

            obj.UnRegisterMovement(playerNumber, currentMovement);
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
            gameEnemies[enemyNumber].Top = enemy.GetY() - enemy.GetSizeY();
            gameEnemies[enemyNumber].Left = enemy.GetX() - enemy.GetSizeX();
        }

        public void doMovement(IPlayer movement, int playerNumber)
        {
            label1.Text = "Score: " + score;
            if (!gameRunning) return;

            pacmans[playerNumber].Top = movement.GetY();
            pacmans[playerNumber].Left = movement.GetX();

            if(movement.isMovementChanged())
                defineMovementImage(movement.GetMovement(), playerNumber);

            //if (playerNumber != this.playerNumber) return;
            ////move ghosts
            //redGhost.Left += ghost1;
            //yellowGhost.Left += ghost2;

            //// if the red ghost hits the picture box 4 then wereverse the speed
            //if (redGhost.Bounds.IntersectsWith(pictureBox1.Bounds))
            //    ghost1 = -ghost1;
            //// if the red ghost hits the picture box 3 we reverse the speed
            //else if (redGhost.Bounds.IntersectsWith(pictureBox2.Bounds))
            //    ghost1 = -ghost1;
            //// if the yellow ghost hits the picture box 1 then wereverse the speed
            //if (yellowGhost.Bounds.IntersectsWith(pictureBox3.Bounds))
            //    ghost2 = -ghost2;
            //// if the yellow chost hits the picture box 2 then wereverse the speed
            //else if (yellowGhost.Bounds.IntersectsWith(pictureBox4.Bounds))
            //    ghost2 = -ghost2;

            ////moving ghosts and bumping with the walls end
            ////for loop to check walls, ghosts and points
            //foreach (Control x in this.Controls)
            //{
            //    // checking if the player hits the wall or the ghost, then game is over
            //    if (x is PictureBox && x.Tag == "wall" || x.Tag == "ghost")
            //    {
            //        if (((PictureBox)x).Bounds.IntersectsWith(pacmans[playerNumber].Bounds))
            //        {
            //            pacmans[playerNumber].Left = 0;
            //            pacmans[playerNumber].Top = 25;
            //            label2.Text = "GAME OVER";
            //            label2.Visible = true;
            //            timer1.Stop();
            //        }
            //    }
            //    if (x is PictureBox && x.Tag == "coin")
            //    {
            //        if (((PictureBox)x).Bounds.IntersectsWith(pacmans[playerNumber].Bounds))
            //        {
            //            this.Controls.Remove(x);
            //            score++;
            //            //TODO check if all coins where "eaten"
            //            if (score == total_coins)
            //            {
            //                //pacmans[playerNumber].Left = 0;
            //                //pacmans[playerNumber].Top = 25;
            //                label2.Text = "GAME WON!";
            //                label2.Visible = true;
            //                timer1.Stop();
            //            }
            //        }
            //    }
            //}
            //pinkGhost.Left += ghost3x;
            //pinkGhost.Top += ghost3y;

            //if (pinkGhost.Left < boardLeft ||
            //    pinkGhost.Left > boardRight ||
            //    (pinkGhost.Bounds.IntersectsWith(pictureBox1.Bounds)) ||
            //    (pinkGhost.Bounds.IntersectsWith(pictureBox2.Bounds)) ||
            //    (pinkGhost.Bounds.IntersectsWith(pictureBox3.Bounds)) ||
            //    (pinkGhost.Bounds.IntersectsWith(pictureBox4.Bounds)))
            //{
            //    ghost3x = -ghost3x;
            //}
            //if (pinkGhost.Top < boardTop || pinkGhost.Top + pinkGhost.Height > boardBottom - 2)
            //{
            //    ghost3y = -ghost3y;
            //}
        }

        private void tbMsg_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                tbMsg.Enabled = false; this.Focus();
                tbMsg.Text = string.Empty;
            }
        }

        public void GameEvent(string Message, string auxMessage)
        {
            switch(Message)
            {
                case "NEWPLAYER":
                    SetTextBox("Player "+auxMessage+" joined the game.");
                    break;
                default:
                    return;
            }
        }

        public void StartGame(int playerNumber, IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovableGameObjects)
        {
            this.playerNumber = playerNumber;
            gameRunning = true;
            addNewPlayers(players);
            addNewEnemies(enemies);
            addUnmovableObjects(unmovableGameObjects);
            SetTextBox("Session full, game is starting!");
        }

        private void addUnmovableObjects(IUnmovable[] unmovableGameObjects)
        {
            unmovableObjects = new List<PictureBox>();
            for (int i = 0; i < unmovableGameObjects.Length; i++)
            {
                PictureBox picture = new PictureBox
                {
                    Name = "unmovableObject" + i,
                    Size = new Size(unmovableGameObjects[i].GetSizeX(), unmovableGameObjects[i].GetSizeY()),
                    Location = new Point(unmovableGameObjects[i].GetX(), unmovableGameObjects[i].GetY()),
                    BackColor = unmovableGameObjects[i].getColor(),
                    Visible = true,
                    SizeMode = PictureBoxSizeMode.Normal
                };
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
                gameEnemies.Add(picture);
                this.Controls.Add(picture);
            }
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

    }

    delegate void DelGameEvent(string mensagem, string auxMessage);
    delegate void StartGameEvent(int playerNumber, IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovables);
    delegate void PlayerMovement(IPlayer movement, int playerNumber);
    delegate void EnemyMovement(IEnemy movement, int playerNumber);

    public class ClientServices : MarshalByRefObject, IClient
    {
        public static FormClient form;

        public ClientServices()
        {
        }

        public void UpdateGame(IPlayer[] movements, IEnemy[] enemies)
        {
            for (int i = 0; i < movements.Length; i++)
            {
                form.Invoke(new PlayerMovement(form.doMovement), movements[i], i);
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                form.Invoke(new EnemyMovement(form.doEnemyMovement), enemies[i], i);
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
    }
}
