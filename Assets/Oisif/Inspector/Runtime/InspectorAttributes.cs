using System;
using UnityEngine;

namespace Oisif.Inspector
{
    // The referenced object is drawn inside the inspector, under its field, with a foldout. On a class, every field
    // referencing an object of the class (or of a derived class) does it
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, Inherited = true)]
    public class InlineEditorAttribute : Attribute {}

    // Left out when the object is drawn inline in the inspector of another object
    [AttributeUsage(AttributeTargets.Field)]
    public class HideInInlineEditorsAttribute : Attribute {}

    // On a field referencing an asset, or a list of them: buttons to create a new asset of the field type or of
    // a type derived from it, saved next to the asset being edited, and to delete it
    [AttributeUsage(AttributeTargets.Field)]
    public class CreateDataButtonAttribute : Attribute {}

    // Shown but not editable
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReadOnlyAttribute : Attribute {}

    // Shown only when the member (field, property or method without parameter) of the same object is true, not null
    // and not 0, or equal to value when given
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ShowIfAttribute : Attribute
    {
        public readonly string member;
        public readonly object value;
        public readonly bool hasValue;

        public ShowIfAttribute(string member)
        {
            this.member = member;
        }

        public ShowIfAttribute(string member, object value)
        {
            this.member = member;
            this.value = value;
            hasValue = true;
        }
    }

    // Hidden when the member of the same object is true, not null and not 0, or equal to value when given
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class HideIfAttribute : ShowIfAttribute
    {
        public HideIfAttribute(string member) : base(member) {}
        public HideIfAttribute(string member, object value) : base(member, value) {}
    }

    // A box above the field showing the text given by the member (field, property or method without parameter)
    // of the same object, nothing when the text is empty
    [AttributeUsage(AttributeTargets.Field)]
    public class InfoBoxAttribute : Attribute
    {
        public readonly string member;

        public InfoBoxAttribute(string member)
        {
            this.member = member;
        }
    }

    // A button at the bottom of the inspector calling the method, which takes no parameter
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : Attribute
    {
        public readonly string label;
        // Only usable in play mode (e.g. a method spawning units)
        public bool playModeOnly;

        public ButtonAttribute(string label = null)
        {
            this.label = label;
        }
    }

    // A field or property the inspector shows, read only, even when it isn't serialized (e.g. a state)
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ShowInInspectorAttribute : Attribute {}

    // A picture of the referenced object under its field (a model, a sprite...), size pixels high
    [AttributeUsage(AttributeTargets.Field)]
    public class PreviewAttribute : Attribute
    {
        public readonly float size;

        public PreviewAttribute(float size = 64f)
        {
            this.size = size;
        }
    }

    // A list of object references drawn as a grid of the width and height fields of the same object, the cells
    // column after column (index x * height + y), the row 0 at the bottom
    [AttributeUsage(AttributeTargets.Field)]
    public class GridAttribute : Attribute
    {
        public readonly string widthField;
        public readonly string heightField;
        // Written above the first column (e.g. "Front")
        public string firstColumnLabel = "";
        public float cellSize = 72f;

        public GridAttribute(string widthField, string heightField)
        {
            this.widthField = widthField;
            this.heightField = heightField;
        }
    }

    // An object telling the inspector how to show it in a picture (e.g. the model of a unit) and under which name
    public interface IInspectorPreview
    {
        UnityEngine.Object previewObject { get; }
        string previewLabel { get; }
    }
}
