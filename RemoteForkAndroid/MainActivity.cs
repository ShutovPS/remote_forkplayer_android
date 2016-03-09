using System;
using System.Net;
using Android.App;
using Android.Widget;
using Android.OS;
using Java.Lang;
using RemoteFork;
using Exception = System.Exception;

namespace RemoteForkAndroid {
    [Activity(Label = "RemoteFork 1.2", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity {
        private HttpServer httpServer;
        private Thread thread;

        Button bStartServer, bStopServer;
        EditText etLogs;
        TextView tvStatus;
        Spinner sIps;

        protected override void OnCreate(Bundle bundle) {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            bStartServer = FindViewById<Button>(Resource.Id.bStartServer);
            bStopServer = FindViewById<Button>(Resource.Id.bStopServer);
            etLogs = FindViewById<EditText>(Resource.Id.etLogs);
            tvStatus = FindViewById<TextView>(Resource.Id.tvStatusServer);
            sIps = FindViewById<Spinner>(Resource.Id.sIps);

            bStartServer.Click += StartServer;
            bStopServer.Click += StopServer;

            var ips = Tools.GetIPAddresses();
            if (ips.Length > 0) {
                var adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem);

                foreach (var ipAddress in ips) {
                    adapter.Add(ipAddress.ToString());
                }
                sIps.Adapter = adapter;

                string ip = SettingManager.GetValue(SettingManager.LastIp);
                if (string.IsNullOrEmpty(ip)) {
                    ip = ips[0].ToString();
                }
                sIps.SetSelection(adapter.GetPosition(ip));

                bStartServer.PerformClick();
            }
        }

        private async void StartServer(object sender, EventArgs e) {
            try {
                etLogs.Text += GetString(Resource.String.StartingStatus) + "\r\n";
                tvStatus.Text = GetString(Resource.String.StartingStatus);

                IPAddress ip;
                if (IPAddress.TryParse(sIps.SelectedItem.ToString(), out ip)) {
                    httpServer = new MyHttpServer(ip, 8028);
                    thread = new Thread(httpServer.Listen);
                    thread.Start();

                    await
                        HttpUtility.GetRequest(
                            string.Format(
                                "http://getlist2.obovse.ru/remote/index.php?v={0}&appl=android&do=list&localip={1}:{2}",
                                SettingManager.AppVersion,
                                ip, 8028));

                    SettingManager.SetValue(SettingManager.LastIp, ip.ToString());

                    etLogs.Text += GetString(Resource.String.StartStatus) + "\r\n";
                    tvStatus.Text = GetString(Resource.String.StartStatus);
                    bStartServer.Enabled = false;
                    bStopServer.Enabled = true;
                }
            } catch (Exception ex) {
                Console.Out.WriteLine("Exception: " + ex.Message);
                etLogs.Text += GetString(Resource.String.ErrorStart) + "\r\n";
            }
        }

        private void StopServer(object sender, EventArgs e) {
            try {
                etLogs.Text += GetString(Resource.String.StopingStatus) + "\r\n";
                tvStatus.Text = GetString(Resource.String.StopingStatus);
                if (httpServer != null) {
                    httpServer.Stop();
                }
                etLogs.Text += GetString(Resource.String.StopStatus) + "\r\n";
                tvStatus.Text = GetString(Resource.String.StopStatus);
                bStartServer.Enabled = true;
                bStopServer.Enabled = false;
            } catch (Exception ex) {
                Console.Out.WriteLine("Exception: " + ex.Message);
                etLogs.Text += GetString(Resource.String.ErrorStop) + "\r\n";
            }
        }
    }
}
