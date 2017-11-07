using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    [Serializable]
    public class UnmovableGameObject
    {
        public Color color;
        public UnmovableType type;

        public int x;
        public int y;

        public int sizeY;
        public int sizeX;

        public bool isVisible;

        private Rectangle rectangle;

        UnmovableGameObject(int _x, int _y, int _sizeY, int _sizeX, bool _isVisible, UnmovableType _type, Color _color)
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
    }
}
