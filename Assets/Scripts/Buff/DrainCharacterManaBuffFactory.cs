using System;
using UnityEngine;
using Oisif.Inspector;

[CreateAssetMenu(menuName = "Custom/Data/Buff/DrainCharacterManaBuff")]
public class DrainCharacterManaBuffFactory : BuffFactory<DrainCharacterManaBuff, DrainCharacterManaBuffData> { }

[Serializable]
public class DrainCharacterManaBuffData
{
    // Negative to drain the mana
    [CreateDataButton]
    public AConsumerFactory consumerFactory;
}

// Applies its consumer to the mana of the player's character, whatever the target hit
public class DrainCharacterManaBuff : ABuff<DrainCharacterManaBuffData>
{
    public override void Instant(GameObject source, GameObject target)
    {
        ResourceAttribute mana = characterMana;
        if (mana == null)
        {
            return;
        }

        // The resolver reads the source attributes, a destroyed source can't be used
        GameObject consumerSource = source != null ? source : mana.gameObject;
        mana.AddResourceModifier(ResourceModifier.Create(data.consumerFactory, consumerSource, mana.gameObject));
    }

    public override void Add(GameObject source, GameObject target) { }

    public override void Remove(GameObject source, GameObject target) { }

    protected virtual ResourceAttribute characterMana
    {
        get
        {
            Character character = PlayerBehaviour.instance.character;
            return character != null ? character.mana : null;
        }
    }
}
