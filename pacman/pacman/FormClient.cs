using System;
using System.Windows.Forms;
using Proxy;
using System.Runtime.Remoting.Channels;
using System.Collections;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels.Tcp;
using System.Drawing;
using System.Runtime.Remoting;

namespace pacman {

    public delegate void SetBoxText(string Message);
    public partial class FormClient : Form {

        IServer obj;
        BinaryClientFormatterSinkProvider clientProv;
        BinaryServerFormatterSinkProvider serverProv;
        string lol = string.Empty;
        private int playerNumber;
        bool gameRunning;

        // direction player is moving in. Only one will be true
        bool goup;
        bool godown;
        bool goleft;
        bool goright;

        int boardRight = 320;
        int boardBottom = 320;
        int boardLeft = 0;
        int boardTop = 40;
        //player speed
        int speed = 5;

        int score = 0; int total_coins = 61;

        //ghost speed for the one direction ghosts
        int ghost1 = 5;
        int ghost2 = 5;
        
        //x and y directions for the bi-direccional pink ghost
        int ghost3x = 5;
        int ghost3y = 5;            

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
                goleft = true;
                pacman.Image = Properties.Resources.Left;
            }
            if (e.KeyCode == Keys.Right) {
                goright = true;
                pacman.Image = Properties.Resources.Right;
            }
            if (e.KeyCode == Keys.Up) {
                goup = true;
                pacman.Image = Properties.Resources.Up;
            }
            if (e.KeyCode == Keys.Down) {
                godown = true;
                pacman.Image = Properties.Resources.down;
            }
            if (e.KeyCode == Keys.Enter) {
                    tbMsg.Enabled = true; tbMsg.Focus();
               }
        }

        private void keyisup(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Left) {
                goleft = false;
            }
            if (e.KeyCode == Keys.Right) {
                goright = false;
            }
            if (e.KeyCode == Keys.Up) {
                goup = false;
            }
            if (e.KeyCode == Keys.Down) {
                godown = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e) {
            label1.Text = "Score: " + score;

            if (!gameRunning) return;
            //move player
            if (goleft) {
                if (pacman.Left > (boardLeft))
                    pacman.Left -= speed;
            }
            if (goright) {
                if (pacman.Left < (boardRight))
                pacman.Left += speed;
            }
            if (goup) {
                if (pacman.Top > (boardTop))
                    pacman.Top -= speed;
            }
            if (godown) {
                if (pacman.Top < (boardBottom))
                    pacman.Top += speed;
            }
            //move ghosts
            redGhost.Left += ghost1;
            yellowGhost.Left += ghost2;

            // if the red ghost hits the picture box 4 then wereverse the speed
            if (redGhost.Bounds.IntersectsWith(pictureBox1.Bounds))
                ghost1 = -ghost1;
            // if the red ghost hits the picture box 3 we reverse the speed
            else if (redGhost.Bounds.IntersectsWith(pictureBox2.Bounds))
                ghost1 = -ghost1;
            // if the yellow ghost hits the picture box 1 then wereverse the speed
            if (yellowGhost.Bounds.IntersectsWith(pictureBox3.Bounds))
                ghost2 = -ghost2;
            // if the yellow chost hits the picture box 2 then wereverse the speed
            else if (yellowGhost.Bounds.IntersectsWith(pictureBox4.Bounds))
                ghost2 = -ghost2;
            //moving ghosts and bumping with the walls end
            //for loop to check walls, ghosts and points
            foreach (Control x in this.Controls) {
                // checking if the player hits the wall or the ghost, then game is over
                if (x is PictureBox && x.Tag == "wall" || x.Tag == "ghost") {
                    if (((PictureBox)x).Bounds.IntersectsWith(pacman.Bounds)) {
                        pacman.Left = 0;
                        pacman.Top = 25;
                        label2.Text = "GAME OVER";
                        label2.Visible = true;
                        timer1.Stop();
                    }
                }
                if (x is PictureBox && x.Tag == "coin") {
                    if (((PictureBox)x).Bounds.IntersectsWith(pacman.Bounds)) {
                        this.Controls.Remove(x);
                        score++;
                        //TODO check if all coins where "eaten"
                        if (score == total_coins) {
                            //pacman.Left = 0;
                            //pacman.Top = 25;
                            label2.Text = "GAME WON!";
                            label2.Visible = true;
                            timer1.Stop();
                            }
                    }
                }
            }
                pinkGhost.Left += ghost3x;
                pinkGhost.Top += ghost3y;

                if (pinkGhost.Left < boardLeft ||
                    pinkGhost.Left > boardRight ||
                    (pinkGhost.Bounds.IntersectsWith(pictureBox1.Bounds)) ||
                    (pinkGhost.Bounds.IntersectsWith(pictureBox2.Bounds)) ||
                    (pinkGhost.Bounds.IntersectsWith(pictureBox3.Bounds)) ||
                    (pinkGhost.Bounds.IntersectsWith(pictureBox4.Bounds))) {
                    ghost3x = -ghost3x;
                }
                if (pinkGhost.Top < boardTop || pinkGhost.Top + pinkGhost.Height > boardBottom - 2) {
                    ghost3y = -ghost3y;
                }
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
                case "START":
                    gameRunning = true;
                    SetTextBox("Session full, game is starting!");
                    addNewPlayer(int.Parse(auxMessage));
                    break;
                case "NEWPLAYER":
                    gameRunning = true;
                    SetTextBox("Player "+auxMessage+" joined the game.");
                    break;
                default:
                    return;
            }
        }

        void eventProxy_PlayerInput(int playerNumber, string Message)
        {
            SetTextBox(Message);
        }

        private void addNewPlayer(int numberOfPlayers)
        {

            for (int i = 1; i < numberOfPlayers; i++)
            {
                PictureBox picture = new PictureBox
                {
                    Name = "pacman"+i,
                    Size = new Size(33, 31),
                    Location = new Point(8, 80),
                    Image = Properties.Resources.Left,
                    Visible = true,
                    SizeMode = PictureBoxSizeMode.StretchImage
                };
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

    delegate void DelAddMsg(string mensagem, string auxMessage);

    public class ClientServices : MarshalByRefObject, IClient
    {
        public static FormClient form;

        public ClientServices()
        {
        }

        public void GameEvent(string message, string auxMessage)
        {
            form.Invoke(new DelAddMsg(form.GameEvent), message, auxMessage);
        }
    }
}
