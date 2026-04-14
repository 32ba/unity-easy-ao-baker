using UnityEditor;
using UnityEngine;

namespace net._32ba.EasyAOBaker.Editor
{
    [InitializeOnLoad]
    public static class PlayModeParameterPersistence
    {
        private const string SessionKey = "EasyAOBaker_PlayModeParams_";

        static PlayModeParameterPersistence()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    SaveAllBakerParams();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    RestoreAllBakerParams();
                    break;
            }
        }

        public static void SaveAllBakerParams()
        {
            var bakers = Object.FindObjectsByType<EasyAOBaker>(FindObjectsSortMode.None);
            foreach (var baker in bakers)
                SaveBakerParams(baker);
        }

        private static void SaveBakerParams(EasyAOBaker baker)
        {
            var id = GlobalObjectId.GetGlobalObjectIdSlow(baker);
            string key = SessionKey + id.ToString();
            SessionState.SetString(key, JsonUtility.ToJson(baker));
        }

        private static void RestoreAllBakerParams()
        {
            var bakers = Object.FindObjectsByType<EasyAOBaker>(FindObjectsSortMode.None);
            foreach (var baker in bakers)
                RestoreBakerParams(baker);
        }

        private static void RestoreBakerParams(EasyAOBaker baker)
        {
            var id = GlobalObjectId.GetGlobalObjectIdSlow(baker);
            string key = SessionKey + id.ToString();

            string json = SessionState.GetString(key, "");
            if (string.IsNullOrEmpty(json)) return;

            Undo.RecordObject(baker, "Restore EasyAOBaker Play Mode Parameters");
            JsonUtility.FromJsonOverwrite(json, baker);
            EditorUtility.SetDirty(baker);

            SessionState.EraseString(key);
            Debug.Log($"[EasyAOBaker] Play Mode parameters restored for '{baker.gameObject.name}'.");
        }
    }
}
