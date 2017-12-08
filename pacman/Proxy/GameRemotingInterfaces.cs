using System;
using System.Collections.Generic;
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
        void RegisterClient(string NewClientPort, string name = "");

        void RegisterMovement(int playerNumber, Movement movement);

        void UnRegisterMovement(int playerNumber, Movement movement);

        void PlayerKilled(int playerNumber);

        void GatheredCoin(int playerNumber, int coinNumber);

        void SetReplicationData(int serverId, int[] serversId, IServer[] servers);

        void message(string type, int senderId);

        bool IsAlive();

        void UpdateData(PlayerGameObject[] _playerObjects, UnmovableGameObject[] _unmovableObjects, EnemyGameObject[] _enemyObjects);

        void crash();

        int getId();
        void Freeze();
        void UnFreeze();
        void DefineVariables(int maxPlayers, int roundTime);
        string Status();
        string LocalState(int round);
    }

    //Client interface
    public interface IClient
    {
        //GameEvent method (Start Game, New Player, End Game, etc...)
        void GameEvent(string message, string auxMessage);
        void StartGame(string filePath, int playerNumber, IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovableObjects);

        //TODO
        //Create methods for Outputs (list of "pacmans" probably)
        void UpdateGame(IPlayer[] movements, IEnemy[] enemies, IUnmovable[] unmovableObjects);
        void UpdatePlayers(List<IClient> client);
        void Message(string Message, string Sender, string auxMessage);

        void StartViewingGame(IPlayer[] players, IEnemy[] enemies, IUnmovable[] unmovableObjects);
        void UpdateServer(IServer server);

    }
}
