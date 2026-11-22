using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using BS;

namespace BS.SDKEditor
{
    // editorForChildClasses so the deprecated Banter-prefixed subclasses get this inspector too.
    [CustomEditor(typeof(BSGaussianSplat), true)]
    public class BSGaussianSplatEditor : Editor
    {
        void OnEnable()
        {
            if (target is BSGaussianSplat)
            {
                var script = (BSGaussianSplat)target;
                var path = AssetDatabase.GetAssetPath(script);
            }
        }
        public override bool UseDefaultMargins() => false;
        public override VisualElement CreateInspectorGUI()
        {
            var script = (BSGaussianSplat)target;
            Editor editor = Editor.CreateEditor(script);

            return BSComponentInspector.Build(
                editor,
                new string[] { "url", "maxSplats", "resolution", "autoQuality", "occludeByWorld", "shDegree", "loadState", "loadError", "splatCount" },
                new string[] { });
        }
    }
}