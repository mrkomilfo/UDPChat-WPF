using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace lab1
{
    public partial class MainWindow : Window
    {        
        const int TTL = 30;
        
        int localPort; //8001
        int remotePort; //8002
        string host; //235.5.5.1

        bool online = false;
        UdpClient client;
        IPAddress groupAddress;
        string userName;

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
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            Regex portRegex = new Regex(@"^[0-9]{1,5}$");
            Regex hostRegex = new Regex(@"^[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}.[0-9]{1,3}$");
            if (portRegex.IsMatch(localPortTextBox.Text.Trim()) && portRegex.IsMatch(remotePortTextBox.Text.Trim()) && hostRegex.IsMatch(hostAddressTextBox.Text.Trim()))
            {
                localPort = int.Parse(localPortTextBox.Text.Trim());
                remotePort = int.Parse(remotePortTextBox.Text.Trim());
                host = hostAddressTextBox.Text.Trim();

                loginButton.IsEnabled = true;
                groupAddress = IPAddress.Parse(host);
                changeMod();
            }
            else {
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
                    MessageBox.Show("Invalid local port");
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
                client = new UdpClient(localPort);
                client.JoinMulticastGroup(groupAddress, TTL);
                Task receiveTask = new Task(ReceiveMessages);
                receiveTask.Start();

                string message = $"*{userName} joined the chat*";
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, host, remotePort);
                if (chatTextBlock.Text.Length == 0)
                {
                    chatTextBlock.Inlines.Add("\n");
                }
                chatTextBlock.Inlines.Add($"[{DateTime.Now.ToShortTimeString()}] *You joined the chat*");

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
        private void ReceiveMessages()
        {
            online = true;
            try
            {
                while (online)
                {
                    IPEndPoint remoteIp = null;
                    byte[] data = client.Receive(ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);

                    Dispatcher.Invoke(new Action(delegate
                    {
                        chatTextBlock.Inlines.Add($"\n[{DateTime.Now.ToShortTimeString()}] {message}");                        
                    }));
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
                string message = $"{userName}: {messageTextBox.Text.Trim()}";
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, host, remotePort);
                messageTextBox.Clear();

                if (localPort != remotePort)
                {
                    chatTextBlock.Inlines.Add($"\n[{DateTime.Now.ToShortTimeString()}] {message}");
                    scrollViewer.ScrollToEnd();
                }                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void chmodButton_Click(object sender, EventArgs e)
        {
            changeMod(); 
        }

        private void changeMod() {
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
            string message = $"{userName} has left the chat";
            byte[] data = Encoding.Unicode.GetBytes(message);
            client.Send(data, data.Length, host, remotePort);
            client.DropMulticastGroup(groupAddress);

            online = false;
            client.Close();

            loginButton.IsEnabled = true;
            logoutButton.IsEnabled = false;
            sendButton.IsEnabled = false;
            saveButton.IsEnabled = true;

            userNameTextBox.IsReadOnly = false;
            localPortTextBox.IsReadOnly = false;
            remotePortTextBox.IsReadOnly = false;
            hostAddressTextBox.IsReadOnly = false;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (online)
                ExitChat();
        }
    }
}
