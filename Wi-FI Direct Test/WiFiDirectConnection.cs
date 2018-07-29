using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.WiFiDirect;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Wi_FI_Direct_Test
{
    class WiFiDirectConnection : IServerConnection
    {

        public string ConnectCode
        {
            get
            {
                //这个是用于建立接口的名字，而不是Wi-Fi Direct连接的
                return EndpointPairs[0].LocalHostName.ToString();
            }
        }

        public ConnectionType connectionType
        {
            get
            {
                return ConnectionType.UDP;
            }
        }
        private ConnectionEstablishState _ConnectionEstablishState = ConnectionEstablishState.Created;
        public ConnectionEstablishState ConnectionEstablishState
        {
            private set
            {
                _ConnectionEstablishState = value;
                //OnConnectionEstalblishResult?.Invoke(this, value);
            }
            get
            {
                return _ConnectionEstablishState;
            }
        }

        public event ConnectionHandler OnConnectionEstalblishResult;
        public event MessageHandler onReceiveMessage;

        public async Task SendAsync(byte[] message)
        {
            
        }

        public async Task ReceiveAsync(byte[] message)
        {
            onReceiveMessage(this, message);
            var packMessageBuffer = new Queue<MessagePack>();
        }

        public void StartServer()
        {
            publisher = new WiFiDirectAdvertisementPublisher();
            listener = new WiFiDirectConnectionListener();
            publisher.Advertisement.ListenStateDiscoverability = WiFiDirectAdvertisementListenStateDiscoverability.Normal;
            listener.ConnectionRequested += OnConnectionRequested;
            _ConnectionEstablishState = ConnectionEstablishState.Connecting;
            OnConnectionEstalblishResult(this, _ConnectionEstablishState);
            publisher.Start();
            //TextBlock_ConnectedState.Text = "开始广播……";
        }

        WiFiDirectAdvertisementPublisher publisher;
        WiFiDirectConnectionListener listener;
        StreamSocket _socket;
        IReadOnlyList<Windows.Networking.EndpointPair> EndpointPairs;

        /// <summary>
        /// 调用UI线程
        /// </summary>
        /// <param name="action"></param>
        /// <param name="Priority"></param>
        public async void Invoke(Action action, Windows.UI.Core.CoreDispatcherPriority Priority = Windows.UI.Core.CoreDispatcherPriority.Normal)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Priority, () => { action(); });
        }

        private async void OnConnectionRequested(WiFiDirectConnectionListener sender, WiFiDirectConnectionRequestedEventArgs args)
        {
            WiFiDirectConnectionRequest ConnectionRequest = args.GetConnectionRequest();
            WiFiDirectDevice wfdDevice = await WiFiDirectDevice.FromIdAsync(ConnectionRequest.DeviceInformation.Id);
            EndpointPairs = wfdDevice.GetConnectionEndpointPairs();
            _ConnectionEstablishState = ConnectionEstablishState.Succeeded;
            OnConnectionEstalblishResult?.Invoke(this, _ConnectionEstablishState);
        }

        private void StartUDP(int port, int size)
        {
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(ipep);
            byte[] recvBytes;
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            bool udpListening = true;
            while (udpListening)
            {
                recvBytes = new byte[size];
                int recv = socket.ReceiveFrom(recvBytes, ref sender);
                string msg = Encoding.ASCII.GetString(recvBytes, 0, recv);
                string printMsg;
                if (sender is IPEndPoint)
                {
                    IPEndPoint ipepSender = sender as IPEndPoint;
                    printMsg = ("(From: " + ipepSender.ToString() + ")");
                }
                else
                {
                    printMsg = ("(From: UNKNOWN)");
                }
                printMsg += msg;
                Debug.WriteLine(printMsg);
            }
        }
}
