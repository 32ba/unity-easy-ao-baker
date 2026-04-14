using System.Collections.Generic;
using UnityEngine;

namespace net._32ba.EasyAOBaker.Editor
{
    public class DepthMapRenderer
    {
        private readonly int _depthTexSize;
        private readonly float _captureDistance;
        private readonly Shader _depthShader;

        public struct DepthRenderResult
        {
            public RenderTexture DepthTextureArray;
            public Matrix4x4[] ViewProjMatrices;
            public Vector3[] CameraForwards;
        }

        public DepthMapRenderer(int depthTexSize, float captureDistance)
        {
            _depthTexSize = depthTexSize;
            _captureDistance = captureDistance;
            _depthShader = Shader.Find("Hidden/EasyAOBaker/DepthOnly");
        }

        /// <summary>
        /// 指定された方向群からOrthographicカメラで深度マップをレンダリングする。
        /// </summary>
        public DepthRenderResult RenderDepthMaps(
            Vector3[] directions,
            Bounds sceneBounds,
            List<GameObject> occluderObjects)
        {
            int dirCount = directions.Length;
            var vpMatrices = new Matrix4x4[dirCount];
            var cameraForwards = new Vector3[dirCount];

            var depthArray = new RenderTexture(_depthTexSize, _depthTexSize, 24, RenderTextureFormat.Depth)
            {
                dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray,
                volumeDepth = dirCount,
                filterMode = FilterMode.Point,
                useMipMap = false
            };
            depthArray.Create();

            // 個別の深度テクスチャとして描画し、後でArrayにコピー
            var individualDepths = new RenderTexture[dirCount];
            var tempCamera = CreateTempCamera(sceneBounds);

            for (int i = 0; i < dirCount; i++)
            {
                var dir = directions[i].normalized;
                cameraForwards[i] = dir;

                var center = sceneBounds.center;
                var camPos = center - dir * _captureDistance;

                tempCamera.transform.position = camPos;
                tempCamera.transform.rotation = Quaternion.LookRotation(dir);

                float orthoSize = CalculateOrthoSize(sceneBounds, dir);
                tempCamera.orthographicSize = orthoSize;

                var depthRT = RenderTexture.GetTemporary(
                    _depthTexSize, _depthTexSize, 24, RenderTextureFormat.Depth);
                depthRT.filterMode = FilterMode.Point;

                tempCamera.targetTexture = depthRT;
                tempCamera.RenderWithShader(_depthShader, "RenderType");

                vpMatrices[i] = tempCamera.projectionMatrix * tempCamera.worldToCameraMatrix;
                individualDepths[i] = depthRT;
            }

            // Tex2DArrayに個別深度をコピー
            for (int i = 0; i < dirCount; i++)
            {
                Graphics.CopyTexture(individualDepths[i], 0, 0, depthArray, i, 0);
                RenderTexture.ReleaseTemporary(individualDepths[i]);
            }

            Object.DestroyImmediate(tempCamera.gameObject);

            return new DepthRenderResult
            {
                DepthTextureArray = depthArray,
                ViewProjMatrices = vpMatrices,
                CameraForwards = cameraForwards
            };
        }

        private Camera CreateTempCamera(Bounds sceneBounds)
        {
            var go = new GameObject("EasyAOBaker_TempCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.nearClipPlane = 0.001f;
            camera.farClipPlane = _captureDistance * 2.0f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            camera.enabled = false;

            return camera;
        }

        private float CalculateOrthoSize(Bounds bounds, Vector3 direction)
        {
            var rotation = Quaternion.LookRotation(direction);
            var inverseRot = Quaternion.Inverse(rotation);

            var extents = bounds.extents;
            var corners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                corners[i] = bounds.center + Vector3.Scale(extents, new Vector3(
                    (i & 1) == 0 ? -1 : 1,
                    (i & 2) == 0 ? -1 : 1,
                    (i & 4) == 0 ? -1 : 1
                ));
            }

            float maxExtent = 0;
            foreach (var corner in corners)
            {
                var localCorner = inverseRot * (corner - bounds.center);
                maxExtent = Mathf.Max(maxExtent, Mathf.Abs(localCorner.x));
                maxExtent = Mathf.Max(maxExtent, Mathf.Abs(localCorner.y));
            }

            return maxExtent * 1.1f; // 少しマージンを追加
        }
    }
}
