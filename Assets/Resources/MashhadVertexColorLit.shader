Shader "Mashhad/VertexColorLit"
{
    Properties
    {
        _MainTex ("Unused", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows nolightmap
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        // Vertex colour = albedo. uv.x = metallic, uv.y = smoothness, 1 - alpha = emission strength / 3.
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 albedo = IN.color.rgb;
            #if !defined(UNITY_COLORSPACE_GAMMA)
            albedo = GammaToLinearSpace(albedo);
            #endif
            o.Albedo = albedo;
            o.Metallic = saturate(IN.uv_MainTex.x);
            o.Smoothness = saturate(IN.uv_MainTex.y);
            o.Emission = albedo * (1.0 - IN.color.a) * 3.0;
            o.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
