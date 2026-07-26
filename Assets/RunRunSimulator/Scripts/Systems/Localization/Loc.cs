using UnityEngine.Localization.Settings;

namespace MoriMonchiSimulator
{
    public static class Loc
    {
        public const string TableName = "Strings";

        public static System.Globalization.CultureInfo Culture
        {
            get
            {
                var locale = LocalizationSettings.SelectedLocale;
                var culture = locale != null ? locale.Identifier.CultureInfo : null;
                return culture ?? System.Globalization.CultureInfo.CurrentCulture;
            }
        }

        const string LocalePrefKey = "mm_locale";

        public static string CurrentCode
        {
            get
            {
                var locale = LocalizationSettings.SelectedLocale;
                return locale != null ? locale.Identifier.Code : "";
            }
        }

        public static void SetLocale(string code)
        {
            var locales = LocalizationSettings.AvailableLocales;
            var locale = locales != null ? locales.GetLocale(code) : null;
            if (locale == null) return;
            LocalizationSettings.SelectedLocale = locale;
            UnityEngine.PlayerPrefs.SetString(LocalePrefKey, code);
            UnityEngine.PlayerPrefs.Save();
        }

        public static void ApplySavedLocale()
        {
            var code = UnityEngine.PlayerPrefs.GetString(LocalePrefKey, "");
            if (!string.IsNullOrEmpty(code) && code != CurrentCode) SetLocale(code);
        }

        public static string Tr(string key)
        {
            var table = LocalizationSettings.StringDatabase.GetTable(TableName);
            var entry = table != null ? table.GetEntry(key) : null;
            return entry != null ? entry.GetLocalizedString() : key;
        }

        public static string Tr(string key, params object[] args)
        {
            var table = LocalizationSettings.StringDatabase.GetTable(TableName);
            var entry = table != null ? table.GetEntry(key) : null;
            return entry != null ? entry.GetLocalizedString(args) : key;
        }
    }
}
