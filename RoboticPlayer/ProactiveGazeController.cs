using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboticPlayer
{
    class ProactiveGazeController : ReactiveGazeController
    {
        public ProactiveGazeController(AutonomousAgent thalamusClient) : base(thalamusClient)
        {
        }

        public override void NextPractiveBehaviour(long timeStamp)
        {
            Player0.UpdateRhythms();
            Player1.UpdateRhythms();

            (string p0NextTarget, int p0NextShift) = Player0.EstimateNextGazeTarget();
            (string p1NextTarget, int p1NextShift) = Player1.EstimateNextGazeTarget();


            if (p0NextTarget != "" && p0NextTarget != currentTarget && p1NextTarget != "" && p1NextTarget != currentTarget)
            {
                if (p0NextShift < p1NextShift)
                {
                    PROACTIVE_NEXT_TARGET = p0NextTarget;
                    PROACTIVE_NEXT_SHIFT = timeStamp + p0NextShift;
                }
                else
                {
                    PROACTIVE_NEXT_TARGET = p1NextTarget;
                    PROACTIVE_NEXT_SHIFT = timeStamp + p1NextShift;
                }
            }
            else if (p0NextTarget != "" && p0NextTarget != currentTarget)
            {
                PROACTIVE_NEXT_TARGET = p0NextTarget;
                PROACTIVE_NEXT_SHIFT = timeStamp + p0NextShift;
            }
            else if (p1NextTarget != "" && p1NextTarget != currentTarget)
            {
                PROACTIVE_NEXT_TARGET = p1NextTarget;
                PROACTIVE_NEXT_SHIFT = timeStamp + p1NextShift;
            }
            else
            {
                PROACTIVE_NEXT_TARGET = "";
                PROACTIVE_NEXT_SHIFT = -1;
            }
            //Console.WriteLine("NEXTPROACTOVE: " + PROACTIVE_NEXT_TARGET + " " + PROACTIVE_NEXT_SHIFT);
        }
    }
}
