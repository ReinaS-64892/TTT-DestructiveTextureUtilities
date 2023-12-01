using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using net.rs64.TexTransCore;
using net.rs64.TexTransCore.TransTextureCore.Utils;
using net.rs64.TexTransCore.Island;
using net.rs64.TexTransTool.EditorIsland;
using net.rs64.TexTransCore.TransTextureCore;
using net.rs64.TexTransTool.Utils;
using System.IO;

namespace net.rs64.DestructiveTextureUtilities
{
    public class SeparatorForIsland : EditorWindow
    {
        [MenuItem("Tools/TexTransTool/DestructiveTextureUtilities/SeparatorForIsland")]
        static void ShowWindow()
        {
            var window = GetWindow<SeparatorForIsland>();
            window.titleContent = new GUIContent(nameof(SeparatorForIsland));
        }

        [SerializeField] Texture2D _separateTarget;
        [SerializeField] Mesh _separateReferenceMesh;
        [SerializeField] float _padding = 2.5f;
        [SerializeField] bool _HighQualityPadding = true;
        [SerializeField] string _outputDirectory;

        private void OnGUI()
        {
            _separateTarget = EditorGUILayout.ObjectField("SeparateTarget", _separateTarget, typeof(Texture2D), true) as Texture2D;
            _separateReferenceMesh = EditorGUILayout.ObjectField("SeparateReferenceMesh", _separateReferenceMesh, typeof(Mesh), true) as Mesh;
            _padding = EditorGUILayout.FloatField("Padding", _padding);
            _HighQualityPadding = EditorGUILayout.Toggle("HighQualityPadding", _HighQualityPadding);
            EditorGUILayout.LabelField(_outputDirectory);
            if (GUILayout.Button("OutputDirectory Select"))
            {
                _outputDirectory = EditorUtility.OpenFolderPanel("OutputDirectory", "Assets", "Assets");
            }

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(_separateTarget == null || _separateReferenceMesh == null || _outputDirectory == null);
            if (GUILayout.Button("SeparatorForIsland !"))
            {
                Separate();
            }
            EditorGUI.EndDisabledGroup();

        }


        void Separate()
        {
            var separateTarget = _separateTarget.TryGetUnCompress();
            var uv = new List<Vector2>();
            _separateReferenceMesh.GetUVs(0, uv);

            var count = 0;

            foreach (var subTri in _separateReferenceMesh.GetSubTriangleIndex())
            {
                var islands = IslandUtility.UVtoIsland(subTri, uv, new EditorIslandCache());

                foreach (var island in islands)
                {
                    count += 1;

                    var targetRt = RenderTexture.GetTemporary(separateTarget.width, separateTarget.height, 32);
                    targetRt.Clear();

                    TransTexture.ForTrans(targetRt, separateTarget, new TransTexture.TransData<Vector2>(island.triangles, uv, uv), _padding, null, true);

                    var tex = targetRt.CopyTexture2D();
                    RenderTexture.ReleaseTemporary(targetRt);

                    File.WriteAllBytes(Path.Combine(_outputDirectory, $"{_separateTarget.name}-{count}.png"), tex.EncodeToPNG());
                }
            }


        }
    }
}