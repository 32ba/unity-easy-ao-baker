Shader "Hidden/EasyAOBaker/UVRasterize"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #include "UnityCG.cginc"

            // ミラーUV等で同一テクセルを複数三角形が覆う場合、
            // それぞれを別スロットに保存し、後段のAO計算で平均化する。
            RWTexture2DArray<float4> _OutPositions : register(u1);
            RWTexture2DArray<float4> _OutNormals : register(u2);
            RWTexture2D<uint> _OutCoverage : register(u3);

            uint _MaxLayers;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNrm : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = float4(v.uv.x * 2.0 - 1.0, v.uv.y * 2.0 - 1.0, 0.5, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                o.pos.y = -o.pos.y;
                #endif
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNrm = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                return o;
            }

            // SV_Target: 有効マスク（JFAパディング用）。任意の被覆位置に 1 を書く
            float4 frag(v2f i) : SV_Target
            {
                uint2 px = uint2(i.pos.xy);
                uint slot;
                InterlockedAdd(_OutCoverage[px], 1, slot);
                if (slot < _MaxLayers)
                {
                    _OutPositions[uint3(px, slot)] = float4(i.worldPos, 1.0);
                    _OutNormals[uint3(px, slot)] = float4(i.worldNrm, 1.0);
                }
                return float4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}
