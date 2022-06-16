using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using SseNamedPipeTest.Annotations;

namespace SseNamedPipeTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _clientOutputText;

        public string ClientOutputText
        {
            get => _clientOutputText;
            set
            {
                if (value == _clientOutputText) return;
                _clientOutputText = value;
                OnPropertyChanged(nameof(ClientOutputText));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private SseNamedPipeClient _client;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _client = new SseNamedPipeClient("com.ifpdos.udi.namedpipe");
            _client.Connect += Client_Connect;
            _client.MessageReceived += Client_MessageReceived;
        }

        private void Client_MessageReceived(object sender, string e)
        {
            ClientOutputText += $"Client Message {e}";
        }

        private void Client_Connect(object sender, EventArgs e)
        {
            ClientOutputText += "Client Connect \r\n";
        }

        private void OpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            _client.Open();
        }

        private static string str =
            "request: /v1/udi/service/register\naction: POST\ndata: {\"apis\":[{\"get\":{\"enabled\":true,\"permission\":0},\"link\":\"\",\"notify\":{\"enabled\":true,\"permission\":4},\"set\":{\"enabled\":true,\"permission\":7},\"topic\":\"v1/test/service/interface\"}],\"group\":0,\"version\":2}\nid: b22043ca-062e-4bee-a2ce-f8fc03db8751\ntoken: xZLhFgzpcBENwvdXkoEy3IGH8MZlM-aL5ZwXkyAo9kgxQrha0xHhyhBav58OFQ1hdECHWfo=\nversion: 1\n\n";

        private void SendButton_OnClick(object sender, RoutedEventArgs e)
        {
            _client.SendMessage(str);
        }
    }
}
