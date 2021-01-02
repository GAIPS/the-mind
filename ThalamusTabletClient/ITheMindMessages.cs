using Thalamus;
using CookComputing.XmlRpc;
using TheMindThalamusMessages;

public interface IThalamusTabletPublisher : IThalamusPublisher, ITabletsGM
{
}


public interface IUnityTabletSubscriber : ITabletsGM
{
    void Dispose();

    [XmlRpcMethod]
    new void ConnectToGM(int id, string name);
    [XmlRpcMethod]
    new void SendBudgetAllocation(int tabletID, int envAllocation);
    [XmlRpcMethod]
    new void Disconnect(int id);

}

public interface IUnityTabletPublisher : IGMTablets, IXmlRpcProxy
{
    [XmlRpcMethod]
    new void AllConnected(int p0Id, string p0Name, int p1Id, string p1Name, int p2Id, string p2Name);
    [XmlRpcMethod]
    new void FinishRound(int[] envAllocations);
}
