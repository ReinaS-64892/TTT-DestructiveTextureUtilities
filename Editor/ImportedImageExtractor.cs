using System;
using System.IO;
using net.rs64.TexTransTool.MultiLayerImage;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace net.rs64.DestructiveTextureUtilities
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

            var canvasBytes = File.ReadAllBytes(AssetDatabase.GetAssetPath(TTTImportedImage.CanvasDescription));
            var imageData = TTTImportedImage.LoadImage(canvasBytes);

            var tex2d = new Texture2D(TTTImportedImage.CanvasDescription.Width, TTTImportedImage.CanvasDescription.Height, TextureFormat.RGBA32, false);

            tex2d.LoadRawTextureData(imageData.GetResult);
            tex2d.name = TTTImportedImage.name + "-Extracted";
            AssetSaveHelper.SavePNG(tex2d);

            imageData.GetResult.Dispose();
        }
    }
}
