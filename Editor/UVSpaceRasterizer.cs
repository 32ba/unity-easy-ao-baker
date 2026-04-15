using UnityEngine;
using UnityEngine.Rendering;

namespace net._32ba.EasyAOBaker.Editor
{
    public class UVSpaceRasterizer
    {
        /// <summary>UV重複時に保存する三角形サンプル数の上限</summary>
        public const int MaxLayers = 4;

        private readonly Material _uvRasterMaterial;

        public UVSpaceRasterizer()
        {
            var shader = Shader.Find("Hidden/EasyAOBaker/UVRasterize");
            _uvRasterMaterial = new Material(shader);
        }

        public struct RasterizeResult
        {
            /// <summary>Texture2DArray: 各テクセルを覆った三角形毎のワールド座標 (最大MaxLayers)</summary>
            public RenderTexture Positions;
            /// <summary>Texture2DArray: 同上のワールド法線</summary>
            public RenderTexture Normals;
            /// <summary>RInt: 各テクセルを覆った三角形数（上限なしでインクリメント）</summary>
            public RenderTexture Coverage;
            /// <summary>float4 RT: 有効マスク（JFAパディング用）。被覆=1, 未被覆=0</summary>
            public RenderTexture ValidMask;

            public int LayerCount => MaxLayers;
        }

        /// <summary>
        /// メッシュのUV空間にラスタライズし、各テクセルのワールド座標と法線を取得する。
        /// ミラーUV等で重複するテクセルには、覆った三角形ごとの値を別スロットに保存する。
        /// </summary>
        public RasterizeResult Rasterize(Mesh mesh, Matrix4x4 localToWorld, int resolution)
        {
            return Rasterize(mesh, localToWorld, resolution, null);
        }

        /// <summary>
        /// includeSubmesh が指定された場合は false のサブメッシュをスキップ。
        /// Face_effect 等のブレンドシェイプ用オーバーレイ三角形を除外できる。
        /// </summary>
        public RasterizeResult Rasterize(Mesh mesh, Matrix4x4 localToWorld, int resolution, bool[] includeSubmesh)
        {
            var posRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = MaxLayers,
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false
            };
            posRT.Create();

            var nrmRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = MaxLayers,
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false
            };
            nrmRT.Create();

            var coverageRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RInt)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false
            };
            coverageRT.Create();

            var maskRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat)
            {
                filterMode = FilterMode.Point,
                useMipMap = false
            };
            maskRT.Create();

            _uvRasterMaterial.SetInt("_MaxLayers", MaxLayers);

            var cmd = new CommandBuffer { name = "EasyAOBaker_UVRasterize" };

            // Coverage/Mask を 0 クリア。Positions/Normals のスロットは coverage で
            // ガードされて未書き込み領域は読まれないのでクリア不要。
            cmd.SetRenderTarget(coverageRT);
            cmd.ClearRenderTarget(false, true, Color.clear);
            cmd.SetRenderTarget(maskRT);
            cmd.ClearRenderTarget(false, true, Color.clear);

            // u0 = maskRT (SV_Target), u1..u3 = UAV
            cmd.SetRenderTarget(maskRT);
            cmd.SetRandomWriteTarget(1, posRT);
            cmd.SetRandomWriteTarget(2, nrmRT);
            cmd.SetRandomWriteTarget(3, coverageRT);

            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                if (includeSubmesh != null && submesh < includeSubmesh.Length && !includeSubmesh[submesh])
                    continue;
                cmd.DrawMesh(mesh, localToWorld, _uvRasterMaterial, submesh);
            }

            cmd.ClearRandomWriteTargets();

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            return new RasterizeResult
            {
                Positions = posRT,
                Normals = nrmRT,
                Coverage = coverageRT,
                ValidMask = maskRT
            };
        }

        public void Dispose()
        {
            if (_uvRasterMaterial != null)
                Object.DestroyImmediate(_uvRasterMaterial);
        }
    }
}
