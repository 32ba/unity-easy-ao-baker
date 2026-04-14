using UnityEngine;
using UnityEngine.Rendering;

namespace net._32ba.EasyAOBaker.Editor
{
    public class UVSpaceRasterizer
    {
        private readonly Material _uvRasterMaterial;

        public UVSpaceRasterizer()
        {
            var shader = Shader.Find("Hidden/EasyAOBaker/UVRasterize");
            _uvRasterMaterial = new Material(shader);
        }

        public struct RasterizeResult
        {
            public RenderTexture Positions;
            public RenderTexture Normals;
        }

        /// <summary>
        /// メッシュのUV空間にラスタライズし、各テクセルのワールド座標と法線を取得する。
        /// </summary>
        public RasterizeResult Rasterize(Mesh mesh, Matrix4x4 localToWorld, int resolution)
        {
            var posRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = false
            };
            posRT.Create();

            var nrmRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = false
            };
            nrmRT.Create();

            // 保守的ラスタライズ用のテクセルサイズを設定
            _uvRasterMaterial.SetFloat("_TexelSize", 1.0f / resolution);

            var cmd = new CommandBuffer { name = "EasyAOBaker_UVRasterize" };

            cmd.SetRenderTarget(
                new RenderTargetIdentifier[] { posRT, nrmRT },
                posRT.depthBuffer);
            cmd.ClearRenderTarget(true, true, Color.clear);

            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                cmd.DrawMesh(mesh, localToWorld, _uvRasterMaterial, submesh);
            }

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            return new RasterizeResult
            {
                Positions = posRT,
                Normals = nrmRT
            };
        }

        public void Dispose()
        {
            if (_uvRasterMaterial != null)
                Object.DestroyImmediate(_uvRasterMaterial);
        }
    }
}
