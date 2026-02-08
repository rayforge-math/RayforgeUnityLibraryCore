Shader "Rayforge/HeightmapProjection"
{
    SubShader
    {
        BlendOp Max
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float worldY : TEXCOORD0;
            };

            //float4x4 unity_MatrixVP;

            CBUFFER_START(_Params)
                float _BakerReferenceY;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.pos = mul(unity_MatrixVP, worldPos);
                
                o.worldY = worldPos.y - _BakerReferenceY;
                
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return float4(i.worldY, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}