using System;
using System.Collections.Generic;
using System.Text;

namespace Proxy
{
    [Serializable]
    public delegate void ChatEvent(string Message);
    public delegate void PlayerInput(int player, string input);
    public delegate void GameEvent(string eventMessage);

    [Serializable]
    public class CommonEvents : MarshalByRefObject
    {
        public event ChatEvent MessageArrived;
        public event PlayerInput ClientInputs;
        public event GameEvent GameEvents;

        public void LocallyHandleMessageArrived(string Message)
        {
            MessageArrived?.Invoke(Message);
        }

        public void LocallyHandlePlayerInput(int player, string input)
        {
            ClientInputs?.Invoke(player, input);
        }

        public void LocallyHandleGameEvent(string eventMessage)
        {
            GameEvents?.Invoke(eventMessage);
        }
    }
}
