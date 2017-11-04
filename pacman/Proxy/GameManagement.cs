using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proxy
{
    [Serializable]
    public class GameManagement : MarshalByRefObject
    {
        public event PlayerInput InputArrived;
        public event GameEvent GameEvents;

        private const int MAX_NUMBER = 2;

        //List with all the clients (can be changed)
        List<Client> clients = new List<Client>();

        public int RegisterClient()
        {
            if (clients.Count < MAX_NUMBER)
            {
                Client client = new Client();
                clients.Add(client);

                if (clients.Count == MAX_NUMBER)
                {
                    Thread startGame = new Thread(new ThreadStart(PublishGameStarting));
                    startGame.Start();
                }
                return clients.Count;
            }
            else return -1;
        }

        public void PublishGameStarting()
        {
            GameEvent eventList = null;
            Delegate[] delegates = GameEvents.GetInvocationList();

            foreach (Delegate del in delegates)
            {
                try
                {
                    eventList = (GameEvent)del;
                    eventList.Invoke("START");
                }
                catch (Exception)
                {
                    Console.WriteLine("Client has disconnected!!");
                    GameEvents -= eventList;
                }
            }
        }

        public void PublishInput(int player, string input)
        {
            //Not really like this, this will be used to check the input of the player
            SafeInvokeMessageArrived(input);
        }

        public void SafeInvokeMessageArrived(string Message)
        {
            PlayerInput eventList = null;
            Delegate[] delegates = InputArrived.GetInvocationList();

            foreach (Delegate del in delegates)
            {
                try
                {
                    eventList = (PlayerInput)del;
                    eventList.Invoke(1,Message);
                }
                catch (Exception)
                {
                    Console.WriteLine("Client has disconnected!!");
                    InputArrived -= eventList;
                }
            }
        }
    }
}
