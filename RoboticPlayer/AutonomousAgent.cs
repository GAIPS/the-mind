using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thalamus;
using TheMindThalamusMessages;
using GazeOFMessages;
using EmoteCommonMessages;

namespace RoboticPlayer
{
    public enum GameState
    {
        Connection,
        Syncing,
        Game,
        Mistake,
        NextLevel,
        GameFinished,
        NoMoreCards,
        Waiting,
        StopMainLoop
    }

    public interface IAutonomousAgentPublisher : IThalamusPublisher, ITabletsGM, IGazeStateActions { }

    class AutonomousAgent : ThalamusClient, IGMTablets, IGazeOpenFacePerceptions
    {
        public class TheMindPublisher : IAutonomousAgentPublisher
        {
            dynamic publisher;
            public TheMindPublisher(dynamic publisher)
            {
                this.publisher = publisher;
            }

            public void ConnectToGM(int playerID, string name)
            {
                this.publisher.ConnectToGM(playerID, name);
            }

            public void ContinueAfterMistake(int playerID)
            {
                this.publisher.ContinueAfterMistake(playerID);
            }

            public void PlayCard(int playerID, int card)
            {
                this.publisher.PlayCard(playerID, card);
            }

            public void ReadyForNextLevel(int playerID)
            {
                this.publisher.ReadyForNextLevel(playerID);
            }

            public void RefocusSignal(int playerID)
            {
                this.publisher.RefocusSignal(playerID);
            }

            public void GazeAtScreen(double x, double y)
            {
                publisher.GazeAtScreen(x, y);
            }

            public void GazeAtTarget(string targetName)
            {
                publisher.GazeAtTarget(targetName);
            }

            public void GlanceAtScreen(double x, double y)
            {
                publisher.GlanceAtScreen(x, y);
            }

            public void GlanceAtTarget(string targetName)
            {
                publisher.GlanceAtTarget(targetName);
            }
        }

        public TheMindPublisher TMPublisher;
        protected ReactiveGazeController gazeController;
        protected int ID;
        protected GameState _gameState;
        protected List<GameState> eventsList;
        protected static Mutex mut = new Mutex();
        protected Random randomNums;
        protected int MaxLevel;
        protected int TopOfThePile;
        protected float Pace;
        protected List<int> cards;
        protected List<int> cardsLeft;
        protected Stopwatch PlayStopWatch;
        protected Stopwatch lastCardStopWatch;
        protected Stopwatch sessionStartStopWatch;
        protected int nextTimeToPlay;
        private string GazeType;

        public AutonomousAgent(string clientName, string character, int playerID, string gazeType)
            : base(clientName, character)
        {

            SetPublisher<IAutonomousAgentPublisher>();
            TMPublisher = new TheMindPublisher(base.Publisher);
            GazeType = gazeType;
            if (gazeType == "f")
            {
            }
            else if (gazeType == "r")
            {
                gazeController = new ReactiveGazeController(this);
            }
            else if (gazeType == "p")
            {
                gazeController = new ProactiveGazeController(this);
            }
            ID = playerID;
            TopOfThePile = 0;
            Pace = 1000;
            _gameState = GameState.Waiting;
            eventsList = new List<GameState>();
            randomNums = new Random();
            PlayStopWatch = new Stopwatch();
            lastCardStopWatch = new Stopwatch();
            sessionStartStopWatch = new Stopwatch();
            sessionStartStopWatch.Start();
            nextTimeToPlay = -1;
            Thread mainLoopThread = new Thread(MainLoop);
            mainLoopThread.Start();
        }

        private void MainLoop()
        {
            while(_gameState != GameState.StopMainLoop)
            {
                mut.WaitOne();
                if (_gameState == GameState.Waiting && eventsList.Count > 0)
                {
                    _gameState = eventsList[0];
                    eventsList.RemoveAt(0);
                }
                mut.ReleaseMutex();

                if (_gameState == GameState.NextLevel)
                {
                    int randomWait = randomNums.Next(2000, 5000);
                    Thread.Sleep(randomWait);
                    TMPublisher.ReadyForNextLevel(ID);
                    _gameState = GameState.Waiting;
                }
                if (_gameState == GameState.Syncing)
                {
                    int randomWait = randomNums.Next(2000, 5000);
                    Thread.Sleep(randomWait);
                    TMPublisher.RefocusSignal(ID);
                    _gameState = GameState.Waiting;
                }
                if (_gameState == GameState.Mistake)
                {
                    int randomWait = randomNums.Next(2000, 5000);
                    Thread.Sleep(randomWait);
                    TMPublisher.ContinueAfterMistake(ID);
                    nextTimeToPlay = -1;
                    _gameState = GameState.Waiting;
                }
                if (_gameState == GameState.Game)
                {
                    mut.WaitOne();
                    if (nextTimeToPlay == -1)
                    {
                        if (cards.Count > 0)
                        {
                            if (ID == 2 && cardsLeft[0] == 0 && cardsLeft[1] == 0)
                            {
                                PlayStopWatch.Restart();
                                nextTimeToPlay = 1500;
                            }
                            else
                            {
                                PlayStopWatch.Restart();
                                nextTimeToPlay = EstimateTimeToPlay();
                                //Console.WriteLine(">>>>> NextTimeToPlay in " + (lowestCard - TopOfThePile) + "s : " + lowestCard + " - " + TopOfThePile + " / " + cardsLeft[0] + " / " + cardsLeft[1] + " / " + cardsLeft[2]);
                                Console.WriteLine(">>>>> NextTimeToPlay in " + nextTimeToPlay);
                            }
                        }
                        else
                        {
                            _gameState = GameState.Waiting;
                            Console.WriteLine("---- No more cards!!!!!");
                        }
                    }
                    else if (PlayStopWatch.IsRunning && PlayStopWatch.ElapsedMilliseconds >= nextTimeToPlay)
                    {
                        PlayStopWatch.Stop();
                        TMPublisher.PlayCard(ID, cards[0]);
                        cards.RemoveAt(0);
                        nextTimeToPlay = -1;
                    }
                    mut.ReleaseMutex();
                }
            }
        }

        public virtual int EstimateTimeToPlay()
        {
            return (cards[0] - TopOfThePile) * 1000;
        }

        public void ConnectToGM()
        {
            TMPublisher.ConnectToGM(ID, "Agent-" + GazeType);
        }

        public void StopMainLoop()
        {
            _gameState = GameState.StopMainLoop;
        }

        public override void Dispose()
        {
            _gameState = GameState.StopMainLoop;
            gazeController.Dispose();
            base.Dispose();
        }

        public void AllConnected(int maxLevel, int p0Id, string p0Name, int p1Id, string p1Name, int p2Id, string p2Name)
        {
            MaxLevel = maxLevel;
            mut.WaitOne();
            eventsList.Add(GameState.NextLevel);
            mut.ReleaseMutex();
            sessionStartStopWatch.Restart();
        }

        public void StartLevel(int level, int teamLives, int[] p0Hand, int[] p1Hand, int[] p2Hand)
        {
            TopOfThePile = 0;
            Pace = 1000;
            cards = new List<int>();
            cardsLeft = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                cardsLeft.Add(level);
            }

            if (ID == 0)
            {
                foreach (int card in p0Hand)
                {
                    cards.Add(card);
                }
            }
            else if (ID == 1)
            {
                foreach (int card in p1Hand)
                {
                    cards.Add(card);
                }
            }
            else if (ID == 2)
            {
                foreach (int card in p2Hand)
                {
                    cards.Add(card);
                }
            }
            mut.WaitOne();
            eventsList.Add(GameState.Syncing);
            mut.ReleaseMutex();
        }

        public void FinishLevel(int level, int teamLives)
        {
            if (level < MaxLevel)
            {
                mut.WaitOne();
                eventsList.Add(GameState.NextLevel);
                mut.ReleaseMutex();
            }
        }

        public void AllRefocused()
        {
            if (_gameState != GameState.NoMoreCards)
            {
                mut.WaitOne();
                eventsList.Add(GameState.Game);
                lastCardStopWatch.Restart();
                mut.ReleaseMutex();
            }
        }

        public void RefocusRequest(int playerID)
        {
            if (playerID == -1)
            {
                mut.WaitOne();
                eventsList.Add(GameState.Game);
                mut.ReleaseMutex();
            }
            else if (cards.Count > 0)
            {
                _gameState = GameState.Syncing;
            }
            else
            {
                _gameState = GameState.Waiting;
            }
        }

        public void CardPlayed(int playerID, int card)
        {
            mut.WaitOne();
            long timeDelta = lastCardStopWatch.ElapsedMilliseconds;
            int cardsDelta = card - TopOfThePile;
            Pace = timeDelta / cardsDelta;
            Console.WriteLine("New PACE: " + Pace);
            lastCardStopWatch.Restart();
            TopOfThePile = card;
            nextTimeToPlay = -1;
            cardsLeft[playerID]--;
            PlayStopWatch.Stop();
            mut.ReleaseMutex();
        }

        public void Mistake(int playerID, int card, int[] p0WrongCards, int[] p1WrongCards, int[] p2WrongCards)
        {
            if (p0WrongCards == null)
            {
                p0WrongCards = new int[] { };
            }
            if (p1WrongCards == null)
            {
                p1WrongCards = new int[] { };
            }
            if (p2WrongCards == null)
            {
                p2WrongCards = new int[] { };
            }
            TopOfThePile = card;
            cardsLeft[playerID]--;
            bool shouldAckMistake = false;
            if (playerID != ID)
            {
                if (ID == 0)
                {
                    if (p0WrongCards.Length > 0)
                    {
                        foreach (int wrongCard in p0WrongCards)
                        {
                            cards.Remove(wrongCard);
                            shouldAckMistake = true;
                        }
                    }
                    cardsLeft[1] -= p1WrongCards.Length;
                    cardsLeft[2] -= p2WrongCards.Length;
                }
                else if (ID == 1)
                {
                    if (p1WrongCards.Length > 0)
                    {
                        foreach (int wrongCard in p1WrongCards)
                        {
                            cards.Remove(wrongCard);
                            shouldAckMistake = true;
                        }
                    }
                    cardsLeft[0] -= p0WrongCards.Length;
                    cardsLeft[2] -= p2WrongCards.Length;
                }
                else if (ID == 2)
                {
                    if (p2WrongCards.Length > 0)
                    {
                        foreach (int wrongCard in p2WrongCards)
                        {
                            cards.Remove(wrongCard);
                            shouldAckMistake = true;
                        }
                    }
                    cardsLeft[0] -= p0WrongCards.Length;
                    cardsLeft[1] -= p1WrongCards.Length;
                }
            }
            else
            {
                cardsLeft[0] -= p0WrongCards.Length;
                cardsLeft[1] -= p1WrongCards.Length;
                cardsLeft[2] -= p2WrongCards.Length;
            }

            if (cards.Count > 0 || shouldAckMistake)
            {
                _gameState = GameState.Mistake;
            }
        }

        public void GameOver(int level)
        {
            //throw new NotImplementedException();
        }

        public void GameCompleted()
        {
            //throw new NotImplementedException();
        }

        public void GazeOpenFace(int faceId, double angleX, double angleY, string target, double timeMiliseconds)
        {
            if (faceId != ID && gazeController.SessionStarted)
            {
                if (gazeController.Player0.ID == faceId)
                {
                    gazeController.Player0.GazeEvent(target, sessionStartStopWatch.ElapsedMilliseconds);
                }
                else if (gazeController.Player1.ID == faceId)
                {
                    gazeController.Player1.GazeEvent(target, sessionStartStopWatch.ElapsedMilliseconds);
                }

            }
        }

        public void TargetCalibrationStarted(int faceId, string target)
        {
            //DO SOMETHING
        }

        public void TargetCalibrationFinished(int faceId, string target)
        {
            //DO SOMETHING
        }

        public void CalibrationPhaseFinished(int faceId)
        {
            if (faceId == gazeController.Player0.ID)
            {
                gazeController.Player0.SessionStarted = true;
                gazeController.SessionStarted = true;
            }
            else if (faceId == gazeController.Player1.ID)
            {
                gazeController.Player1.SessionStarted = true;
            }

            if (gazeController.Player0.SessionStarted && gazeController.Player1.SessionStarted)
            {
                gazeController.SessionStarted = true;
            }
        }
    }
}
