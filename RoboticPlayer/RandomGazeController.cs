using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboticPlayer
{
    class RandomGazeController : ReactiveGazeController
    {
        private Random random;
        private long nextgazeShift;
        private string[] targets;
        private string PLAYER_A;
        private string PLAYER_B;
        private string SCREEN;
        private int AVG_DUR_SCREEN;
        private int AVG_DUR_PLAYER;

        public RandomGazeController(AutonomousAgent thalamusClient) : base(thalamusClient)
        {
            random = new Random();
            PLAYER_A = "player0";
            PLAYER_B = "player1";
            SCREEN = "mainscreen";
            AVG_DUR_SCREEN = 5000;
            AVG_DUR_PLAYER = 2000;
            targets = new string[] { PLAYER_A, PLAYER_B, SCREEN};
        }

        public override void Update()
        {
            while (true)
            {
                if (SessionStarted)
                {
                    if (currentGazeDuration.ElapsedMilliseconds >= nextgazeShift)
                    {
                        string target = targets[random.Next(0, 3)];
                        Console.WriteLine("------------------------ gaze RANDOMLY at SCREEN");
                        aa.TMPublisher.GazeBehaviourFinished("player2", currentTarget, (int) aa.SessionStartStopWatch.ElapsedMilliseconds);
                        currentTarget = target;
                        aa.TMPublisher.GazeAtTarget(currentTarget);
                        currentGazeDuration.Restart();
                        aa.TMPublisher.GazeBehaviourStarted("player2", currentTarget, (int) aa.SessionStartStopWatch.ElapsedMilliseconds);
                        NextPractiveBehaviour(currentGazeDuration.ElapsedMilliseconds);

                        if (target == SCREEN)
                        {
                            nextgazeShift = random.Next((int)(AVG_DUR_SCREEN * 0.75), (int)(AVG_DUR_SCREEN * 1.25));
                        }
                        else
                        {
                            nextgazeShift = random.Next((int)(AVG_DUR_PLAYER * 0.75), (int)(AVG_DUR_PLAYER * 1.25));
                        }
                    }
                }
            }
        }
    }
}
