using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using BS;

namespace BS.SDKEditor
{
    // editorForChildClasses so the deprecated Banter-prefixed subclasses get this inspector too.
    [CustomEditor(typeof(BSPhotoWall), true)]
    public class BSPhotoWallEditor : Editor
    {
        void OnEnable()
        {
            if (target is BSPhotoWall)
            {
                var script = (BSPhotoWall)target;
                var path = AssetDatabase.GetAssetPath(script);
            }
        }
        public override bool UseDefaultMargins() => false;
        public override VisualElement CreateInspectorGUI()
        {
            var script = (BSPhotoWall)target;
            Editor editor = Editor.CreateEditor(script);

            return BSComponentInspector.Build(
                editor,
                new string[] { "size", "photoSize", "maxPhotos", "replaceOldest", "adminsOnly" },
                new string[] { });
        }
    }
}
