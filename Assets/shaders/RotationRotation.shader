Shader "UI/UnlitRotation"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        _RotationSpeed ("Rotation Speed", Float) = 2.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _RotationSpeed;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // Rotation logic happens here on the GPU
                float angle = _Time.y * _RotationSpeed;
                float s = sin(angle);
                float c = cos(angle);
                
                // Rotate UVs around the center (0.5, 0.5)
                float2 uv = v.uv - 0.5;
                float2 rotatedUV;
                rotatedUV.x = uv.x * c - uv.y * s;
                rotatedUV.y = uv.x * s + uv.y * c;
                o.uv = rotatedUV + 0.5;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}