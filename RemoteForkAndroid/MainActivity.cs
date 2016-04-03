using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Java.Lang;
using RemoteForkAndroid;
using tv.forkplayer.remotefork.server;
using Exception = System.Exception;
using Uri = Android.Net.Uri;

namespace tv.forkplayer.remotefork {
    [Activity(Label = "RemoteFork 1.2", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity {
        private static HttpServer httpServer;
        private static Thread thread;
        private static string logs;

        Button bStartServer, bStopServer, bLoadPlaylist;
        EditText etLogs;
        TextView tvStatus;
        Spinner sIps;

        protected override void OnCreate(Bundle bundle) {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            bStartServer = FindViewById<Button>(Resource.Id.bStartServer);
            bStopServer = FindViewById<Button>(Resource.Id.bStopServer);
            bLoadPlaylist = FindViewById<Button>(Resource.Id.bLoadPlaylist);
            etLogs = FindViewById<EditText>(Resource.Id.etLogs);
            tvStatus = FindViewById<TextView>(Resource.Id.tvStatusServer);
            sIps = FindViewById<Spinner>(Resource.Id.sIps);

            bStartServer.Click += StartServerOnClick;
            bStopServer.Click += StopServerOnClick;
            bLoadPlaylist.Click += LoadPlaylistOnClick;

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

                if (httpServer == null) {
                    bStartServer.PerformClick();
                } else {
                    etLogs.Text = logs;
                    if (httpServer != null && !httpServer.IsWork) {
                        tvStatus.Text = GetString(Resource.String.StartStatus);
                        bStartServer.Enabled = false;
                        bStopServer.Enabled = true;
                    }
                }
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data) {
            base.OnActivityResult(requestCode, resultCode, data);
            if ((requestCode == Resource.Id.bLoadPlaylist) && (resultCode == Result.Ok) && (data != null)) {
                Uri uri = data.Data;
                var text = File.ReadAllText(uri.Path);

                if (text.Length < 102401 &&
                    (text.Contains("EXTM3U") || text.Contains("<title>") || text.Contains("http://"))) {
                    string fileName = Path.GetFileName(uri.Path);
                    foreach (var device in MyHttpServer.Devices) {
                        string url = "http://forkplayer.tv/remote/index.php?do=uploadfile&fname=" +
                                     fileName + "&initial=" + device;

                        var data1 = new Dictionary<string, string> {{"text", text}};
                        string text2 = HttpUtility.PostRequest(url, data1).Result;
                    }

                    ShowAlert(GetString(Resource.String.LoadedPlaylist));
                } else {
                    ShowAlert(GetString(Resource.String.PlaylistBadFormat));
                }
            }
        }

        private async void StartServerOnClick(object sender, EventArgs e) {
            try {
                WriteLine(GetString(Resource.String.StartingStatus));
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

                    WriteLine(GetString(Resource.String.StartStatus));
                    tvStatus.Text = GetString(Resource.String.StartStatus);
                    bStartServer.Enabled = false;
                    bStopServer.Enabled = true;
                }
            } catch (Exception ex) {
                Console.Out.WriteLine("Exception: " + ex.Message);
                WriteLine(GetString(Resource.String.ErrorStart));
            }
        }

        private void StopServerOnClick(object sender, EventArgs e) {
            try {
                WriteLine(GetString(Resource.String.StopingStatus));
                tvStatus.Text = GetString(Resource.String.StopingStatus);
                if (httpServer != null) {
                    httpServer.Stop();
                }
                WriteLine(GetString(Resource.String.StopStatus));
                tvStatus.Text = GetString(Resource.String.StopStatus);
                bStartServer.Enabled = true;
                bStopServer.Enabled = false;
            } catch (Exception ex) {
                Console.Out.WriteLine("Exception: " + ex.Message);
                WriteLine(GetString(Resource.String.ErrorStop));
            }
        }

        private void LoadPlaylistOnClick(object sender, EventArgs eventArgs) {
            if (MyHttpServer.Devices.Count > 0) {
                Intent = new Intent();
                Intent.SetType("*/m3u");
                Intent.SetAction(Intent.ActionGetContent);
                StartActivityForResult(Intent.CreateChooser(Intent, "Select Playlist"), Resource.Id.bLoadPlaylist);
            } else {
                ShowAlert(GetString(Resource.String.DevicesNotFound));
            }
        }

        public void ShowAlert(string str) {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle(str);
            alert.SetPositiveButton("OK", (senderAlert, args) => {
            });

            RunOnUiThread(() => {
                alert.Show();
            });
        }

        public void WriteLine(string text, bool save = true) {
            etLogs.Text = text + "\r\n" + etLogs.Text;
            if (save) {
                logs = etLogs.Text;
            }
        }
    }
}
