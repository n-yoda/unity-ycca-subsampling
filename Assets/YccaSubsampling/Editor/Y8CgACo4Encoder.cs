using UnityEngine;
using UnityEditor;
using System.IO;

public static class Y8CgACo4Encoder
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

    public static void Encode (Texture2D src, Texture2D dst)
    {
        var pixels = dst.GetPixels ();
        Encode (pixels, pixels);
        dst.SetPixels (pixels);
    }

    public static void EncodeGPU (Texture2D src, Texture2D dst)
    {
		var shader = Shader.Find ("Hidden/RGBA to CgACoY");
        var mat = new Material (shader);
        var temp = RenderTexture.GetTemporary (src.width, src.height);
        Graphics.Blit (src, temp, mat);
        dst.ReadPixels (new Rect (0, 0, dst.width, dst.height), 0, 0);
        RenderTexture.ReleaseTemporary (temp);
        Object.DestroyImmediate (mat);
    }

    public static void EncodeSave (Texture2D src, TextureImporterFormat fmt4bpp, bool gpu,
       string ySuffix = "y", string ccaSuffix = "c")
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

	[MenuItem("Assets/Ycca Subsampling/Y (Alpha8) CgACo (PVRTC4)")]
    static void EncodeSavePVRTC()
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex) EncodeSave (tex, TextureImporterFormat.PVRTC_RGB4, false);
    }

	[MenuItem("Assets/Ycca Subsampling/Y (Alpha8) CgACo (PVRTC4) [GPU]")]
    static void EncodeSavePVRTC_GPU()
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex) EncodeSave (tex, TextureImporterFormat.PVRTC_RGB4, true);
    }
}
