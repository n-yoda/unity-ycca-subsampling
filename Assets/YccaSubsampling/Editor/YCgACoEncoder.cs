using UnityEngine;
using UnityEditor;
using System.IO;

public enum YCgACoFormat
{
    CgACoY_DontChange, // For testing purpose
    Y_Alpha8_8bpp,
    CgACo_PVRTC_4bpp,
    CgACo_PVRTC_2bpp,
    CgACo_DXT_4bpp,
    CgACo_ETC_4bpp,
    CgACo_RGB24_Half_6bpp,
    CgACo_RGB565_Half_4bpp,
}

public static class YCgACoEncoder
{
    public static void RGBAToCgACoY(Color[] src, Color[] dst)
    {
        for (int i = 0; i < src.Length; i ++)
        {
            var c2 = src [i] / 2;
            var c4 = src [i] / 4;
            var y = c4.r + c2.g + c4.b;
            var cg = -c4.r + c2.g - c4.b + 0.5f;
            var co = c2.r - c2.b + 0.5f;
            dst [i] = new Color(cg, src [i].a, co, y);
        }
    }

    public static void Encode(Texture2D src, Texture2D dst, bool gpu = false,
                              YCgACoFormat format = YCgACoFormat.CgACoY_DontChange,
                              int quality = 100)
    {
        var pixels = src.GetPixels();
        Resize(src, dst, format);
        var resized = src.width != dst.width || src.height != dst.height;
        if (gpu)
        {
            // TODO: Force mipmap and trilinear when resized.
            var shader = Shader.Find("Hidden/RGBA to CgACoY");
            var mat = new Material(shader);
            var temp = RenderTexture.GetTemporary(dst.width, dst.height);
            Graphics.Blit(src, temp, mat);
            dst.ReadPixels(new Rect(0, 0, dst.width, dst.height), 0, 0);
            RenderTexture.ReleaseTemporary(temp);
            Object.DestroyImmediate(mat);
        }
        else
        {
            if (resized)
            {
                var srcPixels = pixels;
                pixels = dst.GetPixels();
                Shrink(srcPixels, pixels, src.width, dst.width);
            }
            RGBAToCgACoY(pixels, pixels);
            dst.SetPixels(pixels);
        }
        Compress(dst, format, quality);
    }

    static void Resize(Texture src, Texture2D dst, YCgACoFormat format)
    {
        var width = src.width;
        var height = src.height;
        switch (format)
        {
            case YCgACoFormat.CgACo_RGB24_Half_6bpp:
            case YCgACoFormat.CgACo_RGB565_Half_4bpp:
                width /= 2;
                height /= 2;
                break;
        }
        if (dst.width != width || dst.height != height)
            dst.Resize(width, height);
    }

    public static void Shrink(Color[] src, Color[] dst, int srcW, int dstW)
    {
        int srcH = src.Length / srcW;
        int dstH = dst.Length / dstW;
        float scaleX = (float)srcW / dstW;
        float scaleY = (float)srcH / dstH;
        for (int y = 0; y < dstH; y ++)
        {
            for (int x = 0; x < dstW; x++)
            {
                Color c = new Color(0, 0, 0, 0);
                float weights = 0;
                float vStart = y * scaleY;
                float vEnd = vStart + scaleY;
                int vStartInt = Mathf.FloorToInt(vStart);
                int vEndInt = Mathf.Min(srcH, Mathf.CeilToInt(vEnd));
                for (int v = vStartInt; v < vEndInt; v ++)
                {
                    float vWeight = 1 - Mathf.Max(0, vStart - v) - Mathf.Max(0, v + 1 - vEnd);
                    float uStart = x * scaleX;
                    float uEnd = uStart + scaleX;
                    int uStartInt = Mathf.FloorToInt(uStart);
                    int uEndInt = Mathf.Min(srcW, Mathf.CeilToInt(uEnd));
                    for (int u = uStartInt; u < uEndInt; u ++)
                    {
                        float uWeight = 1 - Mathf.Max(0, uStart - u) - Mathf.Max(0, u + 1 - uEnd);
                        float weight = uWeight * vWeight;
                        weights += weight;
                        c += src[v * srcW + u] * weight;
                    }
                }
                
                dst[y * dstW + x] = c * (1 / weights);
            }
        }
    }

    static void Compress(Texture2D dst, YCgACoFormat format, int quality)
    {
        var texFormat = dst.format;
        switch (format)
        {
            case YCgACoFormat.Y_Alpha8_8bpp:
                texFormat = TextureFormat.Alpha8;
                break;
            case YCgACoFormat.CgACo_PVRTC_4bpp:
                texFormat = TextureFormat.PVRTC_RGB4;
                break;
            case YCgACoFormat.CgACo_PVRTC_2bpp:
                texFormat = TextureFormat.PVRTC_RGBA2;
                break;
            case YCgACoFormat.CgACo_ETC_4bpp:
                texFormat = TextureFormat.ETC_RGB4;
                break;
            case YCgACoFormat.CgACo_DXT_4bpp:
                texFormat = TextureFormat.DXT1;
                break;
            case YCgACoFormat.CgACo_RGB24_Half_6bpp:
                texFormat = TextureFormat.RGB24;
                break;
            case YCgACoFormat.CgACo_RGB565_Half_4bpp:
                texFormat = TextureFormat.RGB565;
                break;
            case YCgACoFormat.CgACoY_DontChange:
                return;
        }
        EditorUtility.CompressTexture(dst, texFormat, quality);
    }
}
