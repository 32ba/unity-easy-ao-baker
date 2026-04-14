using System;
using System.Text.RegularExpressions;

namespace net._32ba.EasyAOBaker.Editor
{
    /// <summary>
    /// セマンティックバージョン文字列の比較ユーティリティ。
    /// "v1.2.3" / "1.2.3-beta" / "1.2" 等の揺れに対応。
    /// </summary>
    public static class VersionUtility
    {
        public static bool IsNewerVersion(string currentVersion, string latestVersion)
        {
            if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(latestVersion))
                return false;

            try
            {
                return ParseVersion(latestVersion) > ParseVersion(currentVersion);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    $"[EasyAOBaker] Failed to compare versions '{currentVersion}' and '{latestVersion}': {ex.Message}");
                return false;
            }
        }

        private static Version ParseVersion(string versionString)
        {
            string clean = versionString.TrimStart('v', 'V');

            var m = Regex.Match(clean, @"^(\d+)\.(\d+)\.(\d+)");
            if (m.Success)
            {
                return new Version(
                    int.Parse(m.Groups[1].Value),
                    int.Parse(m.Groups[2].Value),
                    int.Parse(m.Groups[3].Value));
            }

            m = Regex.Match(clean, @"^(\d+)\.(\d+)");
            if (m.Success)
            {
                return new Version(
                    int.Parse(m.Groups[1].Value),
                    int.Parse(m.Groups[2].Value),
                    0);
            }

            return new Version(clean);
        }

        public static string FormatVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return "Unknown";
            return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : "v" + version;
        }
    }
}
