using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio
{

public class CreatureGrammarValidatorTests
{
    readonly List<Object> _objects = new List<Object>();
    CreatureGrammarPreset _preset;

    [SetUp]
    public void SetUp()
    {
        _preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
        _preset.vocabulary = RenderTestAssets.LoadLookVocabulary();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object trackedObject in _objects)
        {
            if (trackedObject != null)
            {
                Object.DestroyImmediate(trackedObject);
            }
        }
        _objects.Clear();
    }

    T Track<T>(T instance) where T : Object
    {
        _objects.Add(instance);
        return instance;
    }

    LookVocabulary CopyVocabulary()
    {
        return Track(Object.Instantiate(_preset.vocabulary));
    }

    [Test]
    public void Validate_ShippedVocabulary_ReturnsNoError()
    {
        string[] errors = CreatureGrammarValidator.Validate(_preset);

        Assert.IsEmpty(errors, string.Join("; ", errors));
    }

    [Test]
    public void Validate_NoVocabularyOrEntity_ReportsBoth()
    {
        _preset.deriveFromEntity = true;
        _preset.vocabulary = null;

        string text = string.Join(" ", CreatureGrammarValidator.Validate(_preset));

        StringAssert.Contains("EntityData", text);
        StringAssert.Contains("look vocabulary", text);
        Assert.IsNull(_preset.Compose());
    }

    [Test]
    public void Validate_EntityWithoutPrimarySkill_AsksForOne()
    {
        _preset.sourceEntity = Track(ScriptableObject.CreateInstance<EntityData>());
        _preset.deriveFromEntity = true;

        string[] errors = CreatureGrammarValidator.Validate(_preset);

        StringAssert.Contains("primary skill", string.Join(" ", errors));
    }

    [Test]
    public void Validate_MissingTables_ReportsTheMissingEntries()
    {
        LookVocabulary malformed = Track(ScriptableObject.CreateInstance<LookVocabulary>());
        malformed.bodies = null;
        malformed.heads = null;
        malformed.stems = null;
        malformed.roots = null;
        _preset.vocabulary = malformed;

        string text = string.Join(" ", CreatureGrammarValidator.Validate(_preset));

        StringAssert.Contains("mass/body entry", text);
        StringAssert.Contains("root table", text);
        Assert.IsNull(_preset.Compose());
    }

    [Test]
    public void Validate_SelectedBodyWithoutParts_StopsBeforeTheComposer()
    {
        LookVocabulary malformed = CopyVocabulary();
        LookVocabulary.BodyEntry shipped = _preset.vocabulary.bodies[_preset.mass];
        malformed.bodies = new Dictionary<MassBand, LookVocabulary.BodyEntry>(malformed.bodies);
        malformed.bodies[_preset.mass] = new LookVocabulary.BodyEntry { plant = null };
        _preset.vocabulary = malformed;

        string[] errors = CreatureGrammarValidator.Validate(_preset);

        StringAssert.Contains("body fragment has no parts", string.Join(" ", errors));
        Assert.IsNull(_preset.Compose());
        Assert.NotNull(shipped.plant);
    }

    [Test]
    public void Validate_UnknownHead_KeepsItAndRefuses()
    {
        _preset.head = (HeadKind)999;

        string[] errors = CreatureGrammarValidator.Validate(_preset);

        Assert.IsNotEmpty(errors);
        Assert.AreEqual((HeadKind)999, _preset.head);
    }

    [Test]
    public void Validate_PaletteColourNotFinite_ReportsIt()
    {
        LookVocabulary custom = CopyVocabulary();
        custom.palette = Track(Object.Instantiate(_preset.vocabulary.palette));
        custom.palette.stoneWilt = new Color(float.NaN, 0f, 0f, 1f);
        _preset.vocabulary = custom;
        _preset.side = LookSide.Stone;

        string[] errors = CreatureGrammarValidator.Validate(_preset);

        StringAssert.Contains("Palette colours", string.Join(" ", errors));
    }

    [Test]
    public void Validate_StoneWithBrokenPlantSettings_ReturnsNoError()
    {
        LookVocabulary custom = CopyVocabulary();
        LookVocabulary.StemEntry stem = custom.stems[_preset.stem];
        custom.roots = null;
        custom.pinnedReach = float.NaN;
        custom.plantScale = float.NaN;
        custom.stems = new Dictionary<StemBand, LookVocabulary.StemEntry>(custom.stems);
        custom.stems[_preset.stem] = new LookVocabulary.StemEntry
        {
            length = float.NaN,
            thickness = float.NaN,
            limbLength = stem.limbLength
        };
        _preset.vocabulary = custom;
        _preset.side = LookSide.Stone;

        string[] errors = CreatureGrammarValidator.Validate(_preset);

        Assert.IsEmpty(errors, string.Join("; ", errors)); // a stone never reads the plant's roots, reach or stem
        Assert.NotNull(Track(_preset.Compose()));
    }

    [Test]
    public void Validate_UnpinnedPlantWithUnusedBrokenBand_ReturnsNoError()
    {
        LookVocabulary custom = CopyVocabulary();
        custom.isReachPinned = false;
        custom.pinnedReach = float.NaN;
        custom.roots = new Dictionary<ReachBand, LookVocabulary.RootEntry>(custom.roots);
        custom.roots[ReachBand.Short] = null;
        custom.roots[ReachBand.Mid] = new LookVocabulary.RootEntry { reach = 0.8f };
        custom.roots[ReachBand.Long] = new LookVocabulary.RootEntry { reach = 1f };
        _preset.vocabulary = custom;
        _preset.reach = ReachBand.Long;

        string[] errors = CreatureGrammarValidator.Validate(_preset);

        Assert.IsEmpty(errors, string.Join("; ", errors));
        Assert.AreEqual(custom.Unit(LookSide.Plant), Track(_preset.Compose()).roots.footRadius);
    }

    [Test]
    public void Validate_PlantRootCountOutOfRange_ReportsIt()
    {
        LookVocabulary custom = CopyVocabulary();
        custom.rootCount = 20;
        _preset.vocabulary = custom;

        string[] errors = CreatureGrammarValidator.Validate(_preset);

        StringAssert.Contains("supported ranges", string.Join(" ", errors));
    }
}

}
