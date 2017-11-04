using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    [Serializable]
    public class ChatManagement : MarshalByRefObject
    {
        public event ChatEvent MessageArrived;

        public void PublishMessage(string Message)
        {
            SafeInvokeMessageArrived(Message);
        }

        public void SafeInvokeMessageArrived(string Message)
        {
            ChatEvent eventList = null;
            Delegate[] delegates = MessageArrived.GetInvocationList();

            foreach (Delegate del in delegates)
            {
                try
                {
                    eventList = (ChatEvent)del;
                    eventList.Invoke(Message);
                }
                catch (Exception)
                {
                    Console.WriteLine("Client has disconnected!!");
                    MessageArrived -= eventList;
                }
            }
        }
    }
}
