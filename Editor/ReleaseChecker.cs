using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace net._32ba.EasyAOBaker.Editor
{
    /// <summary>
    /// 起動時・手動要求時に VPM API から最新バージョンを確認し、
    /// 現在バージョンより新しいものがあれば HasNewVersion を true にする。
    /// 24時間に1回の頻度で自動チェック。
    /// </summary>
    [InitializeOnLoad]
    public static class ReleaseChecker
    {
        private const string PackageId = "net.32ba.easy-ao-baker";
        private const string ReleasePageUrl = "https://github.com/32ba/unity-easy-ao-baker/releases";
        private const string LastCheckKey = "net.32ba.EasyAOBaker.LastVersionCheck";
        private const double CheckIntervalHours = 24.0;

        private static readonly VpmApiClient Api = new VpmApiClient(PackageId);

        public static string LatestVersion { get; private set; }
        public static bool HasNewVersion { get; private set; }
        public static bool IsChecking { get; private set; }
        public static string CheckError { get; private set; }

        public static event Action OnUpdateCheckCompleted;

        static ReleaseChecker()
        {
            EditorApplication.delayCall += () => CheckForUpdates();
        }

        public static void CheckForUpdates(bool forceCheck = false)
        {
            if (!forceCheck && !ShouldCheckForUpdates()) return;

            IsChecking = true;
            HasNewVersion = false;
            CheckError = null;
            OnUpdateCheckCompleted?.Invoke();

            EditorCoroutine.Start(CheckRoutine());
        }

        private static IEnumerator CheckRoutine()
        {
            yield return Api.GetLatestVersionCoroutine(HandleSuccess, HandleError);
        }

        private static void HandleSuccess(string latest)
        {
            IsChecking = false;

            if (string.IsNullOrEmpty(latest))
            {
                CheckError = "Empty version response";
                OnUpdateCheckCompleted?.Invoke();
                return;
            }

            LatestVersion = latest;
            string current = GetCurrentVersion();
            EditorPrefs.SetString(LastCheckKey, DateTime.Now.ToBinary().ToString());

            if (VersionUtility.IsNewerVersion(current, latest))
            {
                HasNewVersion = true;
                Debug.Log($"[EasyAOBaker] New version available: {current} → {latest}");
            }
            else
            {
                Debug.Log($"[EasyAOBaker] Package is up to date: {current}");
            }

            OnUpdateCheckCompleted?.Invoke();
        }

        private static void HandleError(string error)
        {
            IsChecking = false;
            CheckError = error;
            Debug.LogWarning($"[EasyAOBaker] Update check failed: {error}");
            OnUpdateCheckCompleted?.Invoke();
        }

        public static void OpenReleasePage() => Application.OpenURL(ReleasePageUrl);

        public static string GetCurrentVersion()
        {
            try
            {
                var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(ReleaseChecker).Assembly);
                if (pkg != null && !string.IsNullOrEmpty(pkg.version))
                    return pkg.version;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EasyAOBaker] Failed to read package version: {ex.Message}");
            }
            return "0.0.0";
        }

        private static bool ShouldCheckForUpdates()
        {
            string stored = EditorPrefs.GetString(LastCheckKey, "");
            if (string.IsNullOrEmpty(stored)) return true;

            if (long.TryParse(stored, out long binary))
            {
                var last = DateTime.FromBinary(binary);
                return (DateTime.Now - last).TotalHours >= CheckIntervalHours;
            }
            return true;
        }
    }

    /// <summary>
    /// Editor 用の軽量コルーチンランナー。Unity の EditorCoroutines パッケージ非依存。
    /// </summary>
    internal class EditorCoroutine
    {
        private readonly IEnumerator _routine;
        private IEnumerator _nested;

        public static EditorCoroutine Start(IEnumerator routine)
        {
            var co = new EditorCoroutine(routine);
            EditorApplication.update += co.Update;
            return co;
        }

        private EditorCoroutine(IEnumerator routine) => _routine = routine;

        private void Update()
        {
            if (_nested != null)
            {
                if (_nested.MoveNext()) return;
                _nested = null;
            }

            if (!_routine.MoveNext())
            {
                EditorApplication.update -= Update;
                return;
            }

            if (_routine.Current is IEnumerator nestedRoutine)
                _nested = nestedRoutine;
        }
    }
}
