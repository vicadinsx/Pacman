using System;
using System.Collections.Generic;
using System.Text;

namespace Proxy
{
    [Serializable]
    public delegate void ChatEvent(string Message);

    [Serializable]
    public class CommonEvents : MarshalByRefObject
    {
        public event ChatEvent MessageArrived;

        public void LocallyHandleMessageArrived(string Message)
        {
            MessageArrived?.Invoke(Message);
        }

    }
}
