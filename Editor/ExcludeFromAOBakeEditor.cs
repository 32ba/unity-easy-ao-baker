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
            var candidates = CollectBakersToRoot(exclusion.transform);
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

        private static List<EasyAOBaker> CollectBakersToRoot(Transform start)
        {
            var bakers = new List<EasyAOBaker>();
            var current = start;
            while (current != null)
            {
                bakers.AddRange(current.GetComponents<EasyAOBaker>());
                current = current.parent;
            }
            return bakers;
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
