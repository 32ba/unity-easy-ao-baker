using System;
using System.Collections.Generic;
using UnityEngine;

namespace net._32ba.EasyAOBaker.Editor
{
    public static class BVHBuilder
    {
        private const int MaxLeafTriangles = 4;
        private const int SAHBinCount = 12;

        public struct BVHData : IDisposable
        {
            public ComputeBuffer NodesBuffer;
            public ComputeBuffer TrianglesBuffer;

            public void Dispose()
            {
                NodesBuffer?.Release();
                TrianglesBuffer?.Release();
            }
        }

        // GPU側と一致するレイアウト (32 bytes)
        private struct BVHNode
        {
            public Vector3 BoundsMin;
            public int LeftFirst;
            public Vector3 BoundsMax;
            public int Count; // >0: leaf (tri count), 0: internal
        }

        // GPU側と一致するレイアウト (36 bytes)
        private struct BVHTriangle
        {
            public Vector3 V0, V1, V2;
        }

        private struct TriInfo
        {
            public int TriIndex;
            public Vector3 Centroid;
            public Bounds AABB;
        }

        public static BVHData Build(List<Mesh> meshes, List<Matrix4x4> transforms)
        {
            var triangles = CollectTriangles(meshes, transforms);
            var triInfos = new TriInfo[triangles.Count];

            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                var bounds = new Bounds(tri.V0, Vector3.zero);
                bounds.Encapsulate(tri.V1);
                bounds.Encapsulate(tri.V2);
                triInfos[i] = new TriInfo
                {
                    TriIndex = i,
                    Centroid = (tri.V0 + tri.V1 + tri.V2) / 3f,
                    AABB = bounds
                };
            }

            var nodes = new List<BVHNode>();
            var sortedTriangles = new BVHTriangle[triangles.Count];
            int triWriteIndex = 0;

            BuildRecursive(nodes, triInfos, triangles, sortedTriangles, ref triWriteIndex, 0, triangles.Count);

            var nodesBuffer = new ComputeBuffer(nodes.Count, 32);
            nodesBuffer.SetData(nodes.ToArray());

            var trisBuffer = new ComputeBuffer(sortedTriangles.Length, 36);
            trisBuffer.SetData(sortedTriangles);

            return new BVHData
            {
                NodesBuffer = nodesBuffer,
                TrianglesBuffer = trisBuffer
            };
        }

        private static List<BVHTriangle> CollectTriangles(List<Mesh> meshes, List<Matrix4x4> transforms)
        {
            var triangles = new List<BVHTriangle>();

            for (int m = 0; m < meshes.Count; m++)
            {
                var mesh = meshes[m];
                var mtx = transforms[m];
                var vertices = mesh.vertices;

                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var indices = mesh.GetTriangles(sub);
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        triangles.Add(new BVHTriangle
                        {
                            V0 = mtx.MultiplyPoint3x4(vertices[indices[i]]),
                            V1 = mtx.MultiplyPoint3x4(vertices[indices[i + 1]]),
                            V2 = mtx.MultiplyPoint3x4(vertices[indices[i + 2]])
                        });
                    }
                }
            }

            return triangles;
        }

        private static int BuildRecursive(
            List<BVHNode> nodes,
            TriInfo[] infos,
            List<BVHTriangle> srcTriangles,
            BVHTriangle[] sortedTriangles,
            ref int triWriteIndex,
            int start, int count)
        {
            int nodeIndex = nodes.Count;
            var node = new BVHNode();

            // AABBを計算
            var bounds = infos[start].AABB;
            for (int i = start + 1; i < start + count; i++)
                bounds.Encapsulate(infos[i].AABB);

            node.BoundsMin = bounds.min;
            node.BoundsMax = bounds.max;

            if (count <= MaxLeafTriangles)
            {
                // リーフノード
                node.LeftFirst = triWriteIndex;
                node.Count = count;
                for (int i = start; i < start + count; i++)
                    sortedTriangles[triWriteIndex++] = srcTriangles[infos[i].TriIndex];

                nodes.Add(node);
                return nodeIndex;
            }

            // SAHで最適な分割軸・位置を探す
            int bestAxis = -1;
            int bestSplit = -1;
            float bestCost = float.MaxValue;

            var centroidBounds = new Bounds(infos[start].Centroid, Vector3.zero);
            for (int i = start + 1; i < start + count; i++)
                centroidBounds.Encapsulate(infos[i].Centroid);

            for (int axis = 0; axis < 3; axis++)
            {
                float axisMin = centroidBounds.min[axis];
                float axisMax = centroidBounds.max[axis];
                if (Mathf.Approximately(axisMin, axisMax)) continue;

                float binSize = (axisMax - axisMin) / SAHBinCount;
                var binCounts = new int[SAHBinCount];
                var binBounds = new Bounds[SAHBinCount];
                for (int b = 0; b < SAHBinCount; b++)
                    binBounds[b] = new Bounds();

                for (int i = start; i < start + count; i++)
                {
                    int bin = Mathf.Clamp((int)((infos[i].Centroid[axis] - axisMin) / binSize), 0, SAHBinCount - 1);
                    binCounts[bin]++;
                    if (binCounts[bin] == 1)
                        binBounds[bin] = infos[i].AABB;
                    else
                        binBounds[bin].Encapsulate(infos[i].AABB);
                }

                // 各分割位置のコスト計算
                for (int split = 1; split < SAHBinCount; split++)
                {
                    int leftCount = 0;
                    var leftBounds = new Bounds();
                    bool leftInit = false;
                    for (int b = 0; b < split; b++)
                    {
                        if (binCounts[b] == 0) continue;
                        leftCount += binCounts[b];
                        if (!leftInit) { leftBounds = binBounds[b]; leftInit = true; }
                        else leftBounds.Encapsulate(binBounds[b]);
                    }

                    int rightCount = 0;
                    var rightBounds = new Bounds();
                    bool rightInit = false;
                    for (int b = split; b < SAHBinCount; b++)
                    {
                        if (binCounts[b] == 0) continue;
                        rightCount += binCounts[b];
                        if (!rightInit) { rightBounds = binBounds[b]; rightInit = true; }
                        else rightBounds.Encapsulate(binBounds[b]);
                    }

                    if (leftCount == 0 || rightCount == 0) continue;

                    float cost = leftCount * SurfaceArea(leftBounds) + rightCount * SurfaceArea(rightBounds);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestAxis = axis;
                        bestSplit = split;
                    }
                }
            }

            // 分割できない場合はリーフに
            if (bestAxis < 0)
            {
                node.LeftFirst = triWriteIndex;
                node.Count = count;
                for (int i = start; i < start + count; i++)
                    sortedTriangles[triWriteIndex++] = srcTriangles[infos[i].TriIndex];
                nodes.Add(node);
                return nodeIndex;
            }

            // パーティション
            float splitMin = centroidBounds.min[bestAxis];
            float splitSize = (centroidBounds.max[bestAxis] - splitMin) / SAHBinCount;
            float splitPos = splitMin + bestSplit * splitSize;

            int mid = Partition(infos, start, count, bestAxis, splitPos);
            if (mid == start || mid == start + count)
                mid = start + count / 2; // フォールバック

            // ノード予約（左子は nodeIndex+1 で暗黙、右子インデックスを後で格納）
            nodes.Add(node);

            // 左子は直後に作成される（DFS順序で nodeIndex + 1）
            BuildRecursive(nodes, infos, srcTriangles, sortedTriangles, ref triWriteIndex, start, mid - start);
            int rightChild = BuildRecursive(nodes, infos, srcTriangles, sortedTriangles, ref triWriteIndex, mid, start + count - mid);

            // 内部ノード: LeftFirst = 右子インデックス, Count = 0
            var n = nodes[nodeIndex];
            n.LeftFirst = rightChild;
            n.Count = 0;
            nodes[nodeIndex] = n;

            return nodeIndex;
        }

        private static int Partition(TriInfo[] infos, int start, int count, int axis, float splitPos)
        {
            int left = start;
            int right = start + count - 1;
            while (left <= right)
            {
                if (infos[left].Centroid[axis] < splitPos)
                    left++;
                else
                {
                    (infos[left], infos[right]) = (infos[right], infos[left]);
                    right--;
                }
            }
            return left;
        }

        private static float SurfaceArea(Bounds b)
        {
            var s = b.size;
            return 2f * (s.x * s.y + s.y * s.z + s.z * s.x);
        }
    }
}
