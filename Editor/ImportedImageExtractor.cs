using System;
using net.rs64.TexTransCoreEngineForUnity;
using net.rs64.TexTransTool.MultiLayerImage;
using net.rs64.TexTransTool.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace net.rs64.TexTransTool.DestructiveTextureUtilities
{
    internal class ImportedImageExtractor : DestructiveUtility
    {
        public TTTImportedImage TTTImportedImage;
        public override void CreateUtilityPanel(VisualElement rootElement)
        {
            var serializedObject = new SerializedObject(this);
            rootElement.hierarchy.Add(new Label("PSDなどからインポートされたレイヤーを抽出します。"));
            rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(TTTImportedImage))));

            var button = new Button(Extract);
            button.text = "Execute";
            rootElement.hierarchy.Add(button);
        }

        void Extract()
        {
            if (TTTImportedImage == null) { EditorUtility.DisplayDialog("ImportedImageExtractor - 実行不可能", "TTTImportedImage が存在しません！", "Ok"); return; }

            var canvasData = TTTImportedImage.CanvasDescription.LoadCanvasSource(AssetDatabase.GetAssetPath(TTTImportedImage.CanvasDescription));
            var diskLoader = new UnityDiskUtil(false);
            var ttce = new TTCEUnityWithTTT4Unity(diskLoader);

            using var rt = ttce.CreateRenderTexture(TTTImportedImage.CanvasDescription.Width, TTTImportedImage.CanvasDescription.Height);
            TTTImportedImage.LoadImage(canvasData, ttce, rt);

            var tex2D = ttce.DownloadToTexture2D(rt, false);
            tex2D.name = TTTImportedImage.name + "-Extracted";
            var extractedPath = AssetSaveHelper.SavePNG(tex2D);
            UnityEngine.Object.DestroyImmediate(tex2D);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(extractedPath));
        }
    }
}
