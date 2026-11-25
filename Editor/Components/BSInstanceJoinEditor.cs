using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using BS;

namespace BS.SDKEditor
{
    // editorForChildClasses so the deprecated Banter-prefixed subclasses get this inspector too.
    [CustomEditor(typeof(BSInstanceJoin), true)]
    public class BSInstanceJoinEditor : Editor
    {
        void OnEnable()
        {
            if (target is BSInstanceJoin)
            {
                var script = (BSInstanceJoin)target;
                var path = AssetDatabase.GetAssetPath(script);
            }
        }
        public override bool UseDefaultMargins() => false;
        public override VisualElement CreateInspectorGUI()
        {
            var script = (BSInstanceJoin)target;
            Editor editor = Editor.CreateEditor(script);

            return BSComponentInspector.Build(
                editor,
                new string[] { "access", "instanceKey", "joinCode" },
                new string[] { });
        }
    }
}
