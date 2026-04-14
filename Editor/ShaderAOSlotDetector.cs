using System.Collections.Generic;
using UnityEngine;

namespace net._32ba.EasyAOBaker.Editor
{
    public static class ShaderAOSlotDetector
    {
        public static bool TryApplyAO(Material mat, Texture2D aoTex, EasyAOBaker baker)
        {
            if (baker.targetShader == AOTargetShader.VertexColor)
                return false;

            var detected = baker.targetShader != AOTargetShader.Auto
                ? baker.targetShader
                : ShaderTypeUtil.DetectFromMaterial(mat);

            Debug.Log($"[EasyAOBaker] Shader '{mat.shader.name}' on '{mat.name}' → {detected}");

            switch (detected)
            {
                case AOTargetShader.LilToon:
                    return ApplyToLilToon(mat, aoTex, baker.lilToonSettings);
                case AOTargetShader.Poiyomi:
                    return ApplyToPoiyomi(mat, aoTex, baker.poiyomiSettings);
                case AOTargetShader.ToonStandard:
                case AOTargetShader.StandardLit:
                    return ApplyToStandard(mat, aoTex, baker.standardSettings);
                default:
                    Debug.LogWarning($"[EasyAOBaker] No AO slot found for shader '{mat.shader.name}'.");
                    LogMaterialProperties(mat);
                    return false;
            }
        }

        private static bool ApplyToLilToon(Material mat, Texture2D aoTex, LilToonSettings settings)
        {
            if (!mat.HasProperty("_ShadowBorderMask"))
            {
                Debug.LogWarning($"[EasyAOBaker] lilToon: _ShadowBorderMask not found.");
                LogMaterialProperties(mat);
                return false;
            }

            var existing = mat.GetTexture("_ShadowBorderMask") as Texture2D;
            if (existing != null)
                aoTex = MultiplyTextures(existing, aoTex);
            mat.SetTexture("_ShadowBorderMask", aoTex);

            if (mat.HasProperty("_ShadowAOShift"))
                mat.SetVector("_ShadowAOShift", new Vector4(
                    settings.aoScale1st, settings.aoOffset1st,
                    settings.aoScale2nd, settings.aoOffset2nd));

            if (mat.HasProperty("_ShadowAOShift2"))
                mat.SetVector("_ShadowAOShift2", new Vector4(
                    settings.aoScale3rd, settings.aoOffset3rd, 0, 0));

            if (mat.HasProperty("_ShadowPostAO"))
                mat.SetInt("_ShadowPostAO", settings.postAO ? 1 : 0);

            if (mat.HasProperty("_ShadowBorderMaskLOD"))
                mat.SetFloat("_ShadowBorderMaskLOD", settings.BorderMaskLODRaw);

            Debug.Log($"[EasyAOBaker] lilToon: applied AO (1st:{settings.aoScale1st}/{settings.aoOffset1st}, " +
                $"2nd:{settings.aoScale2nd}/{settings.aoOffset2nd}, 3rd:{settings.aoScale3rd}/{settings.aoOffset3rd}, " +
                $"postAO:{settings.postAO}, LOD:{settings.borderMaskLOD}(raw:{settings.BorderMaskLODRaw}))");
            return true;
        }

        private static bool ApplyToPoiyomi(Material mat, Texture2D aoTex, PoiyomiSettings settings)
        {
            // Poiyomi 8.0+: _LightingAOMaps, 旧バージョン: _LightingAOTex 等
            string[] aoProps = { "_LightingAOMaps", "_LightingAOTex", "_LightDataAOMap", "_AOMap" };

            foreach (var prop in aoProps)
            {
                if (!mat.HasProperty(prop)) continue;

                mat.SetTexture(prop, aoTex);

                // チャンネル別Strength (Poiyomi 8.0+)
                if (mat.HasProperty("_LightDataAOStrengthR"))
                    mat.SetFloat("_LightDataAOStrengthR", settings.aoStrengthR);
                if (mat.HasProperty("_LightDataAOStrengthG"))
                    mat.SetFloat("_LightDataAOStrengthG", settings.aoStrengthG);
                if (mat.HasProperty("_LightDataAOStrengthB"))
                    mat.SetFloat("_LightDataAOStrengthB", settings.aoStrengthB);
                if (mat.HasProperty("_LightDataAOStrengthA"))
                    mat.SetFloat("_LightDataAOStrengthA", settings.aoStrengthA);

                Debug.Log($"[EasyAOBaker] Poiyomi: set {prop} " +
                    $"(R:{settings.aoStrengthR}, G:{settings.aoStrengthG}, " +
                    $"B:{settings.aoStrengthB}, A:{settings.aoStrengthA})");
                return true;
            }

            Debug.LogWarning($"[EasyAOBaker] Poiyomi: no AO slot found.");
            LogMaterialProperties(mat);
            return false;
        }

        private static bool ApplyToStandard(Material mat, Texture2D aoTex, StandardSettings settings)
        {
            if (!mat.HasProperty("_OcclusionMap"))
                return false;

            mat.SetTexture("_OcclusionMap", aoTex);
            if (mat.HasProperty("_OcclusionStrength"))
                mat.SetFloat("_OcclusionStrength", settings.occlusionStrength);

            Debug.Log($"[EasyAOBaker] Standard: set _OcclusionMap (strength={settings.occlusionStrength})");
            return true;
        }

        public static void BakeAOToVertexColors(Mesh mesh, Texture2D aoTex, int channel = 0)
        {
            var uvs = mesh.uv;
            var colors = mesh.colors;
            if (colors.Length == 0)
            {
                colors = new Color[mesh.vertexCount];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = Color.white;
            }

            for (int i = 0; i < mesh.vertexCount; i++)
            {
                float ao = aoTex.GetPixelBilinear(uvs[i].x, uvs[i].y).r;
                colors[i][channel] = ao;
            }

            mesh.colors = colors;
        }

        private static void LogMaterialProperties(Material mat)
        {
            var shader = mat.shader;
            var props = new List<string>();
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                    props.Add(shader.GetPropertyName(i));
            }
            Debug.Log($"[EasyAOBaker] Texture properties on '{shader.name}': {string.Join(", ", props)}");
        }

        private static Texture2D MultiplyTextures(Texture2D a, Texture2D b)
        {
            var readableA = MakeReadable(a);
            var readableB = MakeReadable(b);

            int width = Mathf.Max(readableA.width, readableB.width);
            int height = Mathf.Max(readableA.height, readableB.height);

            var pixelsA = readableA.GetPixels();
            var pixelsB = readableB.GetPixels();
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var output = new Color[width * height];

            for (int i = 0; i < output.Length; i++)
            {
                float u = ((i % width) + 0.5f) / width;
                float v = ((i / width) + 0.5f) / height;
                output[i] = readableA.GetPixelBilinear(u, v) * readableB.GetPixelBilinear(u, v);
            }

            result.SetPixels(output);
            result.Apply();

            if (readableA != a) Object.DestroyImmediate(readableA);
            if (readableB != b) Object.DestroyImmediate(readableB);

            return result;
        }

        private static Texture2D MakeReadable(Texture2D source)
        {
            if (source.isReadable) return source;

            var tmp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default);
            Graphics.Blit(source, tmp);

            var prev = RenderTexture.active;
            RenderTexture.active = tmp;

            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);
            return readable;
        }
    }
}
