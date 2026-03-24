Shader "Custom/PixelFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Float) = 1.0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _PixelSize;

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate the screen width and height in pixels
                float2 res = _ScreenParams.xy;
                
                // Divide the UVs into a grid based on PixelSize
                float2 pixelatedUV = i.uv * res;
                pixelatedUV = floor(pixelatedUV / _PixelSize) * _PixelSize;
                pixelatedUV /= res;

                // Sample the texture at the new "snapped" UV coordinate
                fixed4 col = tex2D(_MainTex, pixelatedUV);
                return col;
            }
            ENDCG
        }
    }
}