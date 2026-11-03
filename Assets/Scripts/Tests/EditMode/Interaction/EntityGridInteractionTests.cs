using NUnit.Framework;
using UnityEngine;

namespace Interaction
{

public class EntityGridInteractionTests
{
    GameObject _model;
    EntityData _data;
    EntityGridInteraction _placement;

    [SetUp]
    public void SetUp()
    {
        _model = new GameObject("Placement model");
        _data = ScriptableObject.CreateInstance<EntityData>();
        _data.model = _model;
    }

    [TearDown]
    public void TearDown()
    {
        GameObject preview = _placement != null ? TestHelpers.GetPrivateField<GameObject>(_placement, "_entity") : null;
        if (preview != null)
        {
            Object.DestroyImmediate(preview);
        }
        Object.DestroyImmediate(_data);
        Object.DestroyImmediate(_model);
    }

    [Test]
    public void Preview_BeforeHoveringACell_IsHidden()
    {
        _placement = new EntityGridInteraction(_data);

        GameObject preview = TestHelpers.GetPrivateField<GameObject>(_placement, "_entity");
        Assert.IsNotNull(preview);
        Assert.IsFalse(preview.activeSelf, "The placed unit must not show where its model spawns, before the finger reaches the board.");
    }
}

}
