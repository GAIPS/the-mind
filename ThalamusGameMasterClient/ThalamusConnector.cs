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

        public void AllConnected(int p0Id, string p0Name, int p1Id, string p1Name, int p2Id, string p2Name)
        {
            _publisher.AllConnected(p0Id, p0Name, p1Id, p1Name, p2Id, p2Name);
        }

        public void FinishRound(int[] envAllocations)
        {
            _publisher.FinishRound(envAllocations);
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

    public void ConnectToGM(int id, string name)
    {
        UnityConnector.RPCProxy.ConnectToGM(id, name);
    }

    public void SendBudgetAllocation(int tabletID, int envAllocation)
    {
        UnityConnector.RPCProxy.SendBudgetAllocation(tabletID, envAllocation);
    }

    public void Disconnect(int id)
    {
        UnityConnector.RPCProxy.Disconnect(id);
    }
}
