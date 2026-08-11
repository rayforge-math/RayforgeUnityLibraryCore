Shader "Rayforge/HeightmapProjection"
{
    SubShader
    {
        BlendOp Max
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

        CBUFFER_START(_Params)
            float4 _BakerYParams;  // x: reference y, y: range min max y
            float4 _TerrainUvParams;
            float4 _TerrainYParams;
        CBUFFER_END

        CBUFFER_START(UnityPerDraw)
            float4x4 unity_ObjectToWorld;
        CBUFFER_END

        float4x4 unity_MatrixVP;
        
        ENDHLSL

        // --- Pass 0: Standard Mesh Baking ---
        Pass
        {
            Name "MeshBaking"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float normalizedY : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.pos = mul(unity_MatrixVP, worldPos);
                o.normalizedY = (worldPos.y - _BakerYParams.x) * _BakerYParams.w;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(i.normalizedY, 0, 0, 1);
            }
            ENDHLSL
        }

        // --- Pass 1: Procedural Terrain Baking ---
        Pass
        {
            Name "TerrainBaking"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.rayforge.core/Runtime/ShaderLibrary/Rendering/FullscreenTriangle.hlsl"

            TEXTURE2D(_TerrainHeightmap);
            SAMPLER(sampler_PointClamp);

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (uint vID : SV_VertexID)
            {
                v2f o;

                FullscreenTriangle(vID, o.pos, o.uv);
                o.uv = (o.uv - _TerrainUvParams.xy) * _TerrainUvParams.zw;
    
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                if (i.uv.x < 0 || i.uv.x > 1 || i.uv.y < 0 || i.uv.y > 1)
                    return float4(0, 0, 0, 1);

                float rawHeight = SAMPLE_TEXTURE2D(_TerrainHeightmap, sampler_PointClamp, i.uv).r;
                float worldHeight = _TerrainYParams.x + rawHeight * _TerrainYParams.y;
                float normalizedY = (worldHeight - _BakerYParams.x) * _BakerYParams.w;
    
                return float4(normalizedY, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}