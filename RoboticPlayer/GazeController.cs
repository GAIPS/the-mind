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
        private Stopwatch stopWatch;
        private int nextGazeShiftEstimate;
        //private long previousGazeShitTime;
        private string currentTarget;
        public bool SessionStarted;
        private int PROACTIVE_THRESHOLD = 3000;//miliseconds
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
            nextGazeShiftEstimate = 0;
            stopWatch = new Stopwatch();
            stopWatch.Start();
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

        private void Update()
        {
            while (true)
            {
                if (SessionStarted)
                {
                    if (stopWatch.ElapsedMilliseconds >= GAZE_MIN_DURATION && Player0.SessionStarted && Player0.CurrentGazeBehaviour != null && Player1.SessionStarted && Player1.CurrentGazeBehaviour != null)
                    {
                        //reactive
                        if (LastMovingPlayer.IsGazingAtRobot() && currentTarget != LastMovingPlayer.Name)
                        {
                            currentTarget = LastMovingPlayer.Name;
                            aa.TMPublisher.GazeAtTarget(LastMovingPlayer.Name);
                            stopWatch.Restart();
                            Console.WriteLine("------------ gaze back " + LastMovingPlayer.Name);
                        }
                        else if (!LastMovingPlayer.IsGazingAtRobot() && LastMovingPlayer.CurrentGazeBehaviour.Target != "elsewhere" && currentTarget != LastMovingPlayer.CurrentGazeBehaviour.Target)
                        {
                            currentTarget = LastMovingPlayer.CurrentGazeBehaviour.Target;
                            aa.TMPublisher.GazeAtTarget(LastMovingPlayer.CurrentGazeBehaviour.Target);
                            stopWatch.Restart();
                            Console.WriteLine("------------ gaze at where " + LastMovingPlayer.Name + " is gazing " + LastMovingPlayer.CurrentGazeBehaviour.Target);
                        }

                        //proactive
                        if (stopWatch.ElapsedMilliseconds > PROACTIVE_THRESHOLD)
                        {
                            string newTarget = "";
                            if (currentTarget == "mainscreen")
                            {
                                bool shouldLookAtP0 = Player0.GazeRobotPeriod < Player0.PERIOD_TIME_WINDOW && stopWatch.ElapsedMilliseconds > Player0.GazeRobotPeriod;
                                bool shouldLookAtP1 = Player1.GazeRobotPeriod < Player1.PERIOD_TIME_WINDOW && stopWatch.ElapsedMilliseconds > Player1.GazeRobotPeriod;
                                if (shouldLookAtP0 && shouldLookAtP1)
                                {
                                    int randomize = random.Next(2);
                                    if (randomize == 0)
                                    {
                                        newTarget = Player0.Name;
                                    }
                                    else
                                    {
                                        newTarget = Player1.Name;
                                    }
                                }
                                else if (shouldLookAtP0)
                                {
                                    newTarget = Player0.Name;
                                }
                                else if (shouldLookAtP1)
                                {
                                    newTarget = Player1.Name;
                                }
                            }
                            else if (currentTarget == Player0.Name)
                            {
                                if (Player1.GazeRobotPeriod < Player1.PERIOD_TIME_WINDOW && stopWatch.ElapsedMilliseconds > Player1.GazeRobotPeriod)
                                {
                                    newTarget = Player1.Name;
                                }
                            }
                            else if (currentTarget == Player1.Name)
                            {
                                if (Player0.GazeRobotPeriod < Player0.PERIOD_TIME_WINDOW && stopWatch.ElapsedMilliseconds > Player0.GazeRobotPeriod)
                                {
                                    newTarget = Player0.Name;
                                }
                            }

                            if (newTarget != "")
                            {
                                currentTarget = newTarget;
                                aa.TMPublisher.GazeAtTarget(newTarget);
                                stopWatch.Restart();
                                Console.WriteLine("------PROACTIVE------ gaze at " + newTarget);
                            }
                        }
                    }
                }
            }
        }
    }
}
