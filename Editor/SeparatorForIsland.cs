using System.Collections;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransCoreEngineForUnity;
using net.rs64.TexTransTool.Utils;
using UnityEngine.UIElements;
using System;
using net.rs64.TexTransTool.IslandSelector;
using Unity.Collections;
using net.rs64.TexTransTool.Decal;
using net.rs64.TexTransCore;
using net.rs64.TexTransTool.UVIsland;

namespace net.rs64.TexTransTool.DestructiveTextureUtilities
{
    internal class SeparatorForIsland : DestructiveUtility
    {

        [SerializeField] Renderer SeparateTarget;
        [SerializeField] AbstractIslandSelector IslandSelector;
        [SerializeField] float Padding = 5f;
        [SerializeField] bool HighQualityPadding = true;
        [SerializeField] TexTransTool.PropertyName TargetPropertyName = TexTransTool.PropertyName.DefaultValue;



        public override void CreateUtilityPanel(VisualElement rootElement)
        {
            rootElement.hierarchy.Add(new Label("SeparatorForIsland は現在未実装です。"));

            // var serializedObject = new SerializedObject(this);

            // rootElement.hierarchy.Add(new Label("テクスチャをアイランド単位で分割した個別のテクスチャーにします。"));

            // rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(SeparateTarget))));
            // rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(IslandSelector))));
            // rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(Padding))));
            // rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(HighQualityPadding))));
            // rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(TargetPropertyName))));

            // var button = new Button(Separate);
            // button.text = "Execute";
            // rootElement.hierarchy.Add(button);
        }



        void Separate()
        {
            if (SeparateTarget == null) { EditorUtility.DisplayDialog("SeparatorForIsland - 実行不可能", "SeparateTarget が存在しません！", "Ok"); return; }
            if (SeparateTarget.GetMesh() == null) { EditorUtility.DisplayDialog("SeparatorForIsland - 実行不可能", "SeparateTarget が SkiedMeshRenderer か MeshRenderer ではないか、Meshが割り当てられていません!", "Ok"); return; }
            try
            {
                EditorUtility.DisplayProgressBar("SeparatorForIsland", "Start", 0);
                var meshData = new MeshData(SeparateTarget);
                var outputDirectory = AssetSaveHelper.CreateUniqueNewFolder(SeparateTarget.name + "-IslandSeparateResult");

                for (var subMeshI = 0; meshData.TriangleIndex.Length > subMeshI; subMeshI += 1)
                {
                    EditorUtility.DisplayProgressBar("SeparatorForIsland", "SubMesh-" + subMeshI, subMeshI / (float)meshData.TriangleIndex.Length);
                    var progressStartAndEnd = (subMeshI / (float)meshData.TriangleIndex.Length, (subMeshI + 1) / (float)meshData.TriangleIndex.Length);
                    if (SeparateTarget.sharedMaterials.Length <= subMeshI) { continue; }
                    var material = SeparateTarget.sharedMaterials[subMeshI];
                    var texture2D = material.GetTexture(TargetPropertyName) as Texture2D;
                    if (texture2D == null) { continue; }
                    var fullTexture2D = texture2D.TryGetUnCompress();

                    var islands = UnityIslandUtility.UVtoIsland(meshData.TriangleIndex[subMeshI].AsSpan(), meshData.VertexUV.AsSpan()).ToArray();

                    BitArray selectBitArray;
                    if (IslandSelector != null)
                    {
                        var islandDescriptions = new IslandDescription[islands.Length];
                        Array.Fill(islandDescriptions, new IslandDescription(meshData.Vertices, meshData.VertexUV, SeparateTarget, SeparateTarget.sharedMaterials, subMeshI));
                        selectBitArray = IslandSelector.IslandSelect(new(islands, islandDescriptions, new NotWorkDomain(Array.Empty<Renderer>(), null)));
                    }
                    else { selectBitArray = new(islands.Length, true); }

                    for (var islandIndex = 0; islands.Length > islandIndex; islandIndex += 1)
                    {
                        EditorUtility.DisplayProgressBar("SeparatorForIsland", "SubMesh-" + subMeshI + "-" + islandIndex, Mathf.Lerp(progressStartAndEnd.Item1, progressStartAndEnd.Item2, (islandIndex + 1) / (float)islands.Length));
                        if (!selectBitArray[islandIndex]) { continue; }

                        var targetRt = RenderTexture.GetTemporary(fullTexture2D.width, fullTexture2D.height, 32);
                        targetRt.Clear();

                        using (var triNa = new NativeArray<TriangleIndex>(islands[islandIndex].Triangles.Count, Allocator.TempJob))
                        {
                            var writeSpan = triNa.AsSpan();
                            for (var i = 0; writeSpan.Length > i; i += 1) { writeSpan[i] = islands[islandIndex].Triangles[i]; }
                            // TransTexture.ForTrans(targetRt, fullTexture2D, new TransTexture.TransData(triNa, meshData.VertexUV, meshData.VertexUV), Padding, null, true);
                        }
                        var tex = targetRt.CopyTexture2D();
                        RenderTexture.ReleaseTemporary(targetRt);

                        tex.name = $"{subMeshI}-{islandIndex}";
                        AssetSaveHelper.SavePNG(outputDirectory, tex);
                        UnityEngine.Object.DestroyImmediate(tex);
                    }

                    if (fullTexture2D != texture2D) { UnityEngine.Object.DestroyImmediate(fullTexture2D); }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }
    }
}
