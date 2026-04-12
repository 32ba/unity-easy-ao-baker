using UnityEngine;

namespace net._32ba.AOBaker.Editor
{
    public class AOTexturePostFilter
    {
        private readonly ComputeShader _blurShader;
        private readonly ComputeShader _paddingShader;
        private readonly int _jfaInitKernel;
        private readonly int _jfaStepKernel;
        private readonly int _jfaApplyKernel;
        private readonly int _blurHUnmaskedKernel;
        private readonly int _blurVUnmaskedKernel;

        public AOTexturePostFilter()
        {
            _blurShader = AOBakerAssetLoader.LoadComputeShader("AOBlur");
            _paddingShader = AOBakerAssetLoader.LoadComputeShader("AOPadding");
            _jfaInitKernel = _paddingShader.FindKernel("JFAInit");
            _jfaStepKernel = _paddingShader.FindKernel("JFAStep");
            _jfaApplyKernel = _paddingShader.FindKernel("JFAApply");
            _blurHUnmaskedKernel = _blurShader.FindKernel("BlurHorizontalUnmasked");
            _blurVUnmaskedKernel = _blurShader.FindKernel("BlurVerticalUnmasked");
        }

        public RenderTexture Apply(
            RenderTexture aoInput,
            RenderTexture validMask,
            int blurIterations,
            float blurRadius)
        {
            int width = aoInput.width;
            int height = aoInput.height;

            var padded = ApplyJFAPadding(aoInput, validMask, width, height);

            var result = padded;
            for (int i = 0; i < blurIterations; i++)
            {
                var blurred = ApplyBlurUnmasked(result, width, height, blurRadius);
                if (result != padded)
                    result.Release();
                result = blurred;
            }

            // ブラー後の再パディング
            if (blurIterations > 0)
            {
                var rePadded = ApplyJFAPadding(result, validMask, width, height);
                if (result != padded)
                    result.Release();
                result = rePadded;
            }

            if (padded != result)
                padded.Release();

            return result;
        }

        /// <summary>
        /// Jump Flood Algorithm でパディング。
        /// O(log2(max(width,height))) パスで全テクセルの最近傍有効テクセルを求める。
        /// </summary>
        private RenderTexture ApplyJFAPadding(
            RenderTexture aoInput,
            RenderTexture validMask,
            int width, int height)
        {
            int groupsX = Mathf.CeilToInt(width / 8.0f);
            int groupsY = Mathf.CeilToInt(height / 8.0f);

            // シードマップ (int2: 最近傍有効テクセルの座標)
            var seedA = CreateSeedRT(width, height);
            var seedB = CreateSeedRT(width, height);

            _paddingShader.SetInt("_Width", width);
            _paddingShader.SetInt("_Height", height);

            // 初期化: 有効テクセル→自座標、無効→(-1,-1)
            _paddingShader.SetTexture(_jfaInitKernel, "_ValidMask", validMask);
            _paddingShader.SetTexture(_jfaInitKernel, "_SeedMap", seedA);
            _paddingShader.Dispatch(_jfaInitKernel, groupsX, groupsY, 1);

            // JFAステップ: ステップサイズを半減しながら伝搬
            int maxDim = Mathf.Max(width, height);
            int stepSize = Mathf.NextPowerOfTwo(maxDim) / 2;
            bool pingPong = true;

            while (stepSize >= 1)
            {
                var src = pingPong ? seedA : seedB;
                var dst = pingPong ? seedB : seedA;

                _paddingShader.SetInt("_StepSize", stepSize);
                _paddingShader.SetTexture(_jfaStepKernel, "_SeedInput", src);
                _paddingShader.SetTexture(_jfaStepKernel, "_SeedOutput", dst);
                _paddingShader.Dispatch(_jfaStepKernel, groupsX, groupsY, 1);

                pingPong = !pingPong;
                stepSize /= 2;
            }

            // 最終シードマップから値をコピー
            var finalSeed = pingPong ? seedA : seedB;
            var output = CreateTempRT(width, height);

            _paddingShader.SetTexture(_jfaApplyKernel, "_AOInput", aoInput);
            _paddingShader.SetTexture(_jfaApplyKernel, "_ValidMask", validMask);
            _paddingShader.SetTexture(_jfaApplyKernel, "_FinalSeedMap", finalSeed);
            _paddingShader.SetTexture(_jfaApplyKernel, "_AOOutput", output);
            _paddingShader.Dispatch(_jfaApplyKernel, groupsX, groupsY, 1);

            if (RenderTexture.active == seedA || RenderTexture.active == seedB)
                RenderTexture.active = null;

            seedA.Release();
            seedB.Release();

            return output;
        }

        private RenderTexture ApplyBlurUnmasked(
            RenderTexture input,
            int width, int height,
            float blurRadius)
        {
            var temp = CreateTempRT(width, height);
            var output = CreateTempRT(width, height);

            int groupsX = Mathf.CeilToInt(width / 8.0f);
            int groupsY = Mathf.CeilToInt(height / 8.0f);

            _blurShader.SetInt("_Width", width);
            _blurShader.SetInt("_Height", height);
            _blurShader.SetFloat("_BlurRadius", blurRadius);

            _blurShader.SetTexture(_blurHUnmaskedKernel, "_Input", input);
            _blurShader.SetTexture(_blurHUnmaskedKernel, "_Output", temp);
            _blurShader.Dispatch(_blurHUnmaskedKernel, groupsX, groupsY, 1);

            _blurShader.SetTexture(_blurVUnmaskedKernel, "_Input", temp);
            _blurShader.SetTexture(_blurVUnmaskedKernel, "_Output", output);
            _blurShader.Dispatch(_blurVUnmaskedKernel, groupsX, groupsY, 1);

            temp.Release();
            return output;
        }

        private static RenderTexture CreateTempRT(int width, int height)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true
            };
            rt.Create();
            return rt;
        }

        private static RenderTexture CreateSeedRT(int width, int height)
        {
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.RGInt)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true
            };
            rt.Create();
            return rt;
        }
    }
}
