using System;
using System.Collections.Generic;
using UnityEngine;

namespace NKStudio.ShaderGraph.HotKey
{
    public class ShaderGraphHotKeySettings : ScriptableObject
    {
        public enum KStartUp
        {
            Always,
            Never,
        }

        public enum KLanguage
        {
            English,
            한국어
        }

        [field: SerializeField] public bool AutoShaderGraphOverride { get; set; }
        [field: SerializeField] public KLanguage Language { get; set; }
        [field: SerializeField] public KStartUp StartAtShow { get; set; }

        public static List<string> GetLanguageScript(KLanguage kLanguage)
        {
            switch (kLanguage)
            {
                case KLanguage.English:
                    return new List<string>()
                    {
                        "Always",
                        "Never",
                    };
                case KLanguage.한국어:
                    return new List<string>()
                    {
                        "항상",
                        "끄기",
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(kLanguage), kLanguage, null);
            } 
        }
    }
}