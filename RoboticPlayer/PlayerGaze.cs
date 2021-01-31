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
        public string Name;
        public GazeBehavior CurrentGazeBehaviour;
        private double lastEventTime;
        public double GAZE_ROBOT_AVG_DUR;
        public double GAZE_SCREEN_AVG_DUR;
        public double GAZE_ROBOT_PERIOD;
        public double GAZE_SCREEN_PERIOD;
        public int PERIOD_TIME_WINDOW = 10; //5 seconds
        private List<GazeBehavior> gazeBehaviors;
        private List<GazeEvent> gazeEvents;
        public Thread UpdatesDispatcher;
        public Thread GazeEventsDispatcher;
        public static Mutex mut = new Mutex();
        public bool SessionStarted;
        private List<string> buffer;

        public PlayerGaze(int id)
        {
            ID = id;
            PlayerGazeAtRobot = "player2";
            Name = "player" + id;
            CurrentGazeBehaviour = null;
            SessionStarted = false;
            buffer = new List<string>();
            gazeBehaviors = new List<GazeBehavior>();
            gazeEvents = new List<GazeEvent>();
            GazeEventsDispatcher = new Thread(DispacthGazeEvents);
            GazeEventsDispatcher.Start();
            //UpdatesDispatcher = new Thread(Updates);
            //UpdatesDispatcher.Start();
        }

        public bool IsGazingAtRobot()
        {
            return CurrentGazeBehaviour.Target == PlayerGazeAtRobot;
        }


        public void GazeEvent(string target, double timeMiliseconds)
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

        /*private void Updates()
        {
            while (true)
            {
                if (gazeBehaviors.Count > 0)
                {
                    UpdateGazeShiftRate();
                }
                Thread.Sleep(1000);
            }
        }*/

        public void UpdateRhythms()
        {
            if (SessionStarted && gazeBehaviors.Count > 0)
            {
                double timeThreshold = lastEventTime - PERIOD_TIME_WINDOW;
                int numGazeAtRobot = 0;
                double durGazeAtRobot = 0;
                int numGazeAtMainscreen = 0;
                double durGazeAtMainscreen = 0;
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
                GAZE_ROBOT_AVG_DUR = durGazeAtRobot;

                if (numGazeAtMainscreen != 0)
                {
                    durGazeAtMainscreen /= numGazeAtMainscreen;
                    GAZE_SCREEN_AVG_DUR = durGazeAtMainscreen;
                    GAZE_SCREEN_PERIOD = PERIOD_TIME_WINDOW / numGazeAtMainscreen;
                }
                else
                {
                    GAZE_SCREEN_AVG_DUR = durGazeAtMainscreen;
                    GAZE_SCREEN_AVG_DUR = PERIOD_TIME_WINDOW;
                }
            }
        }

        internal void Dispose()
        {
            Console.WriteLine("------------------------- gazeBehaviors.size - " + gazeBehaviors.Count);
            GazeEventsDispatcher.Join();
            //UpdatesDispatcher.Join();
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
                    }
                    else if (ge.Target != CurrentGazeBehaviour.Target)
                    {
                        CurrentGazeBehaviour.UpdateEndtingTime(ge.Timestamp);
                        gazeBehaviors.Add(CurrentGazeBehaviour);
                        CurrentGazeBehaviour = new GazeBehavior(ID, ge.Target, ge.Timestamp);
                        if (ge.Target != "elsewhere")
                        {
                            GazeController.LastMovingPlayer = this;
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

