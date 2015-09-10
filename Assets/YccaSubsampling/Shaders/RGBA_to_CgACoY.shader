Shader "Hidden/RGBA to CgACoY"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;

    float4 frag(v2f_img i) : SV_Target 
    {
        float4 c = tex2D(_MainTex, i.uv);
        float3 c2 = c.rgb / 2;
        float3 c4 = c.rgb / 4;
        float y = c4.r + c2.g + c4.b;
        float cg = -c4.r + c2.g - c4.b + 0.5;
        float co = c2.r - c2.b + 0.5;
        return float4 (cg, c.a, co, y);
    }

    ENDCG

    SubShader
    {
        Tags { "Queue"="Transparent" }
        Blend One Zero
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}
