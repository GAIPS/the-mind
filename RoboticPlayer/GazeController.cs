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
                            currentTarget = LastMovingPlayer.Name;
                            aa.TMPublisher.GazeAtTarget(LastMovingPlayer.Name);
                            currentGazeDuration.Restart();
                            Console.WriteLine("------------------------ gaze back " + LastMovingPlayer.Name);
                        }
                        else if (!LastMovingPlayer.IsGazingAtRobot() && LastMovingPlayer.CurrentGazeBehaviour.Target != "elsewhere" && currentTarget != LastMovingPlayer.CurrentGazeBehaviour.Target)
                        {
                            currentTarget = LastMovingPlayer.CurrentGazeBehaviour.Target;
                            aa.TMPublisher.GazeAtTarget(LastMovingPlayer.CurrentGazeBehaviour.Target);
                            currentGazeDuration.Restart();
                            Console.WriteLine("------------------------ gaze at where " + LastMovingPlayer.Name + " is gazing " + LastMovingPlayer.CurrentGazeBehaviour.Target);
                        }

                        //proactive
                        string newTarget = "";
                        string target1 = "";
                        string target2 = "";
                        bool possibleNewTarget1 = false;
                        bool possibleNewTarget2 = false;
                        if (currentTarget == "mainscreen")
                        {
                            double players_screen_avg_dur = (Player0.GAZE_SCREEN_AVG_DUR + Player1.GAZE_SCREEN_AVG_DUR) / 2;
                            if (currentGazeDuration.ElapsedMilliseconds > players_screen_avg_dur)
                            {
                                Player0.UpdateRhythms();
                                Player1.UpdateRhythms();
                                possibleNewTarget1 = Player0.GAZE_ROBOT_PERIOD <= Player0.PERIOD_TIME_WINDOW && currentGazeDuration.ElapsedMilliseconds >= Player0.GAZE_ROBOT_PERIOD;
                                possibleNewTarget2 = Player1.GAZE_ROBOT_PERIOD <= Player1.PERIOD_TIME_WINDOW && currentGazeDuration.ElapsedMilliseconds >= Player1.GAZE_ROBOT_PERIOD;
                                target1 = Player0.Name;
                                target2 = Player1.Name;
                            }
                        }
                        else if (currentTarget == Player0.Name && currentGazeDuration.ElapsedMilliseconds > Player0.GAZE_ROBOT_AVG_DUR)
                        {
                            Player0.UpdateRhythms();
                            Player1.UpdateRhythms();
                            possibleNewTarget1 = (Player0.GAZE_SCREEN_PERIOD <= Player0.PERIOD_TIME_WINDOW && Player1.GAZE_SCREEN_PERIOD <= Player1.PERIOD_TIME_WINDOW && currentGazeDuration.ElapsedMilliseconds >= (Player0.GAZE_SCREEN_PERIOD + Player1.GAZE_SCREEN_PERIOD) / 2) || (Player0.GAZE_SCREEN_PERIOD <= Player0.PERIOD_TIME_WINDOW && currentGazeDuration.ElapsedMilliseconds >= Player0.GAZE_SCREEN_PERIOD) || (Player1.GAZE_SCREEN_PERIOD <= Player1.PERIOD_TIME_WINDOW && currentGazeDuration.ElapsedMilliseconds >= Player1.GAZE_SCREEN_PERIOD);
                            possibleNewTarget2 = Player1.GAZE_ROBOT_PERIOD <= Player1.PERIOD_TIME_WINDOW && currentGazeDuration.ElapsedMilliseconds >= Player1.GAZE_ROBOT_PERIOD;
                            target1 = "mainscreen";
                            target2 = Player1.Name;
                        }
                        else if (currentTarget == Player1.Name && currentGazeDuration.ElapsedMilliseconds > Player1.GAZE_ROBOT_AVG_DUR)
                        {
                            Player0.UpdateRhythms();
                            Player1.UpdateRhythms();
                            possibleNewTarget1 = (Player0.GAZE_SCREEN_PERIOD <= Player0.PERIOD_TIME_WINDOW && Player1.GAZE_SCREEN_PERIOD <= Player1.PERIOD_TIME_WINDOW && currentGazeDuration.ElapsedMilliseconds >= (Player0.GAZE_SCREEN_PERIOD + Player1.GAZE_SCREEN_PERIOD) / 2) || (Player0.GAZE_SCREEN_PERIOD <= Player0.PERIOD_TIME_WINDOW && currentGazeDuration.ElapsedMilliseconds >= Player0.GAZE_SCREEN_PERIOD) || (Player1.GAZE_SCREEN_PERIOD <= Player1.PERIOD_TIME_WINDOW && currentGazeDuration.ElapsedMilliseconds >= Player1.GAZE_SCREEN_PERIOD);
                            possibleNewTarget2 = Player0.GAZE_ROBOT_PERIOD <= Player0.PERIOD_TIME_WINDOW && currentGazeDuration.ElapsedMilliseconds >= Player0.GAZE_ROBOT_PERIOD;
                            target1 = "mainscreen";
                            target2 = Player0.Name;
                        }

                        //label target
                        if (possibleNewTarget1 && possibleNewTarget2)
                        {
                            int randomize = random.Next(2);
                            if (randomize == 0)
                            {
                                newTarget = target1;
                            }
                            else
                            {
                                newTarget = target2;
                            }
                        }
                        else if (possibleNewTarget1)
                        {
                            newTarget = target1;
                        }
                        else if (possibleNewTarget2)
                        {
                            newTarget = target2;
                        }

                        if (newTarget != "")
                        {
                            currentTarget = newTarget;
                            aa.TMPublisher.GazeAtTarget(newTarget);
                            currentGazeDuration.Restart();
                            Console.WriteLine(">>>>> PROACTIVE <<<<< gaze at " + newTarget);
                        }
                    }
                }
            }
        }
    }
}
