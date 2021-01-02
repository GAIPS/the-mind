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

        public void ConnectToGM(int id, string name)
        {
            _publisher.ConnectToGM(id, name);
        }

        public void SendBudgetAllocation(int tabletID, int envAllocation)
        {
            _publisher.SendBudgetAllocation(tabletID, envAllocation);
        }

        public void Disconnect(int id)
        {
            _publisher.Disconnect(id);
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

    public void FinishRound(int[] envAllocations)
    {
        UnityConnector.RPCProxy.FinishRound(envAllocations);
    }
}
