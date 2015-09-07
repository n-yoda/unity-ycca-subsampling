using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Diagnostics;

public static class Y8CgCoA4Encoder
{
    public static void Encode (Color[] src, Color[] dst)
    {
        for (int i = 0; i < src.Length; i ++)
        {
            var c2 = src[i] / 2;
            var c4 = src[i] / 4;
            var y = c4.r + c2.g + c4.b;
            var cg = -c4.r + c2.g - c4.b + 0.5f;
            var co = c2.r - c2.b + 0.5f;
            dst[i] = new Color (cg, src[i].a, co, y);
        }
    }

    public static void EncodeGPU (Texture2D src, Texture2D dst)
    {
        var shader = Shader.Find ("Hidden/Y8CgCoA4 Encoder");
        var mat = new Material (shader);
        var temp = RenderTexture.GetTemporary (src.width, src.height);
        Graphics.Blit (src, temp, mat);
        dst.ReadPixels (new Rect (0, 0, dst.width, dst.height), 0, 0);
        RenderTexture.ReleaseTemporary (temp);
        Object.DestroyImmediate (mat);
    }

    public static void EncodeSave (Texture2D src, TextureImporterFormat fmt4bpp, bool gpu,
       string ySuffix = "y", string ccaSuffix = "cgcoa")
    {
        var path = AssetDatabase.GetAssetPath (src);
        var yPath = Path.ChangeExtension (path, ySuffix + ".png");
        var ccaPath = Path.ChangeExtension (path, ccaSuffix + ".png");
   
        // Load the raw PNG instead of modifying the importer.
        var dst = new Texture2D (1, 1);
        dst.LoadImage (File.ReadAllBytes (path));
        if (gpu)
        {
            EncodeGPU(dst, dst);
        }
        else
        {
            var pixels = dst.GetPixels ();
            Encode (pixels, pixels);
            dst.SetPixels (pixels);
        }

        // Write and import
        var png = dst.EncodeToPNG ();
        File.WriteAllBytes (yPath, png);
        File.WriteAllBytes (ccaPath, png);
        Object.DestroyImmediate (dst);
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
        ccaImp.compressionQuality = 100;

        yImp.SaveAndReimport();
        ccaImp.SaveAndReimport();
    }

    [MenuItem("Assets/Y8CgCoA4/Encode PVRTC")]
    static void EncodeSavePVRTC()
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex) EncodeSave (tex, TextureImporterFormat.PVRTC_RGB4, false);
    }

    [MenuItem("Assets/Y8CgCoA4/Encode PVRTC (GPU)")]
    static void EncodeSaveETC()
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex) EncodeSave (tex, TextureImporterFormat.PVRTC_RGB4, true);
    }
}
