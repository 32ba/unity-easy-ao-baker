using UnityEditor;
using UnityEngine;

namespace net._32ba.EasyAOBaker.Editor
{
    /// <summary>
    /// Inspector UI の多言語対応。翻訳データは Translations.Entries から引く。
    /// EditorPrefs に選択言語を保存する。
    /// </summary>
    internal static class L
    {
        public enum Lang { English, 日本語, 中文, 한국어 }

        private const string PrefKey = "net.32ba.EasyAOBaker.Lang";
        private static Lang? _cached;

        public static Lang Current
        {
            get
            {
                if (_cached.HasValue) return _cached.Value;
                var stored = EditorPrefs.GetString(PrefKey, "");
                if (System.Enum.TryParse(stored, out Lang parsed))
                    _cached = parsed;
                else
                    _cached = DetectFromSystem();
                return _cached.Value;
            }
            set
            {
                _cached = value;
                EditorPrefs.SetString(PrefKey, value.ToString());
            }
        }

        private static Lang DetectFromSystem()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Japanese: return Lang.日本語;
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                case SystemLanguage.Chinese: return Lang.中文;
                case SystemLanguage.Korean: return Lang.한국어;
                default: return Lang.English;
            }
        }

        public static string Tr(string key)
        {
            if (!Translations.Entries.TryGetValue(key, out var t))
                return key;
            switch (Current)
            {
                case Lang.日本語: return t.ja;
                case Lang.中文: return t.zh;
                case Lang.한국어: return t.ko;
                default: return t.en;
            }
        }

        public static string Format(string key, params object[] args) => string.Format(Tr(key), args);

        /// <summary>
        /// ラベルキーから GUIContent を作る。"キー.tooltip" が存在すれば tooltip として自動紐付け。
        /// </summary>
        public static GUIContent G(string labelKey)
        {
            var tooltipKey = labelKey + ".tooltip";
            var tooltip = Translations.Entries.ContainsKey(tooltipKey) ? Tr(tooltipKey) : "";
            return new GUIContent(Tr(labelKey), tooltip);
        }

        /// <summary>
        /// tooltip キーを明示指定（複数のラベルで tooltip を共有したいとき）。
        /// </summary>
        public static GUIContent G(string labelKey, string tooltipKey)
            => new GUIContent(Tr(labelKey), Tr(tooltipKey));

        public static void DrawLanguageSwitcher()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var newLang = (Lang)EditorGUILayout.EnumPopup(Current, GUILayout.Width(100));
                if (newLang != Current) Current = newLang;
            }
        }
    }
}
