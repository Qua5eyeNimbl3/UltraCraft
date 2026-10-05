Shader "UltraCraft/World" {
    Properties {
        _MainTex ("Minecraft texture", 2D) = "white" {}
        _MinecraftLightMap ("Minecraft lightmap", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _AlphaClip ("Alpha cutout", Float) = 0
        _Cutoff ("Alpha threshold", Range(0,1)) = 0.1
        _ShadowStrength ("Sun shadow strength", Range(0,1)) = 0.55
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Culling", Float) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 0
        _ZWrite ("Write depth", Float) = 1
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_Cull]
        Pass {
            Name "World"
            Tags { "LightMode"="ForwardBase" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            sampler2D _MainTex, _MinecraftLightMap;
            float4 _Color;
            float _AlphaClip, _Cutoff, _ShadowStrength;
            // Set globally by the plugin (alpha 0 = no fog): no fog keyword variants to be stripped.
            float4 _UltraCraftFogColor; float _UltraCraftFogStart, _UltraCraftFogEnd;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 light:TEXCOORD1; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 light:TEXCOORD1; float4 color:COLOR; float3 worldPos:TEXCOORD2; SHADOW_COORDS(3) };
            v2f vert(appdata v) {
                v2f o;
                // Ordinary perspective transforms: no pixel snapping, affine UVs or vertex warp.
                o.pos=UnityObjectToClipPos(v.vertex);
                o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.uv=v.uv; o.light=v.light; o.color=v.color*_Color;
                TRANSFER_SHADOW(o);
                return o;
            }
            fixed4 frag(v2f i):SV_Target {
                fixed4 color=tex2D(_MainTex,i.uv)*i.color;
                if(_AlphaClip>0.5) clip(color.a-_Cutoff);
                color.rgb*=tex2D(_MinecraftLightMap,i.light).rgb;
                // Torch light remains visible; outdoor skylight also receives the sun's shadow.
                float sunlight=saturate((i.light.y*16.0-4.5)/11.0);
                color.rgb*=lerp(1.0-_ShadowStrength*sunlight,1.0,SHADOW_ATTENUATION(i));
                // Distance fog: day haze at the view edge, blood-red at night.
                float fog=saturate((distance(i.worldPos,_WorldSpaceCameraPos)-_UltraCraftFogStart)/max(_UltraCraftFogEnd-_UltraCraftFogStart,0.001))*_UltraCraftFogColor.a;
                color.rgb=lerp(color.rgb,_UltraCraftFogColor.rgb,fog);
                return color;
            }
            ENDCG
        }
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            Blend Off
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _AlphaClip,_Cutoff;
            struct v2f { V2F_SHADOW_CASTER; float2 uv:TEXCOORD1; };
            v2f vertShadow(appdata_base v) { v2f o; o.uv=v.texcoord; TRANSFER_SHADOW_CASTER_NORMALOFFSET(o); return o; }
            float4 fragShadow(v2f i):SV_Target { if(_AlphaClip>0.5)clip(tex2D(_MainTex,i.uv).a-_Cutoff); SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
    Fallback Off
}
