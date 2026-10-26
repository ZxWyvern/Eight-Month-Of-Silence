#ifndef PSX_URP_LITE_COMMON
#define PSX_URP_LITE_COMMON

// Material properties (SRP-batcher friendly).
CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    float4 _MainTex_ST;
    half _ObjectDithering;
#ifdef PSX_VERTEX_LIT
    half4 _EmissionColor;
    float4 _EmissiveTex_ST;
    half _FlatShading;
#endif
#ifdef PSX_CUTOUT
    half _Cutoff;
#endif
#ifdef PSX_CUBEMAP
    half4 _CubemapColor;
#endif
CBUFFER_END

TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
#ifdef PSX_VERTEX_LIT
TEXTURE2D(_EmissiveTex); SAMPLER(sampler_EmissiveTex);
#endif
#ifdef PSX_CUBEMAP
TEXTURECUBE(_Cubemap); SAMPLER(sampler_Cubemap);
TEXTURE2D(_ReflectionMap); SAMPLER(sampler_ReflectionMap);
#endif

// Kit globals: PSXShaderManager keeps driving these (pipeline-agnostic).
float _PSX_GridSize;
half _PSX_VertexWobbleMode;
half _PSX_TextureWarpingFactor;
half _PSX_TextureWarpingMode;
half _PSX_LightingNormalFactor;
half _PSX_LightFalloffPercent;
float _PSX_ObjectDithering;

float3 PSXSnapVertexToGrid(float3 v)
{
    return _PSX_GridSize < 0.00001f ? v : (floor(v * _PSX_GridSize) / _PSX_GridSize);
}

float4 PSXCalculateAffineUV(float3 viewPos, float2 uv)
{
    float affineFactor = _PSX_TextureWarpingMode < 0.5f
        ? length(viewPos)
        : max(mul(UNITY_MATRIX_P, float4(viewPos, 1.0)).w, 0.1);
    affineFactor = lerp(1.0, affineFactor, _PSX_TextureWarpingFactor);
    return float4(uv * affineFactor, affineFactor, 0.0);
}

int PSXDitherOffset(int2 p)
{
    const int m[16] = { -4, 0, -3, 1, 2, -2, 3, -1, -3, 1, -4, 0, 3, -1, 2, -2 };
    return m[p.x % 4 + (p.y % 4) * 4];
}

half4 PSXDitherColor(half4 color, int2 p)
{
    int4 c = (int4)round(color * 255.0);
    c = (c + PSXDitherOffset(p)) >> 3;
    return (half4)c / 31.0h;
}

// Retro vertex lighting on URP lights. Deviation from kit (documented):
// falloff uses URP physical distanceAttenuation blended by the kit's
// slider, since built-in light-range tables don't exist in URP.
half3 PSXShadeLights(float3 worldPos, half3 n)
{
#ifdef PSX_ENABLE_CUSTOM_VERTEX_LIGHTING
    half3 lightColor = SampleSH(n);
    Light mainLight = GetMainLight();
        float mainDiff = max(dot(n, mainLight.direction), 0.0h);
        lightColor += mainLight.color * lerp(1.0h, mainDiff, _PSX_LightingNormalFactor);
        int lightCount = GetAdditionalLightsCount();
        lightCount = lightCount < 4 ? lightCount : 4;
        for (int i = 0; i < lightCount; i++)
    {
        Light light = GetAdditionalLight(i, worldPos);
        float diff = max(dot(n, light.direction), 0.0h);
        float atten = lerp(1.0h, light.distanceAttenuation, _PSX_LightFalloffPercent);
        lightColor += light.color * atten * 0.25h * lerp(1.0h, diff, _PSX_LightingNormalFactor);
    }
    return lightColor;
#else
    half3 lightColor = SampleSH(n);
    Light mainLight = GetMainLight();
    lightColor += mainLight.color * max(dot(n, mainLight.direction), 0.0h);
    int lightCount = GetAdditionalLightsCount();
    lightCount = lightCount < 4 ? lightCount : 4;
    for (int i = 0; i < lightCount; i++)
    {
        Light light = GetAdditionalLight(i, worldPos);
        lightColor += light.color * light.distanceAttenuation * max(dot(n, light.direction), 0.0h);
    }
    return lightColor;
#endif
}

struct PSXAttributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PSXVaryings
{
    float4 positionCS : SV_POSITION;
    float4 affineUV : TEXCOORD0;
    float fogFactor : TEXCOORD1;
#ifdef PSX_VERTEX_LIT
    half4 vlight : COLOR0;
    float4 affineUV2 : TEXCOORD2;
    float3 worldPos : TEXCOORD3;
    float3 worldN : TEXCOORD5;
#endif
#ifdef PSX_CUBEMAP
    float3 reflDir : TEXCOORD4;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

PSXVaryings PSXVert(PSXAttributes v)
{
    PSXVaryings o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);

    float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
    float3 viewPos = TransformWorldToView(worldPos);

#ifdef PSX_VERTEX_LIT
    float3 worldN = TransformObjectToWorldNormal(v.normalOS);
    o.vlight = half4(PSXShadeLights(worldPos, worldN), 1.0h);
    o.affineUV2 = PSXCalculateAffineUV(viewPos, TRANSFORM_TEX(v.uv, _EmissiveTex));
    o.worldPos = worldPos;
    o.worldN = worldN;
#endif
    o.affineUV = PSXCalculateAffineUV(viewPos, TRANSFORM_TEX(v.uv, _MainTex));

#ifdef PSX_CUBEMAP
    float3 viewDirW = worldPos - _WorldSpaceCameraPos;
    float3 normalW = TransformObjectToWorldNormal(v.normalOS);
    o.reflDir = reflect(viewDirW, normalW);
#endif

    float4 viewSnapped = mul(UNITY_MATRIX_P, float4(PSXSnapVertexToGrid(viewPos), 1.0));
    float4 clipPos = mul(UNITY_MATRIX_P, float4(viewPos, 1.0));
    clipPos.xy = PSXSnapVertexToGrid(clipPos.xyz).xy;
    o.positionCS = lerp(viewSnapped, clipPos, _PSX_VertexWobbleMode);
    o.fogFactor = ComputeFogFactor(o.positionCS.z);
    return o;
}

half4 PSXFrag(PSXVaryings i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);

    half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.affineUV.xy / i.affineUV.z) * _Color;

#ifdef PSX_VERTEX_LIT
    // Flat shading: face facet in center mode, smooth per-pixel in average
    // mode (triangle average needs a geometry stage, unavailable in URP).
#ifdef PSX_FLAT_SHADING_MODE_CENTER
    float3 fN = normalize(cross(ddx(i.worldPos), ddy(i.worldPos)));
    float3 toCam = normalize(i.worldPos - _WorldSpaceCameraPos);
    if (dot(fN, toCam) > 0.0) fN = -fN;
    half3 modeLight = PSXShadeLights(i.worldPos, (half3)fN);
#else
    half3 modeLight = PSXShadeLights(i.worldPos, normalize((half3)i.worldN));
#endif
    col.rgb *= lerp(i.vlight.rgb, modeLight, _FlatShading);
#endif

#ifdef PSX_CUTOUT
    clip(col.a - _Cutoff);
#endif

#ifdef PSX_VERTEX_LIT
    col.rgb += SAMPLE_TEXTURE2D(_EmissiveTex, sampler_EmissiveTex, i.affineUV2.xy / i.affineUV2.z).rgb
        * _EmissionColor.rgb * _EmissionColor.a;
#endif

#ifdef PSX_CUBEMAP
    half3 refl = SAMPLE_TEXTURECUBE(_Cubemap, sampler_Cubemap, i.reflDir).rgb;
    half3 rmask = SAMPLE_TEXTURE2D(_ReflectionMap, sampler_ReflectionMap, i.affineUV.xy / i.affineUV.z).rgb;
    col.rgb += refl * _CubemapColor.rgb * _CubemapColor.a * rmask;
#endif

    MixFog(col.rgb, i.fogFactor);

    int2 px = (int2)i.positionCS.xy;
    col = (_PSX_ObjectDithering * _ObjectDithering) > 0.5f ? PSXDitherColor(col, px) : col;
    return col;
}

#endif
