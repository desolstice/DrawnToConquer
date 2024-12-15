Shader "Unlit/SplatShader2"
{
    Properties
    {
        _MainTex ("Splat Map", 2D) = "white" {}
        _Texture1 ("Texture 1", 2D) = "white" {}
        _Texture2 ("Texture 2", 2D) = "white" {}
        _Texture3 ("Texture 3", 2D) = "white" {}
        _Texture4 ("Texture 4", 2D) = "white" {}
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
            };

            sampler2D _MainTex;
            sampler2D _Texture1;
            sampler2D _Texture2;
            sampler2D _Texture3;
            sampler2D _Texture4;

            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 splatmapMask = tex2D(_MainTex, i.uv);

                fixed4 combinedColor = tex2D (_Texture1, i.uv) * splatmapMask.r;
                combinedColor += tex2D (_Texture2, i.uv) * splatmapMask.g;
                combinedColor += tex2D (_Texture3, i.uv) * splatmapMask.b;
                combinedColor += tex2D (_Texture4, i.uv) * splatmapMask.a;

                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, combinedColor);
                return combinedColor;
            }
            ENDCG
        }
    }
}
