using System;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class CreateUniqueIdAttribute : System.Attribute { }

public class CreateUniqueIdAttributeDrawer : OdinAttributeDrawer<CreateUniqueIdAttribute, string>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        if (string.IsNullOrEmpty(this.ValueEntry.SmartValue))
        {
            this.ValueEntry.SmartValue = Guid.NewGuid().ToString();
        }
        this.CallNextDrawer(label);
    }
}