using System;
using System.Drawing;

namespace Proxy
{

    [Serializable]
    public class PlayerGameObject : IPlayer
    {
        public int SIZE_X = 33;
        public int SIZE_Y = 31;

        private int boardRight = 320;
        private int boardBottom = 320;
        private int boardLeft = 0;
        private int boardTop = 40;
        private int speed = 5;

        Rectangle rectangle;

        public bool goup;
        public bool godown;
        public bool goleft;
        public bool goright;
        public bool movementChanged;

        public int score;

        public int x;
        public int y;

        public bool isDead;

        public Movement direction;

        public PlayerGameObject(int playerNumber)
        {
            x = 8;
            y = 40 * (playerNumber + 1);
            score = 0;

            rectangle = new Rectangle(x, y, SIZE_X, SIZE_Y);
            isDead = false;
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
            rectangle.X = x;
            rectangle.Y = y;

            movementChanged = true;
            direction = newDirection;
        }

        
        public int GetY()
        {
            return y;
        }

        public int GetX()
        {
            return x;
        }

        public int GetSizeY()
        {
            return SIZE_Y;
        }

        public int GetSizeX()
        {
            return SIZE_X;
        }

        public Movement GetMovement()
        {
            return direction;
        }

        public bool isMovementChanged()
        {
            return movementChanged;
        }

        public Rectangle getRectangle()
        {
            return rectangle;
        }

        public bool isPlayerDead()
        {
            return isDead;
        }
    }
}
