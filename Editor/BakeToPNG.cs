
using System.Collections.Generic;
using System.IO;
using System.Linq;
using net.rs64.TexTransCoreEngineForUnity.Utils;
using net.rs64.TexTransTool;
using net.rs64.TexTransTool.Build;
using net.rs64.TexTransTool.TextureAtlas.FineTuning;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace net.rs64.DestructiveTextureUtilities
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


            var phaseDict = AvatarBuildUtils.FindAtPhase(target);
            var assetSaver = new AssetSaver(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(outputDirectory, "OtherAssetsContainer.asset")));

            var deferredDestroyer = new DeferredDestroyer();
            var compressionManager = new ToPNGCompress(outputDirectory);
            var textureManager = new TextureManager(deferredDestroyer, new GetOriginTexture(false, deferredDestroyer.DeferredDestroyOf), compressionManager);

            var domain = new AvatarDomain(target, false, textureManager, assetSaver);
            var session = new TexTransBuildSession(domain, phaseDict);

            AvatarBuildUtils.ExecuteAllPhaseAndEnd(session);
            AvatarBuildUtils.DestroyITexTransToolTags(target);
            var texDict = compressionManager.CreateCompresses();

            var renderers = target.GetComponentsInChildren<Renderer>(true);
            var mats = RendererUtility.GetFilteredMaterials(renderers);
            var newMatPair = MaterialUtility.ReplaceTextureAll(mats, texDict);
            foreach (var r in renderers) { r.sharedMaterials = r.sharedMaterials.Select(m => m == null ? m : (newMatPair.TryGetValue(m, out var nm) ? nm : m)).ToArray(); }
            foreach (var mat in newMatPair.Values) { assetSaver.TransferAsset(mat); }
            AssetDatabase.Refresh();
        }


    }

    internal class ToPNGCompress : TextureCompress
    {
        string _outputDirectory;
        public ToPNGCompress(string outputDirectory)
        {
            _outputDirectory = outputDirectory;
        }
        public override void CompressDeferred() { }
        public Dictionary<Texture2D, Texture2D> CreateCompresses()
        {
            var swapTexture2D = new Dictionary<Texture2D, Texture2D>();

            foreach (var compressKV in _compressDict)
            {
                var sourceTex2D = compressKV.Key;
                if (sourceTex2D == null) { continue; }
                var path = AssetSaveHelper.SavePNG(_outputDirectory, sourceTex2D);
                AssetDatabase.ImportAsset(path);
                var importer = TextureImporter.GetAtPath(path) as TextureImporter;
                switch (compressKV.Value)
                {
                    default: { break; }
                    case TextureCompress.RefAtImporterFormat refAt:
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

            return swapTexture2D;
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
