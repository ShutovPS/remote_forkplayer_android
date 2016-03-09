using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace RemoteFork {
    public class HttpProcessor {
        public Hashtable httpHeaders = new Hashtable();

        public string http_method { get; private set; }
        public string http_url { get; private set; }
        public string http_protocol_versionstring { get; private set; }
        public Stream inputStream { get; private set; }
        public StreamWriter outputStream { get; private set; }

        private readonly TcpClient socket;
        private readonly HttpServer server;

        private static int MAX_POST_SIZE = 10485760;
        private const int BUF_SIZE = 4096;

        public HttpProcessor(TcpClient client, HttpServer server) {
            socket = client;
            this.server = server;
        }

        public void Process(object state) {
            inputStream = new BufferedStream(socket.GetStream());
            outputStream = new StreamWriter(new BufferedStream(socket.GetStream()));
            try {
                ParseRequest();
                ReadHeaders();
                if (http_method.Equals("GET")) {
                    HandleGetRequest();
                } else {
                    if (http_method.Equals("POST")) {
                        HandlePostRequest();
                    }
                }
            } catch (Exception ex) {
                Console.Out.WriteLine("Exception: " + ex.Message);
                WriteFailure();
            }
            try {
                outputStream.Flush();
            } catch (Exception ex) {
                Console.Out.WriteLine("Exception: " + ex.Message);
            } finally {
                if (inputStream != null) {
                    inputStream.Close();
                }
                if (outputStream != null) {
                    outputStream.Close();
                }
                if (socket != null) {
                    socket.Close();
                }
                Console.Out.WriteLine("Socked close");
            }
        }

        private void ParseRequest() {
            string text = StreamReadLine(inputStream);
            string[] array = text.Split(' ');
            if (array.Length == 3) {
                http_method = array[0].ToUpper();
                http_url = array[1];
                http_protocol_versionstring = array[2];
                Console.Out.WriteLine("starting: " + text);
            } else {
                throw new Exception("invalid http request line");
            }
        }

        private void ReadHeaders() {
            string text;
            while ((text = StreamReadLine(inputStream)) != null) {
                if (!string.IsNullOrEmpty(text)) {
                    if (text.Contains(":")) {
                        int num = text.IndexOf(':');
                        string key = text.Substring(0, num);
                        string value = text.Remove(0, num + 1).Trim();
                        httpHeaders[key] = value;
                    } else {
                        throw new Exception("invalid http header line: " + text);
                    }
                } else {
                    break;
                }
            }
        }

        private void HandleGetRequest() {
            server.HandleGetRequest(this);
        }

        private void HandlePostRequest() {
            Console.Out.WriteLine("get post data start");
            MemoryStream memoryStream = new MemoryStream();
            if (httpHeaders.ContainsKey("Content-Length")) {
                int num = Convert.ToInt32(httpHeaders["Content-Length"]);
                if (num <= MAX_POST_SIZE) {
                    byte[] buffer = new byte[BUF_SIZE];
                    int i = num;
                    while (i > 0) {
                        Console.Out.WriteLine("starting Read, to_read={0}", i);
                        int num2 = inputStream.Read(buffer, 0, Math.Min(BUF_SIZE, i));
                        Console.Out.WriteLine("read finished, numread={0}", num2);
                        if (num2 == 0) {
                            if (i != 0) {
                                throw new Exception("client disconnected during post");
                            }
                            break;
                        }
                        i -= num2;
                        memoryStream.Write(buffer, 0, num2);
                    }
                    memoryStream.Seek(0L, SeekOrigin.Begin);
                } else {
                    throw new Exception(string.Format("POST Content-Length({0}) too big for this simple server", num));
                }
            }
            Console.Out.WriteLine("get post data end");
            server.HandlePostRequest(this, new StreamReader(memoryStream));
        }

        public void WriteSuccess(string contentType = "text/html") {
            outputStream.WriteLine("HTTP/1.0 200 OK");
            outputStream.WriteLine("Access-Control-Allow-Origin: *");
            outputStream.WriteLine("Content-Type: text/html; charset=UTF-8");
            outputStream.WriteLine("Connection: close");
            outputStream.WriteLine("");
        }

        public void WriteFailure() {
            try {
                outputStream.WriteLine("HTTP/1.0 404 File not found");
                outputStream.WriteLine("Connection: close");
                outputStream.WriteLine("");
            } catch (Exception ex) {
                Console.Out.WriteLine("Exception: " + ex.Message);
            }
        }

        private static string StreamReadLine(Stream inputStream) {
            string text = string.Empty;
            while (true) {
                int num = inputStream.ReadByte();
                if (num != 10) {
                    if (num != 13) {
                        if (num != -1) {
                            text += Convert.ToChar(num).ToString();
                        } else {
                            Thread.Sleep(1);
                        }
                    }
                } else {
                    break;
                }
            }
            return text;
        }
    }
}
