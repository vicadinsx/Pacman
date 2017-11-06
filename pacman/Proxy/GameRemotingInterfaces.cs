using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    //Server interface
    public interface IServer
    {
        //Regist a client, with his port to be able to communicate
        void RegisterClient(string NewClientPort);

        //TODO
        //Create methods for Outputs (list of "pacmans" probably)
    }

    //Client interface
    public interface IClient
    {
        //GameEvent method (Start Game, New Player, End Game, etc...)
        void GameEvent(string message, string auxMessage);

        //TODO
        //Create methods for Input receiving
    }
}
