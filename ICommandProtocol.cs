using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FollowerV2
{
    interface ICommandProtocol
    {
        void Start();
        void Restart();
        void Stop();
        void Work(NetworkActivityObject obj);
    }
}
