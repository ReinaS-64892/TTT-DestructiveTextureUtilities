using System.Collections;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransUnityCore;
using net.rs64.TexTransUnityCore.Utils;
using net.rs64.TexTransUnityCore.Island;
using net.rs64.TexTransTool.Utils;
using System.IO;
using UnityEngine.UIElements;
using System;
using net.rs64.TexTransTool.IslandSelector;
using net.rs64.TexTransUnityCore.Decal;
using Unity.Collections;

namespace net.rs64.DestructiveTextureUtilities
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
            var serializedObject = new SerializedObject(this);

            rootElement.hierarchy.Add(new Label("テクスチャをアイランド単位で分割した個別のテクスチャーにします。"));

            rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(SeparateTarget))));
            rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(IslandSelector))));
            rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(Padding))));
            rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(HighQualityPadding))));
            rootElement.hierarchy.Add(CreateVIProperyFiled(serializedObject.FindProperty(nameof(TargetPropertyName))));

            var button = new Button(Separate);
            button.text = "Execute";
            rootElement.hierarchy.Add(button);
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

                for (var subMeshI = 0; meshData.Triangles.Length > subMeshI; subMeshI += 1)
                {
                    EditorUtility.DisplayProgressBar("SeparatorForIsland", "SubMesh-" + subMeshI, subMeshI / (float)meshData.Triangles.Length);
                    var progressStartAndEnd = (subMeshI / (float)meshData.Triangles.Length, (subMeshI + 1) / (float)meshData.Triangles.Length);
                    if (SeparateTarget.sharedMaterials.Length <= subMeshI) { continue; }
                    var material = SeparateTarget.sharedMaterials[subMeshI];
                    var texture2D = material.GetTexture(TargetPropertyName) as Texture2D;
                    if (texture2D == null) { continue; }
                    var fullTexture2D = texture2D.TryGetUnCompress();

                    var islands = IslandUtility.UVtoIsland(meshData.TriangleIndex[subMeshI].AsList(), meshData.VertexUV.AsList()).ToArray();

                    BitArray selectBitArray;
                    if (IslandSelector != null)
                    {
                        var islandDescriptions = new IslandDescription[islands.Length];
                        Array.Fill(islandDescriptions, new IslandDescription(meshData.Vertices, meshData.VertexUV, SeparateTarget, subMeshI));
                        selectBitArray = IslandSelector.IslandSelect(islands, islandDescriptions);
                    }
                    else { selectBitArray = new(islands.Length, true); }

                    for (var islandIndex = 0; islands.Length > islandIndex; islandIndex += 1)
                    {
                        EditorUtility.DisplayProgressBar("SeparatorForIsland", "SubMesh-" + subMeshI + "-" + islandIndex, Mathf.Lerp(progressStartAndEnd.Item1, progressStartAndEnd.Item2, (islandIndex + 1) / (float)islands.Length));
                        if (!selectBitArray[islandIndex]) { continue; }

                        var targetRt = RenderTexture.GetTemporary(fullTexture2D.width, fullTexture2D.height, 32);
                        targetRt.Clear();

                        using (var triNa = new NativeArray<TriangleIndex>(islands[islandIndex].triangles.Count, Allocator.TempJob))
                        {
                            var writeSpan = triNa.AsSpan();
                            for (var i = 0; writeSpan.Length > i; i += 1) { writeSpan[i] = islands[islandIndex].triangles[i]; }
                            TransTexture.ForTrans(targetRt, fullTexture2D, new TransTexture.TransData<Vector2>(triNa, meshData.VertexUV, meshData.VertexUV), Padding, null, true);
                        }
                        var tex = targetRt.CopyTexture2D();
                        RenderTexture.ReleaseTemporary(targetRt);

                        tex.name = $"{subMeshI}-{islandIndex}";
                        AssetSaveHelper.SavePNG(outputDirectory,tex);
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
