using System.Collections.Generic;
using UnityEngine;

namespace ShaderGraphShotKey.Editor.Settings
{
    public class ShaderGraphHotKeySettings : ScriptableObject
    {
        public readonly Dictionary<KLanguage, List<string>> StartAtShowText = new()
        {
            {
                KLanguage.English, new List<string>()
                {
                    "Always",
                    "Never",
                }
            },
            {
                KLanguage.한국어, new List<string>()
                {
                    "항상",
                    "끄기",
                }
            }
        };

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

        [field: SerializeField]
        public bool AutoShaderGraphOverride { get; set; }
        [field: SerializeField]
        public KLanguage Language { get; set; }
        [field: SerializeField]
        public KStartUp StartAtShow { get; set; }
    }
}