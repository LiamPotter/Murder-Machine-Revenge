using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Reflection;
[ExecuteInEditMode]
[RequireComponent(typeof(Light))]
public class HxVolumetricLight : MonoBehaviour 
{
    static float ShadowDistanceExtra = 0.75f;
    Light myLight;
    public Light LightSafe()
    {
        if (myLight == null)
        {
            myLight = GetComponent<Light>();
        }
        return myLight;
    }
    CommandBuffer BufferRender;
    CommandBuffer BufferCopy;

    public Vector3 NoiseScale = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 NoiseVelocity = new Vector3(1,1,0);
    bool dirty = true;
    
    GameObject ShadowCaster;

    public bool NoiseEnabled = false;
    public bool CustomMieScatter = false;
    public bool CustomExtinction = false;
    public bool CustomExtinctionEffect = false;
    public bool CustomDensity = false;
    public bool CustomSampleCount = false;
    public bool CustomColor = false;
    public bool CustomNoiseEnabled = false;
    public bool CustomNoiseScale = false;
    public bool CustomNoiseVelocity = false;


    public bool CustomFogHeightEnabled = false;
    public bool CustomFogHeight = false;
    public bool CustomFogTransitionSize = false;
    public bool CustomAboveFogPercent = false;


    public bool CustomSunSize = false;
    public bool CustomSunBleed = false;
    public bool ShadowCasting = true;
    public bool CustomStrength = false;
    public bool CustomIntensity = false;
    public bool CustomTintMode = false;
    public bool CustomTintColor = false;
    public bool CustomTintColor2 = false;
    public bool CustomTintGradient = false;
    public bool CustomTintIntensity = false;
    public bool CustomMaxLightDistance = false;

    public HxVolumetricCamera.HxTintMode TintMode = HxVolumetricCamera.HxTintMode.Off;
    public Color TintColor = Color.red;
    public Color TintColor2 = Color.blue;
    public float TintIntensity = 0.2f;
    [Range(0, 1)]
    public float TintGradient = 0.2f;

    [Range(0,8)]
    public float Intensity = 1;
    [Range(0, 1)]
    public float Strength = 1;
    public Color Color = Color.white;
    [Range(0.0f, 0.9999f)]
    [Tooltip("0 for even scattering, 1 for forward scattering")]
    public float MieScattering = 0.05f;
    [Range(0.0f, 1)]
    [Tooltip("Create a sun using mie scattering")]
    public float SunSize = 0f;
    [Tooltip("Allows the sun to bleed over the edge of objects (recommend using bloom)")]
    public bool SunBleed = true;
    [Range(0.0f, 0.5f)]
    [Tooltip("dimms results over distance")]
    public float Extinction = 0.01f;
    [Range(0.0f, 1f)]
    [Tooltip("Density of air")]
    public float Density = 0.2f;
    [Range(0.0f, 1.0f)]
    [Tooltip("Useful when you want a light to have slightly more density")]
    public float ExtraDensity = 0;
    [Range(4, 128)]
    [Tooltip("How many samples per pixel, Recommended 16-32 for point, 32 - 64 for Directional")]
    public int SampleCount = 16;


    [Tooltip("Ray marching Shadows can be expensive, save some frames by not marching shadows")]
    public bool Shadows = true;
    public bool FogHeightEnabled = false;
    public float FogHeight = 5;
    public float FogTransitionSize = 5;
    public float MaxLightDistance = 128;
 
    public float AboveFogPercent = 0.1f;
    
    bool OffsetUpdated = false;

    public Vector3 Offset = Vector3.zero;

    static MaterialPropertyBlock propertyBlock;

    void OnEnable()
    {
        myLight = GetComponent<Light>();
        HxVolumetricCamera.AllVolumetricLight.Add(this); 
        UpdatePosition(true);
       
        if (myLight.type != LightType.Directional)
        {
            octreeNode = HxVolumetricCamera.AddLightOctree(this, minBounds, maxBounds);
        }
        else
        {
            HxVolumetricCamera.ActiveDirectionalLights.Add(this);
        }
        
    }
    
    void OnDisable()
    {
        HxVolumetricCamera.AllVolumetricLight.Remove(this);
        if (myLight.type != LightType.Directional)
        {
            HxVolumetricCamera.RemoveLightOctree(this);
            octreeNode = null;
        }
        else
        {
            HxVolumetricCamera.ActiveDirectionalLights.Remove(this);
        }
    }

    void OnDestroy()
    {
        HxVolumetricCamera.AllVolumetricLight.Remove(this);
        if (lastType == LightType.Spot || lastType == LightType.Point)
        {
            HxVolumetricCamera.RemoveLightOctree(this);
            octreeNode = null;
        }
        if (lastType == LightType.Directional)
        {
            HxVolumetricCamera.ActiveDirectionalLights.Remove(this);
        }
    }
   
    void Start()
    {      
        myLight = GetComponent<Light>();
    }

    public void BuildBuffer(CommandBuffer CameraBuffer)
    {
        if (myLight == null) { myLight = GetComponent<Light>(); if (myLight == null) { enabled = false; Debug.LogWarning("No light attached"); return; } }

        if (myLight.enabled && myLight.intensity > 0)
        {
           

            switch (myLight.type)
            {
                case LightType.Directional:
                    BuildDirectionalBuffer(CameraBuffer); 
                    break;
                case LightType.Spot:
                    BuildSpotLightBuffer(CameraBuffer);
                    break;
                case LightType.Point:
                    BuildPointBuffer(CameraBuffer); 
                    break;
                default:
                    break;
            }
           
        }
    }

    public void ReleaseBuffer()
    {
        if (myLight != null)
        {
            //if (myLight.type == LightType.Directional)
            //{
            //
            //    myLight.RemoveAllCommandBuffers(); //bug in unity that doesnt remove light command buffers, so i have to do it this way
            //    
            //    //myLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, BufferCopy); 
            //    //myLight.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, BufferRender);
            //
            //}
            //else
            //{
            //    myLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, BufferRender);
            //}

            myLight.RemoveAllCommandBuffers();
        }
    }

    static public int VolumetricMVPPID;
    static public int VolumetricMVPID;
    static int LightColourPID;
    static int LightColour2PID;
    static int FogHeightsPID;
    static int PhasePID;
    static int _LightParamsPID;
    static int DensityPID;
    static int ShadowBiasPID;
    static int _CustomLightPositionPID;
    static int NoiseScalePID;
    static int NoiseOffsetPID;
    static int _SpotLightParamsPID;
    static int _LightTexture0PID;
    public static void CreatePID()
    {
            VolumetricMVPPID = Shader.PropertyToID("VolumetricMVP");
            LightColourPID = Shader.PropertyToID("LightColour");
        LightColour2PID = Shader.PropertyToID("LightColour2");
        VolumetricMVPID = Shader.PropertyToID("VolumetricMV");
            FogHeightsPID = Shader.PropertyToID("FogHeights");
            PhasePID = Shader.PropertyToID("Phase");
            _LightParamsPID = Shader.PropertyToID("_LightParams");
            DensityPID = Shader.PropertyToID("Density");
            ShadowBiasPID = Shader.PropertyToID("ShadowBias");
            _CustomLightPositionPID = Shader.PropertyToID("_CustomLightPosition");
            NoiseScalePID = Shader.PropertyToID("NoiseScale");
            NoiseOffsetPID = Shader.PropertyToID("NoiseOffset");
            _SpotLightParamsPID = Shader.PropertyToID("_SpotLightParams");
            _LightTexture0PID = Shader.PropertyToID("_LightTexture0");
    }

    float GetNearPlane()
    {

#if UNITY_5_3_OR_NEWER
        return myLight.shadowNearPlane;     
#else
#if UNITY_5_3 
        return myLight.shadowNearPlane;
#else
        return myLight.range * 0.03987963438034f;
#endif

#endif
    }

    int DirectionalPass(CommandBuffer buffer)
    {
        if (HxVolumetricCamera.Active.Ambient == HxVolumetricCamera.HxAmbientMode.UseRenderSettings)
        {
            if (RenderSettings.ambientMode == AmbientMode.Flat)
            {
                buffer.SetGlobalVector("AmbientSkyColor", RenderSettings.ambientSkyColor.linear * RenderSettings.ambientIntensity);
                return 0;
            }

            if (RenderSettings.ambientMode == AmbientMode.Trilight)
            {

                buffer.SetGlobalVector("AmbientSkyColor", RenderSettings.ambientSkyColor.linear * RenderSettings.ambientIntensity);
                buffer.SetGlobalVector("AmbientEquatorColor", RenderSettings.ambientEquatorColor.linear * RenderSettings.ambientIntensity);
                buffer.SetGlobalVector("AmbientGroundColor", RenderSettings.ambientGroundColor.linear * RenderSettings.ambientIntensity);

                return 1;
            }

            return 2;
        }
        else if (HxVolumetricCamera.Active.Ambient == HxVolumetricCamera.HxAmbientMode.Color)
        {
            buffer.SetGlobalVector("AmbientSkyColor", HxVolumetricCamera.Active.AmbientSky * HxVolumetricCamera.Active.AmbientIntensity);
            return 0;
        }
        else if (HxVolumetricCamera.Active.Ambient == HxVolumetricCamera.HxAmbientMode.Gradient)
        {
            buffer.SetGlobalVector("AmbientSkyColor", HxVolumetricCamera.Active.AmbientSky * HxVolumetricCamera.Active.AmbientIntensity);
            buffer.SetGlobalVector("AmbientEquatorColor", HxVolumetricCamera.Active.AmbientEquator * HxVolumetricCamera.Active.AmbientIntensity);
            buffer.SetGlobalVector("AmbientGroundColor", HxVolumetricCamera.Active.AmbientGround * HxVolumetricCamera.Active.AmbientIntensity);
            return 1;
        }
        return 2;
        //if (RenderSettings.ambientMode == AmbientMode.Skybox)
        //{
        //    return 2;
        //}
        //
        //if (RenderSettings.ambientMode == AmbientMode.Custom)
        //{
        //    return 3;
        //}

    }

    void BuildDirectionalBuffer(CommandBuffer CameraBuffer)
    {
        bool RenderShadows = myLight.shadows != LightShadows.None && Shadows;

        if (dirty)
        {
            if (RenderShadows)
            {
                if (BufferCopy == null) { BufferCopy = new CommandBuffer(); BufferCopy.name = "ShadowCopy"; BufferCopy.SetGlobalTexture(HxVolumetricCamera.ShadowMapTexturePID, BuiltinRenderTextureType.CurrentActive); }
                if (BufferRender == null) { BufferRender = new CommandBuffer(); BufferRender.name = "VolumetricRender"; }

                CameraBuffer = BufferRender;
                BufferRender.Clear();
            }
            if (RenderShadows) { Graphics.DrawMesh(HxVolumetricCamera.BoxMesh, HxVolumetricCamera.Active.transform.position, HxVolumetricCamera.Active.transform.rotation, HxVolumetricCamera.ShadowMaterial, 0, HxVolumetricCamera.ActiveCamera, 0, null, true); }
    
            Vector3 forward = transform.forward;

            if (CustomFogHeightEnabled ? FogHeightEnabled : HxVolumetricCamera.Active.FogHeightEnabled)
            { 
                    CameraBuffer.SetGlobalVector(FogHeightsPID, new Vector3((CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight) - (CustomFogTransitionSize ? FogTransitionSize : HxVolumetricCamera.Active.FogTransitionSize), (CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight), (CustomAboveFogPercent ? AboveFogPercent : HxVolumetricCamera.Active.AboveFogPercent)));
            }
            float d = GetFogDensity();
            CameraBuffer.SetGlobalVector("MaxRayDistance", new Vector2(Mathf.Min(QualitySettings.shadowDistance, (CustomMaxLightDistance ? MaxLightDistance : HxVolumetricCamera.Active.MaxDirectionalRayDistance)), (CustomMaxLightDistance ? MaxLightDistance : HxVolumetricCamera.Active.MaxDirectionalRayDistance)));
            float phaseG = (CustomMieScatter ? MieScattering : HxVolumetricCamera.Active.MieScattering);
            Vector4 phase = new Vector4(1.0f / (4.0f * Mathf.PI), 1.0f - (phaseG * phaseG), 1.0f + (phaseG * phaseG), 2.0f * phaseG);
            float phaseG2 =  (CustomSunSize ? SunSize : HxVolumetricCamera.Active.SunSize);
            CameraBuffer.SetGlobalVector("SunSize",new Vector2( (phaseG2 == 0 ? 0 : 1),((CustomSunBleed ? SunBleed : HxVolumetricCamera.Active.SunBleed) ? 1 : 0)));
            phaseG2 = Mathf.Lerp(0.9999f,  0.995f, Mathf.Pow(phaseG2,4));
            
                
            Vector4 phase2 = new Vector4(1.0f / (4.0f * Mathf.PI), 1.0f - (phaseG2 * phaseG2), 1.0f + (phaseG2 * phaseG2), 2.0f * phaseG2);
            CameraBuffer.SetGlobalVector("Phase2", phase2);
            CameraBuffer.SetGlobalVector(PhasePID, phase);
            SetColors(CameraBuffer);
    
            CameraBuffer.SetGlobalVector(_LightParamsPID, new Vector4((CustomStrength ? Strength : myLight.shadowStrength), 0, 0, (CustomIntensity ? Intensity : myLight.intensity)));
            CameraBuffer.SetGlobalVector(DensityPID, new Vector4(d, GetSampleCount(RenderShadows), 0, (CustomExtinction ? Extinction : HxVolumetricCamera.Active.Extinction)));
            CameraBuffer.SetGlobalVector(ShadowBiasPID, new Vector3(myLight.shadowBias, GetNearPlane(), (1.0f - (CustomStrength ? Strength : myLight.shadowStrength)) * phase.x * (phase.y / (Mathf.Pow(phase.z - phase.w * -1, 1.5f)))));
            CameraBuffer.SetGlobalVector(_SpotLightParamsPID, new Vector4(forward.x, forward.y, forward.z, 0));
            Vector3 finalScale = (CustomNoiseScale ? NoiseScale : HxVolumetricCamera.Active.NoiseScale);
            finalScale = new Vector3(1f / finalScale.x, 1f / finalScale.y, 1f / finalScale.z) / 32.0f;
            CameraBuffer.SetGlobalVector(NoiseScalePID, finalScale);
            if (OffsetUpdated == false) { OffsetUpdated = true; Offset += NoiseVelocity * Time.deltaTime; }
            CameraBuffer.SetGlobalVector(NoiseOffsetPID, (CustomNoiseVelocity ? Offset : HxVolumetricCamera.Active.Offset));
            CameraBuffer.SetGlobalFloat("FirstLight", (HxVolumetricCamera.FirstDirectional ? 1 : 0));
            CameraBuffer.SetGlobalFloat("AmbientStrength", HxVolumetricCamera.Active.AmbientLightingStrength);

            HxVolumetricCamera.FirstDirectional = false;
            if (HxVolumetricCamera.Active.TransparencySupport)
            {
                CameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTransparency[(int)HxVolumetricCamera.Active.compatibleTBuffer()], HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
            }
            else
            {
                CameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTextureRTID, HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
            }

            CameraBuffer.SetGlobalMatrix(VolumetricMVPPID, HxVolumetricCamera.BlitMatrixMVP);
            CameraBuffer.SetGlobalMatrix(VolumetricMVPID, HxVolumetricCamera.BlitMatrixMV);

            CameraBuffer.DrawMesh(HxVolumetricCamera.QuadMesh, HxVolumetricCamera.BlitMatrix, HxVolumetricCamera.DirectionalMaterial[MID(RenderShadows)],0,DirectionalPass(CameraBuffer));




            //need to figure out how cookies work with directional lights.
           // if (myLight.cookie != null)
           // {
           //         
           //     propertyBlock.SetTexture(Shader.PropertyToID("DirectionCookieTexture"), myLight.cookie);
           //     CameraBuffer.DrawMesh(HxVolumetricCamera.QuadMesh, HxVolumetricCamera.BlitMatrix, HxVolumetricCamera.DirectionalMaterial[MID()], 0, 0, //propertyBlock);
           //
           // }
           // else
           // {
           //     CameraBuffer.DrawMesh(HxVolumetricCamera.QuadMesh, HxVolumetricCamera.BlitMatrix, HxVolumetricCamera.DirectionalMaterial[MID()]);
           // }
            

            if (RenderShadows)
            {
                myLight.AddCommandBuffer(LightEvent.AfterShadowMap, BufferCopy); //have to add again because of bug.
                myLight.AddCommandBuffer(LightEvent.AfterScreenspaceMask, BufferRender);
            }
        }
    }

    float CalcLightInstensityDistance(float distance)
    {
        
        return 1.0f - Mathf.Clamp01(1.0f/ ((CustomMaxLightDistance ? MaxLightDistance : HxVolumetricCamera.Active.MaxLightDistance) * 0.2f) * (distance - ((CustomMaxLightDistance ? MaxLightDistance : HxVolumetricCamera.Active.MaxLightDistance) * 0.8f)));
    }

    void BuildSpotLightBuffer(CommandBuffer cameraBuffer)
    {
        bool RenderShadows = myLight.shadows != LightShadows.None && Shadows;
        float Distance = ClosestDistanceToCone(HxVolumetricCamera.Active.transform.position);

        if (RenderShadows)
        {
            if (RenderShadows)
            {
                if (Distance > QualitySettings.shadowDistance - ShadowDistanceExtra)
                {
                    RenderShadows = false;
                }
            }
        }

        if (dirty)
        {
            if (RenderShadows)
            {
                if (BufferRender == null) { BufferRender = new CommandBuffer(); BufferRender.name = "VolumetricRender"; }


                cameraBuffer = BufferRender;
                BufferRender.Clear();
            }

            cameraBuffer.SetGlobalTexture(HxVolumetricCamera.ShadowMapTexturePID, BuiltinRenderTextureType.CurrentActive);
            //if (SystemInfo.supportsRawShadowDepthSampling)
            //{
            ////    cameraBuffer.SetShadowSamplingMode(new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive), ShadowSamplingMode.RawDepth);
            //}//set directional color and fog settings
            SetColors(cameraBuffer, Distance);



            if (HxVolumetricCamera.Active.TransparencySupport)
            {
                cameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTransparency[(int)HxVolumetricCamera.Active.compatibleTBuffer()], HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
            }
            else
            {
                cameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTextureRTID, HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
            }


            if (CustomFogHeightEnabled ? FogHeightEnabled : HxVolumetricCamera.Active.FogHeightEnabled)
            {
                cameraBuffer.SetGlobalVector(FogHeightsPID, new Vector3((CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight) - (CustomFogTransitionSize ? FogTransitionSize : HxVolumetricCamera.Active.FogTransitionSize), (CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight), (CustomAboveFogPercent ? AboveFogPercent : HxVolumetricCamera.Active.AboveFogPercent)));
            }

            float d = GetFogDensity();
            cameraBuffer.SetGlobalMatrix(VolumetricMVPPID, HxVolumetricCamera.Active.MatrixVP * LightMatrix);
            cameraBuffer.SetGlobalMatrix(VolumetricMVPID, HxVolumetricCamera.Active.MatrixV * LightMatrix);
            float phaseG = (CustomMieScatter ? MieScattering : HxVolumetricCamera.Active.MieScattering);

            Vector4 phase = new Vector4(1.0f / (4.0f * Mathf.PI), 1.0f - (phaseG * phaseG), 1.0f + (phaseG * phaseG), 2.0f * phaseG);
            cameraBuffer.SetGlobalVector(PhasePID, phase);
            cameraBuffer.SetGlobalVector(_CustomLightPositionPID, transform.position);

            cameraBuffer.SetGlobalVector(_LightParamsPID, new Vector4((CustomStrength ? Strength : myLight.shadowStrength), 1f / myLight.range, myLight.range, (CustomIntensity ? Intensity : myLight.intensity)));
            
            cameraBuffer.SetGlobalVector(DensityPID, new Vector4(d, GetSampleCount(RenderShadows), 0, (CustomExtinction ? Extinction : HxVolumetricCamera.Active.Extinction)));
            if (RenderShadows) { Graphics.DrawMesh(HxVolumetricCamera.SpotLightMesh, LightMatrix, HxVolumetricCamera.ShadowMaterial, 0, HxVolumetricCamera.ActiveCamera, 0, null, ShadowCastingMode.ShadowsOnly); }
            float a = myLight.range / (myLight.range - (GetNearPlane()));
            float b = myLight.range * (GetNearPlane()) / (GetNearPlane() - myLight.range);
            cameraBuffer.SetGlobalVector(ShadowBiasPID, new Vector4(a, b, (1.0f - (CustomStrength ? Strength : myLight.shadowStrength)) * phase.x * (phase.y / (Mathf.Pow(phase.z - phase.w * -1, 1.5f))), myLight.shadowBias));

            Vector3 finalScale = (CustomNoiseScale ? NoiseScale : HxVolumetricCamera.Active.NoiseScale);
            finalScale = new Vector3(1f / finalScale.x, 1f / finalScale.y, 1f / finalScale.z) / 32.0f;
            cameraBuffer.SetGlobalVector(NoiseScalePID, finalScale);
            if (OffsetUpdated == false) { OffsetUpdated = true; Offset += NoiseVelocity * Time.deltaTime; }
            cameraBuffer.SetGlobalVector(NoiseOffsetPID, (CustomNoiseVelocity ? Offset : HxVolumetricCamera.Active.Offset));
            Vector3 forward = transform.forward;
            cameraBuffer.SetGlobalVector(_SpotLightParamsPID, new Vector4(forward.x, forward.y, forward.z, (myLight.spotAngle + 0.01f) / 2f * Mathf.Deg2Rad));
            if (propertyBlock == null) { propertyBlock = new MaterialPropertyBlock(); }
            propertyBlock.SetTexture(_LightTexture0PID, (myLight.cookie == null ? HxVolumetricCamera.Active.SpotLightCookie : myLight.cookie));
            cameraBuffer.DrawMesh(HxVolumetricCamera.SpotLightMesh, LightMatrix, HxVolumetricCamera.SpotMaterial[MID(RenderShadows)], 0, (lastBounds.SqrDistance(HxVolumetricCamera.Active.transform.position) < ((HxVolumetricCamera.ActiveCamera.nearClipPlane * 2) * (HxVolumetricCamera.ActiveCamera.nearClipPlane * 2)) ? 0 : 1), propertyBlock);
           

            if (RenderShadows)
            {
                myLight.AddCommandBuffer(LightEvent.AfterShadowMap, BufferRender);
            }
        }
    }

    void SetColors(CommandBuffer buffer, float distance)
    {
        Vector4 BasedColor = (CustomColor ? Color.linear : myLight.color.linear) * (CustomIntensity ? Intensity : myLight.intensity) * CalcLightInstensityDistance(distance);

        if (CustomTintMode ? TintMode == HxVolumetricCamera.HxTintMode.Off : HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Off)
        {
            buffer.SetGlobalVector(LightColourPID, BasedColor);
            buffer.SetGlobalVector(LightColour2PID, BasedColor);
        }
        else if (CustomTintMode ? TintMode == HxVolumetricCamera.HxTintMode.Color : HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Color)
        {       
            buffer.SetGlobalVector(LightColourPID, CalcTintColor(BasedColor));
            buffer.SetGlobalVector(LightColour2PID, CalcTintColor(BasedColor));
        }
        else if (CustomTintMode ? TintMode == HxVolumetricCamera.HxTintMode.Edge : HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Edge)
        {
            buffer.SetGlobalVector(LightColourPID, BasedColor);
            buffer.SetGlobalVector(LightColour2PID, CalcTintColor(BasedColor));
            buffer.SetGlobalFloat("TintPercent", 1f / (CustomTintGradient ? TintGradient : HxVolumetricCamera.Active.TintGradient) / 2f);
        }
        else if (CustomTintMode ? TintMode == HxVolumetricCamera.HxTintMode.Gradient : HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Gradient)
        {
            buffer.SetGlobalVector(LightColourPID, CalcTintColor(BasedColor));
            buffer.SetGlobalVector(LightColour2PID, CalcTintColorEdge(BasedColor));
            buffer.SetGlobalFloat("TintPercent", 1f / (CustomTintGradient ? TintGradient : HxVolumetricCamera.Active.TintGradient) / 2f);
        }
        
    }

    void SetColors(CommandBuffer buffer)
    {
        Vector4 BasedColor = (CustomColor ? Color.linear : myLight.color.linear) * (CustomIntensity ? Intensity : myLight.intensity);

        if (HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Off)
        {
            buffer.SetGlobalVector(LightColourPID, BasedColor);
            buffer.SetGlobalVector(LightColour2PID, BasedColor);
        }
        else if (HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Color)
        {
            buffer.SetGlobalVector(LightColourPID, CalcTintColor(BasedColor));
            buffer.SetGlobalVector(LightColour2PID, CalcTintColor(BasedColor));
        }
        else if (HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Edge)
        {
            buffer.SetGlobalVector(LightColourPID, BasedColor);
            buffer.SetGlobalVector(LightColour2PID, CalcTintColor(BasedColor));
            buffer.SetGlobalFloat("TintPercent", 1f / HxVolumetricCamera.Active.TintGradient / 2f);
        }
        else if (HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Gradient)
        {
            buffer.SetGlobalVector(LightColourPID, CalcTintColor(BasedColor));
            buffer.SetGlobalVector(LightColour2PID, CalcTintColorEdge(BasedColor));
            buffer.SetGlobalFloat("TintPercent", 1f / HxVolumetricCamera.Active.TintGradient / 2f);
        }
    }

    Vector3 CalcTintColor(Vector4 c)
    {
        Vector3 old = new Vector3(c.x, c.y, c.z);
        float mag = old.magnitude;
        if (CustomTintColor)
        {
            old += new Vector3(TintColor.linear.r, TintColor.linear.g, TintColor.linear.b) * (CustomTintIntensity ? TintIntensity : HxVolumetricCamera.Active.TintIntensity);
        }
        else
        {
            old += HxVolumetricCamera.Active.CurrentTint;
        }
        

        return old.normalized * mag;
    }

    Vector3 CalcTintColorEdge(Vector4 c)
    {
        Vector3 old = new Vector3(c.x, c.y, c.z);
        float mag = old.magnitude;
  
        if (CustomTintColor2)
        {
            old += new Vector3(TintColor2.linear.r, TintColor2.linear.g, TintColor2.linear.b) * (CustomTintIntensity ? TintIntensity : HxVolumetricCamera.Active.TintIntensity);
        }
        else
        {
            old += HxVolumetricCamera.Active.CurrentTintEdge;
        }
        return old.normalized * mag;
    }

    void BuildPointBuffer(CommandBuffer cameraBuffer)
    {
        bool RenderShadows = myLight.shadows != LightShadows.None && Shadows;
        float distance = Mathf.Max(Vector3.Distance(HxVolumetricCamera.Active.transform.position, transform.position) - myLight.range, 0);
        if (RenderShadows)
        {
            if (distance >= QualitySettings.shadowDistance - ShadowDistanceExtra)
            {
                RenderShadows = false;
            }
        }
        if (distance > HxVolumetricCamera.Active.MaxLightDistance)
        {
            return;
        }

        if (dirty)
        {
            if (RenderShadows)
            {
                if (BufferRender == null) { BufferRender = new CommandBuffer(); BufferRender.name = "VolumetricRender"; }

                cameraBuffer = BufferRender;
                BufferRender.Clear();
            }

            cameraBuffer.SetGlobalTexture(HxVolumetricCamera.ShadowMapTexturePID, BuiltinRenderTextureType.CurrentActive);
            SetColors(cameraBuffer, distance);

            if (CustomFogHeightEnabled ? FogHeightEnabled : HxVolumetricCamera.Active.FogHeightEnabled)
            {
                cameraBuffer.SetGlobalVector(FogHeightsPID, new Vector3((CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight) - (CustomFogTransitionSize ? FogTransitionSize : HxVolumetricCamera.Active.FogTransitionSize), (CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight), (CustomAboveFogPercent ? AboveFogPercent : HxVolumetricCamera.Active.AboveFogPercent)));
            }


            if (HxVolumetricCamera.Active.TransparencySupport)
            {
                cameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTransparency[(int)HxVolumetricCamera.Active.compatibleTBuffer()], HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
            }
            else
            {
                cameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTextureRTID, HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
            }
            // }
            float d = GetFogDensity();
            cameraBuffer.SetGlobalMatrix(VolumetricMVPPID, HxVolumetricCamera.Active.MatrixVP * LightMatrix);
            cameraBuffer.SetGlobalMatrix(VolumetricMVPID, HxVolumetricCamera.Active.MatrixV * LightMatrix);
            float phaseG = (CustomMieScatter ? MieScattering : HxVolumetricCamera.Active.MieScattering);
            Vector4 phase = new Vector4(1.0f / (4.0f * Mathf.PI), 1.0f - (phaseG * phaseG), 1.0f + (phaseG * phaseG),2.0f * phaseG);
                        
            cameraBuffer.SetGlobalVector(PhasePID, phase); 
            cameraBuffer.SetGlobalVector(_CustomLightPositionPID, transform.position);
            cameraBuffer.SetGlobalVector(_LightParamsPID, new Vector4((CustomStrength ? Strength : myLight.shadowStrength), 1f / myLight.range, myLight.range, (CustomIntensity ? Intensity : myLight.intensity)));
            cameraBuffer.SetGlobalVector(DensityPID, new Vector4(d, GetSampleCount(RenderShadows), 0, (CustomExtinction ? Extinction : HxVolumetricCamera.Active.Extinction)));

            cameraBuffer.SetGlobalVector(ShadowBiasPID, new Vector3(myLight.shadowBias, GetNearPlane(), (1.0f - (CustomStrength ? Strength : myLight.shadowStrength)) * phase.x * (phase.y / (Mathf.Pow(phase.z - phase.w * -1, 1.5f)))));

            Vector3 finalScale = (CustomNoiseScale ? NoiseScale : HxVolumetricCamera.Active.NoiseScale);
            finalScale = new Vector3(1f / finalScale.x, 1f / finalScale.y, 1f / finalScale.z) / 32.0f;
            cameraBuffer.SetGlobalVector(NoiseScalePID, finalScale);
            if (OffsetUpdated == false) { OffsetUpdated = true; Offset += NoiseVelocity * Time.deltaTime; }
            cameraBuffer.SetGlobalVector(NoiseOffsetPID, (CustomNoiseVelocity ? Offset : HxVolumetricCamera.Active.Offset));
            if (RenderShadows) { Graphics.DrawMesh(HxVolumetricCamera.BoxMesh, LightMatrix, HxVolumetricCamera.ShadowMaterial, 0, HxVolumetricCamera.ActiveCamera, 0, null, ShadowCastingMode.ShadowsOnly); }

            int pass = (distance <= myLight.range + myLight.range * 0.09f + HxVolumetricCamera.ActiveCamera.nearClipPlane * 2 ? 0 : 1); //near or far


            if (propertyBlock == null) { propertyBlock = new MaterialPropertyBlock(); }

            if (myLight.cookie != null)
            {
                //_LightTexture0PID
                propertyBlock.SetTexture(Shader.PropertyToID("PointCookieTexture"), myLight.cookie);

                cameraBuffer.DrawMesh(HxVolumetricCamera.SphereMesh, LightMatrix, HxVolumetricCamera.PointMaterial[MID(RenderShadows)], 0, pass, propertyBlock); 
            }
            else
            {
                cameraBuffer.DrawMesh(HxVolumetricCamera.SphereMesh, LightMatrix, HxVolumetricCamera.PointMaterial[MID(RenderShadows)], 0, pass); 
            }
            if (RenderShadows)
            {
                myLight.AddCommandBuffer(LightEvent.AfterShadowMap, BufferRender);
            }

        }
    }

    public int MID(bool RenderShadows)
    {
        int i = 0;
        if (RenderShadows) { i += 1; }
        if (myLight.cookie != null) { i += 2; }
        if (CustomNoiseEnabled ? NoiseEnabled : HxVolumetricCamera.Active.NoiseEnabled) { i += 4; }
        if (CustomFogHeight ? FogHeightEnabled : HxVolumetricCamera.Active.FogHeightEnabled) { i += 8; }
        if (HxVolumetricCamera.Active.renderDensityParticleCheck()) { i += 16; }
        if (HxVolumetricCamera.Active.TransparencySupport) { i += 32; }

        return i;
    }
 
    void Update()
    {
        OffsetUpdated = false;
    }

    float GetFogDensity()
    {
        if (CustomDensity)
        {
            return Density + ExtraDensity;
        }
        return HxVolumetricCamera.Active.Density + ExtraDensity;
    }

    int GetSampleCount(bool RenderShadows)
    {
        int sample = (CustomSampleCount ? SampleCount : (myLight.type != LightType.Directional ? HxVolumetricCamera.Active.SampleCount : HxVolumetricCamera.Active.DirectionalSampleCount));
      
       // if (!RenderShadows)
       // {
       //     sample = Mathf.Max(4, sample/8);
       // }

        return Mathf.Max(2, sample);
    }

    public static Vector3 ClosestPointOnLine(Vector3 vA, Vector3 vB, Vector3 vPoint)
    {
        Vector3 vVector1 = vPoint - vA;
        Vector3 vVector2 = (vB - vA).normalized;

        float d = Vector3.Distance(vA, vB);
        float t = Vector3.Dot(vVector2, vVector1);

        if (t <= 0)
            return vA;

        if (t >= d)
            return vB;

        var vVector3 = vVector2 * t;

        var vClosestPoint = vA + vVector3;

        return vClosestPoint;
    }

    float ClosestDistanceToCone(Vector3 Point)
    {
        //this could be faster. but it works.
        Vector3 Axis = transform.forward * myLight.range;
        Vector3 planePosition = (transform.position + Axis);
        float planeDistance = Vector3.Dot(transform.forward, (Point - planePosition));
        if (planeDistance == 0) { return 0; }
 
        Vector3 closestPoint = Point - planeDistance * transform.forward;
        float s = Mathf.Tan(myLight.spotAngle / 2f * Mathf.Deg2Rad) * myLight.range;

        Vector3 dif = (closestPoint - planePosition);
                             
        if (planeDistance > 0)
        {
            closestPoint = planePosition + dif.normalized * Mathf.Min(dif.magnitude, s);        
            return Vector3.Distance(Point, closestPoint);
        }

       float a = Mathf.Deg2Rad * myLight.spotAngle;

       float c = Mathf.Acos((Vector3.Dot((Point - transform.position), -Axis)) / ((Point - transform.position).magnitude * myLight.range));
       if (Mathf.Abs(c - a) >= Mathf.PI / 2.0f)
       {
           return 0; //inside
       }
        closestPoint = planePosition + dif.normalized * s;


        closestPoint = ClosestPointOnLine(closestPoint, transform.position, Point);



        return Vector3.Distance(Point, closestPoint);

    }

    float LastSpotAngle = 0;
    float LastRange = 0;
    LightType lastType = LightType.Area;
    Matrix4x4 LightMatrix;

    Bounds lastBounds = new Bounds();
    Vector3 minBounds;
    Vector3 maxBounds;
    HxOctreeNode<HxVolumetricLight>.NodeObject octreeNode;

    void UpdateLightMatrix()
    {
        LastRange = myLight.range;
        LastSpotAngle = myLight.spotAngle;
        lastType = myLight.type;
        if (myLight.type == LightType.Point)
        {
            LightMatrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(myLight.range * 2f, myLight.range * 2f, myLight.range * 2f)); matrixReconstruct = false;
        }
        else
        {

            float s = Mathf.Tan(myLight.spotAngle / 2f * Mathf.Deg2Rad) * myLight.range;
            LightMatrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(s * 2f, s * 2f, myLight.range));
        }
        transform.hasChanged = false;
        matrixReconstruct = false;
    }

    void CheckLightType()
    {
        if (lastType != myLight.type)
        {
            if (lastType == LightType.Directional)
            {
                octreeNode = HxVolumetricCamera.AddLightOctree(this, minBounds, maxBounds);
                HxVolumetricCamera.ActiveDirectionalLights.Remove(this);
            }
            else if (myLight.type == LightType.Directional && (lastType == LightType.Point || lastType == LightType.Spot))
            {
                HxVolumetricCamera.RemoveLightOctree(this);
                octreeNode = null;
                HxVolumetricCamera.ActiveDirectionalLights.Add(this);
            }
        }

        lastType = myLight.type;
    }

    public void UpdatePosition(bool first = false)
    {
        if (transform.hasChanged || matrixReconstruct || LastRange != myLight.range || LastSpotAngle != myLight.spotAngle || lastType != myLight.type)
        {
            if (myLight.type == LightType.Point)
            {
                Vector3 dif = new Vector3(myLight.range, myLight.range, myLight.range);
                minBounds = transform.position - dif;
                maxBounds = transform.position + dif;
                lastBounds.SetMinMax(minBounds, maxBounds);

                if (!first) { CheckLightType(); HxVolumetricCamera.LightOctree.Move(octreeNode, minBounds, maxBounds); } else { lastType = myLight.type; }
                UpdateLightMatrix();
            }
            else if (myLight.type == LightType.Spot)
            {
                Vector3 pos = transform.position;
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                Vector3 up = transform.up;

                Vector3 farCenter = pos + forward * myLight.range;

                float farHeight = Mathf.Tan((myLight.spotAngle * Mathf.Deg2Rad) / 2f) * myLight.range;

                Vector3 farTopLeft = farCenter + up * (farHeight) - right * (farHeight);
                Vector3 farTopRight = farCenter + up * (farHeight) + right * (farHeight);
                Vector3 farBottomLeft = farCenter - up * (farHeight) - right * (farHeight);
                Vector3 farBottomRight = farCenter - up * (farHeight) + right * (farHeight);

                minBounds = new Vector3(Mathf.Min(farTopLeft.x, Mathf.Min(farTopRight.x, Mathf.Min(farBottomLeft.x, Mathf.Min(farBottomRight.x, pos.x)))), Mathf.Min(farTopLeft.y, Mathf.Min(farTopRight.y, Mathf.Min(farBottomLeft.y, Mathf.Min(farBottomRight.y, pos.y)))), Mathf.Min(farTopLeft.z, Mathf.Min(farTopRight.z, Mathf.Min(farBottomLeft.z, Mathf.Min(farBottomRight.z, pos.z)))));
                maxBounds = new Vector3(Mathf.Max(farTopLeft.x, Mathf.Max(farTopRight.x, Mathf.Max(farBottomLeft.x, Mathf.Max(farBottomRight.x, pos.x)))), Mathf.Max(farTopLeft.y, Mathf.Max(farTopRight.y, Mathf.Max(farBottomLeft.y, Mathf.Max(farBottomRight.y, pos.y)))), Mathf.Max(farTopLeft.z, Mathf.Max(farTopRight.z, Mathf.Max(farBottomLeft.z, Mathf.Max(farBottomRight.z, pos.z)))));
                lastBounds.SetMinMax(minBounds, maxBounds);
                if (!first) { CheckLightType(); HxVolumetricCamera.LightOctree.Move(octreeNode, minBounds, maxBounds); } else { lastType = myLight.type; }
                UpdateLightMatrix();
            }
            else
            {
                if (!first) { CheckLightType(); } else { lastType = myLight.type; }
            }
        
        }
    }

    public void DrawBounds()
    {
        if (myLight.type != LightType.Directional)
        {
            
            Debug.DrawLine(new Vector3(minBounds.x, minBounds.y, minBounds.z),new Vector3(maxBounds.x, minBounds.y, minBounds.z), myLight.color);
            Debug.DrawLine(new Vector3(maxBounds.x, minBounds.y, minBounds.z), new Vector3(maxBounds.x, minBounds.y, maxBounds.z), myLight.color);
            Debug.DrawLine(new Vector3(maxBounds.x, minBounds.y, maxBounds.z), new Vector3(minBounds.x, minBounds.y, maxBounds.z), myLight.color);
            Debug.DrawLine(new Vector3(minBounds.x, minBounds.y, maxBounds.z), new Vector3(minBounds.x, minBounds.y, minBounds.z), myLight.color);
            Debug.DrawLine(new Vector3(minBounds.x, maxBounds.y, minBounds.z), new Vector3(maxBounds.x, maxBounds.y, minBounds.z), myLight.color);
            Debug.DrawLine(new Vector3(maxBounds.x, maxBounds.y, minBounds.z), new Vector3(maxBounds.x, maxBounds.y, maxBounds.z), myLight.color);
            Debug.DrawLine(new Vector3(maxBounds.x, maxBounds.y, maxBounds.z), new Vector3(minBounds.x, maxBounds.y, maxBounds.z), myLight.color);
            Debug.DrawLine(new Vector3(minBounds.x, maxBounds.y, maxBounds.z), new Vector3(minBounds.x, maxBounds.y, minBounds.z), myLight.color);

            Debug.DrawLine(new Vector3(minBounds.x, minBounds.y, minBounds.z), new Vector3(minBounds.x, maxBounds.y, minBounds.z), myLight.color);
            Debug.DrawLine(new Vector3(maxBounds.x, minBounds.y, minBounds.z), new Vector3(maxBounds.x, maxBounds.y, minBounds.z), myLight.color);
            Debug.DrawLine(new Vector3(maxBounds.x, minBounds.y, maxBounds.z), new Vector3(maxBounds.x, maxBounds.y, maxBounds.z), myLight.color);
            Debug.DrawLine(new Vector3(minBounds.x, minBounds.y, maxBounds.z), new Vector3(minBounds.x, maxBounds.y, maxBounds.z), myLight.color);
        }
    }

    bool matrixReconstruct = true;

}
