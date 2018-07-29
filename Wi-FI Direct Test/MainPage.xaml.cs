using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.WiFiDirect;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.Networking;
using System.Diagnostics;
using Windows.UI.Core;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace Wi_FI_Direct_Test
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        WiFiDirectAdvertisementPublisher publisher;
        WiFiDirectConnectionListener listener;


        /// <summary>
        /// Wi-Fi直连广播者开始广播
        /// </summary>
        public void StartAdvertising()
        {

            publisher = new WiFiDirectAdvertisementPublisher();
            listener = new WiFiDirectConnectionListener();
            publisher.StatusChanged += OnPublisherStatusChanged;
            publisher.Advertisement.ListenStateDiscoverability = WiFiDirectAdvertisementListenStateDiscoverability.Intensive;
            listener.ConnectionRequested += OnConnectionRequested;
            publisher.Start();
            //TextBlock_ConnectedState.Text = "开始广播……";
        }

        private async void OnPublisherStatusChanged(WiFiDirectAdvertisementPublisher sender, WiFiDirectAdvertisementPublisherStatusChangedEventArgs args)
        {
            if(args.Status == WiFiDirectAdvertisementPublisherStatus.Aborted)
            {
                Debug.WriteLine("因错误终止广播");
                publisher.Start();
            }
            else if(args.Status == WiFiDirectAdvertisementPublisherStatus.Started)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { TextBlock_ConnectedState.Text = "开始广播……"; });
            }
        }

        IReadOnlyList<Windows.Networking.EndpointPair> EndpointPairs;
        /// <summary>
        /// 广播者接收到请求连接消息后的操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void OnConnectionRequested(WiFiDirectConnectionListener sender, WiFiDirectConnectionRequestedEventArgs args)
        {
            try
            {
                WiFiDirectConnectionRequest ConnectionRequest = args.GetConnectionRequest();
                WiFiDirectDevice wfdDevice = await WiFiDirectDevice.FromIdAsync(ConnectionRequest.DeviceInformation.Id);
                //与之连接过的列表
                EndpointPairs = wfdDevice.GetConnectionEndpointPairs();
                //StartUDPServer("50001");
                //EstablishSocketFromAdvertiser(EndpointPairs[0].LocalHostName, "50001");
                //Invoke(() => { TextBlock_ConnectedState.Text = "连接到" + EndpointPairs[0].LocalHostName.ToString(); });
            }
            catch (Exception exp)
            {
                Invoke(() => { TextBlock_ConnectedState.Text = exp.Message; });
            }
        }


        public async void Invoke(Action action, Windows.UI.Core.CoreDispatcherPriority Priority = Windows.UI.Core.CoreDispatcherPriority.Normal)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Priority, () => { action(); });
        }


        /// <summary>
        /// 连接者开始搜索发出广播的设备
        /// </summary>
        public async void StartConnecting()
        {
            string deviceSelector = WiFiDirectDevice.GetDeviceSelector(WiFiDirectDeviceSelectorType.AssociationEndpoint);

            //获取所有检测到的Wi-Fi直连设备
            DeviceInformationCollection devInfoCollection = await DeviceInformation.FindAllAsync(deviceSelector);
            Invoke(() =>
            {
                if (devInfoCollection.Count == 0)
                {
                    TextBlock_ConnectedState.Text = "找不到设备";
                }
                else
                {
                    TextBlock_ConnectedState.Text = "";
                    foreach (var devInfo in devInfoCollection)
                    {
                        TextBlock_ConnectedState.Text += devInfo.Id + '\n';
                    }
                    TextBox_SelectedIP.Text = devInfoCollection[0].Id;
                }
            });
        }

        /// <summary>
        /// 建立连接
        /// </summary>
        public async void Connecting()
        {
            //连接参数
            WiFiDirectConnectionParameters connectionParams = new WiFiDirectConnectionParameters();
            connectionParams.GroupOwnerIntent = 1;

            WiFiDirectDevice wfdDevice;
            string deviceId = TextBox_SelectedIP.Text;
            try
            {
                wfdDevice = await WiFiDirectDevice.FromIdAsync(deviceId, connectionParams);
                Invoke(() =>
                      {
                          TextBlock_ConnectedState.Text = "连接到设备" + deviceId;
                      });
                EndpointPairs = wfdDevice.GetConnectionEndpointPairs();
                EstablishSocketFromConnector(EndpointPairs[0].RemoteHostName, "50001");
            }
            catch (Exception exp)
            {
                Invoke(() => { TextBlock_ConnectedState.Text = exp.Message; });
            }
        }

        /// <summary>
        /// 建立接口
        /// </summary>
        StreamSocket _socket;

        /// <summary>
        /// 连接者套接字建立
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="port"></param>
        async void EstablishSocketFromAdvertiser(HostName hostName, string port)
        {

            StreamSocketListener listener = new StreamSocketListener();
            listener.ConnectionReceived += OnConnectionReceived;
            await listener.BindEndpointAsync(hostName, port);
            Invoke(() => { TextBlock_ConnectedState.Text = "成功与设备"+ EndpointPairs[0].LocalHostName.ToString()+"建立接口"; });
        }

        /// <summary>
        /// 广播者套接字建立
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="port"></param>
        async void EstablishSocketFromConnector(HostName hostName, string port)
        {
            _socket = new StreamSocket();
            await _socket.ConnectAsync(hostName, port);
        }

        void OnConnectionReceived(StreamSocketListener listener, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // Stop advertising/listening so that we're only serving one client
            //_provider.StopAdvertising();
            _socket = args.Socket;
            //TextBlock_Log.Text = "成功";

        }

        private void Button_StartAdvertising_Click(object sender, RoutedEventArgs e)
        {
            StartAdvertising();
        }

        private void Button_ShowIPAdresses_Click(object sender, RoutedEventArgs e)
        {
            StartConnecting();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Connecting();
        }

        private void Button_StopAdvertising_Click(object sender, RoutedEventArgs e)
        {
            publisher.Stop();
        }

        private void Send_Button_Click(object sender, RoutedEventArgs e)
        {
            DataTransport data = new DataTransport(_socket);
            data.SendData(10);
        }

        private void Button_Receive_Click(object sender, RoutedEventArgs e)
        {
            DataTransport data = new DataTransport(_socket);
            data.ReceiveData(4);
        }

        private async void StartUDPServer(string port)
        {
            var serverDatagramSocket = new DatagramSocket();
            serverDatagramSocket.MessageReceived += ServerDatagramSocket_MessageReceived;
            await serverDatagramSocket.BindServiceNameAsync(port);
        }

        private void ServerDatagramSocket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            using (var dataReader = args.GetDataReader())
            {
                while (true)
                {
                    Debug.WriteLine(dataReader.ReadInt32());
                }
            }
        }
    }
}
