Shader "Custom/Y8CgCoA4"
{
    Properties
    {
        _Y("Y", 2D) = "white" {}
        _CgCoA("CgCoA", 2D) = "white" {}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _Y;
    sampler2D _CgCoA;

    half4 frag(v2f_img i) : SV_Target 
    {
        half y = tex2D(_Y, i.uv).a;
        half3 cgcoa = tex2D(_CgCoA, i.uv).rgb;
        return half4(
            y - cgcoa.x + cgcoa.y,
            y + cgcoa.x - 0.5,
            y - cgcoa.x - cgcoa.y + 1,
            cgcoa.z);
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
