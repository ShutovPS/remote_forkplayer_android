using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Encoding = System.Text.Encoding;

namespace RemoteFork {
    public static class HttpUtility {
        private const string DefaultUserAgent =
            "Mozilla/5.0 (Web0S; Linux/SmartTV) AppleWebKit/537.41 (KHTML, like Gecko) Large Screen WebAppManager Safari/537.41";

        private static readonly string[] ExceptionsHeader = {
            "Content-Type", "Connection"
        };

        public static async Task<string> GetRequest(string link, Dictionary<string, string> header = null) {
            try {
                using (var httpClient = new WebClient()) {
                    if (header != null) {
                        AddToHeader(httpClient, header);
                    } else {
                        httpClient.Headers.Add("User-Agent", DefaultUserAgent);
                    }

                    var response = await httpClient.DownloadDataTaskAsync(new Uri(link));
                    return ReadContext(response);
                }
            } catch (Exception ex) {
                Console.Out.WriteLine(ex.Message);
                return ex.Message;
            }
        }

        public static async Task<string> PostRequest(string link, Dictionary<string, string> data,
            Dictionary<string, string> header = null) {
            try {
                using (var httpClient = new WebClient()) {
                    if (header != null) {
                        AddToHeader(httpClient, header);
                    } else {
                        httpClient.Headers.Add("User-Agent", DefaultUserAgent);
                    }

                    var content = new NameValueCollection();
                    foreach (var d in data) {
                        content.Add(d.Key, d.Value);
                    }

                    var response = await httpClient.UploadValuesTaskAsync(new Uri(link), content).ConfigureAwait(false);
                    return ReadContext(response);
                }
            } catch (Exception ex) {
                Console.Out.WriteLine("Exception: " + ex.Message);
                return ex.Message;
            }
        }

        private static void AddToHeader(WebClient httpClient, Dictionary<string, string> header) {
            foreach (var h in header) {
                bool add = ExceptionsHeader.All(s => !Regex.IsMatch(h.Key, s, RegexOptions.IgnoreCase));
                if (add) {
                    httpClient.Headers.Add(h.Key, h.Value);
                }
            }
        }

        private static string ReadContext(byte[] context) {
            try {
                return Encoding.Default.GetString(context);
            } catch (Exception) {
                try {
                    return Encoding.UTF8.GetString(context);
                } catch (Exception) {
                    try {
                        return Encoding.ASCII.GetString(context);
                    } catch (Exception) {
                        try {
                            return Encoding.Unicode.GetString(context);
                        } catch (Exception) {
                            return string.Empty;
                        }
                    }
                }
            }
        }
    }
}
