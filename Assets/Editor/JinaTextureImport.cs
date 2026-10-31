using UnityEditor;
using UnityEngine;
public class JinaTextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/JinaV4/")) return;
        var t=(TextureImporter)assetImporter;
        t.textureType=TextureImporterType.Sprite; t.spriteImportMode=SpriteImportMode.Single;
        t.filterMode=FilterMode.Point; t.textureCompression=TextureImporterCompression.Uncompressed;
        t.mipmapEnabled=false; t.isReadable=true; t.alphaIsTransparency=true;
        t.npotScale=TextureImporterNPOTScale.None; t.maxTextureSize=2048;
    }
}
