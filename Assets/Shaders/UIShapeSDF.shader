Shader "UI/ShapeSDF"
{
    // 圆角矩形 + 描边 的 SDF UI 着色器（spec 004 Phase 3）。零纹理、矢量级清晰。
    // 所有参数走顶点数据（uv1=尺寸/半径/描边宽, uv2=描边色, color=填充色），
    // 故所有 UIShape 共用同一材质 → 可合批，加形状不增 DC。
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
            "PreviewType"="Plane" "CanUseSpriteAtlas"="True"
        }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float4 uv0      : TEXCOORD0; // (u,v) 0..1
                float4 uv1      : TEXCOORD1; // (w, h, radius, borderWidth)
                float4 uv2      : TEXCOORD2; // 描边色 rgba
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float4 uv0      : TEXCOORD0;
                float4 uv1      : TEXCOORD1;
                float4 uv2      : TEXCOORD2;
                float4 worldPos : TEXCOORD3;
            };

            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv0 = v.uv0;
                o.uv1 = v.uv1;
                o.uv2 = v.uv2;
                return o;
            }

            // 圆角矩形 SDF：p 为中心坐标，b 为半尺寸，r 为圆角半径
            float sdRoundBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - (b - r);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 size = i.uv1.xy;
                float radius = min(i.uv1.z, min(size.x, size.y) * 0.5);
                float bw = i.uv1.w;

                float2 p = (i.uv0.xy - 0.5) * size;     // 像素中心坐标
                float dist = sdRoundBox(p, size * 0.5, radius);
                float aa = fwidth(dist) + 1e-4;

                float cover = saturate(0.5 - dist / aa); // 形状覆盖（含抗锯齿）
                fixed4 col = i.color;                    // 填充色
                if (bw > 0.0)
                {
                    float innerFill = saturate(0.5 - (dist + bw) / aa); // 描边内侧=填充
                    col = lerp(i.uv2, i.color, innerFill);
                }
                col.a *= cover;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
