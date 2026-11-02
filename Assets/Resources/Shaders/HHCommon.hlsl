// Hostile Horizon: shared haze for the rock and the sky, so the ground fades into the same colour as the horizon.
// Globals set by Environment.cs: _HHFogColor (horizon haze), _HHSunHaze (haze toward the sun), _HHSunDir (towards the sun),
// _HHFog = (density per metre, height of the low haze, its fall-off in metres, distance where everything is haze).
#ifndef HH_COMMON
#define HH_COMMON
float4 _HHFogColor, _HHSunHaze, _HHSunDir, _HHFog;

float3 HHHazeColor(float3 viewDir)
{
    float s = saturate(dot(viewDir, _HHSunDir.xyz));
    return lerp(_HHFogColor.rgb, _HHSunHaze.rgb, pow(s, 6.0));
}

float HHHazeAmount(float3 posWS, float3 camPos)
{
    float d = distance(posWS, camPos);
    float a = 1.0 - exp(-pow(d * _HHFog.x, 1.5));
    float low = exp(-max(posWS.y - _HHFog.y, 0.0) / _HHFog.z);       // thicker haze down in the canyons
    a = saturate(a * (0.55 + 0.75 * low));
    return max(a, smoothstep(_HHFog.w * 0.72, _HHFog.w * 0.98, d));  // fully hazed before the streamed ground ends
}
#endif
