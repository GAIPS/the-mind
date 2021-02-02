using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboticPlayer
{
    class PaceAdapter : AutonomousAgent
    {


        public PaceAdapter(string clientName, string character, int playerID, string gazeType)
            : base(clientName, character, playerID, gazeType)
        {

        }

        public override int EstimateTimeToPlay()
        {
            return (int) ((cards[0] - TopOfThePile) * Pace);
        }
    }
}
