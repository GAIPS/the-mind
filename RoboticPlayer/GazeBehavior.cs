using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboticPlayer
{
    class GazeBehavior
    {
        public int GazerID;
        public string Target;
        public long StartingTime;
        public long EndingTime;
        public long Duration;

        public GazeBehavior(int id, string target, long startingTime, long endingTime)
        {
            GazerID = id;
            Target = target;
            StartingTime = startingTime;
            EndingTime = endingTime;
            Duration = endingTime - startingTime;
        }

        public GazeBehavior(int id, string target, long startingTime)
        {
            GazerID = id;
            Target = target;
            StartingTime = startingTime;
            EndingTime = 0;
            Duration = 0;
        }

        public void UpdateEndtingTime(long endingTime)
        {
            EndingTime = endingTime;
            Duration = endingTime - StartingTime;
        }

    }
}
