using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public static class Y8CgCoA4Encoder
{
    public static Color RGBAToYCgCoA (Color color)
    {
        var c2 = color / 2;
        var c4 = color / 4;
        return new Color (
            c4.r + c2.g + c4.b,
           -c4.r + c2.g - c4.b,
            c2.r + 0 - c2.b,
            color.a);
    }

    public static void Encode (Color[] pixels, Color[] dst)
    {
        for (int i = 0; i < pixels.Length; i ++)
        {
            var ycca = RGBAToYCgCoA (pixels[i]);
            dst[i] = new Color (ycca.g + 0.5f, ycca.b + 0.5f, ycca.a, ycca.r);
        }
    }

    public static void EncodeSave (Texture2D src, TextureImporterFormat fmt4bpp,
       string ySuffix = "y", string ccaSuffix = "cgcoa")
    {
        var path = AssetDatabase.GetAssetPath (src);
        var yPath = Path.ChangeExtension (path, ySuffix + ".png");
        var ccaPath = Path.ChangeExtension (path, ccaSuffix + ".png");
   
        // Load the raw PNG instead of modifying the importer.
        var dst = new Texture2D (1, 1);
        dst.LoadImage (File.ReadAllBytes (path));
        var pixels = dst.GetPixels ();
        Encode (pixels, pixels);
        dst.SetPixels (pixels);
        dst.Apply ();

        // Write and import
        var png = dst.EncodeToPNG ();
        File.WriteAllBytes (yPath, png);
        File.WriteAllBytes (ccaPath, png);
        AssetDatabase.Refresh (ImportAssetOptions.ForceSynchronousImport);

        // Importer settings
        var settings = new TextureImporterSettings ();
        var srcImp = AssetImporter.GetAtPath (path) as TextureImporter;
        srcImp.ReadTextureSettings (settings);
        var yImp = AssetImporter.GetAtPath (yPath) as TextureImporter;
        yImp.SetTextureSettings (settings);
        yImp.alphaIsTransparency = true;
        yImp.textureFormat = TextureImporterFormat.Alpha8;
        var ccaImp = AssetImporter.GetAtPath (ccaPath) as TextureImporter;
        ccaImp.SetTextureSettings (settings);
        ccaImp.alphaIsTransparency = false;
        ccaImp.textureFormat = fmt4bpp;

        yImp.SaveAndReimport();
        ccaImp.SaveAndReimport();
    }

    [MenuItem("Assets/Y8CgCoA4/Encode Alpha8 PVRTC")]
    static void EncodeSavePVRTC()
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex) EncodeSave (tex, TextureImporterFormat.PVRTC_RGB4);
    }

    [MenuItem("Assets/Y8CgCoA4/Encode Alpha8 ETC")]
    static void EncodeSaveETC()
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex) EncodeSave (tex, TextureImporterFormat.ETC2_RGB4);
    }
}
