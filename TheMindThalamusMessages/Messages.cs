using Thalamus;

namespace TheMindThalamusMessages
{
    public interface IGMTablets : IPerception
    {
        void AllConnected(int p0Id, string p0Name, int p1Id, string p1Name, int p2Id, string p2Name);
        void FinishRound(int[] envAllocations);
    }


    public interface ITabletsGM : IAction
    {
        void ConnectToGM(int id, string name);
        void SendBudgetAllocation(int tabletID, int envAllocation);
        void Disconnect(int id);
    }
}
