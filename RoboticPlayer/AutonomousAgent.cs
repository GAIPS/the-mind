using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thalamus;
using TheMindThalamusMessages;

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

    public interface IAutonomousAgentPublisher : IThalamusPublisher, ITabletsGM { }

    class AutonomousAgent : ThalamusClient, IGMTablets
    {
        private class WindowShopPublisher : IAutonomousAgentPublisher
        {
            dynamic publisher;
            public WindowShopPublisher(dynamic publisher)
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
        }

        private WindowShopPublisher theMindPublisher;
        private int ID;
        private GameState _gameState;
        private List<GameState> eventsList;
        private static Mutex mut = new Mutex();
        private Random randomNums;
        private int MaxLevel;
        private int TopOfThePile;
        private List<int> cards;
        private List<int> cardsLeft;
        private Stopwatch stopWatch;
        private int nextTimeToPlay;

        public AutonomousAgent(string clientName, string character, int playerID)
            : base(clientName, character)
        {

            SetPublisher<IAutonomousAgentPublisher>();
            theMindPublisher = new WindowShopPublisher(Publisher);
            ID = playerID;
            TopOfThePile = 0;
            _gameState = GameState.Waiting;
            eventsList = new List<GameState>();
            randomNums = new Random();
            stopWatch = new Stopwatch();
            nextTimeToPlay = -1;
            Thread thread = new Thread(MainLoop);
            thread.Start();
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
                    theMindPublisher.ReadyForNextLevel(ID);
                    _gameState = GameState.Waiting;
                }
                if (_gameState == GameState.Syncing)
                {
                    int randomWait = randomNums.Next(2000, 5000);
                    Thread.Sleep(randomWait);
                    theMindPublisher.RefocusSignal(ID);
                    _gameState = GameState.Waiting;
                }
                if (_gameState == GameState.Mistake)
                {
                    int randomWait = randomNums.Next(2000, 5000);
                    Thread.Sleep(randomWait);
                    theMindPublisher.ContinueAfterMistake(ID);
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
                                stopWatch.Restart();
                                nextTimeToPlay = 1500;
                            }
                            else
                            {
                                stopWatch.Restart();
                                int lowestCard = cards[0];
                                nextTimeToPlay = (lowestCard - TopOfThePile) * 1000;
                                Console.WriteLine(">>>>> NextTimeToPlay in " + (lowestCard - TopOfThePile) + "s : " + lowestCard + " - " + TopOfThePile + " / " + cardsLeft[0] + " / " + cardsLeft[1] + " / " + cardsLeft[2]);
                            }
                        }
                        else
                        {
                            _gameState = GameState.Waiting;
                            Console.WriteLine("---- No more cards!!!!!");
                        }
                    }
                    else if (stopWatch.IsRunning && stopWatch.ElapsedMilliseconds >= nextTimeToPlay)
                    {
                        stopWatch.Stop();
                        theMindPublisher.PlayCard(ID, cards[0]);
                        cards.RemoveAt(0);
                        nextTimeToPlay = -1;
                    }
                    mut.ReleaseMutex();
                }
            }
        }

        public void ConnectToGM()
        {
            theMindPublisher.ConnectToGM(ID, "Agent" + ID);
        }

        public void StopMainLoop()
        {
            _gameState = GameState.StopMainLoop;
        }

        public void AllConnected(int maxLevel, int p0Id, string p0Name, int p1Id, string p1Name, int p2Id, string p2Name)
        {
            MaxLevel = maxLevel;
            mut.WaitOne();
            eventsList.Add(GameState.NextLevel);
            mut.ReleaseMutex();
        }

        public void StartLevel(int level, int teamLives, int[] p0Hand, int[] p1Hand, int[] p2Hand)
        {
            TopOfThePile = 0;
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
            TopOfThePile = card;
            nextTimeToPlay = -1;
            cardsLeft[playerID]--;
            stopWatch.Stop();
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
    }
}
