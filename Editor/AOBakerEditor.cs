using UnityEditor;
using UnityEngine;

namespace net._32ba.AOBaker.Editor
{
    [CustomEditor(typeof(AOBaker))]
    public class AOBakerEditor : UnityEditor.Editor
    {
        private static readonly string[] ResolutionLabels = { "256", "512", "1024", "2048", "4096" };
        private static readonly int[] ResolutionValues = { 256, 512, 1024, 2048, 4096 };

        private bool _shaderSettingsFoldout = true;
        private bool _advancedFoldout = false;

        public override void OnInspectorGUI()
        {
            var baker = (AOBaker)target;
            serializedObject.Update();

            L.DrawLanguageSwitcher();

            var renderer = baker.GetComponent<Renderer>();
            if (renderer == null)
            {
                EditorGUILayout.HelpBox(L.Tr("msg.no_renderer"), MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();

            DrawBasicSettings(baker);

            EditorGUILayout.Space();

            var detectedShader = baker.targetShader != AOTargetShader.Auto
                ? baker.targetShader
                : DetectShaderFromRenderer(renderer);
            DrawShaderSettings(detectedShader);

            EditorGUILayout.Space();

            DrawAdvancedSettings(baker);

            serializedObject.ApplyModifiedProperties();

            bool changed = EditorGUI.EndChangeCheck();
            if (Application.isPlaying && changed)
                PlayModeParameterPersistence.SaveAllBakerParams();

            EditorGUILayout.Space();

            if (Application.isPlaying)
                EditorGUILayout.HelpBox(L.Tr("msg.play_mode_preserve"), MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                L.Format("msg.target_info",
                    renderer.GetType().Name, renderer.gameObject.name, baker.bakeMode, detectedShader),
                MessageType.Info);
        }

        private void DrawBasicSettings(AOBaker baker)
        {
            EditorGUILayout.LabelField(L.Tr("section.bake_settings"), EditorStyles.boldLabel);

            int currentResIndex = System.Array.IndexOf(ResolutionValues, (int)baker.resolution);
            if (currentResIndex < 0) currentResIndex = 3;
            int newResIndex = EditorGUILayout.Popup(L.G("field.resolution"), currentResIndex, ResolutionLabels);
            if (newResIndex != currentResIndex)
            {
                Undo.RecordObject(baker, "Change AO Resolution");
                baker.resolution = (TextureResolution)ResolutionValues[newResIndex];
            }

            DrawField("intensity", "field.intensity");
            DrawField("targetShader", "field.target_shader");
            DrawField("aoMask", "field.ao_mask");
        }

        private void DrawAdvancedSettings(AOBaker baker)
        {
            _advancedFoldout = EditorGUILayout.Foldout(_advancedFoldout, L.Tr("section.advanced"), true);
            if (!_advancedFoldout) return;

            EditorGUI.indentLevel++;

            DrawField("bakeMode", "field.bake_mode");

            EditorGUILayout.Space(4);

            if (baker.bakeMode == AOBakeMode.RayCast)
            {
                EditorGUILayout.LabelField("RayCast", EditorStyles.miniBoldLabel);
                DrawField("rayCount", "field.ray_count");
                DrawField("maxRayDistance", "field.max_ray_distance");
                DrawField("rayOriginOffset", "field.ray_origin_offset");
            }
            else
            {
                EditorGUILayout.LabelField("SSAO", EditorStyles.miniBoldLabel);
                DrawField("sampleCount", "field.sample_count");
                DrawField("radius", "field.radius");
                DrawField("bias", "field.bias");
                DrawField("cameraDirections", "field.camera_directions");
                DrawField("captureDistance", "field.capture_distance");
                DrawField("includeAlphaTestedMeshes", "field.include_alpha_tested");
            }

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField(L.Tr("section.filter"), EditorStyles.miniBoldLabel);
            DrawField("blurIterations", "field.blur_iterations");
            DrawField("blurRadius", "field.blur_radius");

            EditorGUI.indentLevel--;
        }

        private void DrawField(string propName, string labelKey)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propName), L.G(labelKey));
        }

        private static AOTargetShader DetectShaderFromRenderer(Renderer renderer)
        {
            if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                return AOTargetShader.Auto;
            return ShaderTypeUtil.DetectFromMaterial(renderer.sharedMaterials[0]);
        }

        private void DrawShaderSettings(AOTargetShader detectedShader)
        {
            _shaderSettingsFoldout = EditorGUILayout.Foldout(_shaderSettingsFoldout, L.Tr("section.shader_settings"), true);
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
                    EditorGUILayout.LabelField(L.Tr("msg.vertex_color"));
                    break;
                default:
                    EditorGUILayout.HelpBox(L.Tr("msg.shader_not_detected"), MessageType.Warning);
                    break;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawLilToonSettings()
        {
            var settings = serializedObject.FindProperty("lilToonSettings");
            EditorGUILayout.LabelField(L.Tr("section.lil_toon"), EditorStyles.miniBoldLabel);

            DrawLilToonShadow(settings, "1st Shadow", "aoScale1st", "aoOffset1st");
            DrawLilToonShadow(settings, "2nd Shadow", "aoScale2nd", "aoOffset2nd");
            DrawLilToonShadow(settings, "3rd Shadow", "aoScale3rd", "aoOffset3rd");

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("postAO"), L.G("field.post_ao"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative("borderMaskLOD"), L.G("field.border_mask_lod"));
        }

        private static void DrawLilToonShadow(SerializedProperty settings, string label, string scaleProp, string offsetProp)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(settings.FindPropertyRelative(scaleProp), L.G("field.scale"));
            EditorGUILayout.PropertyField(settings.FindPropertyRelative(offsetProp), L.G("field.offset"));
        }

        private void DrawPoiyomiSettings()
        {
            var settings = serializedObject.FindProperty("poiyomiSettings");
            EditorGUILayout.LabelField(L.Tr("section.poiyomi"), EditorStyles.miniBoldLabel);
            DrawPoiyomiChannel(settings, "aoStrengthR", "field.poi_r_strength");
            DrawPoiyomiChannel(settings, "aoStrengthG", "field.poi_g_strength");
            DrawPoiyomiChannel(settings, "aoStrengthB", "field.poi_b_strength");
            DrawPoiyomiChannel(settings, "aoStrengthA", "field.poi_a_strength");
        }

        private static void DrawPoiyomiChannel(SerializedProperty settings, string prop, string labelKey)
        {
            EditorGUILayout.PropertyField(
                settings.FindPropertyRelative(prop),
                L.G(labelKey, "field.poi_strength.tooltip"));
        }

        private void DrawStandardSettings()
        {
            var settings = serializedObject.FindProperty("standardSettings");
            EditorGUILayout.LabelField(L.Tr("section.standard"), EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(
                settings.FindPropertyRelative("occlusionStrength"),
                L.G("field.occlusion_strength"));
        }
    }
}
