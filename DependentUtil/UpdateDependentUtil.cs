using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace net.rs64.DestructiveTextureUtilities
{
    static class UpdateDependentUtil
    {
        const string TTT_DTU_PACKAGE_DOT_JSON_PATH = "Packages/TTT-DestructiveTextureUtilities/package.json";
        const string TTT_DTU_ASMDEF = "Packages/TTT-DestructiveTextureUtilities/Editor/net.rs64.ttt-destructive-texture-utilities.editor.asmdef";
        const string TEX_TRANS_TOOL_PACKAGE_DOT_JSON_PATH = "Packages/TexTransTool/package.json";

        [InitializeOnLoadMethod]
        static void UpdateNow()
        {
            string tttVersion = GetTTTVersion();
            writeTTTVersion(tttVersion);
        }

        private static string GetTTTVersion()
        {
            var ttt = File.ReadAllText(TEX_TRANS_TOOL_PACKAGE_DOT_JSON_PATH).Split("\n");
            var vstr = "\"version\":";
            var tttVersionLine = ttt.First(str => str.Contains(vstr));
            var tttVersion = GetString(tttVersionLine);
            return tttVersion;
        }

        private static void writeTTTVersion(string tttVersion)
        {
            var duAsmdef = File.ReadAllText(TTT_DTU_ASMDEF).Split("\n");
            var du = File.ReadAllText(TTT_DTU_PACKAGE_DOT_JSON_PATH).Split("\n");
            foreach (var i in FindIndexAll(du, str => str.Contains("\"net.rs64.tex-trans-tool\":")))
            {
                du[i] = du[i].Replace(GetString(du[i]), tttVersion);
            }
            foreach (var i in FindIndexAll(duAsmdef, str => str.Contains("\"expression\":")))
            {
                duAsmdef[i] = duAsmdef[i].Replace(GetString(duAsmdef[i]), $"[{tttVersion}]");
            }
            File.WriteAllText(TTT_DTU_PACKAGE_DOT_JSON_PATH, string.Join("\n", du));
            File.WriteAllText(TTT_DTU_ASMDEF, string.Join("\n", duAsmdef));
        }

        private static string GetString(string tttVersionLine)
        {
            var spIndex = tttVersionLine.IndexOf(":");
            var stringStart = tttVersionLine.IndexOf("\"", spIndex + 1);
            var stringEnd = tttVersionLine.LastIndexOf("\"");

            var stringIndex = stringStart + 1;
            var stringLength = stringEnd - stringIndex;

            return tttVersionLine.Substring(stringIndex, stringLength);
        }
        private static IEnumerable<int> FindIndexAll<T>(T[] array, Predicate<T> predicate)
        {
            for (var i = 0; i < array.Length; i += 1)
            {
                if (predicate.Invoke(array[i])) yield return i;
            }
        }
    }
}
