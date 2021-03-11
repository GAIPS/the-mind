using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboticPlayer
{
    class ReactiveGazeController
    {
        protected AutonomousAgent aa;
        private int ID;
        public PlayerGaze Player0;
        public PlayerGaze Player1;
        public static PlayerGaze LastMovingPlayer;
        private Thread mainLoop;
        protected Stopwatch currentGazeDuration;
        //private long previousGazeShitTime;
        protected string currentTarget;
        public bool SessionStarted;
        protected string PROACTIVE_NEXT_TARGET;
        protected long PROACTIVE_NEXT_SHIFT;
        private int GAZE_MIN_DURATION = 1000;//miliseconds
        public bool JOINT_ATTENTION;

        public ReactiveGazeController(AutonomousAgent thalamusClient)
        {
            aa = thalamusClient;
            ID = 2;
            Player0 = new PlayerGaze(0, aa.TMPublisher);
            Player1 = new PlayerGaze(1, aa.TMPublisher);
            LastMovingPlayer = Player1;
            currentTarget = "mainscreen";
            currentGazeDuration = new Stopwatch();
            currentGazeDuration.Start();
            mainLoop = new Thread(Update);
            mainLoop.Start();
        }

        public void Dispose()
        {
            //base.Dispose();
            Player0.Dispose();
            Player1.Dispose();
            mainLoop.Join();
        }

        public virtual void NextPractiveBehaviour(long timeStamp)
        {
            PROACTIVE_NEXT_TARGET = "";
            PROACTIVE_NEXT_SHIFT = -1;
        }


        public virtual void Update()
        {
            while (true)
            {
                if (SessionStarted)
                {
                    //if (currentGazeDuration.ElapsedMilliseconds >= GAZE_MIN_DURATION && Player0.SessionStarted && Player0.CurrentGazeBehaviour != null && Player1.SessionStarted && Player1.CurrentGazeBehaviour != null)
                    if (currentGazeDuration.ElapsedMilliseconds >= GAZE_MIN_DURATION && Player0.SessionStarted && Player1.SessionStarted && Player1.CurrentGazeBehaviour != null)
                    {
                        //reactive
                        if (LastMovingPlayer.IsGazingAtRobot() && currentTarget != LastMovingPlayer.Name)
                        {
                            Console.WriteLine("------------------------ gaze back " + LastMovingPlayer.Name);
                            aa.TMPublisher.GazeBehaviourFinished("player2", currentTarget, (int)aa.SessionStartStopWatch.ElapsedMilliseconds);
                            currentTarget = LastMovingPlayer.Name;
                            aa.TMPublisher.GazeAtTarget(LastMovingPlayer.Name);
                            currentGazeDuration.Restart();
                            aa.TMPublisher.GazeBehaviourStarted("player2", currentTarget, (int)aa.SessionStartStopWatch.ElapsedMilliseconds);
                            NextPractiveBehaviour(currentGazeDuration.ElapsedMilliseconds);
                        }
                        else if (JOINT_ATTENTION && !LastMovingPlayer.IsGazingAtRobot() && LastMovingPlayer.CurrentGazeBehaviour.Target != "elsewhere" && currentTarget != LastMovingPlayer.CurrentGazeBehaviour.Target)
                        {
                            Console.WriteLine("------------------------ gaze at where " + LastMovingPlayer.Name + " is gazing " + LastMovingPlayer.CurrentGazeBehaviour.Target);
                            aa.TMPublisher.GazeBehaviourFinished("player2", currentTarget, (int)aa.SessionStartStopWatch.ElapsedMilliseconds);
                            currentTarget = LastMovingPlayer.CurrentGazeBehaviour.Target;
                            aa.TMPublisher.GazeAtTarget(LastMovingPlayer.CurrentGazeBehaviour.Target);
                            currentGazeDuration.Restart();
                            aa.TMPublisher.GazeBehaviourStarted("player2", currentTarget, (int)aa.SessionStartStopWatch.ElapsedMilliseconds);
                            NextPractiveBehaviour(currentGazeDuration.ElapsedMilliseconds);
                        }
                        else if (!JOINT_ATTENTION && !LastMovingPlayer.IsGazingAtRobot() && LastMovingPlayer.CurrentGazeBehaviour.Target != "elsewhere" && currentTarget != "mainscreen")
                        {
                            Console.WriteLine("------------------------ mutual gaze break");
                            aa.TMPublisher.GazeBehaviourFinished("player2", currentTarget, (int)aa.SessionStartStopWatch.ElapsedMilliseconds);
                            currentTarget = "mainscreen";
                            aa.TMPublisher.GazeAtTarget("mainscreen");
                            currentGazeDuration.Restart();
                            aa.TMPublisher.GazeBehaviourStarted("player2", currentTarget, (int)aa.SessionStartStopWatch.ElapsedMilliseconds);
                            NextPractiveBehaviour(currentGazeDuration.ElapsedMilliseconds);
                        }


                        //proactive
                        if (PROACTIVE_NEXT_SHIFT != -1 && currentGazeDuration.ElapsedMilliseconds >= PROACTIVE_NEXT_SHIFT)
                        {
                            Console.WriteLine(">>>>> PROACTIVE <<<<< gaze at " + PROACTIVE_NEXT_TARGET + " prev-dur " + currentGazeDuration.ElapsedMilliseconds);
                            aa.TMPublisher.GazeBehaviourFinished("player2", currentTarget, (int)aa.SessionStartStopWatch.ElapsedMilliseconds);
                            currentTarget = PROACTIVE_NEXT_TARGET;
                            aa.TMPublisher.GazeAtTarget(PROACTIVE_NEXT_TARGET);
                            currentGazeDuration.Restart();
                            aa.TMPublisher.GazeBehaviourStarted("player2", currentTarget, (int)aa.SessionStartStopWatch.ElapsedMilliseconds);
                            NextPractiveBehaviour(currentGazeDuration.ElapsedMilliseconds);
                        }
                    }
                }
            }
        }
    }
}
