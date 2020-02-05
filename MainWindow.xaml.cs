using lab1.Classes;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using messageType = lab1.Classes.Type;

namespace lab1
{
    public partial class MainWindow : Window
    {        
        const int TTL = 30;
        Server server;
        int localPort;
        int remotePort;
        string host; //235.5.5.1 / 224.0.0.4
        bool online = false;
        UdpClient udpClient;
        IPAddress groupAddress;
        string userName;
        readonly Regex portRegex = new Regex(@"^[0-9]{1,5}$");
        readonly Regex hostRegex = new Regex(@"^[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}$");

        public MainWindow()
        {
            InitializeComponent();
            loginButton.IsEnabled = false;
            logoutButton.IsEnabled = false;
            sendButton.IsEnabled = false;
            saveButton.IsEnabled = true;
               
            loginButton.Click += loginButton_Click;
            logoutButton.Click += logoutButton_Click;
            sendButton.Click += sendButton_Click;
            saveButton.Click += saveButton_Click;
            chmodButton.Click += chmodButton_Click;
            newServerButton.Click += newServerButton_Click;
        }
        private void ReceiveMessages()
        {
            online = true;
            try
            {
                while (online)
                {
                    IPEndPoint remoteIp = null;
                    byte[] data = udpClient.Receive(ref remoteIp);
                    Message message = (Message)ByteParser.ByteArrayToObj(data);
                    switch (message.Type)
                    {
                        case messageType.Confirm:
                            Dispatcher.Invoke(new Action(delegate
                            {
                                chatTextBlock.Inlines.Add($"\n*message #{message.Id} delivered*");
                            }));
                            break;
                        case messageType.Message:
                            Dispatcher.Invoke(new Action(delegate
                            {
                                if (message.SenderPort == localPort)
                                {
                                    chatTextBlock.Inlines.Add($"\n[#{message.Id}]({DateTime.Now.ToShortTimeString()}) {message.Content}");
                                }
                                else {
                                    chatTextBlock.Inlines.Add($"\n({DateTime.Now.ToShortTimeString()}) {message.Content}");
                                }                              
                            }));
                            Message responseMessage = new Message() { Type = messageType.Confirm, Id = message.Id, SenderPort = localPort };
                            byte[] newData = ByteParser.ObjToByteArray(responseMessage);
                            udpClient.Send(newData, newData.Length, host, remotePort);
                            break;
                        case messageType.Join:
                            Dispatcher.Invoke(new Action(delegate
                            {
                                if (chatTextBlock.Text.Length > 0)
                                {
                                    chatTextBlock.Inlines.Add("\n");
                                }
                                chatTextBlock.Inlines.Add($"({DateTime.Now.ToShortTimeString()}) {message.Content}");
                            }));
                            break;
                        case messageType.Leave:
                            Dispatcher.Invoke(new Action(delegate
                            {
                                chatTextBlock.Inlines.Add($"\n({DateTime.Now.ToShortTimeString()}) {message.Content}");
                            }));
                            break;
                        case messageType.ServerShutDown:
                            Dispatcher.Invoke(new Action(delegate
                            {
                                ExitChat();
                                MessageBox.Show("Server shut down");
                                chatTextBlock.Inlines.Clear();                               
                            }));                           
                            break;
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

        private void sendButton_Click(object sender, EventArgs e)
        {
            if (messageTextBox.Text.Trim().Length == 0)
            {
                return;
            }
            try
            {
                Message message = new Message() { Content = $"{userName}: {messageTextBox.Text.Trim()}", SenderPort=localPort, Type = messageType.Message};
                byte[] data = ByteParser.ObjToByteArray(message);
                udpClient.Send(data, data.Length, host, remotePort);
                messageTextBox.Clear();
                scrollViewer.ScrollToEnd();               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (portRegex.IsMatch(localPortTextBox.Text.Trim()) && portRegex.IsMatch(remotePortTextBox.Text.Trim()) && hostRegex.IsMatch(hostAddressTextBox.Text.Trim()))
            {
                localPort = int.Parse(localPortTextBox.Text.Trim());
                remotePort = int.Parse(remotePortTextBox.Text.Trim());
                host = hostAddressTextBox.Text.Trim();

                loginButton.IsEnabled = true;
                groupAddress = IPAddress.Parse(host);
                changeMod();
            }
            else
            {
                if (!portRegex.IsMatch(localPortTextBox.Text.Trim()))
                {
                    MessageBox.Show("Invalid local port");
                }
                if (!portRegex.IsMatch(remotePortTextBox.Text.Trim()))
                {
                    MessageBox.Show("Invalid remote port");
                }
                if (!hostRegex.IsMatch(hostAddressTextBox.Text.Trim()))
                {
                    MessageBox.Show("Invalid host address");
                }
            }
        }
        private void loginButton_Click(object sender, EventArgs e)
        {
            if (userNameTextBox.Text.Trim().Length == 0)
            {
                userNameTextBox.Clear();
                return;
            }
            userName = userNameTextBox.Text.Trim();
            userNameTextBox.IsReadOnly = true;

            try
            {
                udpClient = new UdpClient(localPort);
                udpClient.JoinMulticastGroup(groupAddress, TTL);
                Task receiveTask = new Task(ReceiveMessages);
                receiveTask.Start();

                Message message = new Message() { Content = $"*{userName} joined the chat*", SenderPort = localPort, Type = messageType.Join };
                byte[] data = ByteParser.ObjToByteArray(message);
                udpClient.Send(data, data.Length, host, remotePort);               

                localPortTextBox.IsReadOnly = true;
                remotePortTextBox.IsReadOnly = true;
                hostAddressTextBox.IsReadOnly = true;

                saveButton.IsEnabled = false;
                loginButton.IsEnabled = false;
                logoutButton.IsEnabled = true;
                sendButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void newServerButton_Click(object sender, EventArgs e)
        {
            ServerWindow serverWindow = new ServerWindow();

            if (serverWindow.ShowDialog() == true)
            {
                if (portRegex.IsMatch(serverWindow.Port.Trim()) && hostRegex.IsMatch(serverWindow.Host.Trim()))
                {
                    int serverPort = int.Parse(serverWindow.Port.Trim());
                    string serverHost = serverWindow.Host.Trim();
                    if (server != null)
                        server.Close();
                    server = new Server(serverPort, serverHost);
                    MessageBox.Show($"Server created (port={serverPort}, hostAddress={serverHost})");
                }
                else
                {
                    if (!portRegex.IsMatch(serverWindow.Port.Trim()))
                    {
                        MessageBox.Show("Invalid local port");
                    }
                    if (!hostRegex.IsMatch(serverWindow.Host.Trim()))
                    {
                        MessageBox.Show("Invalid host address");
                    }
                }
            }
        }

        private void chmodButton_Click(object sender, EventArgs e)
        {
            changeMod();
        }

        private void changeMod()
        {
            if (optionsPanel.Visibility == Visibility.Visible)
            {
                optionsPanel.Visibility = Visibility.Collapsed;
                identityPanel.Visibility = Visibility.Visible;
            }
            else
            {
                identityPanel.Visibility = Visibility.Collapsed;
                optionsPanel.Visibility = Visibility.Visible;
            }
        }
        private void logoutButton_Click(object sender, EventArgs e)
        {
            ExitChat();
        }

        private void ExitChat()
        {
            Message message = new Message() { Content = $"*{userName} has left the chat*", SenderPort = localPort, Type = messageType.Leave };
            byte[] data = ByteParser.ObjToByteArray(message);
            udpClient.Send(data, data.Length, host, remotePort);
            udpClient.DropMulticastGroup(groupAddress);
           
            online = false;
            udpClient.Close();

            chatTextBlock.Inlines.Add($"\n({DateTime.Now.ToShortTimeString()}) *You has left the chat*");

            loginButton.IsEnabled = true;
            logoutButton.IsEnabled = false;
            sendButton.IsEnabled = false;
            saveButton.IsEnabled = true;

            userNameTextBox.IsReadOnly = false;
            localPortTextBox.IsReadOnly = false;
            remotePortTextBox.IsReadOnly = false;
            hostAddressTextBox.IsReadOnly = false;
        }
 
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (server != null)
                server.Close();
            if (online)
                ExitChat();
        }
    }
}
