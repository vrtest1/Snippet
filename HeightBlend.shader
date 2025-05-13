Shader "Custom/HeightBlend_LocalY"
{
    Properties
    {
        _TexLow("Low Texture", 2D) = "white" {}
        _TexHigh("High Texture", 2D) = "white" {}
        _BlendStart("Blend Start Height", Float) = 0.0
        _BlendEnd("Blend End Height", Float) = 1.0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                sampler2D _TexLow;
                sampler2D _TexHigh;
                float _BlendStart;
                float _BlendEnd;

                struct appdata {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f {
                    float2 uv : TEXCOORD0;
                    float heightLocalY : TEXCOORD1;
                    float4 vertex : SV_POSITION;
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;

                    // オブジェクト空間の高さ（Y）をそのまま渡す
                    o.heightLocalY = v.vertex.y;

                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float blend = saturate((i.heightLocalY - _BlendStart) / (_BlendEnd - _BlendStart));
                    fixed4 lowTex = tex2D(_TexLow, i.uv);
                    fixed4 highTex = tex2D(_TexHigh, i.uv);

                    return lerp(lowTex, highTex, blend);
                }
                ENDCG
            }
        }
}
