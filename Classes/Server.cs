using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
namespace lab1.Classes
{
    class Server
    {
        int messageId = 1;
        int TTL = 30;
        public int Port { get; set; }
        public string Host { get; set; }
        public List<int> Clients = new List<int>();
        public List<Message> Messages = new List<Message>();
        public UdpClient udpClient;
        bool online;

        public Server(int port, string host)
        {
            Port = port;
            Host = host;
            online = true;
            try
            {
                udpClient = new UdpClient(port);
                udpClient.JoinMulticastGroup(IPAddress.Parse(host), TTL);
                Task receiveTask = new Task(ReceiveMessages);
                receiveTask.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void ReceiveMessages()
        {
            try
            {
                online = true;
                while (online)
                {
                    IPEndPoint remoteIp = null;
                    byte[] data = udpClient.Receive(ref remoteIp);
                    Message message = (Message)ByteParser.ByteArrayToObj(data);
                    switch (message.Type)
                    {
                        case Type.Join:
                            Clients.Add(message.SenderPort);
                            break;
                        case Type.Leave:
                            Clients.RemoveAll(p => p == message.SenderPort);
                            break;
                        case Type.Message:
                            message.Id = messageId++;
                            Messages.Add(message);
                            break;
                        case Type.Confirm:
                            Message storedMessage = Messages.Find(m => m.Id == message.Id); 
                            if (!storedMessage.IsDelivered && (storedMessage.SenderPort != message.SenderPort || Clients.Count == 1))
                            {
                                storedMessage.IsDelivered = true;
                                byte[] newData = ByteParser.ObjToByteArray(new Message() { Id = message.Id, Type=Type.Confirm });
                                udpClient.Send(newData, newData.Length, Host, storedMessage.SenderPort);
                            }
                            break;                          
                    }
                    if (message.Type != Type.Confirm) {
                        byte[] newData = ByteParser.ObjToByteArray(message);
                        foreach (int port in Clients)
                        {                           
                            udpClient.Send(newData, newData.Length, Host, port);
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                if (!online)
                    return;
                throw;
            }
            catch (SocketException ex) when (ex.ErrorCode == 10004)
            {
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void Close()
        {
            byte[] data = ByteParser.ObjToByteArray(new Message() { Type = Type.ServerShutDown });
            foreach (int client in Clients)
            {
                udpClient.Send(data, data.Length, Host, client);
            }
        }
    }
}
