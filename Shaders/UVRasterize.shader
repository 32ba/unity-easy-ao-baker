Shader "Hidden/AOBaker/UVRasterize"
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
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"

            float _TexelSize; // 1.0 / resolution

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2g
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNrm : TEXCOORD1;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNrm : TEXCOORD1;
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.pos = float4(v.uv.x * 2.0 - 1.0, v.uv.y * 2.0 - 1.0, 0.5, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                o.pos.y = -o.pos.y;
                #endif
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNrm = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                return o;
            }

            // 保守的ラスタライズ: 三角形の辺をテクセルサイズ分だけ外側に膨張させる
            // これによりUVシーム上のギャップを防ぐ
            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> stream)
            {
                // 三角形の辺ベクトルと法線から膨張方向を計算
                float2 edge0 = input[1].pos.xy - input[0].pos.xy;
                float2 edge1 = input[2].pos.xy - input[1].pos.xy;
                float2 edge2 = input[0].pos.xy - input[2].pos.xy;

                // 各頂点の膨張方向（隣接2辺の法線の平均）
                float2 expand0 = normalize(float2(-edge2.y, edge2.x) + float2(-edge0.y, edge0.x));
                float2 expand1 = normalize(float2(-edge0.y, edge0.x) + float2(-edge1.y, edge1.x));
                float2 expand2 = normalize(float2(-edge1.y, edge1.x) + float2(-edge2.y, edge2.x));

                // 三角形の向き判定（反時計回りなら反転）
                float cross = edge0.x * edge1.y - edge0.y * edge1.x;
                float sign = cross > 0 ? 1.0 : -1.0;

                // テクセルサイズの2倍分膨張（十分なマージン）
                float expandAmount = _TexelSize * 2.0;

                g2f o;

                o.pos = input[0].pos;
                o.pos.xy += expand0 * expandAmount * sign;
                o.worldPos = input[0].worldPos;
                o.worldNrm = input[0].worldNrm;
                stream.Append(o);

                o.pos = input[1].pos;
                o.pos.xy += expand1 * expandAmount * sign;
                o.worldPos = input[1].worldPos;
                o.worldNrm = input[1].worldNrm;
                stream.Append(o);

                o.pos = input[2].pos;
                o.pos.xy += expand2 * expandAmount * sign;
                o.worldPos = input[2].worldPos;
                o.worldNrm = input[2].worldNrm;
                stream.Append(o);
            }

            void frag(g2f i,
                out float4 position : SV_Target0,
                out float4 normal : SV_Target1)
            {
                position = float4(i.worldPos, 1.0);
                normal = float4(i.worldNrm, 1.0);
            }
            ENDCG
        }
    }
}
