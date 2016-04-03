using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace tv.forkplayer.remotefork.server {
    public abstract class HttpServer {
        private readonly TcpListener listener;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public bool IsWork { get; private set; }

        protected HttpServer(IPAddress ip, int port) {
            listener = new TcpListener(new IPEndPoint(ip, port));
            listener.Start();
        }

        public async void Listen() {
            IsWork = true;
            while (!cts.IsCancellationRequested) {
                try {
                    var client = await listener.AcceptTcpClientAsync();
                    var processor = new HttpProcessor(client, this);

                    ThreadPool.QueueUserWorkItem(processor.Process);
                } catch (Exception) {
                    Console.WriteLine("Stop");
                }
            }
            IsWork = false;
        }

        public void Stop() {
            cts.Cancel();
            if (listener != null) {
                listener.Stop();
            }
        }

        public abstract void HandleGetRequest(HttpProcessor processor);

        public abstract void HandlePostRequest(HttpProcessor processor, StreamReader inputData);
    }
}
