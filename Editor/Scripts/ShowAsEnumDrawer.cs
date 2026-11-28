using System;
using UnityEditor;
using UnityEngine;

namespace BS.SDKEditor
{
    /// <summary>Draws a <see cref="ShowAsEnumAttribute"/> int field as a drop-down of its enum's names.</summary>
    [CustomPropertyDrawer(typeof(ShowAsEnumAttribute))]
    public class ShowAsEnumDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enumType = ((ShowAsEnumAttribute)attribute).enumType;
            if (property.propertyType != SerializedPropertyType.Integer || enumType == null || !enumType.IsEnum)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            var current = (Enum)Enum.ToObject(enumType, property.intValue);
            EditorGUI.BeginChangeCheck();
            var chosen = EditorGUI.EnumPopup(position, label, current);
            if (EditorGUI.EndChangeCheck())
                property.intValue = Convert.ToInt32(chosen);
            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }
    }
}
