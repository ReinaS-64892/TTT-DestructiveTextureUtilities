using System.IO;
using net.rs64.TexTransCoreEngineForUnity;
using net.rs64.TexTransCoreEngineForUnity.Utils;
using net.rs64.TexTransTool.MultiLayerImage;
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
            var diskLoader = new UnityDiskUtil(new TextureManager(false));
            var ttce = new TTCE4UnityWithTTT4Unity(diskLoader);

            using var rt = ttce.CreateRenderTexture(TTTImportedImage.CanvasDescription.Width, TTTImportedImage.CanvasDescription.Height);
            TTTImportedImage.LoadImage(canvasData, ttce, rt);

            var tex2D = rt.Unwrap().CopyTexture2D();
            tex2D.name = TTTImportedImage.name + "-Extracted";
            AssetSaveHelper.SavePNG(tex2D);
            UnityEngine.Object.DestroyImmediate(tex2D);
        }
    }
}
