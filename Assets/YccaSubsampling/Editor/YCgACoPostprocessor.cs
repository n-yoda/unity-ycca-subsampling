using System;
using System.IO;
using UnityEngine;
using UnityEditor;

public class YCgACoUserData
{
    public const string Head = "YCgACoTexture";
    public YCgACoFormat Format { get; set; }
    public string SourceGuid { get; set; }
    public bool UseGpuEncoder { get; set; }

    public override string ToString()
    {
        return string.Join(" ", new string[] {
            Head,
            Format.ToString(),
            SourceGuid,
            UseGpuEncoder.ToString(),
        });
    }
    
    public static YCgACoUserData Parse(string str)
    {
        if (!str.StartsWith(Head + " ")) return null;
        try
        {
            var words = str.Split(' ');
            return new YCgACoUserData() {
                Format = (YCgACoFormat)Enum.Parse(typeof(YCgACoFormat), words [1]),
                SourceGuid = words[2],
                UseGpuEncoder = bool.Parse(words[3])
            };
        }
        catch { return null; }
    }

    static T EnumParse<T>(string str)
    {
        return (T)Enum.Parse(typeof(T), str);
    }
}

public class YCgACoPostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (YCgACoUserData.Parse(assetImporter.userData) != null)
        {
            var importer = assetImporter as TextureImporter;
            importer.textureType = TextureImporterType.GUI;
            importer.textureFormat = TextureImporterFormat.ARGB32;
        }
    }

    void OnPostprocessTexture(Texture2D dst)
    {
        var userData = YCgACoUserData.Parse(assetImporter.userData);
        if (userData == null) return;
        var importer = assetImporter as TextureImporter;

        // Load texture
        var srcPath = AssetDatabase.GUIDToAssetPath(userData.SourceGuid);
        var src = new Texture2D (1, 1, TextureFormat.RGBA32, false, true);
        src.LoadImage(File.ReadAllBytes(srcPath));

        // Encode
        YCgACoEncoder.Encode(src, dst, userData.UseGpuEncoder, userData.Format, importer.compressionQuality);
    }
}
