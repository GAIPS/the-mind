using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboticPlayer
{
    class Program
    {
        static void Main(string[] args)
        {
            string clientName;
            string character;
            int playerID;
            string gazeType;

            if (args.Length != 4 && args[4] != "r" && args[4] != "m" && args[4] != "mj" && args[4] != "p")
            {
                Console.WriteLine("Usage: " + Environment.GetCommandLineArgs()[0] + " <ClientName> <CharacterName> <PlayerID> [frp]");
                return;
            }
            else
            {
                clientName = args[0];
                character = args[1];
                playerID = Int16.Parse(args[2]);
                gazeType = args[3];
                AutonomousAgent theMindPlayer = new AutonomousAgent(clientName, character, playerID, gazeType);
                //AutonomousAgent theMindPlayer = new PaceAdapter(clientName, character, playerID, gazeType);

                string command = Console.ReadLine();
                while (command != "exit")
                {
                    if (command == "c")
                    {
                        theMindPlayer.ConnectToGM();
                    }
                    command = Console.ReadLine();
                }
                theMindPlayer.StopMainLoop();

                theMindPlayer.Dispose();
            }
        }
    }
}
