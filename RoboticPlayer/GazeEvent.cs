using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboticPlayer
{
    class GazeEvent
    {
        public string Target;
        public long Timestamp;

        public GazeEvent(string target, long timestamp)
        {
            Target = target;
            Timestamp = timestamp;
        }
    }
}
