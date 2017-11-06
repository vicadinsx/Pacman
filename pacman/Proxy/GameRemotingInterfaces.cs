using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    public enum Movement
    {
        UNDEFINED,
        UP,
        DOWN,
        LEFT,
        RIGHT
    };

    //Server interface
    public interface IServer
    {
        //Regist a client, with his port to be able to communicate
        void RegisterClient(string NewClientPort);

        //TODO
        //Create methods for Input receiving
        void RegisterMovement(int playerNumber, Movement movement);
    }

    //Client interface
    public interface IClient
    {
        //GameEvent method (Start Game, New Player, End Game, etc...)
        void GameEvent(string message, string auxMessage);
        void StartGame(int playerNumber, int numberOfPlayers);

        //TODO
        //Create methods for Outputs (list of "pacmans" probably)
        void DoMovements(Movement[] movements);
    }
}
