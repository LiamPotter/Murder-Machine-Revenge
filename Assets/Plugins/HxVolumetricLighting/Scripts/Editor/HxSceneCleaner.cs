#if UNITY_EDITOR
public class MyAssetModificationProcessor : UnityEditor.AssetModificationProcessor
{
    // need this for 5.1 bug
    public static string[] OnWillSaveAssets(string[] paths)
    {
        HxVolumetricCamera.ReleaseTempTextures();
        HxVolumetricCamera.ReleaseShaders();
        return paths;
    }
}
#endif
