using System;
using UnityEngine;

namespace net._32ba.EasyAOBaker
{
    /// <summary>
    /// AOをベイクしたいRendererがあるGameObjectに追加する。
    /// アバター全体のジオメトリを考慮して、他メッシュからの遮蔽も反映される。
    /// </summary>
    [AddComponentMenu("EasyAOBaker/EasyAOBaker")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public class EasyAOBaker : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        public AOBakeMode bakeMode = AOBakeMode.RayCast;

        [Header("Bake Settings")]
        public TextureResolution resolution = TextureResolution._2048;

        [Range(0f, 5f)]
        public float intensity = 1.0f;

        // SSAO固有
        [Range(32, 512)]
        public int sampleCount = 256;

        [Range(0.001f, 2f)]
        public float radius = 0.15f;

        [Range(0f, 0.1f)]
        public float bias = 0.005f;

        [Range(16, 256)]
        public int cameraDirections = 128;

        [Range(0.1f, 10f)]
        public float captureDistance = 3.0f;

        public bool includeAlphaTestedMeshes = true;

        // RayCast固有
        [Range(16, 512)]
        public int rayCount = 128;

        [Range(0.001f, 10f)]
        public float maxRayDistance = 2.0f;

        [Range(0f, 0.01f)]
        public float rayOriginOffset = 0.001f;

        [Tooltip("AO生成マスク（白=生成する、黒=スキップ）。UVと同じ空間")]
        public Texture2D aoMask;

        [Header("Output")]
        public AOTargetShader targetShader = AOTargetShader.Auto;

        [Header("Filter")]
        [Range(0, 5)]
        public int blurIterations = 1;

        [Range(0f, 2f)]
        public float blurRadius = 0.5f;

        // シェーダー別設定
        public LilToonSettings lilToonSettings = new LilToonSettings();
        public PoiyomiSettings poiyomiSettings = new PoiyomiSettings();
        public StandardSettings standardSettings = new StandardSettings();

        public int ResolutionValue => (int)resolution;

        /// <summary>
        /// コンポーネント追加時（Reset）にマテリアルの現在値を読み取って初期値に設定する。
        /// </summary>
        private void Reset()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                return;

            var mat = renderer.sharedMaterials[0];
            if (mat == null) return;

            string shaderName = mat.shader.name;

            if (shaderName.Contains("lilToon") || shaderName.Contains("lil/"))
                InitLilToonFromMaterial(mat);
            else if (shaderName.Contains("poiyomi") || shaderName.Contains("Poiyomi") || shaderName.Contains(".poyi/"))
                InitPoiyomiFromMaterial(mat);
            else if (mat.HasProperty("_OcclusionStrength"))
                standardSettings.occlusionStrength = mat.GetFloat("_OcclusionStrength");
        }

        private void InitLilToonFromMaterial(Material mat)
        {
            if (mat.HasProperty("_ShadowAOShift"))
            {
                var shift = mat.GetVector("_ShadowAOShift");
                lilToonSettings.aoScale1st = shift.x;
                lilToonSettings.aoOffset1st = shift.y;
                lilToonSettings.aoScale2nd = shift.z;
                lilToonSettings.aoOffset2nd = shift.w;
            }

            if (mat.HasProperty("_ShadowAOShift2"))
            {
                var shift2 = mat.GetVector("_ShadowAOShift2");
                lilToonSettings.aoScale3rd = shift2.x;
                lilToonSettings.aoOffset3rd = shift2.y;
            }

            if (mat.HasProperty("_ShadowPostAO"))
                lilToonSettings.postAO = mat.GetInt("_ShadowPostAO") != 0;

            if (mat.HasProperty("_ShadowBorderMaskLOD"))
                lilToonSettings.borderMaskLOD = Mathf.Pow(mat.GetFloat("_ShadowBorderMaskLOD"), 0.25f);
        }

        private void InitPoiyomiFromMaterial(Material mat)
        {
            if (mat.HasProperty("_LightDataAOStrengthR"))
                poiyomiSettings.aoStrengthR = mat.GetFloat("_LightDataAOStrengthR");
            if (mat.HasProperty("_LightDataAOStrengthG"))
                poiyomiSettings.aoStrengthG = mat.GetFloat("_LightDataAOStrengthG");
            if (mat.HasProperty("_LightDataAOStrengthB"))
                poiyomiSettings.aoStrengthB = mat.GetFloat("_LightDataAOStrengthB");
            if (mat.HasProperty("_LightDataAOStrengthA"))
                poiyomiSettings.aoStrengthA = mat.GetFloat("_LightDataAOStrengthA");
        }
    }

    [Serializable]
    public class LilToonSettings
    {
        [Tooltip("1st Shadowへの適用スケール")]
        [Range(0f, 2f)]
        public float aoScale1st = 1.0f;

        [Tooltip("1st Shadowへの適用オフセット")]
        [Range(-1f, 1f)]
        public float aoOffset1st = 0.0f;

        [Tooltip("2nd Shadowへの適用スケール")]
        [Range(0f, 2f)]
        public float aoScale2nd = 1.0f;

        [Tooltip("2nd Shadowへの適用オフセット")]
        [Range(-1f, 1f)]
        public float aoOffset2nd = 0.0f;

        [Tooltip("3rd Shadowへの適用スケール")]
        [Range(0f, 2f)]
        public float aoScale3rd = 1.0f;

        [Tooltip("3rd Shadowへの適用オフセット")]
        [Range(-1f, 1f)]
        public float aoOffset3rd = 0.0f;

        [Tooltip("AO適用後にBorderプロパティを無視する")]
        public bool postAO = false;

        [Tooltip("AO MapのLODレベル（lilToon Inspector表示と同じ値）")]
        [Range(0f, 1f)]
        public float borderMaskLOD = 0.0f;

        /// <summary>lilToonの内部値に変換（表示値の4乗）</summary>
        public float BorderMaskLODRaw => Mathf.Pow(borderMaskLOD, 4.0f);
    }

    [Serializable]
    public class PoiyomiSettings
    {
        [Tooltip("Rチャンネル AO強度")]
        [Range(0f, 1f)]
        public float aoStrengthR = 1.0f;

        [Tooltip("Gチャンネル AO強度")]
        [Range(0f, 1f)]
        public float aoStrengthG = 0.0f;

        [Tooltip("Bチャンネル AO強度")]
        [Range(0f, 1f)]
        public float aoStrengthB = 0.0f;

        [Tooltip("Aチャンネル AO強度")]
        [Range(0f, 1f)]
        public float aoStrengthA = 0.0f;
    }

    [Serializable]
    public class StandardSettings
    {
        [Tooltip("Occlusion強度")]
        [Range(0f, 1f)]
        public float occlusionStrength = 1.0f;
    }

    public enum AOBakeMode
    {
        SSAO,
        RayCast
    }

    public enum TextureResolution
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }

    public enum AOTargetShader
    {
        Auto,
        LilToon,
        Poiyomi,
        ToonStandard,
        StandardLit,
        VertexColor
    }
}
