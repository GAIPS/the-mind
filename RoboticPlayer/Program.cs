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

            if (args.Length != 3)
            {
                Console.WriteLine("Usage: " + Environment.GetCommandLineArgs()[0] + " <ClientName> <CharacterName> <PlayerID>");
                return;
            }
            else
            {
                clientName = args[0];
                character = args[1];
                playerID = Int16.Parse(args[2]);
                AutonomousAgent theMindPlayer = new PaceAdapter(clientName, character, playerID);
                
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
