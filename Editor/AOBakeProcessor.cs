using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

namespace net._32ba.AOBaker.Editor
{
    public class AOBakeProcessor
    {
        private readonly GameObject _avatarRoot;
        private readonly BuildContext _buildContext;

        public AOBakeProcessor(GameObject avatarRoot, BuildContext buildContext)
        {
            _avatarRoot = avatarRoot;
            _buildContext = buildContext;
        }

        /// <summary>
        /// NDMFパイプラインからの実行エントリポイント。
        /// 複数のAOBakerコンポーネントを処理する。
        /// 深度マップはアバター全体から1回だけ生成し共有する。
        /// </summary>
        public void Execute(AOBaker[] bakers)
        {
            var allRenderers = CollectAllRenderers();
            if (allRenderers.Count == 0)
            {
                Debug.LogWarning("[AO Baker] No renderers found in avatar.");
                return;
            }

            var allMeshData = CollectMeshData(allRenderers);

            // モード別にグループ分け
            var ssaoBakers = bakers.Where(b => b.bakeMode == AOBakeMode.SSAO).ToArray();
            var raycastBakers = bakers.Where(b => b.bakeMode == AOBakeMode.RayCast).ToArray();

            var rasterizer = new UVSpaceRasterizer();
            var postFilter = new AOTexturePostFilter();

            try
            {
                if (ssaoBakers.Length > 0)
                    ExecuteSSAO(ssaoBakers, allMeshData, rasterizer, postFilter);

                if (raycastBakers.Length > 0)
                    ExecuteRayCast(raycastBakers, allMeshData, rasterizer, postFilter);
            }
            finally
            {
                rasterizer.Dispose();
            }
        }

        private void ExecuteSSAO(AOBaker[] bakers, List<MeshData> allMeshData,
            UVSpaceRasterizer rasterizer, AOTexturePostFilter postFilter)
        {
            var occluderObjects = BuildOccluderScene(allMeshData, bakers[0]);

            try
            {
                int maxDirections = bakers.Max(b => b.cameraDirections);
                float maxCaptureDistance = bakers.Max(b => b.captureDistance);
                int maxResolution = bakers.Max(b => b.ResolutionValue);
                int depthTexSize = Mathf.Min(maxResolution, 1024);

                var sceneBounds = CalculateSceneBounds(allMeshData);
                var directions = FibonacciSphere.GenerateFullSphereDirections(maxDirections);
                var depthRenderer = new DepthMapRenderer(depthTexSize, maxCaptureDistance);
                var depthResult = depthRenderer.RenderDepthMaps(directions, sceneBounds, occluderObjects);

                try
                {
                    foreach (var baker in bakers)
                        BakeForBaker(baker, rasterizer, postFilter,
                            (meshData, res) => BakeAOForMesh(meshData, baker, res, depthTexSize, depthResult, postFilter, rasterizer));
                }
                finally
                {
                    depthResult.DepthTextureArray.Release();
                }
            }
            finally
            {
                CleanupOccluderScene(occluderObjects);
            }
        }

        private void ExecuteRayCast(AOBaker[] bakers, List<MeshData> allMeshData,
            UVSpaceRasterizer rasterizer, AOTexturePostFilter postFilter)
        {
            var meshes = allMeshData.Select(d => d.Mesh).ToList();
            var transforms = allMeshData.Select(d => d.LocalToWorld).ToList();

            using var bvhData = BVHBuilder.Build(meshes, transforms);
            Debug.Log($"[AO Baker] BVH built: {meshes.Sum(m => m.triangles.Length / 3)} triangles");

            foreach (var baker in bakers)
                BakeForBaker(baker, rasterizer, postFilter,
                    (meshData, res) => ComputeRayCastAO(meshData, baker, res, bvhData, rasterizer, postFilter));
        }

        private void BakeForBaker(AOBaker baker, UVSpaceRasterizer rasterizer, AOTexturePostFilter postFilter,
            System.Func<MeshData, int, Texture2D> bakeFunc)
        {
            var renderer = baker.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"[AO Baker] No Renderer on '{baker.gameObject.name}'. Skipping.");
                return;
            }

            var meshData = GetMeshDataForRenderer(renderer);
            if (!meshData.HasValue) return;

            var aoTex = bakeFunc(meshData.Value, baker.ResolutionValue);
            if (aoTex == null) return;

            ApplyAOToMaterials(meshData.Value, aoTex, baker);
        }

        private List<Renderer> CollectAllRenderers()
        {
            return _avatarRoot.GetComponentsInChildren<Renderer>(false)
                .Where(r => r is SkinnedMeshRenderer or MeshRenderer)
                .Where(r => r.gameObject.activeInHierarchy)
                .Where(r => r.GetComponent<ExcludeFromAOBake>() == null)
                .ToList();
        }

        private struct MeshData
        {
            public Renderer Renderer;
            public Mesh Mesh;
            public Matrix4x4 LocalToWorld;
            public bool IsTemporaryMesh;
        }

        private MeshData? GetMeshDataForRenderer(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer smr)
            {
                var bakedMesh = new Mesh();
                smr.BakeMesh(bakedMesh);
                return new MeshData
                {
                    Renderer = renderer,
                    Mesh = bakedMesh,
                    LocalToWorld = renderer.transform.localToWorldMatrix,
                    IsTemporaryMesh = true
                };
            }

            if (renderer is MeshRenderer mr)
            {
                var meshFilter = mr.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    Debug.LogWarning($"[AO Baker] No mesh on '{renderer.name}'. Skipping.");
                    return null;
                }
                return new MeshData
                {
                    Renderer = renderer,
                    Mesh = meshFilter.sharedMesh,
                    LocalToWorld = renderer.transform.localToWorldMatrix,
                    IsTemporaryMesh = false
                };
            }

            return null;
        }

        private List<MeshData> CollectMeshData(List<Renderer> renderers)
        {
            var result = new List<MeshData>();
            foreach (var renderer in renderers)
            {
                var data = GetMeshDataForRenderer(renderer);
                if (data.HasValue)
                    result.Add(data.Value);
            }
            return result;
        }

        private List<GameObject> BuildOccluderScene(List<MeshData> meshDataList, AOBaker settings)
        {
            var occluders = new List<GameObject>();
            var unlitMat = new Material(Shader.Find("Hidden/AOBaker/DepthOnly"));

            foreach (var data in meshDataList)
            {
                var go = new GameObject("AOBaker_Occluder")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                go.transform.position = data.LocalToWorld.GetColumn(3);
                go.transform.rotation = data.LocalToWorld.rotation;
                go.transform.localScale = data.LocalToWorld.lossyScale;

                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = data.Mesh;

                var mr = go.AddComponent<MeshRenderer>();

                if (settings.includeAlphaTestedMeshes && data.Renderer.sharedMaterials.Length > 0)
                {
                    var mats = new Material[data.Renderer.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var srcMat = data.Renderer.sharedMaterials[i];
                        mats[i] = new Material(unlitMat);

                        if (srcMat != null && srcMat.HasProperty("_MainTex"))
                        {
                            mats[i].SetTexture("_MainTex", srcMat.GetTexture("_MainTex"));
                            if (srcMat.HasProperty("_Cutoff"))
                            {
                                mats[i].SetFloat("_Cutoff", srcMat.GetFloat("_Cutoff"));
                                mats[i].EnableKeyword("_ALPHATEST_ON");
                            }
                        }
                    }
                    mr.sharedMaterials = mats;
                }
                else
                {
                    mr.sharedMaterial = unlitMat;
                }

                occluders.Add(go);
            }

            return occluders;
        }

        private Bounds CalculateSceneBounds(List<MeshData> meshDataList)
        {
            if (meshDataList.Count == 0)
                return new Bounds(Vector3.zero, Vector3.one);

            var bounds = meshDataList[0].Renderer.bounds;
            for (int i = 1; i < meshDataList.Count; i++)
                bounds.Encapsulate(meshDataList[i].Renderer.bounds);

            return bounds;
        }

        private Texture2D BakeAOForMesh(
            MeshData meshData,
            AOBaker settings,
            int resolution,
            int depthTexSize,
            DepthMapRenderer.DepthRenderResult depthResult,
            AOTexturePostFilter postFilter,
            UVSpaceRasterizer rasterizer)
        {
            return BakeWithCompute(meshData, settings, resolution, rasterizer, postFilter,
                rasterResult => ComputeSSAO(rasterResult, depthResult, settings, resolution, depthTexSize));
        }

        private Texture2D ComputeRayCastAO(
            MeshData meshData,
            AOBaker settings,
            int resolution,
            BVHBuilder.BVHData bvhData,
            UVSpaceRasterizer rasterizer,
            AOTexturePostFilter postFilter)
        {
            return BakeWithCompute(meshData, settings, resolution, rasterizer, postFilter,
                rasterResult =>
                {
                    var shader = AOBakerAssetLoader.LoadComputeShader("RayCastBake");
                    int kernel = shader.FindKernel("BakeRayCastAO");

                    var aoRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat)
                    {
                        enableRandomWrite = true,
                        filterMode = FilterMode.Point
                    };
                    aoRT.Create();

                    shader.SetTexture(kernel, "_WorldPositions", rasterResult.Positions);
                    shader.SetTexture(kernel, "_WorldNormals", rasterResult.Normals);
                    shader.SetBuffer(kernel, "_BVHNodes", bvhData.NodesBuffer);
                    shader.SetBuffer(kernel, "_BVHTriangles", bvhData.TrianglesBuffer);
                    shader.SetTexture(kernel, "_AOResult", aoRT);
                    shader.SetInt("_Width", resolution);
                    shader.SetInt("_Height", resolution);
                    shader.SetInt("_RayCount", settings.rayCount);
                    shader.SetFloat("_MaxRayDistance", settings.maxRayDistance);
                    shader.SetFloat("_RayOriginOffset", settings.rayOriginOffset);
                    shader.SetFloat("_Intensity", settings.intensity);

                    bool hasMask = settings.aoMask != null;
                    shader.SetInt("_HasMask", hasMask ? 1 : 0);
                    shader.SetTexture(kernel, "_AOBakeMask",
                        hasMask ? (Texture)settings.aoMask : Texture2D.whiteTexture);

                    int groups = Mathf.CeilToInt(resolution / 8.0f);
                    shader.Dispatch(kernel, groups, groups, 1);
                    return aoRT;
                });
        }

        private Texture2D BakeWithCompute(
            MeshData meshData,
            AOBaker settings,
            int resolution,
            UVSpaceRasterizer rasterizer,
            AOTexturePostFilter postFilter,
            System.Func<UVSpaceRasterizer.RasterizeResult, RenderTexture> computeAO)
        {
            if (meshData.Mesh.uv == null || meshData.Mesh.uv.Length == 0)
            {
                Debug.LogWarning($"[AO Baker] Mesh on '{meshData.Renderer.name}' has no UV. Skipping.");
                return null;
            }

            var rasterResult = rasterizer.Rasterize(meshData.Mesh, meshData.LocalToWorld, resolution);
            var aoRT = computeAO(rasterResult);

            var filteredRT = postFilter.Apply(
                aoRT, rasterResult.Positions,
                settings.blurIterations, settings.blurRadius);

            var aoTex = ConvertToTexture2D(filteredRT, resolution);

            rasterResult.Positions.Release();
            rasterResult.Normals.Release();
            aoRT.Release();
            if (filteredRT != aoRT)
                filteredRT.Release();

            return aoTex;
        }

        private RenderTexture ComputeSSAO(
            UVSpaceRasterizer.RasterizeResult rasterResult,
            DepthMapRenderer.DepthRenderResult depthResult,
            AOBaker settings,
            int resolution,
            int depthTexSize)
        {
            var ssaoShader = AOBakerAssetLoader.LoadComputeShader("SSAOBake");
            int kernel = ssaoShader.FindKernel("BakeAO");

            var aoRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true,
                filterMode = FilterMode.Point
            };
            aoRT.Create();

            var vpBuffer = new ComputeBuffer(depthResult.ViewProjMatrices.Length, 64);
            vpBuffer.SetData(depthResult.ViewProjMatrices);

            var fwdBuffer = new ComputeBuffer(depthResult.CameraForwards.Length, 12);
            fwdBuffer.SetData(depthResult.CameraForwards);

            ssaoShader.SetTexture(kernel, "_WorldPositions", rasterResult.Positions);
            ssaoShader.SetTexture(kernel, "_WorldNormals", rasterResult.Normals);
            ssaoShader.SetTexture(kernel, "_DepthTextures", depthResult.DepthTextureArray);
            ssaoShader.SetBuffer(kernel, "_ViewProjMatrices", vpBuffer);
            ssaoShader.SetBuffer(kernel, "_CameraForwards", fwdBuffer);
            ssaoShader.SetTexture(kernel, "_AOResult", aoRT);

            ssaoShader.SetInt("_Width", resolution);
            ssaoShader.SetInt("_Height", resolution);
            ssaoShader.SetInt("_DirectionCount", settings.cameraDirections);
            ssaoShader.SetInt("_SampleCount", settings.sampleCount);
            ssaoShader.SetFloat("_Radius", settings.radius);
            ssaoShader.SetFloat("_Bias", settings.bias);
            ssaoShader.SetFloat("_Intensity", settings.intensity);
            ssaoShader.SetInt("_DepthTexSize", depthTexSize);

            int groupsX = Mathf.CeilToInt(resolution / 8.0f);
            int groupsY = Mathf.CeilToInt(resolution / 8.0f);
            ssaoShader.Dispatch(kernel, groupsX, groupsY, 1);

            vpBuffer.Release();
            fwdBuffer.Release();

            return aoRT;
        }

        private void ApplyAOToMaterials(MeshData meshData, Texture2D aoTex, AOBaker settings)
        {
            if (_buildContext != null)
            {
                aoTex.name = $"{meshData.Renderer.gameObject.name}_AO";
                UnityEditor.AssetDatabase.AddObjectToAsset(aoTex, _buildContext.AssetContainer);
            }

            if (settings.targetShader == AOTargetShader.VertexColor)
            {
                Debug.Log($"[AO Baker] Applying AO to vertex colors: {meshData.Renderer.name}");
                ShaderAOSlotDetector.BakeAOToVertexColors(meshData.Mesh, aoTex);
                return;
            }

            var originalMats = meshData.Renderer.sharedMaterials;
            var newMats = new Material[originalMats.Length];
            bool anyApplied = false;

            for (int i = 0; i < originalMats.Length; i++)
            {
                var mat = originalMats[i];
                if (mat == null)
                {
                    newMats[i] = null;
                    continue;
                }

                var clonedMat = new Material(mat);
                clonedMat.name = mat.name + "_AO";

                if (_buildContext != null)
                {
                    UnityEditor.AssetDatabase.AddObjectToAsset(clonedMat, _buildContext.AssetContainer);
                }

                bool applied = ShaderAOSlotDetector.TryApplyAO(clonedMat, aoTex, settings);
                if (applied)
                {
                    Debug.Log($"[AO Baker] Applied AO to material '{mat.name}' (shader: {mat.shader.name})");
                    newMats[i] = clonedMat;
                    anyApplied = true;
                }
                else
                {
                    Debug.LogWarning($"[AO Baker] Could not apply AO to material '{mat.name}' " +
                        $"(shader: {mat.shader.name}). No matching AO slot found.");
                    newMats[i] = mat;
                }
            }

            if (anyApplied)
            {
                meshData.Renderer.sharedMaterials = newMats;
                Debug.Log($"[AO Baker] Reassigned materials on '{meshData.Renderer.name}'");
            }
        }

        private static void CleanupOccluderScene(List<GameObject> occluderObjects)
        {
            foreach (var go in occluderObjects)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            occluderObjects.Clear();
        }

        private static Texture2D ConvertToTexture2D(RenderTexture rt, int resolution)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
            tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            tex.Apply(true);

            RenderTexture.active = prev;
            return tex;
        }
    }
}
