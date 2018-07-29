using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Wi_FI_Direct_Test
{
    class DataTransport
    {
        StreamSocket socket;
        public DataTransport(StreamSocket _socket)
        {
            socket = _socket;
        }

        public async Task SendData(Int32 message)
        {
            DataWriter writer;
            writer = new DataWriter(socket.OutputStream);
            writer.WriteInt32(message);
            await writer.StoreAsync();
        }

        public async Task ReceiveData(uint size)
        {
            DataReader reader;
            reader = new DataReader(socket.InputStream);
            reader.InputStreamOptions = InputStreamOptions.Partial;
            await reader.LoadAsync(size);
            int a = reader.ReadInt32();
            System.Diagnostics.Debug.WriteLine(a);
        }
    }
}
