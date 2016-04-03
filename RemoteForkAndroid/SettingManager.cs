using Android.App;
using Android.Content;

namespace tv.forkplayer.remotefork {
    public static class SettingManager {
        public const string AppName = "RemoteFork 1.2";
        public const string AppVersion = "1.2f2";
        public const string LastIp = "LastIp";

        public static void SetValue( string key, string value) {
            var prefs = Application.Context.GetSharedPreferences(AppName, FileCreationMode.Private);
            var prefEditor = prefs.Edit();
            prefEditor.PutString(key, value);
            prefEditor.Commit();
        }
        
        public static string GetValue(string key) {
            var prefs = Application.Context.GetSharedPreferences(AppName, FileCreationMode.Private);
            return prefs.GetString(key, string.Empty);
        }
    }
}
