using UnityEditor;
using UnityEngine;

namespace net._32ba.AOBaker.Editor
{
    [InitializeOnLoad]
    public static class PlayModeParameterPersistence
    {
        private const string SessionKey = "AOBaker_PlayModeParams_";

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
            var bakers = Object.FindObjectsByType<SSAOBaker>(FindObjectsSortMode.None);
            foreach (var baker in bakers)
                SaveBakerParams(baker);
        }

        private static void SaveBakerParams(SSAOBaker baker)
        {
            var id = GlobalObjectId.GetGlobalObjectIdSlow(baker);
            string key = SessionKey + id.ToString();
            SessionState.SetString(key, JsonUtility.ToJson(baker));
        }

        private static void RestoreAllBakerParams()
        {
            var bakers = Object.FindObjectsByType<SSAOBaker>(FindObjectsSortMode.None);
            foreach (var baker in bakers)
                RestoreBakerParams(baker);
        }

        private static void RestoreBakerParams(SSAOBaker baker)
        {
            var id = GlobalObjectId.GetGlobalObjectIdSlow(baker);
            string key = SessionKey + id.ToString();

            string json = SessionState.GetString(key, "");
            if (string.IsNullOrEmpty(json)) return;

            Undo.RecordObject(baker, "Restore AO Baker Play Mode Parameters");
            JsonUtility.FromJsonOverwrite(json, baker);
            EditorUtility.SetDirty(baker);

            SessionState.EraseString(key);
            Debug.Log($"[AO Baker] Play Mode parameters restored for '{baker.gameObject.name}'.");
        }
    }
}
