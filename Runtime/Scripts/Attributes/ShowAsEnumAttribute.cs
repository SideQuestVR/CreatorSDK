using System;
using UnityEngine;

namespace BS
{
    /// <summary>
    /// Draws an int field as a drop-down of an enum's names in the Inspector, for a setting that crosses to JS as a
    /// number. Inspector only: the field is still an int, and JS still sees the number.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowAsEnumAttribute : PropertyAttribute
    {
        public readonly Type enumType;

        public ShowAsEnumAttribute(Type enumType)
        {
            this.enumType = enumType;
        }
    }
}
