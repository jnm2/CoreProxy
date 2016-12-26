using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace jnm2.CoreProxy.Proxies
{
    public class TcpProxyService : IProxyService
    {
        protected readonly IPEndPoint To;
        protected readonly Action<string> Log;
        protected readonly TcpListener Listener;
        protected readonly CancellationTokenSource DisposalSource = new CancellationTokenSource();

        public TcpProxyService(IPEndPoint from, IPEndPoint to, Action<string> log)
        {
            To = to;
            Log = log;
            Listener = new TcpListener(from);
        }

        public void Start()
        {
            Listener.Start();
            SubscribeToNextConnection();
            Log.Invoke(StartLogMessage());
        }

        protected virtual string StartLogMessage() => $"Proxying all TCP traffic from {Listener.LocalEndpoint} to {To}.";

        private void SubscribeToNextConnection()
        {
            while (true)
            {
                try
                {
                    Listener.AcceptTcpClientAsync().ContinueWith(ClientConnected, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    return;
                }
                catch (InvalidOperationException) when (DisposalSource.IsCancellationRequested)
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
                catch (ObjectDisposedException) when (DisposalSource.IsCancellationRequested) // Handles race condition
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
                        if (DisposalSource.IsCancellationRequested) return;
                        await serverConnection.ConnectAsync(To.Address, To.Port); // TODO: possibly pool?

                        using (var serverStream = serverConnection.GetStream())
                        {
                            try
                            {
                                await Task.WhenAll(
                                    new HalfDuplex(clientStream, serverStream, new byte[4096], new byte[4096]).Run(DisposalSource.Token), // TODO: Pool buffers
                                    new HalfDuplex(serverStream, clientStream, new byte[4096], new byte[4096]).Run(DisposalSource.Token));
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
                int bytesRead;

                while (true)
                {
                    try
                    {
                        bytesRead = await fromStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);
                        break;
                    }
                    catch (IOException ex) when (IsExpectedShutdownException(ex))
                    {
                        return;
                    }
                }

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


        public virtual void Dispose()
        {
            DisposalSource.Cancel();
            Listener.Stop();
        }
    }
}
