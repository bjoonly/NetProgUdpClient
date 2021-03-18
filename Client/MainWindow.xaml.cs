using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace Client
{
    [Serializable]
    public class MessageInfo
    {
        public string Nickname { get; set; }
        public string IPAddress { get; set; }
        public string Time { get; set; }
        public string Text { get; set; }
    }
    public partial class MainWindow : Window
    {
        
        private static string remoteIPAddress = "127.0.0.1";
       public string nickname;
        private static int remotePort = 8080;

        UdpClient client = new UdpClient(0);

        ObservableCollection<MessageInfo> messages = new ObservableCollection<MessageInfo>();
        public MainWindow()
        {
            InitializeComponent();
            list.ItemsSource = messages;

            Task.Run(() => Listen());
        }

        private void Listen()
        {
            IPEndPoint iPEndPoint = null;
            while (true)
            {
                try
                {
                    byte[] bytes = client.Receive(ref iPEndPoint);
                    XmlSerializer xml = new XmlSerializer(typeof(MessageInfo));
                    MemoryStream stream = new MemoryStream();
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Position = 0;
                    var mi = (MessageInfo)xml.Deserialize(stream);
                    App.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        if (mi.Text.Contains(nickname) && mi.Text.ToLower().Contains("join"))
                        {
                            nicknameTB.IsEnabled = false;
                            joinButton.IsEnabled = false;
                            messageTB.IsEnabled = true;
                            leaveButton.IsEnabled = true;
                            sendButton.IsEnabled = true;
                        }
                    }
                    ));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        messages.Insert(0, new MessageInfo()
                        {
                            IPAddress = mi.IPAddress,
                            Time = mi.Time,
                            Text = mi.Text,
                            Nickname=mi.Nickname
                        });
                    }));
                    stream.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void SendMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return;

            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(remoteIPAddress), remotePort);
            XmlSerializer xml = new XmlSerializer(typeof(MessageInfo));
            MessageInfo mi = new MessageInfo() { Text = msg, Time = DateTime.Now.ToShortTimeString(), Nickname = nickname, IPAddress = remoteIPAddress.ToString() };
            MemoryStream stream = new MemoryStream();
            xml.Serialize(stream, mi);
            stream.Position = 0;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);
            client.Send(bytes, bytes.Length, iPEndPoint);
        
        }

        private void joinButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(nicknameTB.Text))
            {
                MessageBox.Show("Enter your nickname.");
                return;
            }
            nickname = nicknameTB.Text;
            SendMessage("<JOIN>");
            
               
           
                    
           
           
        
        }

        private void leaveButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage("<LEAVE>");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                messages.Insert(0, new MessageInfo()
                {
                    IPAddress = remoteIPAddress,
                    Time = DateTime.Now.ToShortTimeString(),
                    Text = $"{nickname} left the chat.",
                    Nickname = nickname
                });
            }));
           
            nicknameTB.IsEnabled = true;
            joinButton.IsEnabled = true;
            messageTB.IsEnabled = false;
            leaveButton.IsEnabled = false;
            sendButton.IsEnabled = false;
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(messageTB.Text);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            SendMessage("<LEAVE>");
            client.Close();
        }
    }
}
