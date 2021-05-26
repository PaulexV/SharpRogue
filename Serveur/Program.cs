using System;
using System.IO;
using System.Net;
using System.Threading;

namespace Serveur
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Server";

            IPAddress myIPAddress = IPAddress.Parse("192.168.1.79");
            int port = 3000;

            Server server = new Server(myIPAddress, port);

            //
            server.StartListening();
            Console.WriteLine("Server started.");

            Thread.Sleep(1000);
            Console.Clear();
            Console.WriteLine("Waiting for connection");

            //If someone wants to connect, accept
            server.AcceptClient();
            Console.WriteLine("Client accepted.");

            string MessageFromClient = "";

            try
            {
                server.ClientData();
                while (server.serverStatus)
                {
                    if (server.socketForClient.Connected)
                    {
                        MessageFromClient = server.streamReader.ReadLine();
                        Console.WriteLine("Client : " + MessageFromClient);

                        if (MessageFromClient == "start game")
                        {
                            server.serverStatus = false;
                            server.streamReader.Close();
                            server.streamWriter.Close();
                            server.networkStream.Close();
                            return;
                        }
                        server.streamWriter.Flush();
                    }
                }
                server.Disconnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
