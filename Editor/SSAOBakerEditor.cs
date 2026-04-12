using UnityEditor;
using UnityEngine;

namespace net._32ba.AOBaker.Editor
{
    [CustomEditor(typeof(SSAOBaker))]
    public class SSAOBakerEditor : UnityEditor.Editor
    {
        private static readonly string[] ResolutionLabels = { "256", "512", "1024", "2048", "4096" };
        private static readonly int[] ResolutionValues = { 256, 512, 1024, 2048, 4096 };

        private static readonly GUIContent ScaleLabel = new GUIContent("  Scale");
        private static readonly GUIContent OffsetLabel = new GUIContent("  Offset");
        private static readonly GUIContent PostAOLabel = new GUIContent("Post AO");
        private static readonly GUIContent BorderMaskLODLabel = new GUIContent("Border Mask LOD");
        private static readonly GUIContent OcclusionStrengthLabel = new GUIContent("Occlusion Strength");
        private static readonly GUIContent PoiStrengthRLabel = new GUIContent("R Strength");
        private static readonly GUIContent PoiStrengthGLabel = new GUIContent("G Strength");
        private static readonly GUIContent PoiStrengthBLabel = new GUIContent("B Strength");
        private static readonly GUIContent PoiStrengthALabel = new GUIContent("A Strength");

        private bool _shaderSettingsFoldout = true;

        // SSAO固有フィールド名
        private static readonly string[] SSAOOnlyFields = {
            "sampleCount", "radius", "bias", "cameraDirections", "captureDistance", "includeAlphaTestedMeshes"
        };

        // RayCast固有フィールド名
        private static readonly string[] RayCastOnlyFields = {
            "rayCount", "maxRayDistance", "rayOriginOffset", "aoMask"
        };

        // 自動描画から除外するフィールド名
        private static readonly string[] ExcludedFields = {
            "resolution", "lilToonSettings", "poiyomiSettings", "standardSettings"
        };

        public override void OnInspectorGUI()
        {
            var baker = (SSAOBaker)target;
            serializedObject.Update();

            var renderer = baker.GetComponent<Renderer>();
            if (renderer == null)
            {
                EditorGUILayout.HelpBox(
                    "このコンポーネントにはRendererが必要です。\n" +
                    "SkinnedMeshRenderer または MeshRenderer があるGameObjectに追加してください。",
                    MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();

            // Bake Mode
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bakeMode"));

            var mode = baker.bakeMode;
            var hideFields = mode == AOBakeMode.RayCast ? SSAOOnlyFields : RayCastOnlyFields;

            // Resolution ドロップダウン
            EditorGUILayout.LabelField("Bake Settings", EditorStyles.boldLabel);
            int currentResIndex = System.Array.IndexOf(ResolutionValues, (int)baker.resolution);
            if (currentResIndex < 0) currentResIndex = 3;
            int newResIndex = EditorGUILayout.Popup("Resolution", currentResIndex, ResolutionLabels);
            if (newResIndex != currentResIndex)
            {
                Undo.RecordObject(baker, "Change AO Resolution");
                baker.resolution = (TextureResolution)ResolutionValues[newResIndex];
            }

            // モード別にフィールドを描画
            var prop = serializedObject.GetIterator();
            prop.NextVisible(true);
            while (prop.NextVisible(false))
            {
                if (System.Array.IndexOf(ExcludedFields, prop.name) >= 0) continue;
                if (prop.name == "bakeMode") continue;
                if (System.Array.IndexOf(hideFields, prop.name) >= 0) continue;
                EditorGUILayout.PropertyField(prop, true);
            }

            EditorGUILayout.Space();

            // シェーダー別設定
            var detectedShader = baker.targetShader != AOTargetShader.Auto
                ? baker.targetShader
                : DetectShaderFromRenderer(renderer);
            DrawShaderSettings(detectedShader);

            serializedObject.ApplyModifiedProperties();

            bool changed = EditorGUI.EndChangeCheck();
            if (Application.isPlaying && changed)
                PlayModeParameterPersistence.SaveAllBakerParams();

            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Play Mode中のパラメータ変更は、Play Mode終了後に自動的に保持されます。",
                    MessageType.Info);
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                $"対象: {renderer.GetType().Name} on '{renderer.gameObject.name}'\n" +
                $"モード: {mode} | シェーダー: {detectedShader}\n" +
                "NDMFビルド時に、アバター全体のジオメトリを考慮してAOをベイクします。",
                MessageType.Info);
        }

        private static AOTargetShader DetectShaderFromRenderer(Renderer renderer)
        {
            if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                return AOTargetShader.Auto;
            return ShaderTypeUtil.DetectFromMaterial(renderer.sharedMaterials[0]);
        }

        private void DrawShaderSettings(AOTargetShader detectedShader)
        {
            _shaderSettingsFoldout = EditorGUILayout.Foldout(_shaderSettingsFoldout, "Shader Settings", true);
            if (!_shaderSettingsFoldout) return;

            EditorGUI.indentLevel++;

            switch (detectedShader)
            {
                case AOTargetShader.LilToon:
                    DrawLilToonSettings();
                    break;
                case AOTargetShader.Poiyomi:
                    DrawPoiyomiSettings();
                    break;
                case AOTargetShader.ToonStandard:
                case AOTargetShader.StandardLit:
                    DrawStandardSettings();
                    break;
                case AOTargetShader.VertexColor:
                    EditorGUILayout.LabelField("頂点カラーのRチャンネルにAOを書き込みます。");
                    break;
                default:
                    EditorGUILayout.HelpBox(
                        "シェーダーが自動検出できません。Target Shaderを手動で選択してください。",
                        MessageType.Warning);
                    break;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawLilToonSettings()
        {
            var settings = serializedObject.FindProperty("lilToonSettings");
            EditorGUILayout.LabelField("lilToon AO Settings", EditorStyles.miniBoldLabel);

            EditorGUILayout.LabelField("1st Shadow", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("aoScale1st"), ScaleLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("aoOffset1st"), OffsetLabel);

            EditorGUILayout.LabelField("2nd Shadow", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("aoScale2nd"), ScaleLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("aoOffset2nd"), OffsetLabel);

            EditorGUILayout.LabelField("3rd Shadow", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("aoScale3rd"), ScaleLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("aoOffset3rd"), OffsetLabel);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("postAO"), PostAOLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("borderMaskLOD"), BorderMaskLODLabel);
        }

        private void DrawPoiyomiSettings()
        {
            var settings = serializedObject.FindProperty("poiyomiSettings");
            EditorGUILayout.LabelField("Poiyomi AO Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("aoStrengthR"), PoiStrengthRLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("aoStrengthG"), PoiStrengthGLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("aoStrengthB"), PoiStrengthBLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("aoStrengthA"), PoiStrengthALabel);
        }

        private void DrawStandardSettings()
        {
            var settings = serializedObject.FindProperty("standardSettings");
            EditorGUILayout.LabelField("Standard AO Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("occlusionStrength"), OcclusionStrengthLabel);
        }
    }
}
