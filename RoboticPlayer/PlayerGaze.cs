using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoboticPlayer
{
    class PlayerGaze
    {
        public int ID;
        public string PlayerGazeAtRobot;
        public string OtherPlayerName;
        public string Name;
        public GazeBehavior CurrentGazeBehaviour;
        private long lastEventTime;
        public long GAZE_ROBOT_AVG_DUR;
        public long GAZE_MAINSCREEN_AVG_DUR;
        public long GAZE_OTHER_PLAYER_AVG_DUR;
        public long GAZE_ROBOT_PERIOD;
        public long GAZE_MAINSCREEN_PERIOD;
        public long GAZE_OTHER_PLAYER_PERIOD;
        public long PERIOD_TIME_WINDOW = 10000; //10 seconds
        private List<GazeBehavior> gazeBehaviors;
        private List<GazeEvent> gazeEvents;
        public Thread GazeEventsDispatcher;
        public static Mutex mut = new Mutex();
        public bool SessionStarted;
        private List<string> buffer;
        private IAutonomousAgentPublisher publisher;

        public PlayerGaze(int id, IAutonomousAgentPublisher pub)
        {
            publisher = pub;
            ID = id;
            PlayerGazeAtRobot = "player2";
            Name = "player" + id;
            if (id == 0)
            {
                OtherPlayerName = "player1";
            }
            else if (id == 1)
            {
                OtherPlayerName = "player0";
            }
            CurrentGazeBehaviour = null;
            SessionStarted = false;
            buffer = new List<string>();
            gazeBehaviors = new List<GazeBehavior>();
            gazeEvents = new List<GazeEvent>();
            GazeEventsDispatcher = new Thread(DispacthGazeEvents);
            GazeEventsDispatcher.Start();
        }

        public bool IsGazingAtRobot()
        {
            return CurrentGazeBehaviour.Target == PlayerGazeAtRobot;
        }


        public void GazeEvent(string target, long timeMiliseconds)
        {
            if (CurrentGazeBehaviour == null || CurrentGazeBehaviour.Target != target)
            {
                if (buffer.Count > 0 && buffer[0] != target)
                {
                    buffer = new List<string>();
                }
                buffer.Add(target);
            }

            if (buffer.Count == 3)
            {
                buffer = new List<string>();
                GazeEvent ge = new GazeEvent(target, timeMiliseconds);

                mut.WaitOne();
                gazeEvents.Add(ge);
                mut.ReleaseMutex();
            }
            lastEventTime = timeMiliseconds;
        }

        public void UpdateRhythms()
        {
            if (SessionStarted && gazeBehaviors.Count > 0)
            {
                long timeThreshold = lastEventTime - PERIOD_TIME_WINDOW;
                int numGazeAtRobot = 0;
                long durGazeAtRobot = 0;
                int numGazeAtMainscreen = 0;
                long durGazeAtMainscreen = 0;
                int numGazeAtOtherPlayer = 0;
                long durGazeAtOtherPlayer = 0;
                if (CurrentGazeBehaviour.Target == PlayerGazeAtRobot)
                {
                    numGazeAtRobot++;
                    durGazeAtRobot += CurrentGazeBehaviour.Duration;
                }
                if (CurrentGazeBehaviour.Target == "mainscreen")
                {
                    numGazeAtMainscreen++;
                    durGazeAtMainscreen += CurrentGazeBehaviour.Duration;
                }
                if (CurrentGazeBehaviour.Target == OtherPlayerName)
                {
                    numGazeAtOtherPlayer++;
                    durGazeAtOtherPlayer += CurrentGazeBehaviour.Duration;
                }
                for (int i = gazeBehaviors.Count - 1; i >= 0 && gazeBehaviors[i].EndingTime > timeThreshold; i--)
                {
                    if (gazeBehaviors[i].Target == PlayerGazeAtRobot)
                    {
                        numGazeAtRobot++;
                        durGazeAtRobot += gazeBehaviors[i].Duration;
                    }
                    else if (gazeBehaviors[i].Target == "mainscreen")
                    {
                        numGazeAtMainscreen++;
                        durGazeAtMainscreen += gazeBehaviors[i].Duration;
                    }
                    else if (gazeBehaviors[i].Target == OtherPlayerName)
                    {
                        numGazeAtOtherPlayer++;
                        durGazeAtOtherPlayer += CurrentGazeBehaviour.Duration;
                    }
                }

                if (numGazeAtRobot != 0)
                {
                    durGazeAtRobot /= numGazeAtRobot;
                    GAZE_ROBOT_AVG_DUR = durGazeAtRobot;
                    GAZE_ROBOT_PERIOD = PERIOD_TIME_WINDOW / numGazeAtRobot;
                }
                else
                {
                    GAZE_ROBOT_AVG_DUR = durGazeAtRobot;
                    GAZE_ROBOT_PERIOD = PERIOD_TIME_WINDOW;
                }

                if (numGazeAtMainscreen != 0)
                {
                    durGazeAtMainscreen /= numGazeAtMainscreen;
                    GAZE_MAINSCREEN_AVG_DUR = durGazeAtMainscreen;
                    GAZE_MAINSCREEN_PERIOD = PERIOD_TIME_WINDOW / numGazeAtMainscreen;
                }
                else
                {
                    GAZE_MAINSCREEN_AVG_DUR = durGazeAtMainscreen;
                    GAZE_MAINSCREEN_PERIOD = PERIOD_TIME_WINDOW;
                }

                if (numGazeAtOtherPlayer != 0)
                {
                    durGazeAtOtherPlayer /= numGazeAtOtherPlayer;
                    GAZE_OTHER_PLAYER_AVG_DUR = durGazeAtOtherPlayer;
                    GAZE_OTHER_PLAYER_PERIOD = PERIOD_TIME_WINDOW / numGazeAtOtherPlayer;
                }
                else
                {
                    GAZE_OTHER_PLAYER_AVG_DUR = durGazeAtOtherPlayer;
                    GAZE_OTHER_PLAYER_PERIOD = PERIOD_TIME_WINDOW;
                }
                //Console.WriteLine("++++++ " + GAZE_ROBOT_AVG_DUR + " " + GAZE_ROBOT_PERIOD + " " + GAZE_SCREEN_AVG_DUR + " " + GAZE_SCREEN_PERIOD);
            }
        }

        public (string, int) EstimateNextGazeTarget()
        {
            string nextTarget = "";
            int expectedPeriod = -1;

            if (CurrentGazeBehaviour.Target != PlayerGazeAtRobot && GAZE_ROBOT_PERIOD < PERIOD_TIME_WINDOW && CurrentGazeBehaviour.Target != OtherPlayerName && GAZE_OTHER_PLAYER_PERIOD < PERIOD_TIME_WINDOW)
            {
                if (GAZE_ROBOT_PERIOD < GAZE_OTHER_PLAYER_PERIOD)
                {
                    nextTarget = Name;
                    expectedPeriod = (int) GAZE_ROBOT_PERIOD;
                }
                else
                {
                    nextTarget = OtherPlayerName;
                    expectedPeriod = (int) GAZE_OTHER_PLAYER_PERIOD;
                }
            }
            else if (GAZE_ROBOT_PERIOD < PERIOD_TIME_WINDOW && CurrentGazeBehaviour.Target != PlayerGazeAtRobot)
            {
                nextTarget = Name;
                expectedPeriod = (int) GAZE_ROBOT_PERIOD;
            }
            else if (GAZE_OTHER_PLAYER_PERIOD < PERIOD_TIME_WINDOW && CurrentGazeBehaviour.Target != OtherPlayerName)
            {
                nextTarget = OtherPlayerName;
                expectedPeriod = (int) GAZE_OTHER_PLAYER_PERIOD;
            }

            return (nextTarget, expectedPeriod);
        }


        internal void Dispose()
        {
            Console.WriteLine("------------------------- gazeBehaviors.size - " + gazeBehaviors.Count);
            GazeEventsDispatcher.Join();
        }

        private void DispacthGazeEvents()
        {
            while (true)
            {
                GazeEvent ge = null;
                mut.WaitOne();
                if (gazeEvents.Count > 0)
                {
                    ge = gazeEvents[0];
                    gazeEvents.RemoveAt(0);
                }
                mut.ReleaseMutex();

                if (ge != null)
                {

                    //first time
                    if (CurrentGazeBehaviour == null)
                    {
                        CurrentGazeBehaviour = new GazeBehavior(ID, ge.Target, ge.Timestamp);
                        publisher.GazeBehaviourStarted(Name, ge.Target, (int)ge.Timestamp);
                    }
                    else if (ge.Target != CurrentGazeBehaviour.Target)
                    {
                        CurrentGazeBehaviour.UpdateEndtingTime(ge.Timestamp);
                        gazeBehaviors.Add(CurrentGazeBehaviour);
                        publisher.GazeBehaviourFinished(Name, CurrentGazeBehaviour.Target, (int)ge.Timestamp);
                        CurrentGazeBehaviour = new GazeBehavior(ID, ge.Target, ge.Timestamp);
                        publisher.GazeBehaviourStarted(Name, ge.Target, (int)ge.Timestamp);
                        if (ge.Target != "elsewhere")
                        {
                            ReactiveGazeController.LastMovingPlayer = this;
                        }
                    }
                    else if (ge.Target == CurrentGazeBehaviour.Target)
                    {
                        CurrentGazeBehaviour.UpdateEndtingTime(ge.Timestamp);
                    }
                }
            }
        }
    }
}

