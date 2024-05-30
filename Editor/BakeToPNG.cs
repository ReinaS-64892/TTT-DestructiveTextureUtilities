
using System.Collections.Generic;
using System.IO;
using System.Linq;
using net.rs64.TexTransCore.Utils;
using net.rs64.TexTransTool;
using net.rs64.TexTransTool.Build;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace net.rs64.DestructiveTextureUtilities
{
    internal class BakeToPNG : DestructiveUtility
    {
        public GameObject DomainRoot;
        public override void CreateUtilityPanel(VisualElement rootElement)
        {
            var serializedObject = new SerializedObject(this);

            rootElement.hierarchy.Add(new Label("アバターなどをマニュアルベイクし、編集したテクスチャーをPNGに書き出します。"));
            rootElement.hierarchy.Add(new Label("DomainRoot にベイクしたい対象を割り当ててください。"));

            rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(DomainRoot))));

            var button = new Button(Execute);
            button.text = "Execute";
            rootElement.hierarchy.Add(button);
        }

        void Execute()
        {
            if (DomainRoot == null) { EditorUtility.DisplayDialog("DomainRoot - 実行不可能", "DomainRoot が存在しません！", "Ok"); return; }

            var outputDirectory = AssetSaveHelper.CreateUniqueNewFolder(DomainRoot.name + "-BakeToPNGResult");
            var duplicate = Instantiate(DomainRoot);
            duplicate.transform.position = new Vector3(duplicate.transform.position.x, duplicate.transform.position.y, duplicate.transform.position.z + 2);

            var phaseDict = AvatarBuildUtils.FindAtPhase(duplicate);
            var assetSaver = new AssetSaver(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(outputDirectory, "OtherAssetsContainer.asset")));

            var deferredDestroyer = new DeferredDestroyer();
            var compressionManager = new ToPNGCompress(outputDirectory);
            var textureManager = new TextureManager(deferredDestroyer, new GetOriginTexture(false, deferredDestroyer.DeferredDestroyOf), compressionManager);

            var domain = new AvatarDomain(duplicate, false, textureManager, assetSaver);
            var session = new TexTransBuildSession(domain, phaseDict);

            AvatarBuildUtils.ExecuteAllPhase(session);
            AvatarBuildUtils.DestroyITexTransToolTags(duplicate);
            var texDict = compressionManager.CreateCompresses();

            var renderers = duplicate.GetComponentsInChildren<Renderer>(true);
            var mats = RendererUtility.GetFilteredMaterials(renderers);
            var newMatPair = MaterialUtility.ReplaceTextureAll(mats, texDict);
            foreach (var r in renderers) { r.sharedMaterials = r.sharedMaterials.Select(m => m == null ? m : (newMatPair.TryGetValue(m, out var nm) ? nm : m)).ToArray(); }
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
                var path = AssetSaveHelper.SavePNG(_outputDirectory, sourceTex2D);
                AssetDatabase.ImportAsset(path);
                var importer = TextureImporter.GetAtPath(path) as TextureImporter;
                importer.compressionQuality = compressKV.Value.Quality;
                // switch (compressKV.Value.CompressFormat) //未実装なう！！！
                // {
                //     default:
                // }

                importer.SaveAndReimport();
                swapTexture2D.Add(sourceTex2D, AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            }

            return swapTexture2D;
        }
    }


}
