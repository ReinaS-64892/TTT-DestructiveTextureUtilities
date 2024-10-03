using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransUnityCore.Utils;
using System.IO;
using UnityEngine.UIElements;
using net.rs64.TexTransTool;
using System.Linq;
using net.rs64.TexTransTool.Build;

namespace net.rs64.DestructiveTextureUtilities
{
    internal class StackExtractor : DestructiveUtility
    {
        public GameObject DomainRoot;
        public override void CreateUtilityPanel(VisualElement rootElement)
        {
            var serializedObject = new SerializedObject(this);

            rootElement.hierarchy.Add(new Label("アバターのスタックに関与するものをすべて抽出します。"));
            rootElement.hierarchy.Add(new Label("DomainRoot に抽出したい対象のルートを割り当ててください。"));

            rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(DomainRoot))));

            var button = new Button(Extract);
            button.text = "Execute";
            rootElement.hierarchy.Add(button);
        }


        void Extract()
        {
            if (DomainRoot == null) { EditorUtility.DisplayDialog("StackExtractor - 実行不可能", "DomainRoot が存在しません！", "Ok"); return; }
            var duplicate = Instantiate(DomainRoot);

            var renderers = duplicate.GetComponentsInChildren<Renderer>().ToList();
            var phaseDict = AvatarBuildUtils.FindAtPhase(duplicate);
            var domain = new StackExtractedDomain(renderers, false, false, false);
            domain.SaveTextureDirectory = AssetSaveHelper.CreateUniqueNewFolder(DomainRoot.name + "-StackExtractResult");
            var session = new StackTracedSession(domain, phaseDict);

            AvatarBuildUtils.ExecuteAllPhase(session);
            DestroyImmediate(duplicate);

            AssetDatabase.Refresh();

            var resultObject = ScriptableObject.CreateInstance<StackExtractResult>();
            resultObject.result = domain.StackTrace.Select(i => new StackExtractResult.Stack() { TargetTexture = i.Key, StackImages = i.Value.Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p)).ToList() }).ToList();
            AssetDatabase.CreateAsset(resultObject, Path.Combine(domain.SaveTextureDirectory, "StackExtractResult.asset"));
        }
    }

    internal class StackExtractedDomain : RenderersDomain
    {
        public string SaveTextureDirectory;
        public string SaveTextureName;
        public Dictionary<Texture, List<string>> StackTrace = new();
        public StackExtractedDomain(List<Renderer> renderers, bool previewing, bool saveAsset = false, bool? useCompress = null) : base(renderers, previewing, saveAsset, useCompress) { }
        public StackExtractedDomain(List<Renderer> renderers, bool previewing, IAssetSaver assetSaver, bool? useCompress = null) : base(renderers, previewing, assetSaver, useCompress) { }
        public StackExtractedDomain(List<Renderer> previewRenderers, bool previewing, ITextureManager textureManager, IAssetSaver assetSaver) : base(previewRenderers, previewing, textureManager, assetSaver) { }

        public override void AddTextureStack<BlendTex>(Texture dist, BlendTex setTex)
        {
            if (!StackTrace.ContainsKey(dist)) { StackTrace.Add(dist, new()); }
            var tmpStack = RenderTexture.GetTemporary(setTex.Texture.width, setTex.Texture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(setTex.Texture, tmpStack);

            var tex = tmpStack.CopyTexture2D();
            tex.name = $"{StackTrace[dist].Count}-{SaveTextureName}";
            var path = AssetSaveHelper.SavePNG(SaveTextureDirectory, tex);
            StackTrace[dist].Add(path);

            RenderTexture.ReleaseTemporary(tmpStack);
            UnityEngine.Object.DestroyImmediate(tex);



            base.AddTextureStack(dist, setTex);
        }

    }

    internal class StackTracedSession : TexTransBuildSession
    {
        public StackTracedSession(RenderersDomain renderersDomain, Dictionary<TexTransPhase, List<TexTransBehavior>> phaseAtList) : base(renderersDomain, phaseAtList)
        {
        }

        protected override void ApplyImpl(TexTransBehavior tf)
        {
            var stackExtractedDomain = _domain as StackExtractedDomain;
            stackExtractedDomain.SaveTextureName = tf.gameObject.name;
            base.ApplyImpl(tf);
        }
    }
}
