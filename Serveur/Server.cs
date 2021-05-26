using System;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Serveur
{
    public class Server
    {
        public IPAddress myIPAddress { get; private set; }
        public int port { get; private set; }
        public bool serverStatus = true;
        private TcpListener tcpListener { get; set; }
        public Socket socketForClient { get; set; }

        public NetworkStream networkStream { get; set; }
        public StreamReader streamReader { get; set; }
        public StreamWriter streamWriter { get; set; }

        public Server(IPAddress myIPAddress, int port)
        {
            this.myIPAddress = myIPAddress;
            this.port = port;
        }

        public void StartListening()
        {
            try
            {
                tcpListener = new TcpListener(myIPAddress, port);
                tcpListener.Start();
            }
            catch
            {
                Console.WriteLine("Could not start");
            }
        }
        public void AcceptClient()
        {
            try
            {
                socketForClient = tcpListener.AcceptSocket();
            }
            catch
            {
                Console.WriteLine("Could not accept Client");
            }
        }

        //allows server to exchange data with client

        public void ClientData()
        {
            //Client data
            networkStream = new NetworkStream(socketForClient);
            //Allow to read from client
            streamReader = new StreamReader(networkStream);
            //Allow to write to client
            streamWriter = new StreamWriter(networkStream);
        }
        public void Disconnect()
        {
            networkStream.Close();
            streamReader.Close();
            streamWriter.Close();
            socketForClient.Close();
        }
    }
}