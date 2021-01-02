using System;

class Program
{
    static void Main(string[] args)
    {
        ThalamusConnector thalamusCS;
        string clientName = "";
        string character = "";
        string address = "";
        int port = 7000;

        if (args.Length != 4)
        {
            Console.WriteLine("Usage: " + Environment.GetCommandLineArgs()[0] + " <ClientName> <CharacterName> <IPadress> <PortNumber>");
            return;
        }
        else
        {
            clientName = args[0];
            character = args[1];
            address = args[2];
            port = Int16.Parse(args[3]);

            thalamusCS = new ThalamusConnector(clientName, character);
            UnityConnector unityCS = new UnityConnector(thalamusCS, address, port);
            thalamusCS.UnityConnector = unityCS;

            Console.ReadLine();
            thalamusCS.Dispose();
        }
        
    }
}
