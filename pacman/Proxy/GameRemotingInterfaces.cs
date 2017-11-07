using System;

namespace Proxy
{
    [Serializable]
    public class PlayerGameObject
    {
        public int SIZE_X = 33;
        public int SIZE_Y = 31;

        private int boardRight = 320;
        private int boardBottom = 320;
        private int boardLeft = 0;
        private int boardTop = 40;
        private int speed = 5;

        public bool goup;
        public bool godown;
        public bool goleft;
        public bool goright;
        public bool movementChanged;

        public int x;
        public int y;

        public Movement direction;

        public PlayerGameObject(int playerNumber)
        {
            x = 8;
            y = 40*(playerNumber+1);
        }

        public void updatePosition()
        {
            Movement newDirection = Movement.UNDEFINED;
            if (goleft)
            {
                if (x > (boardLeft))
                    x -= speed;

                newDirection = Movement.LEFT;
            }
            if (goright)
            {
                if (x < (boardRight))
                    x += speed;

                newDirection = Movement.RIGHT;
            }
            if (goup)
            {
                if (y > (boardTop))
                    y -= speed;

                newDirection = Movement.UP;
            }
            if (godown)
            {
                if (y < (boardBottom))
                    y += speed;

                newDirection = Movement.DOWN;
            }

            if (direction == newDirection || newDirection == Movement.UNDEFINED)
            {
                movementChanged = false;
                return;
            }

            movementChanged = true;
            direction = newDirection;
        }

    }
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

        void UnRegisterMovement(int playerNumber, Movement movement);
    }

    //Client interface
    public interface IClient
    {
        //GameEvent method (Start Game, New Player, End Game, etc...)
        void GameEvent(string message, string auxMessage);
        void StartGame(int playerNumber, PlayerGameObject[] players);

        //TODO
        //Create methods for Outputs (list of "pacmans" probably)
        void DoMovements(PlayerGameObject[] movements);
    }
}
