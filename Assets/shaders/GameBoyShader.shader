Shader "Hidden/GameBoyFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // Change: Removed Blend to prevent "ghosting" from previous frames
        Cull Off ZWrite On ZTest Always 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; float4 screenPos : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // --- Bayer Dithering Matrix ---
                float4x4 bayer = float4x4(
                    0.0, 8.0, 2.0, 10.0,
                    12.0, 4.0, 14.0, 6.0,
                    3.0, 11.0, 1.0, 9.0,
                    15.0, 7.0, 13.0, 5.0
                ) / 16.0;

                uint2 pixelPos = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy;
                float dither = bayer[pixelPos.x % 4][pixelPos.y % 4];

                // Improved Luminance calculation to avoid black-out at start
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                lum = saturate(lum + (dither - 0.5) * 0.1);

                // Darkened 6-Color Palette
                fixed4 c1 = fixed4(0.02, 0.10, 0.02, 1.0); 
                fixed4 c2 = fixed4(0.06, 0.18, 0.06, 1.0); 
                fixed4 c3 = fixed4(0.15, 0.30, 0.15, 1.0); 
                fixed4 c4 = fixed4(0.30, 0.45, 0.15, 1.0); 
                fixed4 c5 = fixed4(0.45, 0.55, 0.05, 1.0); 
                fixed4 c6 = fixed4(0.55, 0.65, 0.05, 1.0); 

                if (lum < 0.16) return c1;
                if (lum < 0.33) return c2;
                if (lum < 0.50) return c3;
                if (lum < 0.66) return c4;
                if (lum < 0.83) return c5;
                return c6;
            }
            ENDCG
        }
    }
}