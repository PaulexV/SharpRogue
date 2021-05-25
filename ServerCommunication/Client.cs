using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;

namespace ServerCommunication
{
    public class Client
    {
        public string myIPAddress { get; private set; }
        public int port { get; set; }
        private TcpClient socketForServer;
        public bool clientStatus = true;

        public NetworkStream networkStream { get; set; }
        public StreamWriter streamWriter { get; set; }
        public StreamReader streamReader { get; set; }

        public Client(string myIPAddress, int port)
        {
            this.myIPAddress = myIPAddress;
            this.port = port;
        }
        public void ConnectToServer()
        {
            try
            {
                socketForServer = new TcpClient(myIPAddress.ToString(), port);
            }
            catch
            {
                Console.WriteLine("Could not connect to server");
            }
        }
        public void ServerData()
        {
            networkStream = socketForServer.GetStream();
            streamReader = new StreamReader(networkStream);
            streamWriter = new StreamWriter(networkStream);
        }
        public void Disconnect()
        {
            networkStream.Close();
            streamReader.Close();
            streamWriter.Close();
            socketForServer.Close();
        }
    }
}