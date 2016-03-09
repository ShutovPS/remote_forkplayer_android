using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace RemoteFork {
    public class MyHttpServer : HttpServer {
        public MyHttpServer(IPAddress ip, int port) : base(ip, port) {
        }

        private string TestRequest() {
            string result =
                "<html><h1>ForkPlayer DLNA Work!</h1><br><b>Server by Visual Studio 2015</b></html>";
            return result;
        }

        private string ParseCurlRequest(string httpUrl) {
            string result = string.Empty;

            string url = Regex.Match(httpUrl, "(?:\")(.*?)(?=\")").Groups[1].Value;
            Dictionary<string, string> header = new Dictionary<string, string>();
            MatchCollection matches = Regex.Matches(httpUrl, "(?:-H\\s\")(.*?)(?=\")");
            foreach (Match match in matches) {
                var groups = match.Groups;
                if (groups.Count > 1) {
                    var value = groups[1].Value;
                    if (value.Contains(": ")) {
                        header.Add(value.Remove(value.IndexOf(": ")), value.Substring(value.IndexOf(": ") + 2));
                    }
                }
            }
            if (httpUrl.Contains("--data")) {
                string dataString = Regex.Match(httpUrl, "(?:--data\\s\")(.*?)(?=\")").Groups[1].Value;
                var dataArray = dataString.Split('&');
                Dictionary<string, string> data = new Dictionary<string, string>();
                foreach (var value in dataArray) {
                    data.Add(value.Remove(value.IndexOf("=")), value.Substring(value.IndexOf("=") + 1));
                }
                result =
                    HttpUtility.PostRequest(url, data, header).Result;
            } else {
                result =
                    HttpUtility.GetRequest(url, header).Result;
            }

            return result;
        }

        private string ParserlinkRequest(string httpUrl) {
            string result = string.Empty;

            string text = WebUtility.UrlDecode(httpUrl.Substring(12));
            string[] array = text.Split('|');
            Console.Out.WriteLine("parse0 " + array[0]);

            string response = array[0].StartsWith("curl")
                ? ParseCurlRequest(array[0])
                : HttpUtility.GetRequest(array[0]).Result;

            if (array.Length == 1) {
                result = response;
            } else {
                if (!array[1].Contains(".*?")) {
                    if (string.IsNullOrEmpty(array[1]) && string.IsNullOrEmpty(array[2])) {
                        result = response;
                    } else {
                        int num1 = response.IndexOf(array[1]);
                        if (num1 == -1) {
                            result = string.Empty;
                        } else {
                            num1 += array[1].Length;
                            int num2 = response.IndexOf(array[2], num1);
                            result = num2 == -1
                                ? string.Empty
                                : response.Substring(num1, num2 - num1);
                        }
                    }
                } else {
                    Console.Out.WriteLine(array[1] + "(.*?)" + array[2]);
                    string pattern = array[1] + "(.*?)" + array[2];
                    Regex regex = new Regex(pattern, RegexOptions.Multiline);
                    Match match = regex.Match(response);
                    if (match.Success) {
                        result = match.Groups[1].Captures[0].ToString();
                    }
                }
            }

            return result;
        }

        public override void HandleGetRequest(HttpProcessor processor) {
            string httpUrl = WebUtility.UrlDecode(processor.http_url);
            Console.Out.WriteLine(httpUrl.Substring(1));

            string result = string.Empty;

            if (httpUrl.StartsWith("/parserlink")) {
                result = ParserlinkRequest(httpUrl);
            } else {
                if (httpUrl.StartsWith("/test")) {
                    result = TestRequest();
                }
            }

            Console.Out.WriteLine("request: {0}", processor.http_url);
            processor.WriteSuccess("text/html");
            processor.outputStream.WriteLine(result);
            Console.Out.WriteLine("List end");
        }

        public override void HandlePostRequest(HttpProcessor processor, StreamReader inputData) {
            Console.Out.WriteLine("POST request: {0}", processor.http_url);
            string arg = inputData.ReadToEnd();
            processor.WriteSuccess("text/html");
            processor.outputStream.WriteLine("<html><body><h1>test server</h1>");
            processor.outputStream.WriteLine("<a href=/test>return</a><p>");
            processor.outputStream.WriteLine("postbody: <pre>{0}</pre>", arg);
        }
    }
}
