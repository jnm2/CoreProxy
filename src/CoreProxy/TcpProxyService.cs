using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace jnm2.CoreProxy
{
    public class TcpProxyService : IDisposable
    {
        private readonly IPEndPoint to;
        private readonly TcpListener listener;

        public TcpProxyService(IPEndPoint from, IPEndPoint to)
        {
            this.to = to;
            listener = new TcpListener(from);
        }

        public void Start()
        {
            listener.Start();
            SubscribeToNextConnection();
        }

        private void SubscribeToNextConnection()
        {
            listener.AcceptTcpClientAsync().ContinueWith(ClientConnected, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private async void ClientConnected(Task<TcpClient> task)
        {
            SubscribeToNextConnection();

            using (var clientConnection = task.GetAwaiter().GetResult())
            using (var serverConnection = new TcpClient()) // TODO: pool
            {
                await serverConnection.ConnectAsync(to.Address, to.Port); // TODO: possibly pool?
                
                using (var serverStream = serverConnection.GetStream())
                using (var clientStream = clientConnection.GetStream())
                {
                    // TODO: Pool buffers
                    // TODO: Careful with cancellation. If one closes, flush before closing the other.

                    await Task.WhenAll(
                        new HalfDuplex(clientStream, serverStream, new byte[4096], new byte[4096]).Run(CancellationToken.None),
                        new HalfDuplex(serverStream, clientStream, new byte[4096], new byte[4096]).Run(CancellationToken.None)); 
                }
            }
        }

        private sealed class HalfDuplex
        {
            private byte[] buffer1, buffer2;
            private readonly NetworkStream fromStream, toStream;

            public HalfDuplex(NetworkStream fromStream, NetworkStream toStream, byte[] buffer1, byte[] buffer2)
            {
                this.buffer1 = buffer1;
                this.buffer2 = buffer2;
                this.fromStream = fromStream;
                this.toStream = toStream;
            }

            public async Task Run(CancellationToken cancellationToken)
            {
                var bytesRead = 0;

                while (true)
                {
                    if (bytesRead != 0)
                    {
                        var writeTask = toStream.WriteAsync(buffer1, 0, bytesRead, cancellationToken);
                        var readTask = fromStream.ReadAsync(buffer2, 0, buffer2.Length, cancellationToken);
                        await Task.WhenAll(writeTask, readTask);
                        bytesRead = readTask.GetAwaiter().GetResult();
                    }
                    else
                    {
                        bytesRead = await fromStream.ReadAsync(buffer2, 0, buffer2.Length, cancellationToken);
                    }

                    var flip = buffer1;
                    buffer1 = buffer2;
                    buffer2 = flip;
                }
            }
        }

        public void Dispose()
        {
            listener.Stop();
        }
    }
}
