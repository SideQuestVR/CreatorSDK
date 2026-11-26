using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using BS;

namespace BS.SDKEditor
{
    // editorForChildClasses so the deprecated Banter-prefixed subclasses get this inspector too.
    [CustomEditor(typeof(BSVerifyGroup), true)]
    public class BSVerifyGroupEditor : Editor
    {
        void OnEnable()
        {
            if (target is BSVerifyGroup)
            {
                var script = (BSVerifyGroup)target;
                var path = AssetDatabase.GetAssetPath(script);
            }
        }
        public override bool UseDefaultMargins() => false;
        public override VisualElement CreateInspectorGUI()
        {
            var script = (BSVerifyGroup)target;
            Editor editor = Editor.CreateEditor(script);

            return BSComponentInspector.Build(
                editor,
                new string[] { "groupId", "outcome", "teleportPosition", "worldUrl", "errorMessage", "isMember", "checkState" },
                new string[] { });
        }
    }
}
