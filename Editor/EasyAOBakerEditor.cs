using UnityEditor;
using UnityEngine;

namespace net._32ba.EasyAOBaker.Editor
{
    [CustomEditor(typeof(EasyAOBaker))]
    public class EasyAOBakerEditor : UnityEditor.Editor
    {
        private static readonly string[] ResolutionLabels = { "256", "512", "1024", "2048", "4096" };
        private static readonly int[] ResolutionValues = { 256, 512, 1024, 2048, 4096 };

        private bool _shaderSettingsFoldout = true;
        private bool _advancedFoldout = false;

        public override void OnInspectorGUI()
        {
            var baker = (EasyAOBaker)target;
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

            DrawBakeNowButton(baker);

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(L.Tr("msg.play_mode_preserve"), MessageType.Info);
            }
        }

        private static void DrawBakeNowButton(EasyAOBaker baker)
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button(L.G("button.bake_now"), GUILayout.Height(30)))
                    ManuallyBake(baker);
            }
        }

        private static void ManuallyBake(EasyAOBaker baker)
        {
            var avatarRoot = FindAvatarRoot(baker.transform);
            if (avatarRoot == null)
            {
                EditorUtility.DisplayDialog(
                    L.Tr("dialog.bake.title"),
                    L.Tr("dialog.no_avatar_root"),
                    L.Tr("dialog.ok"));
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar(
                    L.Tr("dialog.bake.title"),
                    L.Tr("progress.baking"), 0.5f);

                var processor = new AOBakeProcessor(avatarRoot, null);
                processor.Execute(new[] { baker });

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog(
                    L.Tr("dialog.bake.title"),
                    L.Format("dialog.bake.success", processor.ManualOutputDirectory),
                    L.Tr("dialog.ok"));
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(e);
                EditorUtility.DisplayDialog(
                    L.Tr("dialog.bake.title"),
                    L.Format("dialog.bake.failed", e.Message),
                    L.Tr("dialog.ok"));
            }
        }

        /// <summary>
        /// アバタールートを探す。VRC Avatar Descriptorを優先（リフレクションでasmdef依存を回避）、
        /// 見つからなければ最上位のtransformを返す。
        /// </summary>
        private static GameObject FindAvatarRoot(Transform start)
        {
            var descriptorType = System.Type.GetType("VRC.SDKBase.VRC_AvatarDescriptor, VRC.SDKBase");
            if (descriptorType != null)
            {
                var current = start;
                while (current != null)
                {
                    if (current.GetComponent(descriptorType) != null)
                        return current.gameObject;
                    current = current.parent;
                }
            }
            return start.root != null ? start.root.gameObject : null;
        }

        private void DrawBasicSettings(EasyAOBaker baker)
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
            DrawMaterialSelection(baker);
            DrawField("aoMask", "field.ao_mask");
        }

        private static void DrawMaterialSelection(EasyAOBaker baker)
        {
            var renderer = baker.GetComponent<Renderer>();
            var mats = renderer != null ? renderer.sharedMaterials : null;
            if (mats == null || mats.Length == 0) return;

            // フラグ配列をマテリアル数に合わせて同期（不足分は true で追加）
            var flags = baker.materialBakeFlags;
            if (flags == null || flags.Length != mats.Length)
            {
                var resized = new bool[mats.Length];
                for (int i = 0; i < mats.Length; i++)
                    resized[i] = (flags != null && i < flags.Length) ? flags[i] : true;
                baker.materialBakeFlags = resized;
                flags = resized;
            }

            EditorGUILayout.LabelField(L.G("section.materials"), EditorStyles.miniBoldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    string matName = mat != null ? mat.name : L.Tr("materials.none_slot");
                    string suffix = "";
                    if (baker.targetShader == AOTargetShader.Auto && mat != null)
                    {
                        var detected = ShaderTypeUtil.DetectFromMaterial(mat);
                        suffix = detected == AOTargetShader.Auto
                            ? $"  ({L.Tr("field.target_shader.not_detected")})"
                            : $"  ({detected})";
                    }

                    using (new EditorGUI.DisabledScope(mat == null))
                    {
                        bool newFlag = EditorGUILayout.ToggleLeft(matName + suffix, flags[i]);
                        if (newFlag != flags[i])
                        {
                            Undo.RecordObject(baker, "Toggle Material Bake Flag");
                            flags[i] = newFlag;
                            EditorUtility.SetDirty(baker);
                        }
                    }
                }
            }
        }

        private void DrawAdvancedSettings(EasyAOBaker baker)
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
