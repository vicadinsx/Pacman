using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    [Serializable]
    public class EnemyGameObject : IEnemy
    {
        private int enemyXSpeed;
        private int enemyYSpeed;

        public int x;
        public int y;

        public int sizeX;
        public int sizeY;

        private Rectangle rectangle;

        EnemyType enemyType;

        public EnemyGameObject(int _enemyXSpeed, int _enemyYSpeed, int _x, int _y, int _sizeX, int _sizeY, EnemyType _enemyType)
        {
            enemyXSpeed = _enemyXSpeed;
            enemyYSpeed = _enemyYSpeed;
            x = _x;
            y = _y;
            sizeX = _sizeX;
            sizeY = _sizeY;
            enemyType = _enemyType;
            rectangle = new Rectangle(x, y, sizeX, sizeY);
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
            return sizeY;
        }

        public int GetSizeX()
        {
            return sizeX;
        }

        public EnemyType GetEnemyType()
        {
            return enemyType;
        }
    }
}
