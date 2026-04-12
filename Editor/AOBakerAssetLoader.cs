using UnityEditor;
using UnityEngine;

namespace net._32ba.AOBaker.Editor
{
    public static class AOBakerAssetLoader
    {
        public static ComputeShader LoadComputeShader(string name)
        {
            var guids = AssetDatabase.FindAssets($"t:ComputeShader {name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("ao-baker") || path.Contains("AOBaker"))
                    return AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
            }
            Debug.LogError($"[AO Baker] ComputeShader '{name}' not found.");
            return null;
        }
    }
}
