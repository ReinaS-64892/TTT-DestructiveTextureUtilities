using System.Collections.Generic;
using System.IO;
using System.Linq;
using net.rs64.TexTransTool.Build;
using net.rs64.TexTransTool.TextureAtlas.FineTuning;
using net.rs64.TexTransTool.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace net.rs64.TexTransTool.DestructiveTextureUtilities
{
    internal class BakeToPNG : DestructiveUtility
    {
        public GameObject DomainRoot;
        public bool InPlaceMode;
        public override void CreateUtilityPanel(VisualElement rootElement)
        {
            var serializedObject = new SerializedObject(this);

            rootElement.hierarchy.Add(new Label("アバターなどをマニュアルベイクし、編集したテクスチャーをPNGに書き出します。"));
            rootElement.hierarchy.Add(new Label("DomainRoot にベイクしたい対象を割り当ててください。"));

            rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(DomainRoot))));
            rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(InPlaceMode))));

            var button = new Button(Execute);
            button.text = "Execute";
            rootElement.hierarchy.Add(button);
        }

        void Execute()
        {
            if (DomainRoot == null) { EditorUtility.DisplayDialog("BakeToPNG - 実行不可能", "DomainRoot が存在しません！", "Ok"); return; }
            if (InPlaceMode && EditorUtility.DisplayDialog("In-Place Mode is Enable!!!", "インプレースモードが有効です！ 後戻りできない可能性のある操作です！\n\n本当に実行しますか？", "Yes", "No") is false) { return; }
            var outputDirectory = AssetSaveHelper.CreateUniqueNewFolder(DomainRoot.name + "-BakeToPNGResult");

            GameObject target;
            if (InPlaceMode is false)
            {
                target = Instantiate(DomainRoot);
                target.transform.position = new Vector3(target.transform.position.x, target.transform.position.y, target.transform.position.z + 2);
            }
            else { target = DomainRoot; }


            var phaseDict = TexTransBehaviorSearch.FindAtPhase(target);
            var savePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(outputDirectory, "OtherAssetsContainer.asset"));
            var assetSaver = new AssetSaver(savePath);


            var domain = new HookedAvatarDomain(target, assetSaver, outputDirectory);
            var session = new TexTransBuildSession(target, domain, phaseDict);

            AvatarBuildUtils.ExecuteAllPhaseAndEnd(session);
            AvatarBuildUtils.DestroyITexTransToolTags(target);

            domain.CreatePNG();
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(savePath);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(savePath));
        }


    }
    internal class HookedAvatarDomain : AvatarDomain
    {
        private readonly string _savePath;

        public HookedAvatarDomain(GameObject avatarRoot, IAssetSaver assetSaver, string savePath) : base(avatarRoot, assetSaver)
        {
            _savePath = savePath;
        }

        public override void Dispose()
        {
            // base.Dispose();
            MergeStack();
            ReadBackToTexture2D();
        }
        // TODO : もっといい場所にフックをかけるべきだ
        public void CreatePNG()
        {
            var swapTexture2D = new Dictionary<Texture2D, Texture2D>();

            foreach (var compressKV in _renderTextureDescriptorManager.DownloadedDescriptors)
            {
                var sourceTex2D = compressKV.Key;
                if (sourceTex2D == null) { continue; }
                var path = AssetSaveHelper.SavePNG(_savePath, sourceTex2D);
                AssetDatabase.ImportAsset(path);
                var importer = TextureImporter.GetAtPath(path) as TextureImporter;
                switch (compressKV.Value.TextureFormat)
                {
                    default: { break; }
                    case TexTransTool.TextureManagerUtility.RefAtImporterFormat refAt:
                        {
                            importer.compressionQuality = refAt.TextureImporter.compressionQuality;
                            importer.textureCompression = refAt.TextureImporter.textureCompression;
                            importer.alphaIsTransparency = refAt.TextureImporter.alphaIsTransparency;
                            break;
                        }
                    case TextureCompressionData compressedData:
                        {
                            importer.compressionQuality = compressedData.CompressionQuality;
                            importer.textureCompression = GetTextureFormatQualityUnity(compressedData.FormatQualityValue);
                            break;
                        }
                }

                importer.SaveAndReimport();
                swapTexture2D.Add(sourceTex2D, AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            }

            foreach (var r in swapTexture2D)
                this.ReplaceTexture(r.Key, r.Value);
        }
        public TextureImporterCompression GetTextureFormatQualityUnity(FormatQuality formatQuality)
        {
            switch (formatQuality)
            {
                case FormatQuality.None: { return TextureImporterCompression.Uncompressed; }
                case FormatQuality.Low: { return TextureImporterCompression.CompressedLQ; }
                default:
                case FormatQuality.Normal: { return TextureImporterCompression.Compressed; }
                case FormatQuality.High: { return TextureImporterCompression.CompressedHQ; }
            }
        }
    }



}
