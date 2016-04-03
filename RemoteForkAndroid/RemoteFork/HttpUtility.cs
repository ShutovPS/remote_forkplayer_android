using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Encoding = System.Text.Encoding;

namespace tv.forkplayer.remotefork.server {
    public static class HttpUtility {
        private const string DefaultUserAgent =
            "Mozilla/5.0 (Web0S; Linux/SmartTV) AppleWebKit/537.41 (KHTML, like Gecko) Large Screen WebAppManager Safari/537.41";

        public static async Task<string> GetRequest(string link, Dictionary<string, string> header = null) {
            try {
                using (var httpClient = new HttpClient()) {
                    if (header != null) {
                        foreach (var h in header) {
                            try {
                                httpClient.DefaultRequestHeaders.Add(h.Key, h.Value);
                            } catch (Exception) {
                            }
                        }
                    }
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(DefaultUserAgent);

                    var response = await httpClient.GetAsync(link).ConfigureAwait(false);
                    return await ReadContext(response.Content);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return ex.Message;
            }
        }

        public static async Task<string> PostRequest(string link, Dictionary<string, string> data,
            Dictionary<string, string> header = null) {
            try {
                using (var httpClient = new HttpClient()) {
                    if (header != null) {
                        foreach (var h in header) {
                            try {
                                httpClient.DefaultRequestHeaders.Add(h.Key, h.Value);
                            } catch (Exception) {
                            }
                        }
                    }
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(DefaultUserAgent);

                    HttpContent content = new FormUrlEncodedContent(data);

                    var response = await httpClient.PostAsync(link, content).ConfigureAwait(false);
                    return await ReadContext(response.Content);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return ex.Message;
            }
        }

        private static async Task<string> ReadContext(HttpContent context) {
            var result = await context.ReadAsByteArrayAsync();
            try {
                var encoding = Encoding.GetEncoding(context.Headers.ContentType.CharSet);
                result = Encoding.Convert(encoding, Encoding.Default,
                result);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            return Encoding.Default.GetString(result);
        }
    }
}
