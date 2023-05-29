Shader "Custom/CSM"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}        
        _Bias ("Bias" , Float) = 0.0001
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                                
                //float4 lightpos : TEXCOORD1;

                float4 worldpos :TEXCOORD2;

            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Bias;            

            sampler2D _LightDepth;                
            float4x4 _CM;

            v2f vert (appdata v)
            {
                v2f o;                                
                
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.worldpos = mul(unity_ObjectToWorld, v.vertex);                

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 lightpos;
                                
                float2 lightuv;                

                fixed4 depthcol = fixed4(0,0,0,1);                
                

                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);                

                //depthcol = tex2D(_LightDepth0, depthuv);

                lightpos = mul(_CM, i.worldpos);
                lightuv = lightpos.xy / lightpos.w * 0.5 + 0.5;

                //读取灯光相机深度图
                depthcol = tex2D(_LightDepth, lightuv);

                //fixed4 depthcol = tex2D(_LightDepth0, i.lightpos.xy / i.lightpos.w * 0.5 + 0.5);
                float lightdepth = DecodeFloatRGBA(depthcol);

                float depth = lightpos.z / lightpos.w * 0.5 + 0.5;

                if (depth > lightdepth + _Bias)
                {
                    col *= 0.5;
                    //col = fixed4(1, 0, 0, 1);
                }
                    
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                
                col.w = 1;
                return col;
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
    
}
