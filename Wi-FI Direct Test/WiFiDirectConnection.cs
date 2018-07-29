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

        public ConnectionEstablishState ConnectionEstablishState => throw new NotImplementedException();

        public event ConnectionHandler OnConnectionEstalblishResult;
        public event MessageHandler onReceiveMessage;

        public async Task SendAsync(byte[] message)
        {
            DataTransport data = new DataTransport(_socket);
            await data.SendData(10);
        }

        public void StartServer()
        {
            publisher = new WiFiDirectAdvertisementPublisher();
            listener = new WiFiDirectConnectionListener();
            publisher.Advertisement.ListenStateDiscoverability = WiFiDirectAdvertisementListenStateDiscoverability.Normal;
            listener.ConnectionRequested += OnConnectionRequested;
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

        /// <summary>
        /// 广播者套接字建立
        /// </summary>
        /// <param name="hostName">主机名</param>
        /// <param name="port">端口号</param>
        async void EstablishSocketFromAdvertiser(HostName hostName, string port)
        {

            StreamSocketListener listener = new StreamSocketListener();
            listener.ConnectionReceived += OnConnectionReceived;
            await listener.BindEndpointAsync(EndpointPairs[0].LocalHostName, port);
            //Invoke(() => { TextBlock_ConnectedState.Text = "成功建立接口"; });
        }
        void OnConnectionReceived(StreamSocketListener listener, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // Stop advertising/listening so that we're only serving one client
            //_provider.StopAdvertising();
            _socket = args.Socket;
            //TextBlock_Log.Text = "成功";
        }

        /// <summary>
        /// 连接者套接字建立
        /// </summary>
        /// <param name="hostName">主机名</param>
        /// <param name="port">端口号</param>
        async void EstablishSocketFromConnector(HostName hostName, string port)
        {
            _socket = new StreamSocket();
            await _socket.ConnectAsync(hostName, port);
        }
        private async void OnConnectionRequested(WiFiDirectConnectionListener sender, WiFiDirectConnectionRequestedEventArgs args)
        {
            //IUICommand result;
            //Int32 intResult = 0;
            //Invoke(async () =>
            //{
            //    var dialog = new MessageDialog("接受到连接请求，是否要连接到该设备", "消息提示");
            //    dialog.Commands.Add(new UICommand("确定", cmd => { }, commandId: 0));
            //    dialog.Commands.Add(new UICommand("取消", cmd => { }, commandId: 1));
            //    dialog.DefaultCommandIndex = 0;
            //    dialog.CancelCommandIndex = 1;
            //    result = await dialog.ShowAsync();
            //    intResult = Convert.ToInt32(result.Id);
            //});

            WiFiDirectConnectionRequest ConnectionRequest = args.GetConnectionRequest();
            WiFiDirectDevice wfdDevice = await WiFiDirectDevice.FromIdAsync(ConnectionRequest.DeviceInformation.Id);
            //与之连接过的列表
            EndpointPairs = wfdDevice.GetConnectionEndpointPairs();
            EstablishSocketFromAdvertiser(EndpointPairs[0].LocalHostName, "50001");
            //Invoke(() => { TextBlock_ConnectedState.Text = "连接到" + EndpointPairs[0].LocalHostName.ToString(); });
        }
    }
}
