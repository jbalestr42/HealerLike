using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public abstract class AStoneBodyTests
{
    class Consumer : AConsumer
    {
        readonly float _amount;

        public Consumer(float amount)
        {
            _amount = amount;
        }

        public override float GetValue()
        {
            return _amount;
        }

        public override bool ignoreDamageReduction { get { return true; } }

        public override bool ignoreConsumerPrevention { get { return false; } }
    }

    protected GameObject _owner;
    protected GameObject _source;
    protected GameObject _fxObject;
    protected ResourceAttribute _health;
    protected CreatureRecipe _recipe;
    protected Material _material;
    protected StoneBody _body;
    protected StoneEffects _fx;

    protected int visibleCount
    {
        get
        {
            int count = 0;
            foreach (Transform part in _body.parts)
            {
                if (part.gameObject.activeSelf)
                {
                    count++;
                }
            }
            return count;
        }
    }

    [SetUp]
    public void SetUp()
    {
        _owner = new GameObject("Stone");
        _source = new GameObject("Source");
        _health = TestHelpers.CreateResourceAttribute(_owner, AttributeType.HealthMax, 100);
        TestHelpers.CreateAttributeManager(_source);
        _fx = RenderTestAssets.CreateStoneEffects();
        _fxObject = _fx.gameObject;
        _recipe = RenderTestAssets.CreateStoneRecipe();
        _material = new Material(RenderTestAssets.LoadLookMaterial());
        Entity entity = RenderTestAssets.CreateStoneEntity(_owner, _health);
        _body = RenderTestAssets.CreateStoneBody(_owner, entity, _recipe, _material);
        _body.Init(_health, 15, _fx);
    }

    [TearDown]
    public void TearDown()
    {
        if (_body != null)
        {
            TestHelpers.InvokePrivate(_body, "OnDestroy");
            TestHelpers.InvokePrivate(_body.GetComponent<CreatureBuilder>(), "OnDestroy");
        }
        TestHelpers.InvokePrivate(_fx, "OnDestroy");
        Object.DestroyImmediate(_owner);
        Object.DestroyImmediate(_source);
        Object.DestroyImmediate(_fxObject);
        Object.DestroyImmediate(_recipe);
        Object.DestroyImmediate(_material);
    }

    protected ResourceModifier Queue(float delta)
    {
        ResourceModifier modifier = new ResourceModifier { source = _source };
        modifier.consumers.Add(new Consumer(delta));
        _health.AddResourceModifier(modifier);
        return modifier;
    }

    protected void Drain()
    {
        TestHelpers.InvokePrivate(_health, "Update");
        _body.CompleteHealthBatch();
    }

}

}
