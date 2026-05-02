using UnityEngine;

namespace net._32ba.EasyAOBaker
{
    [AddComponentMenu("EasyAOBaker/Exclude From AO Bake")]
    [DisallowMultipleComponent]
    public class ExcludeFromAOBake : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        /// <summary>
        /// ここに含まれる Baker については、このオブジェクトを除外しない。
        /// </summary>
        public EasyAOBaker[] doNotExcludeFor;

        public bool ShouldExcludeFor(EasyAOBaker baker)
        {
            if (baker == null) return true;
            if (doNotExcludeFor == null) return true;

            for (int i = 0; i < doNotExcludeFor.Length; i++)
            {
                if (doNotExcludeFor[i] == baker)
                    return false;
            }

            return true;
        }
    }
}
