using System.Windows;

namespace lab1
{
    public partial class ServerWindow : Window
    {
        public ServerWindow()
        {
            InitializeComponent();
        }
        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public string Port
        {
            get { return portBox.Text; }
        }

        public string Host
        {
            get { return hostAddressBox.Text; }
        }

    }
}
