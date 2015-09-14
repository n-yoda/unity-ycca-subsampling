using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class YCgACoEditor : EditorWindow
{
    Vector2 scroll;
    List<Entry> entries = new List<Entry>();

    class Entry
    {
        public Texture2D Texture { get; private set; }
        public TextureImporter Importer { get; private set; }
        public bool Edited { get; private set; }

        YCgACoUserData userData;
        Texture2D source;
        int quality;

        public bool HasUserData
        {
            get { return userData != null; }
        }

        public YCgACoFormat Format
        {
            get { return userData.Format; }
            set
            {
                if (value != userData.Format)
                {
                    Edited = true;
                    userData.Format = value;
                }
            }
        }

        public bool UseGpuEncoder
        {
            get { return userData.UseGpuEncoder; }
            set
            {
                if (value != userData.UseGpuEncoder)
                {
                    Edited = true;
                    userData.UseGpuEncoder = value;
                }
            }
        }

        public Texture2D Source
        {
            get { return source; }
            set
            {
                if (value != source)
                {
                    Edited = true;
                    source = value;
                    var srcPath = AssetDatabase.GetAssetPath(source);
                    userData.SourceGuid = AssetDatabase.AssetPathToGUID(srcPath);
                }
            }
        }

        public int Quality
        {
            get { return quality; }
            set
            {
                if (value != quality)
                {
                    Edited = true;
                    quality = value;
                }
            }
        }

        public Entry(Texture2D tex, TextureImporter imp)
        {
            this.Texture = tex;
            this.Importer = imp;
            Revert();
        }

        public void MakeUserData(Texture2D source)
        {
            userData = new YCgACoUserData();
            var srcPath = AssetDatabase.GetAssetPath(source);
            userData.SourceGuid = AssetDatabase.AssetPathToGUID(srcPath);
            this.source = source;
            Edited = true;
        }

        public void Apply()
        {
            if (Edited)
            {
                Importer.userData = userData.ToString();
                Importer.compressionQuality = quality;
                Importer.SaveAndReimport();
                Edited = false;
            }
        }

        public void Revert()
        {
            userData = YCgACoUserData.Parse(Importer.userData);
            source = userData == null ? null as Texture2D
                : AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(userData.SourceGuid));
            quality = Importer.compressionQuality;
            Edited = false;
        }

        public void Clear()
        {
            if (userData != null)
            {
                Importer.userData = "";
                Revert();
                Importer.SaveAndReimport();
            }
        }
    }

    [MenuItem("Window/YCgACo Editor")]
    static void ShowWindow()
    {
        EditorWindow.GetWindow<YCgACoEditor>("YCgACo Editor");
    }

    void OnSelectionChange()
    {
        ApplySelection(Selection.objects);
        Repaint();
    }

    void ApplySelection(Object[] selection)
    {
        entries.Clear();
        foreach (var obj in selection)
        {
            var texture = obj as Texture2D;
            var path = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (texture && importer)
                entries.Add(new Entry(texture, importer));
        }
    }
 
    void OnGUI()
    {
        var backup = GUI.enabled;
        Texture2D generate = null;
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var entry in entries)
        {
            EditorGUILayout.ObjectField(entry.Texture, typeof(Texture2D), false);
            if (!entry.HasUserData)
            {
                if (GUILayout.Button("Import This as CgACoY (for Test)"))
                    entry.MakeUserData(entry.Texture);
                if (GUILayout.Button("Generate Y, CgACo and Material"))
                    generate = entry.Texture;
            }
            else
            {
                entry.Format = (YCgACoFormat)EditorGUILayout.EnumPopup("Format", entry.Format);
                entry.Source = EditorGUILayout.ObjectField("Source", entry.Source, typeof(Texture2D), false,
                                                           GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight)) as Texture2D;
                entry.UseGpuEncoder = EditorGUILayout.Toggle("Use GPU Encoder", entry.UseGpuEncoder);
                entry.Quality = EditorGUILayout.IntSlider("Compression Quality", entry.Quality, 0, 100);

                EditorGUILayout.BeginHorizontal();
                GUI.enabled = entry.Edited;
                if (GUILayout.Button("Apply")) entry.Apply();
                if (GUILayout.Button("Revert")) entry.Revert();
                GUI.enabled = backup;
                if (GUILayout.Button("Clear")) entry.Clear();
                GUI.enabled = !entry.Edited;
                if (GUILayout.Button("Reimport")) entry.Importer.SaveAndReimport();
                GUI.enabled = backup;
                EditorGUILayout.EndHorizontal();

                // TODO: Replace a dummy PNG by baked one.
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = false;
                GUILayout.Button("Bake as PNG");
                GUILayout.Button("Bake as Asset");
                GUI.enabled = backup;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndScrollView();
        if (generate)
        {
            Generate(generate);
        }
    }

    void Generate(Texture2D source)
    {
        var path = AssetDatabase.GetAssetPath(source);
        var selection = new Object[]{
            MakeEmptyPng(AddSuffixToPath(path, "Y")),
            MakeEmptyPng(AddSuffixToPath(path, "CgACo")),
        };
        ApplySelection(selection);
        entries[0].MakeUserData(source);
        entries[0].Format = YCgACoFormat.Y_Alpha8_8bpp;
        entries[0].Apply();
        entries[1].MakeUserData(source);
        entries[1].Format = YCgACoFormat.CgACo_RGB24_Half_6bpp;
        entries[1].Apply();
        var mat = new Material(Shader.Find("Custom/Y-CgACo to RGBA"));
        var matPath = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(path, "mat"));
        mat.SetTexture("_Y", entries[0].Texture);
        mat.SetTexture("_CgACo", entries[1].Texture);
        AssetDatabase.CreateAsset(mat, matPath);
        Selection.objects = selection;
    }

    static string AddSuffixToPath(string path, string suffix)
    {
        return Path.ChangeExtension(path, suffix + Path.GetExtension(path));
    }

    static Texture2D MakeEmptyPng(string path)
    {
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        var tex2d = new Texture2D(1, 1);
        File.WriteAllBytes(path, tex2d.EncodeToPNG());
        DestroyImmediate(tex2d);
        var option = ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUncompressedImport;
        AssetDatabase.ImportAsset(path, option);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
