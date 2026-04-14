using UnityEngine;

namespace net._32ba.EasyAOBaker.Editor
{
    public static class FibonacciSphere
    {
        private const float GoldenRatio = 1.6180339887498949f;

        public static Vector3[] GenerateFullSphereDirections(int count)
        {
            var directions = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                float theta = 2.0f * Mathf.PI * i / GoldenRatio;
                float phi = Mathf.Acos(1.0f - 2.0f * (i + 0.5f) / count);

                directions[i] = new Vector3(
                    Mathf.Sin(phi) * Mathf.Cos(theta),
                    Mathf.Cos(phi),
                    Mathf.Sin(phi) * Mathf.Sin(theta)
                ).normalized;
            }

            return directions;
        }
    }
}
