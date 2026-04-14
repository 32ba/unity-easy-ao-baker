using UnityEditor;
using UnityEngine;

namespace net._32ba.EasyAOBaker.Editor
{
    public static class EasyAOBakerAssetLoader
    {
        public static ComputeShader LoadComputeShader(string name)
        {
            var guids = AssetDatabase.FindAssets($"t:ComputeShader {name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("easy-ao-baker") || path.Contains("EasyAOBaker"))
                    return AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
            }
            Debug.LogError($"[EasyAOBaker] ComputeShader '{name}' not found.");
            return null;
        }
    }
}
