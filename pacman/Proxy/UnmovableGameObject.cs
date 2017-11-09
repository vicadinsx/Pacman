using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    [Serializable]
    public class UnmovableGameObject : IUnmovable
    {
        public Color color;
        public UnmovableType type;

        public int x;
        public int y;

        public int sizeY;
        public int sizeX;

        public bool isVisible;

        private Rectangle rectangle;

        public UnmovableGameObject(int _x, int _y, int _sizeX, int _sizeY, bool _isVisible, UnmovableType _type, Color _color)
        {
            x = _x;
            y = _y;
            sizeY = _sizeY;
            sizeX = _sizeX;
            isVisible = _isVisible;
            type = _type;
            color = _color;

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

        bool IUnmovable.isVisible()
        {
            return isVisible;
        }

        public UnmovableType GetEnemyType()
        {
            return type;
        }

        public Color getColor()
        {
            return color;
        }
    }
}
