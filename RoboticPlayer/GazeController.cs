using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboticPlayer
{
    class GazeController
    {
        private AutonomousAgent aa;
        private int ID;
        public PlayerGaze Player0;
        public PlayerGaze Player1;
        public static PlayerGaze LastMovingPlayer;
        private Thread mainLoop;
        private Stopwatch currentGazeDuration;
        //private long previousGazeShitTime;
        private string currentTarget;
        public bool SessionStarted;
        private int PROACTIVE_THRESHOLD = 1500;//miliseconds
        private string PROACTIVE_NEXT_TARGET;
        private long PROACTIVE_NEXT_SHIFT;
        private int GAZE_MIN_DURATION = 1000;//miliseconds
        private Random random;

        public GazeController(AutonomousAgent thalamusClient)
        {
            aa = thalamusClient;
            ID = 2;
            Player0 = new PlayerGaze(0);
            Player1 = new PlayerGaze(1);
            LastMovingPlayer = Player0;
            currentTarget = "mainscreen";
            currentGazeDuration = new Stopwatch();
            currentGazeDuration.Start();
            mainLoop = new Thread(Update);
            mainLoop.Start();
            random = new Random();
        }

        public void Dispose()
        {
            //base.Dispose();
            Player0.Dispose();
            Player1.Dispose();
            mainLoop.Join();
        }

        private void NextPractiveBehaviour(long timeStamp)
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


        private void Update()
        {
            while (true)
            {
                if (SessionStarted)
                {
                    if (currentGazeDuration.ElapsedMilliseconds >= GAZE_MIN_DURATION && Player0.SessionStarted && Player0.CurrentGazeBehaviour != null && Player1.SessionStarted && Player1.CurrentGazeBehaviour != null)
                    {
                        //reactive
                        if (LastMovingPlayer.IsGazingAtRobot() && currentTarget != LastMovingPlayer.Name)
                        {
                            Console.WriteLine("------------------------ gaze back " + LastMovingPlayer.Name);
                            currentTarget = LastMovingPlayer.Name;
                            aa.TMPublisher.GazeAtTarget(LastMovingPlayer.Name);
                            currentGazeDuration.Restart();
                            NextPractiveBehaviour(currentGazeDuration.ElapsedMilliseconds);
                        }
                        else if (!LastMovingPlayer.IsGazingAtRobot() && LastMovingPlayer.CurrentGazeBehaviour.Target != "elsewhere" && currentTarget != LastMovingPlayer.CurrentGazeBehaviour.Target)
                        {
                            Console.WriteLine("------------------------ gaze at where " + LastMovingPlayer.Name + " is gazing " + LastMovingPlayer.CurrentGazeBehaviour.Target);
                            currentTarget = LastMovingPlayer.CurrentGazeBehaviour.Target;
                            aa.TMPublisher.GazeAtTarget(LastMovingPlayer.CurrentGazeBehaviour.Target);
                            currentGazeDuration.Restart();
                            NextPractiveBehaviour(currentGazeDuration.ElapsedMilliseconds);
                        }

                        //proactive
                        if (PROACTIVE_NEXT_SHIFT != -1 && currentGazeDuration.ElapsedMilliseconds >= PROACTIVE_NEXT_SHIFT)
                        {
                            Console.WriteLine(">>>>> PROACTIVE <<<<< gaze at " + PROACTIVE_NEXT_TARGET + " prev-dur " + currentGazeDuration.ElapsedMilliseconds);
                            currentTarget = PROACTIVE_NEXT_TARGET;
                            aa.TMPublisher.GazeAtTarget(PROACTIVE_NEXT_TARGET);
                            currentGazeDuration.Restart();
                            NextPractiveBehaviour(currentGazeDuration.ElapsedMilliseconds);
                        }
                    }
                }
            }
        }
    }
}
