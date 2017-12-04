using System;
using System.Drawing;

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

    public enum UnmovableType
    {
        COIN,
        WALL
    };

    public enum EnemyType
    {
        RED,
        YELLOW,
        PINK
    };

    public interface IPlayer
    {
        int GetY();
        int GetX();
        int GetSizeY();
        int GetSizeX();
        Movement GetMovement();
        bool isMovementChanged();
        bool isPlayerDead();

        int getScore();
    }

    public interface IEnemy
    {
        int GetY();
        int GetX();
        int GetSizeY();
        int GetSizeX();
        EnemyType GetEnemyType();
    }

    public interface IUnmovable
    {
        int GetY();
        int GetX();
        int GetSizeY();
        int GetSizeX();
        bool isVisible();
        Color getColor();
        UnmovableType GetEnemyType();
    }

    //Server interface
    public interface IServer
    {
        //Regist a client, with his port to be able to communicate
        void RegisterClient(string NewClientPort);

        //TODO
        //Create methods for Input receiving
        void RegisterMovement(int playerNumber, Movement movement);

        void UnRegisterMovement(int playerNumber, Movement movement);

        void PlayerKilled(int playerNumber);

        void GatheredCoin(int playerNumber, int coinNumber);
    }

    //Client interface
    public interface IClient
    {
        //GameEvent method (Start Game, New Player, End Game, etc...)
        void GameEvent(string message, string auxMessage);
        void StartGame(int playerNumber, IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovableObjects);

        //TODO
        //Create methods for Outputs (list of "pacmans" probably)
        void UpdateGame(IPlayer[] movements, IEnemy[] enemies, IUnmovable[] unmovableObjects);
    }
}
