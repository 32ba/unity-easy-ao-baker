using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace net._32ba.EasyAOBaker.Editor
{
    [CustomEditor(typeof(ExcludeFromAOBake))]
    public class ExcludeFromAOBakeEditor : UnityEditor.Editor
    {
        private bool _advancedFoldout;

        public override void OnInspectorGUI()
        {
            var exclusion = (ExcludeFromAOBake)target;

            L.DrawLanguageSwitcher();
            EditorGUILayout.HelpBox(L.Tr("msg.exclude_info"), MessageType.Info);

            _advancedFoldout = EditorGUILayout.Foldout(_advancedFoldout, L.Tr("section.advanced"), true);
            if (!_advancedFoldout)
                return;

            EditorGUI.indentLevel++;
            DrawBakerSelection(exclusion);
            EditorGUI.indentLevel--;
        }

        private static void DrawBakerSelection(ExcludeFromAOBake exclusion)
        {
            var candidates = CollectBakersInAvatar(exclusion.transform);
            var selected = new HashSet<EasyAOBaker>(
                (exclusion.doNotExcludeFor ?? new EasyAOBaker[0]).Where(b => b != null));

            EditorGUILayout.LabelField(L.G("section.exclude_exceptions"), EditorStyles.miniBoldLabel);

            if (candidates.Count == 0)
            {
                EditorGUILayout.HelpBox(L.Tr("msg.exclude_no_bakers"), MessageType.Warning);
                return;
            }

            foreach (var baker in candidates)
            {
                string label = $"{GetTransformPath(baker.transform)} ({baker.bakeMode})";
                bool isSelected = selected.Contains(baker);
                bool newSelected = EditorGUILayout.ToggleLeft(label, isSelected);
                if (newSelected == isSelected) continue;

                Undo.RecordObject(exclusion, "Toggle Exclude Exception Baker");
                if (newSelected)
                    selected.Add(baker);
                else
                    selected.Remove(baker);

                exclusion.doNotExcludeFor = candidates.Where(selected.Contains).ToArray();
                EditorUtility.SetDirty(exclusion);
            }
        }

        private static List<EasyAOBaker> CollectBakersInAvatar(Transform start)
        {
            var avatarRoot = FindAvatarRoot(start);
            if (avatarRoot == null)
                return new List<EasyAOBaker>();

            return avatarRoot.GetComponentsInChildren<EasyAOBaker>(true)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// VRC Avatar Descriptor を優先してアバタールートを探し、見つからなければ最上位を返す。
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

        private static string GetTransformPath(Transform transform)
        {
            var segments = new List<string>();
            var current = transform;
            while (current != null)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }
    }
}
