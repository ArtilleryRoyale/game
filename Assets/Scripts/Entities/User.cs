using UnityEngine;

namespace CC
{
    public class User
    {
        private const string PLAYER_PREFS_USERNAME = "username";
        private const string PLAYER_PREFS_UID = "uid";
        private const string PLAYER_PREFS_MUSIC_VOLUME = "vol_music";
        private const string PLAYER_PREFS_SFX_VOLUME = "vol_sfx";
        private const string PLAYER_PREFS_CTK_RULES = "popup_ctk_rules";

        /// <summary>
        /// Return false if the popup never showed before and update the flag so it won't show next time
        /// </summary>
        public static bool HasSeenPopup_CTKRules()
        {
            bool seen = PlayerPrefs.GetInt(PLAYER_PREFS_CTK_RULES, 0) != 0;
            if (seen) return true;
            PlayerPrefs.SetInt(PLAYER_PREFS_CTK_RULES, 1);
            return false;
        }

        public static int GetMusicVolume()
        {
            return PlayerPrefs.GetInt(PLAYER_PREFS_MUSIC_VOLUME, 100);
        }

        public static void SetMusicVolume(int v)
        {
            PlayerPrefs.SetInt(PLAYER_PREFS_MUSIC_VOLUME, v);
        }

        public static int GetSfxVolume()
        {
            return PlayerPrefs.GetInt(PLAYER_PREFS_SFX_VOLUME, 100);
        }

        public static void SetSfxVolume(int v)
        {
            PlayerPrefs.SetInt(PLAYER_PREFS_SFX_VOLUME, v);
        }

        public static bool HasUsername()
        {
            return !string.IsNullOrEmpty(GetUsername());
        }

        public static string GetUsername()
        {
            return PlayerPrefs.GetString(PLAYER_PREFS_USERNAME, null);
        }

        public static void SetUsername(string username)
        {
            PlayerPrefs.SetString(PLAYER_PREFS_USERNAME, username);
        }

        public static string GetUID()
        {
            string uid = PlayerPrefs.GetString(PLAYER_PREFS_UID, null);
            if (string.IsNullOrEmpty(uid)) {
                uid = NetworkObject.NetworkIdentifierHuman();
                PlayerPrefs.SetString(PLAYER_PREFS_UID, uid);
            }
            return uid;
        }

        public static string GetFullIdentifier()
        {
            if (!HasUsername()) {
                Log.Critical("User", "Call to GetFullIdentifier() but no username is set");
            }
            return GetUsername() + "@" + GetUID();
        }
    }
}
