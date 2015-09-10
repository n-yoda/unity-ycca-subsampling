Shader "Custom/Y-CgACo to RGBA"
{
    Properties
    {
        _Y("Y", 2D) = "white" {}
        _CgACo("CgACo", 2D) = "white" {}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _Y;
    sampler2D _CgACo;

    half4 frag(v2f_img i) : SV_Target 
    {
        half y = tex2D(_Y, i.uv).a;
        half3 x = tex2D(_CgACo, i.uv).xyz;
        half cg = x.x;
        half co = x.z;
        return half4(
            y - cg + co,
            y + cg - 0.5,
            y - cg - co + 1,
            x.y
        );
    }

    ENDCG

    SubShader
    {
        Tags { "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            ENDCG
        }
    }
}
