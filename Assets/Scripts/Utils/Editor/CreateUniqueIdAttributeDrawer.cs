using System;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

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
