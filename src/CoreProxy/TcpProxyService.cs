using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace jnm2.CoreProxy
{
    public class TcpProxyService : IDisposable
    {
        private readonly IPEndPoint to;
        protected readonly Action<string> Log;
        private readonly TcpListener listener;
        private CancellationTokenSource disposalSource = new CancellationTokenSource();

        public TcpProxyService(IPEndPoint from, IPEndPoint to, Action<string> log)
        {
            this.to = to;
            Log = log;
            listener = new TcpListener(from);
        }

        public void Start()
        {
            listener.Start();
            SubscribeToNextConnection();
        }

        private void SubscribeToNextConnection()
        {
            while (true)
            {
                try
                {
                    listener.AcceptTcpClientAsync().ContinueWith(ClientConnected, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    return;
                }
                catch (InvalidOperationException) when (disposalSource.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log.Invoke(ex.ToString());
                }
            }
        }

        private async void ClientConnected(Task<TcpClient> task)
        {
            SubscribeToNextConnection();

            try
            {

                TcpClient clientConnection;
                try
                {
                    clientConnection = task.GetAwaiter().GetResult();
                }
                catch (ObjectDisposedException) when (disposalSource.IsCancellationRequested) // Handles race condition
                {
                    return;
                }

                using (clientConnection)
                using (var clientTcpStream = clientConnection.GetStream())
                using (var clientStream = await TryGetClientStream(clientTcpStream))
                {
                    if (clientStream == null) return;

                    using (var serverConnection = new TcpClient()) // TODO: pool
                    {
                        if (disposalSource.IsCancellationRequested) return;
                        await serverConnection.ConnectAsync(to.Address, to.Port); // TODO: possibly pool?

                        using (var serverStream = serverConnection.GetStream())
                        {
                            try
                            {
                                await Task.WhenAll(
                                    new HalfDuplex(clientStream, serverStream, new byte[4096], new byte[4096]).Run(disposalSource.Token), // TODO: Pool buffers
                                    new HalfDuplex(serverStream, clientStream, new byte[4096], new byte[4096]).Run(disposalSource.Token));
                            }
                            catch (OperationCanceledException)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Invoke(ex.ToString()); // TODO: logging
            }  
        }

        private sealed class HalfDuplex
        {
            private byte[] readBuffer, writeBuffer;
            private readonly Stream fromStream, toStream;

            public HalfDuplex(Stream fromStream, Stream toStream, byte[] buffer1, byte[] buffer2)
            {
                readBuffer = buffer1;
                writeBuffer = buffer2;
                this.fromStream = fromStream;
                this.toStream = toStream;
            }

            public async Task Run(CancellationToken cancellationToken)
            {
                var bytesRead = await fromStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);

                while (bytesRead != 0)
                {
                    var flip = writeBuffer;
                    writeBuffer = readBuffer;
                    readBuffer = flip;
                    
                    try
                    {
                        var writeTask = toStream.WriteAsync(writeBuffer, 0, bytesRead, cancellationToken);
                        var readTask = fromStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);
                        await writeTask;
                        bytesRead = await readTask;
                    }
                    catch (IOException ex) when (IsExpectedShutdownException(ex))
                    {
                        break;
                    }
                }
            }

            private static bool IsExpectedShutdownException(IOException ex)
            {
                var socketException = ex.InnerException as SocketException;
                if (socketException == null) return false;

                switch (socketException.SocketErrorCode)
                {
                    case SocketError.ConnectionAborted:
                    case SocketError.ConnectionReset: // Happens e.g. when a browser navigates away from a page using SignalR
                        return true;
                    default:
                        return false;
                }
            }
        }


        protected virtual Task<Stream> TryGetClientStream(NetworkStream tcpStream) => Task.FromResult<Stream>(tcpStream);


        public void Dispose()
        {
            disposalSource.Cancel();
            listener.Stop();
        }
    }
}
