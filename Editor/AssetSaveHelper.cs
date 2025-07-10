using UnityEditor;
using System.IO;
using UnityEngine;

namespace net.rs64.TexTransTool.DestructiveTextureUtilities
{
    internal static class AssetSaveHelper
    {
        public const string SaveDirectory = "Assets/TexTransToolGenerates";


        public static string CreateUniqueNewFolder(string name)
        {
            if (!Directory.Exists(SaveDirectory)) { AssetDatabase.CreateFolder("Assets", "TexTransToolGenerates"); }
            var guid = AssetDatabase.CreateFolder(SaveDirectory, name);
            return AssetDatabase.GUIDToAssetPath(guid);
        }

        internal static string SavePNG(string parentPath, UnityEngine.Texture2D tex2d)
        {
            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(parentPath, FilteringInvalidChars(tex2d.name) + ".png"));
            File.WriteAllBytes(path, tex2d.EncodeToPNG());
            return path;
        }
        internal static string SavePNG(UnityEngine.Texture2D tex2d)
        {
            if (!Directory.Exists(SaveDirectory)) { AssetDatabase.CreateFolder("Assets", "TexTransToolGenerates"); }
            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(SaveDirectory, FilteringInvalidChars(tex2d.name) + ".png"));
            File.WriteAllBytes(path, tex2d.EncodeToPNG());
            return path;
        }

        internal static string FilteringInvalidChars(string str)
        {
            return string.Join("_", str.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
