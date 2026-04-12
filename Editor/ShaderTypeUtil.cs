using UnityEngine;

namespace net._32ba.AOBaker.Editor
{
    public static class ShaderTypeUtil
    {
        public static AOTargetShader DetectFromMaterial(Material mat)
        {
            if (mat == null) return AOTargetShader.Auto;

            string shaderName = mat.shader.name;

            if (shaderName.Contains("lilToon") || shaderName.Contains("lil/"))
                return AOTargetShader.LilToon;
            if (shaderName.Contains("poiyomi") || shaderName.Contains("Poiyomi") || shaderName.Contains(".poyi/"))
                return AOTargetShader.Poiyomi;
            if (shaderName.Contains("ToonStandard") || shaderName.Contains("Toon Standard") || shaderName.Contains("VRChat/Mobile"))
                return AOTargetShader.ToonStandard;
            if (shaderName.Contains("Sunao"))
                return AOTargetShader.StandardLit;
            if (mat.HasProperty("_OcclusionMap"))
                return AOTargetShader.StandardLit;

            return AOTargetShader.Auto;
        }
    }
}
