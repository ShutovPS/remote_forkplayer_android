using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace RemoteFork {
    public static class Tools {
        public static IPAddress[] GetIPAddresses(string hostname = "") {
            var hostEntry = Dns.GetHostEntry(hostname);
            var addressList = hostEntry.AddressList;
            List<IPAddress> result = new List<IPAddress>();
            for (int i = 0; i < addressList.Length; i++) {
                var iPAddress = addressList[i];
                bool flag = iPAddress.AddressFamily == AddressFamily.InterNetwork;
                if (flag) {
                    result.Add(iPAddress);
                }
            }
            return result.ToArray();
        }
    }
}
