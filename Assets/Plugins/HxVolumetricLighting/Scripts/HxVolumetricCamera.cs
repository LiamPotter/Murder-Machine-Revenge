using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
[ExecuteInEditMode]
public class HxVolumetricCamera : MonoBehaviour
{
    //Edit these values to the same values as volumemtricLightCore
    //There is a limit to the amount of textures that the gpu can sample from for each draw call.
    //if you have complicated transparent shaders it can cause compile issues if you have this setting too high.

    //Amount of Transparency slices used in 3D density texture (dx9 will cap at x12)
    TBufferDepth TransparencyBufferDepth = TBufferDepth.x12;

    //Amount of Depth slices used in 3D density texture
    BufferDepth DensityBufferDepth = BufferDepth.x16;
    //end
    public enum ShaderWarm {Off = 0, Scene = 1, All = 2 };
    public enum BufferDepth { x8 = 0, x12 = 1, x16 = 2 };//, x20 = 3, x24 = 4, x28 = 5, x32 = 6 };
    public enum TBufferDepth { x8 = 0, x12 = 1, x16 = 2 };//, x20 = 3, x24 = 4, x28 = 5};
    int EnumBufferDepthLength = 3;

    public TBufferDepth compatibleTBuffer()
    {
        if ((int)TransparencyBufferDepth > 1)
        {
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation4)
            {
                return TBufferDepth.x12;
            }
        }
        return TransparencyBufferDepth;
    }

    BufferDepth compatibleDBuffer()
    {
        return DensityBufferDepth;
    }

    static RenderTexture VolumetricTexture;

    static RenderTexture FullBlurRT;
    static RenderTargetIdentifier FullBlurRTID;

    static RenderTexture downScaledBlurRT;
    static RenderTargetIdentifier downScaledBlurRTID;

    static RenderTexture FullBlurRT2;
    static RenderTargetIdentifier FullBlurRT2ID;

    static RenderTexture[] VolumetricDensityTextures = new RenderTexture[8];
    static int[] VolumetricDensityPID = new int[4] { 0, 0, 0, 0 };
    static int[] VolumetricTransparencyPID = new int[4] { 0, 0, 0, 0 };
    static RenderTexture[] VolumetricTransparencyTextures = new RenderTexture[8];

    public static RenderTargetIdentifier[][] VolumetricDensity = new RenderTargetIdentifier[][] {
        new RenderTargetIdentifier[2] { new RenderTargetIdentifier(), new RenderTargetIdentifier()},
        new RenderTargetIdentifier[3] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[4] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[5] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[6] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[7] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[8] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() }
    };

    public static RenderTargetIdentifier[][] VolumetricTransparency = new RenderTargetIdentifier[][] {
        new RenderTargetIdentifier[3] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[4] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[5] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[6] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[7] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[8] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[9] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() }
    };

    public static RenderTargetIdentifier[][] VolumetricTransparencyI = new RenderTargetIdentifier[][] {
        new RenderTargetIdentifier[2] { new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[3] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[4] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[5] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[6] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[7] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[8] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() }
    };

    static RenderTexture[] ScaledDepthTexture = new RenderTexture[4] { null, null, null, null };

    static ShaderVariantCollection CollectionAll;

    public static Texture2D Tile5x5;

    static int VolumetricTexturePID;
    static int ScaledDepthTexturePID;

    public static int ShadowMapTexturePID;



    public static RenderTargetIdentifier VolumetricTextureRTID;

    public static RenderTargetIdentifier[] ScaledDepthTextureRTID = new RenderTargetIdentifier[4];

    public static Material DownSampleMaterial;
    public static Material VolumeBlurMaterial;
    public static Material TransparencyBlurMaterial;
    public static Material ApplyMaterial;
    [System.NonSerialized]
    public static Texture3D NoiseTexture3D = null;




    static public Matrix4x4 BlitMatrix;
    static public Matrix4x4 BlitMatrixMV;
    static public Matrix4x4 BlitMatrixMVP;
    static public Vector3 BlitScale;

    
    [Tooltip("Rending resolution, Lower for more speed, higher for better quality")]
    public Resolution resolution = Resolution.quarter;
    [Tooltip("How many samples per Fullscreen pixel, Recommended 16-32 for point, 32 - 64 for Directional")]
    [Range(4, 128)]
    public int SampleCount = 16;
    [Tooltip("How many samples per Fullscreen pixel, Recommended 16-32 for point, 32 - 64 for Directional")]
    [Range(4, 128)]
    public int DirectionalSampleCount = 32;
    [Tooltip("Max distance the directional light gets raymarched.")]
    public float MaxDirectionalRayDistance = 128;
    [Tooltip("Any point of spot lights passed this point will not render.")]
    public float MaxLightDistance = 128;


    [Range(0.0f, 1f)]
    [Tooltip("Density of air")]
    public float Density = 0.05f;
    [Range(0.0f, 2f)]
    public float AmbientLightingStrength = 0.5f;


    [Tooltip("0 for even scattering, 1 for forward scattering")]
    [Range(0f, 0.995f)]
    public float MieScattering = 0.4f;
    [Range(0.0f, 1f)]
    [Tooltip("Create a sun using mie Scattering")]
    public float SunSize = 0f;
    [Tooltip("Allows the sun to bleed over the edge of objects (recommend using bloom)")]
    public bool SunBleed = true;
    [Range(0.0f, 0.5f)]
    [Tooltip("dimms results over distance")]
    public float Extinction = 0.05f;
    [Tooltip("Tone down Extinction effect on FinalColor")]
    [Range(0, 1)]
    public float ExtinctionEffect = 0f;

    public bool renderDensityParticleCheck()
    {
        return ParticleDensityRenderCount > 0;// if nothing getting rendered. return false.. todo
    }


    public bool FogHeightEnabled = false;
    public float FogHeight = 5;
    public float FogTransitionSize = 5;
    
    public float AboveFogPercent = 0.1f;

    public enum HxAmbientMode {UseRenderSettings = 0, Color = 1, Gradient = 2};
    public enum HxTintMode { Off = 0, Color = 1, Edge = 2, Gradient = 3 };

    [Tooltip("Ambient Mode - Use unitys or overide your own")]
    public HxAmbientMode Ambient = HxAmbientMode.UseRenderSettings;
    public Color AmbientSky = Color.white;
    public Color AmbientEquator = Color.white;
    public Color AmbientGround = Color.white;
    [Range(0,1)]
    public float AmbientIntensity = 1;

    public HxTintMode TintMode = HxTintMode.Off;
    public Color TintColor = Color.red;
    public Color TintColor2 = Color.blue;
    public float TintIntensity = 0.2f;
    [Range(0,1)]
    public float TintGradient = 0.2f;

    public Vector3 CurrentTint;
    public Vector3 CurrentTintEdge;
    [Tooltip("Use 3D noise")]
    public bool NoiseEnabled = false;
    [Tooltip("The scale of the noise texture")]
    public Vector3 NoiseScale = new Vector3(0.1f,0.1f,0.1f);
    [Tooltip("Used to simulate some wind")]
    public Vector3 NoiseVelocity = new Vector3(1, 0, 1);


    public enum Resolution { full = 0, half = 1, quarter = 2};
    public enum DensityResolution { full = 0, half = 1, quarter = 2, eighth = 3 };


    [Tooltip("Allows particles to modulate the air density")]
    public bool ParticleDensitySupport = false;
    [Tooltip("Rending resolution of density, Lower for more speed, higher for more detailed dust")]
    public DensityResolution densityResolution = DensityResolution.eighth;

    [Tooltip("Max Distance of density particles")]
    public float densityDistance = 64;
    float densityBias = 1.7f;


    [Tooltip("Enabling Transparency support has a cost - disable if you dont need it")]
    public bool TransparencySupport = false;
   
    [Tooltip("Max Distance for transparency Support - lower distance will give greater resilts")]
    public float transparencyDistance = 64;
    [Tooltip("Cost a little extra but can remove the grainy look on Transparent objects when sample count is low")]
    [Range(0,4)]
    public int BlurTransparency = 1;
    float transparencyBias = 1.5f;




    [Range(0, 4)]
    [Tooltip("Blur results of volumetric pass")]
    public int blurCount = 2;
    [Tooltip("Used in final blur pass, Higher number will retain silhouette")]
    public float BlurDepthFalloff = 15f;
    [Tooltip("Used in Downsample blur pass, Higher number will retain silhouette")]
    public float DownsampledBlurDepthFalloff = 20f;
    [Range(0, 4)]
    [Tooltip("Blur bad results after upscaling")]
    public int UpSampledblurCount = 1;

    [Tooltip("If depth is with-in this threshold, bilinearly sample result")]
    public float DepthThreshold = 0.06f;
    [Tooltip("Use gaussian weights - makes blur less blurry but can make it more splotchy")]
    public bool GaussianWeights = false;
    [HideInInspector]
    [Tooltip("Only enable if you arnt using tonemapping and HDR mode")]
    public bool MapToLDR = false;
    [Tooltip("A small amount of noise can be added to remove and color banding from the volumetric effect")]
    public bool RemoveColorBanding = true;
    [Tooltip("Warm shaders used by volumetric lighting, Stops hiccups when a volumetric light is rendered for the first time")]
    public ShaderWarm WarmUpShaders = ShaderWarm.Scene;

    [HideInInspector]
    public Vector3 Offset = Vector3.zero;

    static int DepthThresholdPID;
    static int BlurDepthFalloffPID;

    static int VolumeScalePID;
    static int InverseViewMatrixPID;
    static int InverseProjectionMatrixPID;
    static int NoiseOffsetPID;
    static int ShadowDistancePID;

    void WarmUp()
    {
        if (WarmUpShaders == ShaderWarm.All)
        {
            if (CollectionAll == null) { CollectionAll = (ShaderVariantCollection)Resources.Load("Shaders/HxVolumetricVariantsAll"); if (CollectionAll != null) { CollectionAll.WarmUp(); } }
        }

        if (WarmUpShaders == ShaderWarm.Scene)
        {
            HxVolumetricCamera.Active = this;
           ShaderVariantCollection vc = new ShaderVariantCollection();
            HxVolumetricLight[] allLights = Resources.FindObjectsOfTypeAll(typeof(HxVolumetricLight)) as HxVolumetricLight[];
            for (int i = 0; i < allLights.Length; i++)
            {
                Light l = allLights[i].LightSafe();
                if (l != null)
                {
                    
                    switch (l.type)
                        {
                        case LightType.Directional:
                            vc.Add(DirectionalVariant[allLights[i].MID(true)]);
                            vc.Add(DirectionalVariant[allLights[i].MID(false)]);
                            break;
                        case LightType.Point:
                            vc.Add(PointVariant[allLights[i].MID(true)]);
                            vc.Add(PointVariant[allLights[i].MID(false)]);
                            break;
                        case LightType.Spot:
                            vc.Add(SpotVariant[allLights[i].MID(true)]);
                            vc.Add(SpotVariant[allLights[i].MID(false)]);
                            break;
                        default:
                            break;
                        }                  
                }             
            }
            vc.WarmUp();
        }
    }

    static List<string> ShaderVariantList = new List<string>(10);
    void CreateShaderVariant(Shader source,int i, ref Material[] material, ref ShaderVariantCollection.ShaderVariant[] Variant, bool spot = false)
    {
        ShaderVariantList.Clear();
        material[i] = new Material(source);

        int v = i;
        int vc = 0;
        if (v >= 32) { material[i].EnableKeyword("VTRANSPARENCY_ON"); ShaderVariantList.Add("VTRANSPARENCY_ON"); v -= 32; vc++; }
        if (v >= 16) { material[i].EnableKeyword("DENSITYPARTICLES_ON"); ShaderVariantList.Add("DENSITYPARTICLES_ON"); v -= 16; vc++; }
        if (v >= 8) { material[i].EnableKeyword("HEIGHTFOG_ON"); ShaderVariantList.Add("HEIGHTFOG_ON"); v -= 8; vc++; }
        if (v >= 4) { material[i].EnableKeyword("NOISE_ON"); ShaderVariantList.Add("NOISE_ON"); v -= 4; vc++; }
        if (v >= 2) { if (!spot) { material[i].EnableKeyword("COOKIE_ON"); ShaderVariantList.Add("COOKIE_ON"); vc++; } v -= 2;  }
        if (v >= 1) { material[i].EnableKeyword("SHADOWS_ON"); ShaderVariantList.Add("SHADOWS_ON"); v -= 1; vc++; }

        if (resolution == Resolution.full)
        {
            material[i].EnableKeyword("FULL_ON");
            ShaderVariantList.Add("FULL_ON");
            vc++;
        }


        string[] fv = new string[vc];
        ShaderVariantList.CopyTo(fv);

        //string Final = "";
        //
        //for (int t = 0; t < vc; t++)
        //{
        //    Final += fv[t] + " "; 
        //}
        //Debug.Log(Final);

        Variant[i] = new ShaderVariantCollection.ShaderVariant(source,PassType.Normal, fv);
       
    }

    static int LastLevelID = -100000;
    void OnLevelWasLoaded(int level)
    {
        if (LastLevelID != level)
        {
            LastLevelID = level;
            if (!PIDCreated)
            {
                CreatePIDs();
            }
            else
            {
                WarmUp();
            }                
        }
    }




    void CreatePIDs()
    {
        bool warmup = false;
        if (!PIDCreated)
        {
            warmup = true;
            if (NoiseTexture3D == null) { Create3DNoiseTexture();}
            PIDCreated = true;
            VolumetricTexturePID = Shader.PropertyToID("VolumetricTexture");

            ScaledDepthTexturePID = Shader.PropertyToID("VolumetricDepth");
            ShadowMapTexturePID = Shader.PropertyToID("_ShadowMapTexture");
 
            DepthThresholdPID = Shader.PropertyToID("DepthThreshold");
            BlurDepthFalloffPID = Shader.PropertyToID("BlurDepthFalloff");
           
            VolumeScalePID = Shader.PropertyToID("VolumeScale");
            InverseViewMatrixPID = Shader.PropertyToID("InverseViewMatrix");
            InverseProjectionMatrixPID = Shader.PropertyToID("InverseProjectionMatrix");
            NoiseOffsetPID = Shader.PropertyToID("NoiseOffset");
            ShadowDistancePID = Shader.PropertyToID("ShadowDistance");

            for (int i = 0; i < EnumBufferDepthLength + 1; i++)
            {
                VolumetricDensityPID[i] = Shader.PropertyToID("VolumetricDensityTexture" + i);
                VolumetricTransparencyPID[i] = Shader.PropertyToID("VolumetricTransparencyTexture" + i);
            }

            HxVolumetricLight.CreatePID();

        }
        if (Tile5x5 == null) { CreateTileTexture(); }
        if (DownSampleMaterial == null) { DownSampleMaterial = new Material(Shader.Find("Hidden/HxVolumetricDownscaleDepth")); }
        if (TransparencyBlurMaterial == null) { TransparencyBlurMaterial = new Material(Shader.Find("Hidden/HxTransparencyBlur")); }
        if (DensityMaterial == null) { DensityMaterial = new Material(Shader.Find("Hidden/HxDensityShader")); }

        if (VolumeBlurMaterial == null) { VolumeBlurMaterial = new Material(Shader.Find("Hidden/HxVolumetricDepthAwareBlur")); }
        if (ApplyMaterial == null) { ApplyMaterial = new Material(Shader.Find("Hidden/HxVolumetricApply")); }
        if (QuadMesh == null) { QuadMesh = CreateQuad(); }
        if (BoxMesh == null) { BoxMesh = CreateBox(); }
        if (SphereMesh == null) { SphereMesh = CreateIcoSphere(1, 0.56f); }
        if (SpotLightMesh == null) { SpotLightMesh = CreateCone(4, false); }

        if (DirectionalMaterial[0] == null)
        {
            Shader directionalShader = Shader.Find("Hidden/HxVolumetricDirectionalLight");
            for (int i = 0; i < 64; i++)
            {
                CreateShaderVariant(directionalShader, i, ref DirectionalMaterial, ref DirectionalVariant);
            }
        }

        if (PointMaterial[0] == null)
        {
            Shader pointShader = Shader.Find("Hidden/HxVolumetricPointLight");
            for (int i = 0; i < 64; i++)
            {
                CreateShaderVariant(pointShader, i, ref PointMaterial, ref PointVariant);
            }
        }


        if (SpotMaterial[0] == null)
        {
            Shader spotShader = Shader.Find("Hidden/HxVolumetricSpotLight");
            for (int i = 0; i < 64; i++)
            {
                CreateShaderVariant(spotShader, i, ref SpotMaterial, ref SpotVariant,true);
            }
        }

        if(warmup) WarmUp();
        
        if (ShadowMaterial == null)
        {
            ShadowMaterial = new Material(Shader.Find("Hidden/HxShadowCasterFix"));
        }
    }

    void DefineFull()
    {
        if (resolution == Resolution.full)
        {
            for(int i = 0; i < SpotMaterial.Length;i++)
            {
                SpotMaterial[i].EnableKeyword("FULL_ON");
                SpotMaterial[i].DisableKeyword("FULL_OFF");
            }

            for (int i = 0; i < PointMaterial.Length; i++)
            {
                PointMaterial[i].EnableKeyword("FULL_ON");
                PointMaterial[i].DisableKeyword("FULL_OFF");
            }

            for (int i = 0; i < DirectionalMaterial.Length; i++)
            {
                DirectionalMaterial[i].EnableKeyword("FULL_ON");
                DirectionalMaterial[i].DisableKeyword("FULL_OFF");
            }
        }
        else
        {
            for (int i = 0; i < SpotMaterial.Length; i++)
            {
                SpotMaterial[i].EnableKeyword("FULL_OFF");
                SpotMaterial[i].DisableKeyword("FULL_ON");
            }

            for (int i = 0; i < PointMaterial.Length; i++)
            {
                PointMaterial[i].EnableKeyword("FULL_OFF");
                PointMaterial[i].DisableKeyword("FULL_ON");
            }

            for (int i = 0; i < DirectionalMaterial.Length; i++)
            {
                DirectionalMaterial[i].EnableKeyword("FULL_OFF");
                DirectionalMaterial[i].DisableKeyword("FULL_ON");
            }
        }
    }
    [HideInInspector]
    public static List<HxVolumetricLight> ActiveLights = new List<HxVolumetricLight>();
    public static List<HxVolumetricParticleSystem> ActiveParticleSystems = new List<HxVolumetricParticleSystem>();

    public static HxOctree<HxVolumetricLight> LightOctree;
    public static HxOctree<HxVolumetricParticleSystem> ParticleOctree;

    static void UpdateLight(HxOctreeNode<HxVolumetricLight>.NodeObject node, Vector3 boundsMin, Vector3 boundsMax)
    {
        LightOctree.Move(node, boundsMin, boundsMax);
    }

    public static HxOctreeNode<HxVolumetricLight>.NodeObject AddLightOctree(HxVolumetricLight light, Vector3 boundsMin, Vector3 boundsMax)
    {
        if (LightOctree == null) { LightOctree = new HxOctree<HxVolumetricLight>(Vector3.zero, 100, 0.1f, 10); }
 
        return LightOctree.Add(light, boundsMin, boundsMax);
    }


    public static HxOctreeNode<HxVolumetricParticleSystem>.NodeObject AddParticleOctree(HxVolumetricParticleSystem particle, Vector3 boundsMin, Vector3 boundsMax)
    {
        if (ParticleOctree == null) { ParticleOctree = new HxOctree<HxVolumetricParticleSystem>(Vector3.zero, 100, 0.1f, 10);}
      
        return ParticleOctree.Add(particle, boundsMin, boundsMax);
    }

    public static void RemoveLightOctree(HxVolumetricLight light)
    {
        if (LightOctree != null)
        {       
            LightOctree.Remove(light);
        }
    }

    public static void RemoveParticletOctree(HxVolumetricParticleSystem Particle)
    {
        if (ParticleOctree != null)
        {
            ParticleOctree.Remove(Particle);
        }
    }

    void OnApplicationQuit()
    {
        PIDCreated = false;
        if (SpotMaterial != null)
        {
            for (int i = 0; i < SpotMaterial.Length; i++)
            {
                if (SpotMaterial[i] != null)
                { 
                    GameObject.Destroy(SpotMaterial[i]);
                    SpotMaterial[i] = null;
                }
            }
        }

        if (PointMaterial != null)
        {
            for (int i = 0; i < PointMaterial.Length; i++)
            {
                if (PointMaterial[i] != null)
                {
                    GameObject.Destroy(PointMaterial[i]);
                    PointMaterial[i] = null;
                }
            }
        }

        if (DirectionalMaterial != null)
        {
            for (int i = 0; i < DirectionalMaterial.Length; i++)
            {
                if (DirectionalMaterial[i] != null)
                {
                    GameObject.Destroy(DirectionalMaterial[i]);
                    DirectionalMaterial[i] = null;
                }
            }
        }

        if (VolumeBlurMaterial != null) { GameObject.Destroy(VolumeBlurMaterial); VolumeBlurMaterial = null; }
        if (ApplyMaterial != null) { GameObject.Destroy(ApplyMaterial); ApplyMaterial = null; }
        if (DownSampleMaterial != null) { GameObject.Destroy(DownSampleMaterial); DownSampleMaterial = null; }
        if (TransparencyBlurMaterial != null) { GameObject.Destroy(TransparencyBlurMaterial); TransparencyBlurMaterial = null; }
        if (DensityMaterial != null) { GameObject.Destroy(DensityMaterial); DensityMaterial = null; }
        if (ShadowMaterial != null) { GameObject.Destroy(ShadowMaterial); ShadowMaterial = null; }
        if (SpotLightMesh != null) { GameObject.Destroy(SpotLightMesh); SpotLightMesh = null; }
        if (SphereMesh != null) { GameObject.Destroy(SphereMesh); SphereMesh = null; }
        if (NoiseTexture3D != null) { GameObject.Destroy(NoiseTexture3D); NoiseTexture3D = null; }
       

    }

    static public HashSet<HxVolumetricLight> AllVolumetricLight = new HashSet<HxVolumetricLight>();
    static public HashSet<HxVolumetricParticleSystem> AllParticleSystems = new HashSet<HxVolumetricParticleSystem> ();
    bool test;
    public static Mesh QuadMesh;
    public static Mesh BoxMesh;
    public static Mesh SphereMesh;
    public static Mesh SpotLightMesh;
    [HideInInspector]
    Camera Mycamera;

    public Camera GetCamera()
    {
        if (Mycamera == null) { Mycamera = GetComponent<Camera>(); }
        return Mycamera;
    }

    static float[] ResolutionScale = new float[4] { 1, 0.5f, 0.25f, 0.125f };
    public static float[] SampleScale = new float[4] { 1, 4, 16, 32 };

    CommandBuffer BufferSetup;
    CommandBuffer BufferFinalize;
    bool dirty = true;
    [System.NonSerialized]
    public static bool PIDCreated = false;
    public static Material[] DirectionalMaterial = new Material[64] { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };
    public static Material[] PointMaterial = new Material[64] { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };
    public static Material[] SpotMaterial = new Material[64] { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };

    public static ShaderVariantCollection.ShaderVariant[] DirectionalVariant = new ShaderVariantCollection.ShaderVariant[64];
    public static ShaderVariantCollection.ShaderVariant[] PointVariant = new ShaderVariantCollection.ShaderVariant[64];
    public static ShaderVariantCollection.ShaderVariant[] SpotVariant = new ShaderVariantCollection.ShaderVariant[64];

    public static Material ShadowMaterial;
    public static Material DensityMaterial;

    [HideInInspector]
    public Matrix4x4 MatrixVP;
    [HideInInspector]
    public Matrix4x4 MatrixV;
    static bool OffsetUpdated = false;

    [HideInInspector]
    public Texture2D SpotLightCookie { get { if (_SpotLightCookie == null) { _SpotLightCookie = (Texture2D)Resources.Load("LightSoftCookie"); if (_SpotLightCookie == null) { Debug.Log("couldnt find default cookie"); } } return _SpotLightCookie; } set { _SpotLightCookie = value; } }

    [HideInInspector]
    static Texture2D _SpotLightCookie;


    Vector4 CalculateDensityDistance(int i)
    {
        float slices = (((int)compatibleDBuffer() + 2) * 4) - 1;
        return new Vector4(
            densityDistance * Mathf.Pow((i + 1) / slices, densityBias) - densityDistance * Mathf.Pow(i / slices, densityBias),
            densityDistance * Mathf.Pow((i + 2) / slices, densityBias) - densityDistance * Mathf.Pow((i + 1) / slices, densityBias),
            densityDistance * Mathf.Pow((i + 3) / slices, densityBias) - densityDistance * Mathf.Pow((i + 2) / slices, densityBias),
            densityDistance * Mathf.Pow((i + 4) / slices, densityBias) - densityDistance * Mathf.Pow((i + 3) / slices, densityBias)
            );
    }

    Vector4 CalculateTransparencyDistance(int i)
    {
      
        float slices = (((int)compatibleTBuffer() + 2) * 4) - 1;
        return new Vector4(
            transparencyDistance * Mathf.Pow((i + 1) / slices, transparencyBias) - transparencyDistance * Mathf.Pow(i / slices, transparencyBias),
            transparencyDistance * Mathf.Pow((i + 2) / slices, transparencyBias) - transparencyDistance * Mathf.Pow((i + 1) / slices, transparencyBias),
            transparencyDistance * Mathf.Pow((i + 3) / slices, transparencyBias) - transparencyDistance * Mathf.Pow((i + 2) / slices, transparencyBias),
            transparencyDistance * Mathf.Pow((i + 4) / slices, transparencyBias) - transparencyDistance * Mathf.Pow((i + 3) / slices, transparencyBias)
            );
    }
    int ParticleDensityRenderCount = 0;
    void RenderParticles()
    {
        ParticleDensityRenderCount = 0;
        if (ParticleDensitySupport)
        {
            Shader.DisableKeyword("DensityDepth8");
            Shader.DisableKeyword("DensityDepth12");
            Shader.DisableKeyword("DensityDepth16");
            // Shader.DisableKeyword("DensityDepth20"); 
            // Shader.DisableKeyword("DensityDepth24"); 
            // Shader.DisableKeyword("DensityDepth28"); 
            // Shader.DisableKeyword("DensityDepth32"); 
            if (compatibleDBuffer() == BufferDepth.x8) { Shader.EnableKeyword("DensityDepth8"); }
            if (compatibleDBuffer() == BufferDepth.x12) { Shader.EnableKeyword("DensityDepth12"); }
            if (compatibleDBuffer() == BufferDepth.x16) { Shader.EnableKeyword("DensityDepth16"); }
            // if (compatibleDBuffer() == BufferDepth.x20) { Shader.EnableKeyword("DensityDepth20"); }
            // if (compatibleDBuffer() == BufferDepth.x24) { Shader.EnableKeyword("DensityDepth24"); }
            // if (compatibleDBuffer() == BufferDepth.x28) { Shader.EnableKeyword("DensityDepth28"); }
            // if (compatibleDBuffer() == BufferDepth.x32) { Shader.EnableKeyword("DensityDepth32"); }


            BufferSetup.SetGlobalVector("DensitySliceDistance0", CalculateDensityDistance(0));
            BufferSetup.SetGlobalVector("DensitySliceDistance1", CalculateDensityDistance(1));
            BufferSetup.SetGlobalVector("DensitySliceDistance2", CalculateDensityDistance(2));
            //BufferSetup.SetGlobalVector("DensitySliceDistance3", CalculateDensityDistance(3));
            //BufferSetup.SetGlobalVector("DensitySliceDistance4", CalculateDensityDistance(4));
            //BufferSetup.SetGlobalVector("DensitySliceDistance5", CalculateDensityDistance(5));
            //BufferSetup.SetGlobalVector("DensitySliceDistance6", CalculateDensityDistance(6));
            //BufferSetup.SetGlobalVector("DensitySliceDistance7", CalculateDensityDistance(7));


            ConstructPlanes(Mycamera, 0, Mathf.Max(MaxDirectionalRayDistance, MaxLightDistance));


            FindActiveParticleSystems();
            ParticleDensityRenderCount += RenderSlices();
            if (ParticleDensityRenderCount > 0)
            {
                Shader.DisableKeyword("DENSITYPARTICLES_OFF");
                Shader.EnableKeyword("DENSITYPARTICLES_ON");


                BufferSetup.SetGlobalVector("SliceSettings", new Vector4(densityDistance, 1f / densityBias, ((int)compatibleDBuffer() + 2) * 4, 0));

                for (int i = 0; i < (int)compatibleDBuffer() + 2; i++)
                {

                    BufferSetup.SetGlobalTexture(VolumetricDensityPID[i], VolumetricDensity[(int)compatibleDBuffer()][i]);
                }
            }
            else
            {
                Shader.DisableKeyword("DENSITYPARTICLES_ON");
                Shader.EnableKeyword("DENSITYPARTICLES_OFF");
            }

        }
        else
        {
            Shader.DisableKeyword("DENSITYPARTICLES_ON");
            Shader.EnableKeyword("DENSITYPARTICLES_OFF");
        }     

        //move this shit
        if (TransparencySupport)
        {
            Shader.DisableKeyword("VTRANSPARENCY_OFF");
            Shader.EnableKeyword("VTRANSPARENCY_ON");

            BufferSetup.SetGlobalVector("TransparencySliceSettings", new Vector4(transparencyDistance, 1f / transparencyBias, ((int)compatibleTBuffer() + 2) * 4, 1f/ transparencyDistance)); //transparent settings...  
           
            for (int i = 0; i < (int)compatibleTBuffer() + 2; i++)
            {
                BufferSetup.SetGlobalTexture(VolumetricTransparencyPID[i], VolumetricTransparencyI[(int)compatibleTBuffer()][i]);
            }
        }
        else
        {
            Shader.DisableKeyword("VTRANSPARENCY_ON");
            Shader.EnableKeyword("VTRANSPARENCY_OFF");       
        }

    }

    void OnPostRender()
    {
        Shader.DisableKeyword("VTRANSPARENCY_ON");
        Shader.EnableKeyword("VTRANSPARENCY_OFF");
    }
    static Matrix4x4 particleMatrix;

    int RenderSlices()
    {
        //change thiks to support more slices?
        //calculate view frustum
        //set active texture

        BufferSetup.SetRenderTarget(VolumetricDensity[(int)compatibleDBuffer()], VolumetricDensity[(int)compatibleDBuffer()][0]);
        BufferSetup.ClearRenderTarget(false, true, new Color(0.5f,0.5f,0.5f,0.5f));

        BufferSetup.SetGlobalVector("SliceSettings", new Vector4(densityDistance, 1f / densityBias, ((int)compatibleDBuffer() + 2) * 4, 0));
        int count = 0;


        for (int i = 0; i < ActiveParticleSystems.Count; i++)
        {
            if (ActiveParticleSystems[i].BlendMode == HxVolumetricParticleSystem.ParticleBlendMode.Max)
            {
                BufferSetup.SetGlobalFloat("particleDensity", ActiveParticleSystems[i].DensityStrength);
                DensityMaterial.CopyPropertiesFromMaterial(ActiveParticleSystems[i].particleRenderer.sharedMaterial);
                BufferSetup.DrawRenderer(ActiveParticleSystems[i].particleRenderer, DensityMaterial, 0, (int)ActiveParticleSystems[i].BlendMode);
                count++;
            }
        }

        for (int i = 0; i < ActiveParticleSystems.Count; i++)
        {
            if (ActiveParticleSystems[i].BlendMode == HxVolumetricParticleSystem.ParticleBlendMode.Add)
            {
                BufferSetup.SetGlobalFloat("particleDensity", ActiveParticleSystems[i].DensityStrength);
                DensityMaterial.CopyPropertiesFromMaterial(ActiveParticleSystems[i].particleRenderer.sharedMaterial);
                BufferSetup.DrawRenderer(ActiveParticleSystems[i].particleRenderer, DensityMaterial, 0, (int)ActiveParticleSystems[i].BlendMode);
                count++;
            }
        }

        for (int i = 0; i < ActiveParticleSystems.Count; i++)
        {
            if (ActiveParticleSystems[i].BlendMode == HxVolumetricParticleSystem.ParticleBlendMode.Min)
            {
                BufferSetup.SetGlobalFloat("particleDensity", ActiveParticleSystems[i].DensityStrength);
                DensityMaterial.CopyPropertiesFromMaterial(ActiveParticleSystems[i].particleRenderer.sharedMaterial);
                BufferSetup.DrawRenderer(ActiveParticleSystems[i].particleRenderer, DensityMaterial, 0, (int)ActiveParticleSystems[i].BlendMode);
                count++;
            }
        }

        for (int i = 0; i < ActiveParticleSystems.Count; i++)
        {
            if (ActiveParticleSystems[i].BlendMode == HxVolumetricParticleSystem.ParticleBlendMode.Sub)
            {
                BufferSetup.SetGlobalFloat("particleDensity", ActiveParticleSystems[i].DensityStrength);
                DensityMaterial.CopyPropertiesFromMaterial(ActiveParticleSystems[i].particleRenderer.sharedMaterial);
                BufferSetup.DrawRenderer(ActiveParticleSystems[i].particleRenderer, DensityMaterial, 0, (int)ActiveParticleSystems[i].BlendMode);
                count++;
            }
        }
        // BufferSetup.SetGlobalVector("TexelSize", new Vector2(1.0f/VolumetricDensity.width, 1.0f / VolumetricDensity.height));
        // BufferSetup.SetGlobalVector("offset", new Vector3((slice % 4) / 4f, Mathf.Floor(slice / 4) / 4f,4));
        // BufferSetup.Blit(VolumetricDensitySmallRTID, VolumetricDensityRTID, BlitDensityMaterial);



        //load ortho camera matrix.

        //render mesh into scene using the above texture.

        return count;
    }

    void CreateTempTextures()
    {
        CreatePIDs();
        int w = Mathf.CeilToInt(Mycamera.pixelWidth * ResolutionScale[(int)resolution]);
        int h = Mathf.CeilToInt(Mycamera.pixelHeight * ResolutionScale[(int)resolution]);

        //Mycamera.depthTextureMode = DepthTextureMode.Depth;
        if (resolution != Resolution.full && FullBlurRT == null)
        {
            FullBlurRT = RenderTexture.GetTemporary(Mycamera.pixelWidth, Mycamera.pixelHeight, 16, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            FullBlurRTID = new RenderTargetIdentifier(FullBlurRT);
            FullBlurRT.filterMode = FilterMode.Bilinear;
        }

        if (VolumetricTexture == null)
        {
            VolumetricTexture = RenderTexture.GetTemporary(w, h, 16, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            VolumetricTexture.filterMode = FilterMode.Bilinear;
            VolumetricTextureRTID = new RenderTargetIdentifier(VolumetricTexture);
        }

        if (ScaledDepthTexture[(int)resolution] == null)
        {
            ScaledDepthTexture[(int)resolution] = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear); //Need 4 channels for the upsampling.
            ScaledDepthTexture[(int)resolution].filterMode = FilterMode.Point;
            ScaledDepthTextureRTID[(int)resolution] = new RenderTargetIdentifier(ScaledDepthTexture[(int)resolution]);
        }

        if (TransparencySupport)
        {
            for (int b = 0; b < EnumBufferDepthLength; b++)
            {
                VolumetricTransparency[b][0] = VolumetricTextureRTID;
            }
            for (int i = 0; i < (int)compatibleTBuffer() + 2; i++)
            {
                if (VolumetricTransparencyTextures[i] == null)
                {
                    VolumetricTransparencyTextures[i] = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);//need to create a depth for no reason
                    VolumetricTransparencyTextures[i].filterMode = FilterMode.Bilinear;
                    RenderTargetIdentifier rti = new RenderTargetIdentifier(VolumetricTransparencyTextures[i]);
                    for (int b = Mathf.Max(i - 1, 0); b < EnumBufferDepthLength; b++)
                    {
                        VolumetricTransparency[b][i + 1] = rti;
                        VolumetricTransparencyI[b][i] = rti;
                    }
                }
            }
        }

        if ((blurCount > 0 || ((BlurTransparency > 0 || (MapToLDR == true || Mycamera.hdr == false)) && TransparencySupport)) && resolution != Resolution.full && downScaledBlurRT == null)
        {
            downScaledBlurRT = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            downScaledBlurRT.filterMode = FilterMode.Bilinear;
            downScaledBlurRTID = new RenderTargetIdentifier(downScaledBlurRT);
        }

        if ((FullBlurRT2 == null && resolution != Resolution.full && UpSampledblurCount > 0) || (resolution == Resolution.full && (blurCount > 0 || ((BlurTransparency > 0 || (MapToLDR == true || Mycamera.hdr == false)) && TransparencySupport))) || (MapToLDR || Mycamera.hdr == false))
        {
            FullBlurRT2 = RenderTexture.GetTemporary(Mycamera.pixelWidth, Mycamera.pixelHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            FullBlurRT2.filterMode = FilterMode.Bilinear;
            FullBlurRT2ID = new RenderTargetIdentifier(FullBlurRT2);
           
        }

        w = Mathf.CeilToInt(Mycamera.pixelWidth *  ResolutionScale[Mathf.Max((int)resolution, (int)densityResolution)]);
        h = Mathf.CeilToInt(Mycamera.pixelHeight * ResolutionScale[Mathf.Max((int)resolution, (int)densityResolution)]);


        if (ParticleDensitySupport)
        {
            for (int i = 0; i < (int)compatibleDBuffer() + 2; i++)
            {
                if (VolumetricDensityTextures[i] == null)
                {
                    VolumetricDensityTextures[i] = RenderTexture.GetTemporary(w, h, (i == 0 ? 16 : 0), RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);//need to create a depth for no reason
                    VolumetricDensityTextures[i].filterMode = FilterMode.Bilinear;
                    RenderTargetIdentifier rti = new RenderTargetIdentifier(VolumetricDensityTextures[i]);
                    for (int b = Mathf.Max(i - 1, 0); b < EnumBufferDepthLength; b++)
                    {
                        VolumetricDensity[b][i] = rti;
                    }
                }
            }
        }
    }

  
    //public RenderTargetIdentifier FullBlurRTID;
    //public static RenderTexture FullBlurRT;
    public static HxVolumetricCamera Active;
    public static Camera ActiveCamera;


    CameraEvent setupEvent = CameraEvent.AfterDepthNormalsTexture;
    CameraEvent FinalizeEvent = CameraEvent.AfterLighting;
    public static void ConstructPlanes(Camera cam,float near, float far)
    {
        Vector3 pos = cam.transform.position;
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;
        // Vector3 nearCenter = pos + forward * cam.nearClipPlane;
        Vector3 farCenter = pos + forward * far;
        Vector3 nearCenter = pos + forward * near;

        float farHeight = Mathf.Tan((cam.fieldOfView * Mathf.Deg2Rad) / 2f) * far;

        float farWidth = farHeight * cam.aspect;

        float nearHeight = Mathf.Tan((cam.fieldOfView * Mathf.Deg2Rad) / 2f) * near;

        float nearWidth = farHeight * cam.aspect;

        Vector3 farTopLeft = farCenter + up * (farHeight) - right * (farWidth);
        Vector3 farTopRight = farCenter + up * (farHeight) + right * (farWidth);
        Vector3 farBottomLeft = farCenter - up * (farHeight) - right * (farWidth);
        Vector3 farBottomRight = farCenter - up * (farHeight) + right * (farWidth);

        Vector3 nearTopLeft = nearCenter + up * (nearHeight) - right * (nearWidth);
        Vector3 nearTopRight = nearCenter + up * (nearHeight) + right * (nearWidth);
        Vector3 nearBottomLeft = nearCenter - up * (nearHeight) - right * (nearWidth);
       // Vector3 nearBottomRight = nearCenter - up * (nearHeight) + right * (nearWidth); //dont need all of them

        CameraPlanes[0] = new Plane(farBottomLeft, farTopLeft, farTopRight);   //far

        CameraPlanes[1] = new Plane(nearTopLeft, nearTopRight, nearBottomLeft); //near

        CameraPlanes[2] = new Plane(pos, farTopLeft, farBottomLeft);

        CameraPlanes[3] = new Plane(pos, farBottomRight, farTopRight);

        CameraPlanes[4] = new Plane(pos, farBottomLeft, farBottomRight);

        CameraPlanes[5] = new Plane(pos, farTopRight, farTopLeft);

        MinBounds = new Vector3(Mathf.Min(farTopLeft.x, Mathf.Min(farTopRight.x, Mathf.Min(farBottomLeft.x, Mathf.Min(farBottomRight.x, pos.x)))), Mathf.Min(farTopLeft.y, Mathf.Min(farTopRight.y, Mathf.Min(farBottomLeft.y, Mathf.Min(farBottomRight.y, pos.y)))), Mathf.Min(farTopLeft.z, Mathf.Min(farTopRight.z, Mathf.Min(farBottomLeft.z, Mathf.Min(farBottomRight.z, pos.z)))));
        MaxBounds = new Vector3(Mathf.Max(farTopLeft.x, Mathf.Max(farTopRight.x, Mathf.Max(farBottomLeft.x, Mathf.Max(farBottomRight.x, pos.x)))), Mathf.Max(farTopLeft.y, Mathf.Max(farTopRight.y, Mathf.Max(farBottomLeft.y, Mathf.Max(farBottomRight.y, pos.y)))), Mathf.Max(farTopLeft.z, Mathf.Max(farTopRight.z, Mathf.Max(farBottomLeft.z, Mathf.Max(farBottomRight.z, pos.z)))));
    }

    public static List<HxVolumetricLight> ActiveDirectionalLights = new List<HxVolumetricLight>();
    static Vector3 MinBounds;
    static Vector3 MaxBounds;
    static Plane[] CameraPlanes = new Plane[6] { new Plane(), new Plane(), new Plane(), new Plane(), new Plane(), new Plane() };

    void FindActiveLights()
    {
        ActiveLights.Clear();
        if (LightOctree != null)
        {
            LightOctree.GetObjectsBoundsPlane(ref CameraPlanes, MinBounds, MaxBounds, ActiveLights);
            //LightOctree.GetObjects(MinBounds, MaxBounds, ActiveLights);
        }
        for (int i = 0; i < ActiveDirectionalLights.Count; i++)
        {
            ActiveLights.Add(ActiveDirectionalLights[i]);
        }
    }

    void FindActiveParticleSystems()
    {
        ActiveParticleSystems.Clear();
        if (ParticleOctree != null)
        { ParticleOctree.GetObjectsBoundsPlane(ref CameraPlanes, MinBounds, MaxBounds, ActiveParticleSystems); }
    }

    public void Update()
    {
        OffsetUpdated = false;
    
        //for (int i = 0; i < ActiveLights.Count; i++)
        //{
        //    ActiveLights[i].DrawBounds();
        //}
        /// Debug.Log(ActiveLights.Count);
    }

    void Start()
    {
        CreateTempTextures();
    }

    void BuildBuffer()
    {
        if (Active != null) { ReleaseTempTextures(); }
        Active = this;
        ActiveCamera = Mycamera;
        CurrentTint = new Vector3(TintColor.linear.r, TintColor.linear.g, TintColor.linear.b) * TintIntensity;
        CurrentTintEdge = new Vector3(TintColor2.linear.r, TintColor2.linear.g, TintColor2.linear.b) * TintIntensity;
        if (dirty) //incase resolution was changed.
        {
            CreateTempTextures();

            if (BufferSetup == null) { BufferSetup = new CommandBuffer(); BufferSetup.name = "VolumetricSetup"; } else { BufferSetup.Clear(); }
            
            if (BufferFinalize == null) { BufferFinalize = new CommandBuffer(); BufferFinalize.name = "VolumetricFinalize"; } else { BufferFinalize.Clear(); }



            Matrix4x4 proj = GL.GetGPUProjectionMatrix(Mycamera.projectionMatrix, true);
            MatrixVP = proj * Mycamera.worldToCameraMatrix;
            MatrixV = Mycamera.worldToCameraMatrix;


            Matrix4x4 m_view = Mycamera.worldToCameraMatrix;
            Matrix4x4 m_proj = GL.GetGPUProjectionMatrix(Mycamera.projectionMatrix, false); //was false
            Matrix4x4 m_viewproj = m_proj * m_view;
            Matrix4x4 m_inv_viewproj = m_viewproj.inverse;



            BufferSetup.SetGlobalMatrix("_InvViewProj", m_inv_viewproj);


            BlitScale.z = HxVolumetricCamera.ActiveCamera.nearClipPlane + 1f;
            BlitScale.y = (HxVolumetricCamera.ActiveCamera.nearClipPlane + 1f) * Mathf.Tan(Mathf.Deg2Rad * HxVolumetricCamera.ActiveCamera.fieldOfView * 0.5f);
            BlitScale.x = BlitScale.y * HxVolumetricCamera.ActiveCamera.aspect;
            BlitMatrix = Matrix4x4.TRS(HxVolumetricCamera.Active.transform.position, HxVolumetricCamera.Active.transform.rotation, BlitScale);
            BlitMatrixMVP = HxVolumetricCamera.Active.MatrixVP* BlitMatrix;
            BlitMatrixMV = HxVolumetricCamera.Active.MatrixV* BlitMatrix;




           
            DefineFull();
            RenderParticles();
            //if (VolumetricCamera.Active.resolution == VolumetricCamera.Resolution.full)
            //{
            //    BufferSetup.SetRenderTarget(VolumetricTextureRTID);
            //}
            //else
            //{
            if (TransparencySupport)
            {
                //BufferSetup.SetRenderTarget(TransparencyTexture1RTID);
                //BufferSetup.ClearRenderTarget(false, true, new Color32(0, 0, 0, 0));
                //
                //BufferSetup.SetRenderTarget(TransparencyTexture2RTID);
                //BufferSetup.ClearRenderTarget(false, true, new Color32(0, 0, 0, 0));
                
                BufferSetup.SetRenderTarget(HxVolumetricCamera.VolumetricTransparencyI[(int)compatibleTBuffer()], HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
                BufferSetup.ClearRenderTarget(false, true, new Color32(0, 0, 0, 0));
            }


            BufferSetup.SetRenderTarget(VolumetricTextureRTID, HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);         
            BufferSetup.ClearRenderTarget(false, true, new Color(0,0,0,0));



            BufferSetup.SetGlobalFloat(DepthThresholdPID, DepthThreshold);
            BufferSetup.SetGlobalVector("CameraFoward", transform.forward);
            BufferSetup.SetGlobalFloat(BlurDepthFalloffPID, BlurDepthFalloff);
            BufferSetup.SetGlobalFloat(VolumeScalePID, ResolutionScale[(int)resolution]);
            BufferSetup.SetGlobalMatrix(InverseViewMatrixPID, Mycamera.cameraToWorldMatrix);
            BufferSetup.SetGlobalMatrix(InverseProjectionMatrixPID, Mycamera.projectionMatrix.inverse);
            if (OffsetUpdated == false) { OffsetUpdated = true; Offset += NoiseVelocity * Time.deltaTime; }
            BufferSetup.SetGlobalVector(NoiseOffsetPID, Offset);



            BufferSetup.SetGlobalFloat(ShadowDistancePID, QualitySettings.shadowDistance);

       
            //if (resolution != Resolution.full)
            //{
            //   BufferSetup.Blit(Tile10x10, ScaledDepthTextureRTID[1], DownSampleMaterial, 1);
            //   for (int i = 2; i <= (int)resolution; i++)
            //   {
            //       BufferSetup.Blit(ScaledDepthTextureRTID[i - 1], ScaledDepthTextureRTID[i], DownSampleMaterial, 4);
            //   }
            //   BufferSetup.SetGlobalTexture(ScaledDepthTexturePID, ScaledDepthTextureRTID[(int)resolution]);          
            //}
            //else
            //{
            BufferSetup.Blit(Tile5x5, ScaledDepthTextureRTID[(int)resolution], DownSampleMaterial, (int)resolution);
            BufferSetup.SetGlobalTexture(ScaledDepthTexturePID, ScaledDepthTextureRTID[(int)resolution]);
            //}

            CreateLightbuffers(); //his will add buffers to each light or to applystep for nonshadow casting lights                  
            CalculateEvent();

            if ((BlurTransparency > 0 || (MapToLDR == true || Mycamera.hdr == false)) && TransparencySupport )
            {
                int ctb = (int)compatibleTBuffer();
                int tbc = Mathf.Max(BlurTransparency, 1);
                for (int i = 0; i < ctb + 2; i++)
                {

                    for (int p = 0; p < tbc; p++)
                    {
                        BufferFinalize.Blit(VolumetricTransparencyI[ctb][i], (resolution == Resolution.full ? FullBlurRT2ID : downScaledBlurRTID), TransparencyBlurMaterial, 0);
                        BufferFinalize.Blit((resolution == Resolution.full ? FullBlurRT2ID : downScaledBlurRTID), VolumetricTransparencyI[ctb][i], TransparencyBlurMaterial, (((Mycamera.hdr == false || MapToLDR) && p == tbc - 1) ? 2 : 1));
                    }

                }
            }

            BuildApply();

            //transparency
            



            Mycamera.AddCommandBuffer(setupEvent, BufferSetup);

            Mycamera.AddCommandBuffer(FinalizeEvent, BufferFinalize);

        }
    }

    void OnDestroy()
    {
        if (Active == this) { Active.ReleaseLightBuffers(); ReleaseTempTextures(); }
    }

    void OnDisable()
    {
        if (Active == this) { Active.ReleaseLightBuffers(); ReleaseTempTextures(); }
    }

    void CalculateEvent()
    {
        switch (Mycamera.actualRenderingPath)
        {
            case RenderingPath.DeferredLighting:
                setupEvent = CameraEvent.BeforeLighting;
                FinalizeEvent = CameraEvent.AfterLighting;
                break;
            case RenderingPath.DeferredShading:
                setupEvent = CameraEvent.BeforeLighting;
                FinalizeEvent = CameraEvent.AfterLighting;
                break;
            case RenderingPath.Forward:
                if (Mycamera.depthTextureMode == DepthTextureMode.None) { Mycamera.depthTextureMode = DepthTextureMode.Depth; }
                if (Mycamera.depthTextureMode == DepthTextureMode.Depth) { setupEvent = CameraEvent.AfterDepthTexture; }
                if (Mycamera.depthTextureMode == DepthTextureMode.DepthNormals) { setupEvent = CameraEvent.AfterDepthNormalsTexture; }
                FinalizeEvent = CameraEvent.AfterForwardOpaque;
                break;
        }
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        //Graphics.Blit(VolumetricTexture, dest);

        Graphics.Blit(src, dest, ApplyMaterial, (QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 2) + (RemoveColorBanding ? 0 : 2));
        // BlurApply(src, dest);
        Mycamera.RemoveCommandBuffer(setupEvent, BufferSetup);
        Mycamera.RemoveCommandBuffer(FinalizeEvent, BufferFinalize);
        
        Active.ReleaseLightBuffers();
    }

    void BuildApply()
    {

        //int w = Mathf.CeilToInt(Mycamera.pixelWidth * ResolutionScale[(int)resolution]);
       // int h = Mathf.CeilToInt(Mycamera.pixelHeight * ResolutionScale[(int)resolution]);
        bool DownSampleToggle = true;
        bool SampleToggle = true;
       // RenderTargetIdentifier downScaledBlurRTID = new RenderTargetIdentifier(Shader.PropertyToID("downScaledBlurRT"));


        BufferFinalize.SetGlobalMatrix(HxVolumetricLight.VolumetricMVPPID, HxVolumetricCamera.BlitMatrixMVP);
        BufferFinalize.SetGlobalMatrix(HxVolumetricLight.VolumetricMVPID, HxVolumetricCamera.BlitMatrixMV);




        if (blurCount > 0 && resolution != Resolution.full)
        {

            BufferFinalize.SetGlobalFloat(BlurDepthFalloffPID, DownsampledBlurDepthFalloff);
           // BufferFinalize.GetTemporaryRT(Shader.PropertyToID("downScaledBlurRT"),w, h, 0, FilterMode.Bilinear,RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

            for (int i = 0; i < blurCount; i++)
            {
                if (DownSampleToggle)
                {
                    BufferFinalize.Blit(VolumetricTextureRTID, downScaledBlurRTID, VolumeBlurMaterial, (GaussianWeights ? 2 : 0));

                }
                else
                {
                    BufferFinalize.Blit(downScaledBlurRTID, VolumetricTextureRTID, VolumeBlurMaterial, (GaussianWeights ? 2 : 0));
                }
                DownSampleToggle = !DownSampleToggle;
                // //Shader.SetGlobalVector(blurDirPID, new Vector4(0, 1, 0, 0));
                // Graphics.Blit(VolumetricTexture, downScaledBlurRT, VolumeBlurMaterial, (AverageResults ? 2 : 0));
                //
                // //Shader.SetGlobalVector(blurDirPID, new Vector4(1, 0, 0, 0));
                // Graphics.Blit(downScaledBlurRT, VolumetricTexture, VolumeBlurMaterial, (AverageResults ? 2 : 0));
            }
        }

        if (resolution != Resolution.full)
        {
            //create full resolution
            BufferFinalize.SetRenderTarget(FullBlurRT);

            BufferFinalize.SetGlobalTexture("_MainTex", (DownSampleToggle ? VolumetricTextureRTID : downScaledBlurRTID));

            BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, HxVolumetricCamera.BlitMatrix, ApplyMaterial,0,0);


            //have to do this in command buffer....
            //GL.PushMatrix();
            //ApplyMaterial.SetPass(0);
            //GL.LoadOrtho();
            //GL.Begin(GL.QUADS);
            //GL.Color(Color.red);
            //GL.Vertex3(0, 0, 0);
            //GL.Vertex3(1, 0, 0);
            //GL.Vertex3(1, 1, 0);
            //GL.Vertex3(0, 1, 0);
            //GL.End();
            //GL.PopMatrix();



           // RenderTargetIdentifier FullBlurRT2ID = new RenderTargetIdentifier(Shader.PropertyToID("FullBlurRT2")); 
            if (UpSampledblurCount > 0)
            {
                BufferFinalize.SetGlobalFloat(BlurDepthFalloffPID, BlurDepthFalloff);
                //BufferFinalize.GetTemporaryRT(Shader.PropertyToID("FullBlurRT2"),Mycamera.pixelWidth, Mycamera.pixelHeight, 0,FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                //FullBlurRT2ID = new RenderTargetIdentifier(Shader.PropertyToID("FullBlurRT2"));
                BufferFinalize.Blit(FullBlurRTID, FullBlurRT2ID);

                if (UpSampledblurCount % 2 != 0) { SampleToggle = false; }

                for (int i = 0; i < UpSampledblurCount; i++)
                {
                    if (SampleToggle)
                    {
                        BufferFinalize.SetGlobalTexture("_FullVolumetric", FullBlurRT);
                        BufferFinalize.SetRenderTarget(FullBlurRT2ID, FullBlurRTID);
                        BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, HxVolumetricCamera.BlitMatrix, VolumeBlurMaterial, 0, 1);
                    }
                    else
                    {
                        BufferFinalize.SetGlobalTexture("_FullVolumetric", FullBlurRT2ID);
                        BufferFinalize.SetRenderTarget(FullBlurRTID, FullBlurRTID);
                        BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, HxVolumetricCamera.BlitMatrix, VolumeBlurMaterial, 0, 1);
                    }
                    SampleToggle = !SampleToggle;
                }
            }

            if (MapToLDR || Mycamera.hdr == false)
            {
                BufferFinalize.Blit(FullBlurRTID, FullBlurRT2, TransparencyBlurMaterial, 3);
                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT2);
            }
            else
            {
                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT);
            }

               

           //if ( UpSampledblurCount > 0)
           //{
           //    BufferFinalize.ReleaseTemporaryRT(Shader.PropertyToID("FullBlurRT2"));
           //}

           // if (blurCount > 0)
           // {
           //     BufferFinalize.ReleaseTemporaryRT(Shader.PropertyToID("downScaledBlurRT"));
           // }

        }
        else
        {
            // RenderTargetIdentifier FullBlurRT2ID = new RenderTargetIdentifier(Shader.PropertyToID("FullBlurRT2"));
            if (blurCount > 0)
            {
                BufferFinalize.SetGlobalFloat(BlurDepthFalloffPID, BlurDepthFalloff);
                //BufferFinalize.GetTemporaryRT(Shader.PropertyToID("FullBlurRT2"), Mycamera.pixelWidth, Mycamera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                //FullBlurRT2ID = new RenderTargetIdentifier(Shader.PropertyToID("FullBlurRT2"));
                SampleToggle = true;
                for (int i = 0; i < blurCount; i++)
                {
                    if (SampleToggle)
                    {
                        BufferFinalize.SetGlobalTexture("_FullVolumetric", VolumetricTextureRTID);
                        BufferFinalize.SetRenderTarget(FullBlurRT2ID);
                        BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, HxVolumetricCamera.BlitMatrix, VolumeBlurMaterial, 0, (GaussianWeights ? 5 : 4));
                    }
                    else
                    {
                        BufferFinalize.SetGlobalTexture("_FullVolumetric", FullBlurRT2ID);
                        BufferFinalize.SetRenderTarget(VolumetricTextureRTID);
                        BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, HxVolumetricCamera.BlitMatrix, VolumeBlurMaterial, 0, 4);
                    }

                    //    // BufferFinalize.Blit(VolumetricTextureRTID, FullBlurRT2ID);
                    //     BufferFinalize.Blit(VolumetricTextureRTID, FullBlurRT2ID, VolumeBlurMaterial, (GaussianWeights ? 5 : 4));
                    // }
                    // else
                    // {
                    //     //BufferFinalize.Blit(FullBlurRT2ID, VolumetricTextureRTID);
                    //     BufferFinalize.Blit(FullBlurRT2ID, VolumetricTextureRTID, VolumeBlurMaterial, (GaussianWeights ? 5 : 4));
                    // }
                    SampleToggle = !SampleToggle;
                }

                if (!SampleToggle)
                {
                    //BufferFinalize.Blit(FullBlurRT2ID, VolumetricTextureRTID);
                    if (MapToLDR || Mycamera.hdr == false)
                    {
                        BufferFinalize.Blit(FullBlurRT2ID , VolumetricTextureRTID, TransparencyBlurMaterial, 3);
                        BufferFinalize.SetGlobalTexture(VolumetricTexturePID, VolumetricTexture);
                    }
                    else
                    {
                        BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT2);
                    }
                    
                }
                else
                {
                    if (MapToLDR || Mycamera.hdr == false)
                    {
                        BufferFinalize.Blit(VolumetricTextureRTID, FullBlurRT2, TransparencyBlurMaterial, 3);
                        BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT2);
                    }
                    else
                    {
                        BufferFinalize.SetGlobalTexture(VolumetricTexturePID, VolumetricTextureRTID);
                    }
                       
                }
                // BufferFinalize.ReleaseTemporaryRT(Shader.PropertyToID("FullBlurRT2"));               
            }
            else
            {
                if (MapToLDR || Mycamera.hdr == false)
                {
                    BufferFinalize.Blit(VolumetricTextureRTID, FullBlurRT2, TransparencyBlurMaterial, 3);
                    BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT2);
                }
                else
                {
                    BufferFinalize.SetGlobalTexture(VolumetricTexturePID, VolumetricTextureRTID);
                }

                
            }

        }
    }   

    int ScalePass()
    {
        if (resolution == Resolution.half) { return 0; }
        if (resolution == Resolution.quarter) { return 1; }
        //if (resolution == Resolution.eighth) { return 2; }
        return 2;
    }

    void DownSampledFullBlur(RenderTexture mainColor, RenderBuffer NewColor, RenderBuffer depth,int pass)
    {
        Graphics.SetRenderTarget(NewColor, depth);
        VolumeBlurMaterial.SetTexture("_MainTex", mainColor);
        GL.PushMatrix();
        VolumeBlurMaterial.SetPass(pass);
        GL.LoadOrtho();
        GL.Begin(GL.QUADS);
        GL.Color(Color.red);
        GL.Vertex3(0, 0, 0);
        GL.Vertex3(1, 0, 0);
        GL.Vertex3(1, 1, 0);
        GL.Vertex3(0, 1, 0);
        GL.End();
        GL.PopMatrix();
    }
#if UNITY_EDITOR
    public static void ReleaseShaders()
    {
        GameObject.DestroyImmediate(ApplyMaterial);
        GameObject.DestroyImmediate(DensityMaterial);
        GameObject.DestroyImmediate(ShadowMaterial);
        GameObject.DestroyImmediate(ApplyMaterial);
        GameObject.DestroyImmediate(DensityMaterial);
        GameObject.DestroyImmediate(VolumeBlurMaterial);
        GameObject.DestroyImmediate(DownSampleMaterial);
        GameObject.DestroyImmediate(TransparencyBlurMaterial);
        GameObject.DestroyImmediate(NoiseTexture3D);
        GameObject.DestroyImmediate(Tile5x5);
     
        GameObject.DestroyImmediate(SphereMesh);
        GameObject.DestroyImmediate(SpotLightMesh);
        GameObject.DestroyImmediate(QuadMesh);
        CollectionAll = null;
        //if (CollectionAll != null)
        //{
        //    GameObject.DestroyImmediate(CollectionAll);
        //}

        for (int i = 0; i < DirectionalMaterial.Length; i++)
        {
            GameObject.DestroyImmediate(DirectionalMaterial[i]);
        }
        for (int i = 0; i < SpotMaterial.Length; i++)
        {
            GameObject.DestroyImmediate(SpotMaterial[i]);
        }

        for (int i = 0; i < PointMaterial.Length; i++)
        {
            GameObject.DestroyImmediate(PointMaterial[i]);
        }

    }
#endif

    public static void ReleaseTempTextures()
    {
        //BufferApply.ReleaseTemporaryRT(VolumetricTextureID);
        //if (resolution != Resolution.full)
        //{
        //    BufferApply.ReleaseTemporaryRT(ScaledDepthTextureID);
        //}
       
        if (VolumetricTexture != null) { RenderTexture.ReleaseTemporary(VolumetricTexture); VolumetricTexture = null; }
        if (FullBlurRT != null) {RenderTexture.ReleaseTemporary(FullBlurRT); FullBlurRT = null; }

        for (int i = 0; i < VolumetricTransparencyTextures.Length;i++)
        {
            if (VolumetricTransparencyTextures[i] != null) { RenderTexture.ReleaseTemporary(VolumetricTransparencyTextures[i]); VolumetricTransparencyTextures[i] = null; }
        }


        for (int i = 0; i < VolumetricDensityTextures.Length; i++)
        {
            if (VolumetricDensityTextures[i] != null) { RenderTexture.ReleaseTemporary(VolumetricDensityTextures[i]); VolumetricDensityTextures[i] = null; }
        }

        if (downScaledBlurRT != null)
        {
            RenderTexture.ReleaseTemporary(downScaledBlurRT); downScaledBlurRT = null;
        }

        if (FullBlurRT2 != null)
        {
            RenderTexture.ReleaseTemporary(FullBlurRT2); FullBlurRT2 = null;
        }
        //if (resolution != Resolution.full)
        //{
        if (ScaledDepthTexture[0] != null) { RenderTexture.ReleaseTemporary(ScaledDepthTexture[0]); ScaledDepthTexture[0] = null; }
        if (ScaledDepthTexture[1] != null) { RenderTexture.ReleaseTemporary(ScaledDepthTexture[1]); ScaledDepthTexture[1] = null; }
        if (ScaledDepthTexture[2] != null) { RenderTexture.ReleaseTemporary(ScaledDepthTexture[2]); ScaledDepthTexture[2] = null; }
        if (ScaledDepthTexture[3] != null) { RenderTexture.ReleaseTemporary(ScaledDepthTexture[3]); ScaledDepthTexture[3] = null; }

        //}   

    }

    void OnPreCull()
    {
        ConstructPlanes(Mycamera,0, MaxLightDistance); //set near to 0 just incase.
        UpdateLightPoistions();

        UpdateParticlePoistions();
        FindActiveLights();
       
        BuildBuffer();
    }

    void UpdateLightPoistions()
    {
        for (var e = AllVolumetricLight.GetEnumerator(); e.MoveNext(); )
        {
            e.Current.UpdatePosition();
        }
        if (LightOctree != null) LightOctree.TryShrink();
    }

    void UpdateParticlePoistions()
    {
        if (ParticleDensitySupport)
        {
            for (var e = AllParticleSystems.GetEnumerator(); e.MoveNext();)
            {
                e.Current.UpdatePosition();
            }
            if (ParticleOctree != null) ParticleOctree.TryShrink();
        }
    }

    void Awake()
    {

        if (_SpotLightCookie == null)
        {
            _SpotLightCookie = (Texture2D)Resources.Load("LightSoftCookie");
        }


        CreatePIDs();


        Mycamera = GetComponent<Camera>();
    }

    void start()
    {
        Mycamera = GetComponent<Camera>();
    }

    public void ReleaseLightBuffers()
    {
        for (int i = 0; i < ActiveLights.Count; i++)
        {
            ActiveLights[i].ReleaseBuffer();
        }

    }

    public static bool FirstDirectional = true;

    void CreateLightbuffers()
    {
        FirstDirectional = true;
        for (int i = 0; i < ActiveLights.Count; i++)
        {
            ActiveLights[i].BuildBuffer(BufferSetup);
        }
    }

    static void CreateTileTexture()
    {
        Tile5x5 = new Texture2D(5, 5, TextureFormat.RFloat, false, true);
        Tile5x5.filterMode = FilterMode.Point;
        Tile5x5.wrapMode = TextureWrapMode.Repeat;
        Color[] tempc = new Color[25];
        for (int i = 0; i < tempc.Length; i++)
        {
            tempc[i] = new Color(Tile5x5int[i] * 0.04f, 0, 0, 0);
        }

        Tile5x5.SetPixels(tempc);
        Tile5x5.Apply();
        Shader.SetGlobalTexture("Tile5x5", Tile5x5);
    }

    public static Mesh CreateCone(int sides, bool inner = true)
    {
        Mesh newMesh = new Mesh();
        Vector3[] verts = new Vector3[sides + 1];
        int[] tri = new int[(sides * 3) + ((sides - 2) * 3)];

        float r = (inner ? Mathf.Cos(Mathf.PI / (sides)) : 1f);

        float aLength = r * Mathf.Tan(Mathf.PI / (sides));
        Vector3 topCenter = new Vector3(0.5f - ((1f - r) / 2f), 0, 0);
        Vector3 Offset = new Vector3(0, 0, aLength);


        topCenter += new Vector3(0, 0, aLength / 2f);

        //verts
        Quaternion offsetAngle = Quaternion.Euler(new Vector3(0, (360f / (sides)), 0));
        Quaternion Rotation = Quaternion.Euler(new Vector3(-90, 0, 0));

        verts[0] = new Vector3(0f, 0f, 0);

        for (int i = 1; i < sides + 1; i++)
        {
            verts[i] = Rotation * (topCenter - Vector3.up);

            topCenter -= Offset;
            Offset = offsetAngle * Offset;
        };


        //triSides
        int n = 0;
        for (int i = 0; i < sides - 1; i++)
        {
            n = i * 3;
            tri[n] = 0;
            tri[n + 1] = i + 1;
            tri[n + 2] = i + 2;
        }
        n = (sides - 1) * 3; ;
        tri[n] = 0;
        tri[n + 1] = sides;
        tri[n + 2] = 1;
        n += 3;

        for (int i = 0; i < sides - 2; i++)
        {
            tri[n] = 1;
            tri[n + 2] = i + 2;
            tri[n + 1] = i + 3;
            n += 3;
        }


        newMesh.vertices = verts;
        newMesh.triangles = tri;
        newMesh.uv = new Vector2[verts.Length];
        newMesh.colors = new Color[0];
        newMesh.Optimize();
        newMesh.bounds = new Bounds(Vector3.zero, Vector3.one);
        newMesh.RecalculateNormals();

        return newMesh;
    }

    public static Mesh CreateQuad()
    {
        /*
        GameObject t = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Mesh m = t.GetComponent<MeshFilter>().sharedMesh;
        if (Application.isPlaying)
        {
            GameObject.Destroy(t);
        }
        else
        {
            GameObject.DestroyImmediate(t);
        }
        return m;
        */
        
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];

        vertices[0] = new Vector3(-1, -1, 1);
        vertices[1] = new Vector3(-1, 1, 1);
        vertices[2] = new Vector3(1, -1, 1);
        vertices[3] = new Vector3(1, 1, 1);

        mesh.vertices = vertices;

        int[] indices = new int[6];

        indices[0] = 0;
        indices[1] = 1;
        indices[2] = 2;

        indices[3] = 2;
        indices[4] = 1;
        indices[5] = 3;

        mesh.triangles = indices;
        mesh.RecalculateBounds();

        return mesh;
        
    }

    public static Mesh CreateBox()
    {
        GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh m = t.GetComponent<MeshFilter>().sharedMesh;
        if (Application.isPlaying)
        {
            GameObject.Destroy(t);
        }
        else
        {
            GameObject.DestroyImmediate(t);
        }
        return m;
    }

    public static Mesh CreateIcoSphere(int recursionLevel, float radius)
    {

        Mesh mesh = new Mesh();
        mesh.Clear();

        List<Vector3> vertList = new List<Vector3>();
        Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();

        // create 12 vertices of a icosahedron
        float t = (1f + Mathf.Sqrt(5f)) / 2f;

        vertList.Add(new Vector3(-1f, t, 0f).normalized * radius);
        vertList.Add(new Vector3(1f, t, 0f).normalized * radius);
        vertList.Add(new Vector3(-1f, -t, 0f).normalized * radius);
        vertList.Add(new Vector3(1f, -t, 0f).normalized * radius);

        vertList.Add(new Vector3(0f, -1f, t).normalized * radius);
        vertList.Add(new Vector3(0f, 1f, t).normalized * radius);
        vertList.Add(new Vector3(0f, -1f, -t).normalized * radius);
        vertList.Add(new Vector3(0f, 1f, -t).normalized * radius);

        vertList.Add(new Vector3(t, 0f, -1f).normalized * radius);
        vertList.Add(new Vector3(t, 0f, 1f).normalized * radius);
        vertList.Add(new Vector3(-t, 0f, -1f).normalized * radius);
        vertList.Add(new Vector3(-t, 0f, 1f).normalized * radius);


        // create 20 triangles of the icosahedron
        List<TriangleIndices> faces = new List<TriangleIndices>();

        // 5 faces around point 0
        faces.Add(new TriangleIndices(0, 11, 5));
        faces.Add(new TriangleIndices(0, 5, 1));
        faces.Add(new TriangleIndices(0, 1, 7));
        faces.Add(new TriangleIndices(0, 7, 10));
        faces.Add(new TriangleIndices(0, 10, 11));

        // 5 adjacent faces 
        faces.Add(new TriangleIndices(1, 5, 9));
        faces.Add(new TriangleIndices(5, 11, 4));
        faces.Add(new TriangleIndices(11, 10, 2));
        faces.Add(new TriangleIndices(10, 7, 6));
        faces.Add(new TriangleIndices(7, 1, 8));

        // 5 faces around point 3
        faces.Add(new TriangleIndices(3, 9, 4));
        faces.Add(new TriangleIndices(3, 4, 2));
        faces.Add(new TriangleIndices(3, 2, 6));
        faces.Add(new TriangleIndices(3, 6, 8));
        faces.Add(new TriangleIndices(3, 8, 9));

        // 5 adjacent faces 
        faces.Add(new TriangleIndices(4, 9, 5));
        faces.Add(new TriangleIndices(2, 4, 11));
        faces.Add(new TriangleIndices(6, 2, 10));
        faces.Add(new TriangleIndices(8, 6, 7));
        faces.Add(new TriangleIndices(9, 8, 1));


        // refine triangles
        for (int i = 0; i < recursionLevel; i++)
        {
            List<TriangleIndices> faces2 = new List<TriangleIndices>();
            foreach (var tri in faces)
            {
                // replace triangle by 4 triangles
                int a = getMiddlePoint(tri.v1, tri.v2, ref vertList, ref middlePointIndexCache, radius);
                int b = getMiddlePoint(tri.v2, tri.v3, ref vertList, ref middlePointIndexCache, radius);
                int c = getMiddlePoint(tri.v3, tri.v1, ref vertList, ref middlePointIndexCache, radius);

                faces2.Add(new TriangleIndices(tri.v1, a, c));
                faces2.Add(new TriangleIndices(tri.v2, b, a));
                faces2.Add(new TriangleIndices(tri.v3, c, b));
                faces2.Add(new TriangleIndices(a, b, c));
            }
            faces = faces2;
        }

        mesh.vertices = vertList.ToArray();

        List<int> triList = new List<int>();
        for (int i = 0; i < faces.Count; i++)
        {
            triList.Add(faces[i].v1);
            triList.Add(faces[i].v2);
            triList.Add(faces[i].v3);
        }

        mesh.triangles = triList.ToArray();
        mesh.uv = new Vector2[vertList.Count];

        Vector3[] normales = new Vector3[vertList.Count];
        for (int i = 0; i < normales.Length; i++)
            normales[i] = vertList[i].normalized;


        mesh.normals = normales;

        mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
        mesh.Optimize();

        return mesh;

    }

    private struct TriangleIndices
    {
        public int v1;
        public int v2;
        public int v3;

        public TriangleIndices(int v1, int v2, int v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }

    private static int getMiddlePoint(int p1, int p2, ref List<Vector3> vertices, ref Dictionary<long, int> cache, float radius)
    {
        // first check if we have it already
        bool firstIsSmaller = p1 < p2;
        long smallerIndex = firstIsSmaller ? p1 : p2;
        long greaterIndex = firstIsSmaller ? p2 : p1;
        long key = (smallerIndex << 32) + greaterIndex;

        int ret;
        if (cache.TryGetValue(key, out ret))
        {
            return ret;
        }

        // not in cache, calculate it
        Vector3 point1 = vertices[p1];
        Vector3 point2 = vertices[p2];
        Vector3 middle = new Vector3
        (
            (point1.x + point2.x) / 2f,
            (point1.y + point2.y) / 2f,
            (point1.z + point2.z) / 2f
        );

        // add vertex makes sure point is on unit sphere
        int i = vertices.Count;
        vertices.Add(middle.normalized * radius);

        // store it, return index
        cache.Add(key, i);

        return i;
    }

    public void Create3DNoiseTexture()
    {
        int size = 32;
        NoiseTexture3D = new Texture3D(size, size, size, TextureFormat.Alpha8, false);
        NoiseTexture3D.filterMode = FilterMode.Bilinear;
        NoiseTexture3D.wrapMode = TextureWrapMode.Repeat;
        Tileable3DNoise sn = new Tileable3DNoise();
        Color[] tempc = new Color[size * size * size];

        int idx = 0;

        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++, ++idx)
                {
                    tempc[idx].a = sn.noiseArray[idx] / 255.0f;
                }
            }
        }

        //Debug.Log("min = " + min + " Max = " + max + "Average = " + (average/(32*32*32)));

        NoiseTexture3D.SetPixels(tempc);
        NoiseTexture3D.Apply();

        Shader.SetGlobalTexture("NoiseTexture3D", NoiseTexture3D);
    }

    int PostoIndex(Vector3 pos)
    {
        if (pos.x >= 32) { pos.x = 0; } else if (pos.x < 0) { pos.x = 31; }
        if (pos.y >= 32) { pos.y = 0; } else if (pos.y < 0) { pos.y = 31; }
        if (pos.z >= 32) { pos.z = 0; } else if (pos.z < 0) { pos.z = 31; }

        return (int)(pos.z * 32 * 32 + pos.y * 32 + pos.x);
    }
    //float SampleNoise(Tileable3DNoise sn,Vector3 pos)
    //{
    //    float amount = 0.5f + sn.noiseArray[PostoIndex(pos)];
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward)]) * 0.7f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back)]) * 0.7f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.left)]) * 0.7f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.right)]) * 0.7f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.up)]) * 0.7f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.down)]) * 0.7f;
    //               
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.right)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.left)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.right)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.left)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.up + Vector3.forward)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.up + Vector3.back)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.down + Vector3.forward)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.down + Vector3.back)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.right + Vector3.down)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.right + Vector3.back)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.left + Vector3.up)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.left + Vector3.down)]) * 0.5f;
    //               
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.up + Vector3.right)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.up + Vector3.left)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.up + Vector3.right)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.up + Vector3.left)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.down + Vector3.right)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.down + Vector3.left)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.down + Vector3.right)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.down + Vector3.left)]) * 0.3f;
    //     float f = Mathf.Clamp01((amount / 13.6f) * 1.4285714285714285714285714285714f);
    //     return f * f;
    //}


    //    static int[] Tile10x10int = new int[100] //{ 50, 28, 14, 8, 45, 63, 20, 5, 39, 11, 33, 12, 23, 30, 29, 17, 36, 56, 22, 59, 0, 4, 60, 35, 2, 57, 47, 21, 6, 13, 38, 1, 52, 42, 19, 16, 62, 44, 31, 15, 58, 40, 53, 43, 61, 3, 48, 24, 18, 41, 32, 26, 37, 27, 46, 54, 55, 7, 9, 25, 34, 51, 49, 10 };
    //{35,	67,	48,	0,	82,	24,	76,	45,	8,	97,
    //5,	85,	39,	68,	40,	2,	95,	22,	77,	50,
    //64,	42,	13,	81,	21,	72,	56,	7,	88,	34,
    //90,	30,	75,	52,	12,	93,	29,	62,	55,	17,
    //51,	19,	96,	36,	69,	44,	18,	84,	32,	71,
    //33,	66,	53,	11,	87,	23,	64,	41,	10,	92,
    //15,	86,	26,	74,	46,	16,	83,	25,	60,	54,
    //79,	58,	3,	80,	38,	78,	59,	6,	99,	37,
    //94,	31,	73,	43,	9,	91,	28,	61,	57,	4 ,
    //49,	14,	98,	27,	63,	47,	1,	89,	20,	70};
    //
    //    
    //}

    //{ 50, 28, 14, 8, 45, 63, 20, 5, 39, 11, 33, 12, 23, 30, 29, 17, 36, 56, 22, 59, 0, 4, 60, 35, 2, 57, 47, 21, 6, 13, 38, 1, 52, 42, 19, 16, 62, 44, 31, 15, 58, 40, 53, 43, 61, 3, 48, 24, 18, 41, 32, 26, 37, 27, 46, 54, 55, 7, 9, 25, 34, 51, 49, 10 };   
    static int[] Tile5x5int = new int[25]{
8,	18,	22,	0,	13,
4,	14,	9,	19,	21,
16,	23,	1,	12,	6,
10,	7,	15,	24,	3,
20,	2,	11,	5,	17};
}


