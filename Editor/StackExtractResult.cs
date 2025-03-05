using System.Collections.Generic;
using UnityEngine;
using System;

namespace net.rs64.TexTransTool.DestructiveTextureUtilities
{
    internal class StackExtractResult : ScriptableObject
    {
        public List<Stack> result;

        [Serializable]
        internal class Stack
        {
            public Texture TargetTexture;
            public List<Texture2D> StackImages;
        }
    }
}
