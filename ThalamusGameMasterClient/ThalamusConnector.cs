using System;
using Thalamus;
using TheMindThalamusMessages;



public class ThalamusConnector : ThalamusClient, ITabletsGM
{
    public IThalamusGameMasterPublisher TypifiedPublisher {  get;  private set; }
    public UnityConnector UnityConnector { private get; set; }



    public class ThalamusPublisher : IThalamusGameMasterPublisher
    {
        private readonly dynamic _publisher;
        public ThalamusPublisher(dynamic publisher)
        {
            _publisher = publisher;
        }

        public void AllConnected(int maxLevel, int p0Id, string p0Name, int p1Id, string p1Name, int p2Id, string p2Name)
        {
            _publisher.AllConnected(maxLevel, p0Id, p0Name, p1Id, p1Name, p2Id, p2Name);
        }

        public void AllRefocused()
        {
            _publisher.AllRefocused();
        }

        public void RefocusRequest(int playerID)
        {
            _publisher.RefocusRequest(playerID);
        }

        public void CardPlayed(int playerID, int card)
        {
            _publisher.CardPlayed(playerID, card);
        }

        public void FinishLevel(int level, int teamLives)
        {
            _publisher.FinishLevel(level, teamLives);
        }

        public void GameCompleted()
        {
            _publisher.GameCompleted();
        }

        public void GameOver(int level)
        {
            _publisher.GameOver(level);
        }

        public void Mistake(int playerID, int card, int[] p0WrongCards, int[] p1WrongCards, int[] p2WrongCards)
        {
            _publisher.Mistake(playerID, card, p0WrongCards, p1WrongCards, p2WrongCards);
        }

        public void StartLevel(int level, int teamLives, int[] p0Hand, int[] p1Hand, int[] p2Hand)
        {
            _publisher.StartLevel(level, teamLives, p0Hand, p1Hand, p2Hand);
        }
    }

    public ThalamusConnector(string clientName, string character)
        : base(clientName, character)
    {
        SetPublisher<IThalamusGameMasterPublisher>();
        TypifiedPublisher = new ThalamusPublisher(Publisher);
    }

    public override void Dispose()
    {
        UnityConnector.Dispose();
        base.Dispose();
    }

    public void ConnectToGM(int playerID, string name)
    {
        UnityConnector.RPCProxy.ConnectToGM(playerID, name);
    }

    public void PlayCard(int playerID, int card)
    {
        UnityConnector.RPCProxy.PlayCard(playerID, card);
    }

    public void RefocusSignal(int playerID)
    {
        UnityConnector.RPCProxy.RefocusSignal(playerID);
    }

    public void ReadyForNextLevel(int playerID)
    {
        UnityConnector.RPCProxy.ReadyForNextLevel(playerID);
    }

    public void ContinueAfterMistake(int playerID)
    {
        UnityConnector.RPCProxy.ContinueAfterMistake(playerID);
    }
}
