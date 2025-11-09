Shader "UI/UICardGlowShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        // グローエフェクト用のプロパティ
        _GlowColor ("Glow Color", Color) = (0.5, 0.8, 1.0, 1.0) // デフォルトは青系の光
        _GlowWidth ("Glow Width", Range(0, 0.1)) = 0.02         // グローの幅（0-1の範囲で正規化）
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.0    // グローの強度
        _IsSelected ("Is Selected", Float) = 0                  // 選択状態 (0:非選択, 1:選択)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha // 通常の透明度ブレンドに加えて、発光を加算する

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc" // UIシェーダーで必要なヘルパー関数を含む

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _GlowColor;
            float _GlowWidth;
            float _GlowIntensity;
            float _IsSelected; // 選択状態を受け取る変数

            sampler2D _MainTex;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // テクスチャの色をサンプリング
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                // グローエフェクトを計算
                fixed4 glow = fixed4(0,0,0,0);
                if (_IsSelected > 0.5) // _IsSelectedが1（選択状態）の場合のみグローを適用
                {
                    // 中心からの距離を計算 (UV座標の中心からの距離)
                    float dist = distance(i.texcoord, float2(0.5, 0.5));
                    
                    // カードのフチに近いほどグローが強く出るようにする
                    // smoothstepを使って、内側は0、外側は1に近づくグラデーションを作る
                    float glowFactor = smoothstep(0.5 - _GlowWidth, 0.5, dist);
                    
                    // グローの形状を調整 (フチで急激に明るくなるように)
                    glowFactor = pow(glowFactor, 2.0) * _GlowIntensity; // powでカーブを急峻に

                    // グローの色と透明度を計算
                    glow = _GlowColor * glowFactor;
                    glow.a = glowFactor; // グローの透明度はグローの強さに比例させる
                }
                
                // 元のカードの色とグローをブレンド
                // 加算ブレンド (SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha) をSubShaderで指定しているので、
                // シェーダー内では単に色を足せば、GPUが自動的に加算してくれます。
                return col + glow;
            }
        ENDCG
        }
    }
}