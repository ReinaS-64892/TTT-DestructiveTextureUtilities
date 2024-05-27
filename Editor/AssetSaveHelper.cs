using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace net.rs64.DestructiveTextureUtilities
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

    }
}
