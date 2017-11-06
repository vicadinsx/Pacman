using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxy
{
    public interface IServer
    {
        void RegisterClient(string NewClientPort);
    }

    public interface IClient
    {
        void GameEvent(string message, string auxMessage);
    }
}
