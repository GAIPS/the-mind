using System;
using Thalamus;
using TheMindThalamusMessages;



public class ThalamusConnector : ThalamusClient, IGMTablets
{
    public IThalamusTabletPublisher TypifiedPublisher {  get;  private set; }
    public UnityConnector UnityConnector { private get; set; }



    public class ThalamusPublisher : IThalamusTabletPublisher
    {
        private readonly dynamic _publisher;
        public ThalamusPublisher(dynamic publisher)
        {
            _publisher = publisher;
        }

        public void Dispose()
        {
            _publisher.Dispose();
        }

        public void ConnectToGM(int playerID, string name)
        {
            _publisher.ConnectToGM(playerID, name);
        }

        public void PlayCard(int playerID, int card)
        {
            _publisher.PlayCard(playerID, card);
        }

        public void RefocusSignal(int playerID)
        {
            _publisher.RefocusSignal(playerID);
        }

        public void ReadyForNextLevel(int playerID)
        { 
            _publisher.ReadyForNextLevel(playerID);
        }

        public void ContinueAfterMistake(int playerID)
        {
            _publisher.ContinueAfterMistake(playerID);
        }
    }

    public ThalamusConnector(string clientName, string character)
        : base(clientName, character)
    {
        SetPublisher<IThalamusTabletPublisher>();
        TypifiedPublisher = new ThalamusPublisher(Publisher);
    }

    public override void Dispose()
    {
        UnityConnector.Dispose();
        base.Dispose();
    }

    public void AllConnected(int p0Id, string p0Name, int p1Id, string p1Name, int p2Id, string p2Name)
    {
        UnityConnector.RPCProxy.AllConnected(p0Id, p0Name, p1Id, p1Name, p2Id, p2Name);
    }

    public void StartLevel(int level, int teamLives, int[] p0Hand, int[] p1Hand, int[] p2Hand)
    {
        UnityConnector.RPCProxy.StartLevel(level, teamLives, p0Hand, p1Hand, p2Hand);
    }

    public void FinishLevel(int level, int teamLives)
    {
        UnityConnector.RPCProxy.FinishLevel(level, teamLives);
    }

    public void AllRefocused()
    {
        UnityConnector.RPCProxy.AllRefocused();
    }

    public void RefocusRequest(int playerID)
    {
        UnityConnector.RPCProxy.RefocusRequest(playerID);
    }

    public void CardPlayed(int playerID, int card)
    {
        UnityConnector.RPCProxy.CardPlayed(playerID, card);
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
        UnityConnector.RPCProxy.Mistake(playerID, card, p0WrongCards, p1WrongCards, p2WrongCards);
    }

    public void GameOver(int level)
    {
        UnityConnector.RPCProxy.GameOver(level);
    }

    public void GameCompleted()
    {
        UnityConnector.RPCProxy.GameCompleted();
    }
}
