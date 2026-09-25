using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HealerLike.Render.Grammar;
using HealerLike.Render.Studio;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{
    public class ProceduralCompositionTests
    {
        readonly List<Object> _objects = new List<Object>();
        LookVocabulary _vocabulary;

        [SetUp]
        public void SetUp()
        {
            _vocabulary = Track(ScriptableObject.CreateInstance<LookVocabulary>());
            _vocabulary.palette = RenderTestAssets.LoadLookVocabulary().palette;
            _vocabulary.bodyUnit = 0.5f;
            _vocabulary.plantScale = 1f;
            _vocabulary.stoneScale = 1f;
            _vocabulary.maxParts = CreatureValidator.MaxParts;
            foreach (MassBand band in Enum.GetValues(typeof(MassBand)))
            {
                _vocabulary.bodies[band] = new LookVocabulary.BodyEntry
                {
                    plant = new[] { Part("Body", PartRole.Body, ShapeProfile.Bulb()) },
                    stone = new[] { Part("Body", PartRole.Body, ShapeProfile.Block()) }
                };
            }
            foreach (StemBand band in Enum.GetValues(typeof(StemBand)))
            {
                _vocabulary.stems[band] = new LookVocabulary.StemEntry
                {
                    length = 1f, thickness = 0.12f, limbLength = 0.3f,
                    plantShape = ShapeProfile.Segment(), stoneLimbShape = ShapeProfile.Block()
                };
            }
            foreach (ReachBand band in Enum.GetValues(typeof(ReachBand)))
            {
                _vocabulary.roots[band] = new LookVocabulary.RootEntry
                {
                    reach = 0.8f + 0.3f * (int)band,
                    segmentShape = ShapeProfile.Segment(0.2f + 0.1f * (int)band),
                    jointShape = ShapeProfile.Bulb(), taper = 0.5f, jointScale = 2.3f
                };
            }
            _vocabulary.heads[HeadKind.Bud] = new LookVocabulary.HeadEntry
            {
                plant = new[] { Part("Tip", PartRole.Tip, ShapeProfile.Bulb()) },
                stone = new[] { Part("Tip", PartRole.Tip, ShapeProfile.Block()) }
            };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object value in _objects)
            {
                if (value != null) Object.DestroyImmediate(value);
            }
            _objects.Clear();
        }

        T Track<T>(T value) where T : Object { _objects.Add(value); return value; }

        static LookPart Part(string id, PartRole role, ShapeProfile shape)
        {
            return new LookPart
            {
                id = id, role = role, shape = shape,
                primitive = shape.kind == ShapeKind.Block ? Primitive.Stone : Primitive.Sphere,
                colour = role == PartRole.Tip ? ColourRole.Accent : ColourRole.Body,
                size = Vector3.one * 0.7f
            };
        }

        CreatureRecipe Compose(LookSide side, CountBand count = CountBand.One,
            StemBand stem = StemBand.Steady, ReachBand reach = ReachBand.Short)
        {
            UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud, count, stem);
            channels.reach = reach;
            return Track(LookComposer.Compose(channels, _vocabulary));
        }

        [Test]
        public void Add_DefaultProfile_PreservesTheBakedStonePivotAndSpan()
        {
            Vector3 centre = new Vector3(1f, 2f, 3f);
            Vector3 size = new Vector3(2f, 3f, 4f);
            Vector3 euler = new Vector3(20f, 30f, 40f);
            PrimitiveMeshes.Fit(Primitive.Stone, centre, size, Quaternion.Euler(euler),
                out Vector3 dimensions, out Vector3 pivot);
            PartList parts = new PartList(1f);

            parts.Add("Legacy", Primitive.Stone, centre, size, Color.white, euler, 0f, PartRole.Body);

            Assert.AreEqual(dimensions, parts.ToArray()[0].dimensions);
            Assert.AreEqual(pivot, parts.ToArray()[0].localPosition);
            Assert.IsFalse(parts.ToArray()[0].shape.isProcedural);
        }

        [Test]
        public void Add_ProceduralBlock_UsesItsAuthoredUnitBoxAndKeepsTheProfile()
        {
            ShapeProfile profile = ShapeProfile.Block(0.3f, 0.2f, 0.1f);
            Vector3 centre = new Vector3(1f, 2f, 3f);
            Vector3 size = new Vector3(2f, 3f, 4f);
            PartList parts = new PartList(0.5f);

            parts.Add("Block", Primitive.Stone, centre, size, Color.white, Vector3.zero, 0f,
                PartRole.Body, shape: profile);

            CreaturePart part = parts.ToArray()[0];
            Assert.AreEqual(size * 0.5f, part.dimensions);
            Assert.AreEqual(centre * 0.5f, part.localPosition);
            Assert.AreEqual(profile, part.shape);
            Assert.AreEqual(profile, parts.Source(0).shape);
        }

        [TestCase(LookSide.Plant, CountBand.One, 1)]
        [TestCase(LookSide.Plant, CountBand.Few, 3)]
        [TestCase(LookSide.Plant, CountBand.Many, 5)]
        [TestCase(LookSide.Stone, CountBand.One, 1)]
        [TestCase(LookSide.Stone, CountBand.Few, 3)]
        [TestCase(LookSide.Stone, CountBand.Many, 5)]
        public void Compose_ProceduralBands_PreserveCountAndSideAnatomy(LookSide side, CountBand count, int tips)
        {
            CreatureRecipe recipe = Compose(side, count);

            Assert.NotNull(recipe);
            Assert.AreEqual(tips, Array.FindAll(recipe.parts, p => p.role == PartRole.Tip).Length);
            Assert.AreEqual(side == LookSide.Plant ? 0 : 2,
                Array.FindAll(recipe.parts, p => p.role == PartRole.Limb).Length);
            Assert.AreEqual(side == LookSide.Plant ? _vocabulary.rootCount : 0, recipe.roots.count);
            Assert.That(Array.TrueForAll(recipe.parts, p => p.shape.isProcedural));
        }

        [TestCase(LookSide.Plant)]
        [TestCase(LookSide.Stone)]
        public void Compose_ZeroScaleOverrides_PreserveLegacyMassScaling(LookSide side)
        {
            LookVocabulary.BodyEntry body = _vocabulary.bodies[MassBand.Light];
            body.scale = 1.6f;
            UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud);
            UnitSockets sockets = UnitSockets.Place(channels, _vocabulary);
            CreatureRecipe recipe = Compose(side);
            CreaturePart tip = Array.Find(recipe.parts, p => p.role == PartRole.Tip);

            Assert.AreEqual(0f, body.headScale);
            Assert.AreEqual(0f, body.stemScale);
            Assert.AreEqual(1.6f, sockets.headScale, 0.00001f);
            Assert.AreEqual(side == LookSide.Plant ? 1f : 1.6f, sockets.stemScale, 0.00001f);
            Assert.AreEqual(0.7f * 1.6f * _vocabulary.Unit(side), tip.dimensions.x, 0.00001f);
            if (side == LookSide.Plant)
            {
                Assert.AreEqual(_vocabulary.stems[channels.stem].length,
                    Vector3.Distance(sockets.stemFoot, sockets.neck), 0.00001f);
            }
        }

        [TestCase(LookSide.Plant)]
        [TestCase(LookSide.Stone)]
        public void Compose_ExplicitHeadScale_ChangesBothHeadsWithoutChangingBodyOrLegWidth(LookSide side)
        {
            AddMiniHead();
            _vocabulary.Layout.extendAccessorySupports = false;
            // This test isolates scale from support extension. Keep its authored branch outside even the legs.
            LookVocabulary.AccessoryEntry mini = _vocabulary.accessories[AccessoryKind.MiniHead];
            mini.miniHeadAt = Vector3.right * 1.6f;
            foreach (LookPart[] fragment in new[] { mini.plant, mini.stone })
            {
                fragment[0].position = Vector3.right * 0.8f;
                fragment[0].size = new Vector3(1.6f, 0.12f, 0.12f);
            }
            UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud,
                accessory: AccessoryKind.MiniHead);
            channels.accessoryHead = HeadKind.Bud;
            Assert.GreaterOrEqual(LookMeasure.AccessoryReach(channels, _vocabulary),
                LookComposer.AccessoryClearance(side, _vocabulary));
            CreatureRecipe before = Track(LookComposer.Compose(channels, _vocabulary));
            _vocabulary.bodies[MassBand.Light].headScale = 0.5f;
            Assert.GreaterOrEqual(LookMeasure.AccessoryReach(channels, _vocabulary),
                LookComposer.AccessoryClearance(side, _vocabulary));
            CreatureRecipe after = Track(LookComposer.Compose(channels, _vocabulary));
            CreaturePart[] beforeTips = Array.FindAll(before.parts, p => p.role == PartRole.Tip);
            CreaturePart[] afterTips = Array.FindAll(after.parts, p => p.role == PartRole.Tip);

            Assert.AreEqual(2, afterTips.Length);
            for (int i = 0; i < afterTips.Length; i++)
            {
                Assert.AreEqual(beforeTips[i].dimensions * 0.5f, afterTips[i].dimensions);
            }
            for (int i = 0; i < before.parts.Length; i++)
            {
                if (before.parts[i].role == PartRole.Tip) continue;
                Assert.AreEqual(before.parts[i].dimensions, after.parts[i].dimensions, before.parts[i].id);
                Assert.AreEqual(before.parts[i].localPosition, after.parts[i].localPosition, before.parts[i].id);
            }
            Assert.AreEqual(before.neckLocal, after.neckLocal);
        }

        [TestCase(LookSide.Plant)]
        [TestCase(LookSide.Stone)]
        public void Compose_ExplicitStemScale_ChangesCadenceLengthWithoutChangingHeadOrBodySize(LookSide side)
        {
            _vocabulary.bodies[MassBand.Light].scale = 1.6f;
            CreatureRecipe before = Compose(side);
            _vocabulary.bodies[MassBand.Light].stemScale = 0.45f;
            CreatureRecipe after = Compose(side);
            PartRole supportRole = side == LookSide.Plant ? PartRole.Stem : PartRole.Limb;
            CreaturePart supportBefore = Array.Find(before.parts, p => p.role == supportRole);
            CreaturePart supportAfter = Array.Find(after.parts, p => p.role == supportRole);

            Assert.Less(supportAfter.dimensions.y, supportBefore.dimensions.y);
            Assert.AreEqual(supportBefore.dimensions.x, supportAfter.dimensions.x);
            Assert.AreEqual(supportBefore.dimensions.z, supportAfter.dimensions.z);
            Assert.AreEqual(before.parts[0].dimensions, after.parts[0].dimensions);
            Assert.AreEqual(Array.Find(before.parts, p => p.role == PartRole.Tip).dimensions,
                Array.Find(after.parts, p => p.role == PartRole.Tip).dimensions);
            UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud);
            Assert.AreEqual(0.45f, UnitSockets.Place(channels, _vocabulary).stemScale, 0.00001f);
        }

        [TestCase(true, -0.1f)]
        [TestCase(false, -0.1f)]
        [TestCase(true, float.NaN)]
        [TestCase(false, float.PositiveInfinity)]
        public void Compose_InvalidScaleOverride_RejectsTheEdit(bool head, float value)
        {
            if (head) _vocabulary.bodies[MassBand.Light].headScale = value;
            else _vocabulary.bodies[MassBand.Light].stemScale = value;
            LogAssert.Expect(LogType.Error, "[LookComposer] Invalid layout, shape profile or selected band dimensions.");

            Assert.IsNull(Compose(LookSide.Plant));
        }

        [Test]
        public void Compose_StemBandProfileEdit_ChangesOnlyItsSelectedBand()
        {
            ShapeProfile quick = ShapeProfile.Segment(0.7f, 0.4f, 0.2f);
            _vocabulary.stems[StemBand.Quick].plantShape = quick;

            CreatureRecipe selected = Compose(LookSide.Plant, stem: StemBand.Quick);
            CreatureRecipe other = Compose(LookSide.Plant, stem: StemBand.Steady);

            Assert.AreEqual(quick, Array.Find(selected.parts, p => p.id == "Stem").shape);
            Assert.AreEqual(ShapeProfile.Segment(), Array.Find(other.parts, p => p.id == "Stem").shape);
        }

        [Test]
        public void Roots_LegacyEntryMissingNewFields_KeepsItsOriginalThickness()
        {
            _vocabulary.roots[ReachBand.Short] = new LookVocabulary.RootEntry
            {
                reach = 0.8f, thicknessScale = 0f, taper = 0f, jointScale = 0f
            };

            RootDefinition roots = LookComposer.Roots(ReachBand.Short, _vocabulary);

            Assert.AreEqual(_vocabulary.rootThickness * 0.5f * _vocabulary.Unit(LookSide.Plant), roots.thickness);
            Assert.IsFalse(roots.segmentShape.isProcedural);
            Assert.IsFalse(roots.jointShape.isProcedural);
        }

        [TestCase(ReachBand.Short)]
        [TestCase(ReachBand.Mid)]
        [TestCase(ReachBand.Long)]
        public void Compose_ReachBand_TransmitsItsRootProfilesAndProportions(ReachBand reach)
        {
            _vocabulary.roots[reach].thicknessScale = 1.4f;
            LookVocabulary.RootEntry source = _vocabulary.roots[reach];

            RootDefinition actual = Compose(LookSide.Plant, reach: reach).roots;

            Assert.AreEqual(source.segmentShape, actual.segmentShape);
            Assert.AreEqual(source.jointShape, actual.jointShape);
            Assert.AreEqual(source.taper, actual.taper);
            Assert.AreEqual(source.jointScale, actual.jointScale);
            Assert.AreEqual(source.reach * _vocabulary.Unit(LookSide.Plant), actual.footRadius, 0.00001f);
            Assert.AreEqual(_vocabulary.rootThickness * 0.5f * 1.4f * _vocabulary.Unit(LookSide.Plant),
                actual.thickness, 0.00001f);
        }

        [TestCase(CountBand.Few)]
        [TestCase(CountBand.Many)]
        public void Layout_StoneFan_KeepsBroadHeadCopiesApart(CountBand count)
        {
            _vocabulary.heads[HeadKind.Bud].stone[0].size = new Vector3(2.3f, 1f, 0.5f);
            UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Bud, count);

            Assert.Greater(LookMeasure.HeadGap(channels, _vocabulary), 0f);
        }

        [TestCase(CountBand.Few)]
        [TestCase(CountBand.Many)]
        public void Layout_PlantFan_KeepsItsOuterNeighboursApartForWideHeads(CountBand count)
        {
            _vocabulary.heads[HeadKind.Bud].plant[0].size = new Vector3(2.3f, 1f, 0.5f);
            UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, count);

            Assert.Greater(LookMeasure.HeadGap(channels, _vocabulary), 0f);
        }

        [TestCase(CountBand.Few, 2)]
        [TestCase(CountBand.Many, 4)]
        public void Layout_StoneFan_ConnectsOuterCopiesWithMineralSlabs(CountBand count, int supports)
        {
            UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Stone, HeadKind.Bud, count);
            PartList parts = LookComposer.Layout(channels, _vocabulary);
            Vector3 neck = UnitSockets.Place(channels, _vocabulary).neck;
            LookPart body = parts.Source(0);
            Bounds bodyBounds = new Bounds(body.position, body.size);
            int found = 0;
            foreach (int start in parts.headStarts)
            {
                LookPart support = parts.Source(start);
                if (support.id != HeadFan.BranchId)
                {
                    Assert.IsTrue(bodyBounds.Intersects(new Bounds(support.position, support.size)),
                        "The central head rests on the body.");
                    continue;
                }
                found++;
                LookPart head = parts.Source(start + 1);
                Assert.AreEqual(ShapeKind.Block, support.shape.kind);
                Assert.AreEqual(Primitive.Stone, support.primitive);
                Assert.AreEqual(neck.y, support.position.y, 0.00001f);
                // Supports lie across X after a quarter-turn about Z.
                Bounds slab = new Bounds(support.position,
                    new Vector3(support.size.y, support.size.x, support.size.z));
                Assert.IsTrue(bodyBounds.Intersects(slab), "The slab attaches to the body.");
                Assert.IsTrue(slab.Intersects(new Bounds(head.position, head.size)),
                    "Each outer head rests on a connected slab.");
            }
            Assert.AreEqual(supports, found);
        }

        [Test]
        public void Compose_TooSmallBudget_RejectsTheRequestedFiveHeads()
        {
            _vocabulary.maxParts = 4;
            LogAssert.Expect(LogType.Error,
                "[LookComposer] DerivedPlantBud: Many requires 12 parts, exceeding the budget of 4; count is preserved.");

            Assert.IsNull(Compose(LookSide.Plant, CountBand.Many));
            UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud, CountBand.Many);
            Assert.AreEqual(5, LookComposer.Layout(channels, _vocabulary).headStarts.Count);
        }

        void AddMiniHead()
        {
            _vocabulary.Layout.extendAccessorySupports = true;
            _vocabulary.accessories[AccessoryKind.MiniHead] = new LookVocabulary.AccessoryEntry
            {
                socket = AccessorySocket.NeckOrbit,
                miniHeadAt = Vector3.right * 0.9f, miniHeadScale = 0.3f,
                plant = new[] { new LookPart
                {
                    id = "MiniBranch", primitive = Primitive.Capsule, shape = ShapeProfile.Segment(),
                    role = PartRole.Accessory, colour = ColourRole.Stem,
                    position = Vector3.right * 0.45f, size = new Vector3(0.9f, 0.12f, 0.12f)
                } },
                stone = new[] { new LookPart
                {
                    id = "MiniSlab", primitive = Primitive.Stone, shape = ShapeProfile.Block(),
                    role = PartRole.Accessory, colour = ColourRole.Stem,
                    position = Vector3.right * 0.45f, size = new Vector3(0.9f, 0.12f, 0.12f)
                } }
            };
        }

        [TestCase(LookSide.Plant)]
        [TestCase(LookSide.Stone)]
        public void Layout_ManyMiniHead_ExtendsAnAttachedSupportBeyondTheCrown(LookSide side)
        {
            AddMiniHead();
            UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud, CountBand.Many,
                accessory: AccessoryKind.MiniHead);
            channels.accessoryHead = HeadKind.Bud;
            PartList parts = LookComposer.Layout(channels, _vocabulary);
            LookPart connector = parts.Source(parts.count - 1);
            LookPart shiftedFirst = parts.Source(parts.accessoryStart);
            Vector3 socket = UnitSockets.Place(channels, _vocabulary).neck;
            float shift = shiftedFirst.position.x - socket.x - 0.45f;

            Assert.Greater(shift, 0f);
            Assert.AreEqual("AccessorySupport", connector.id);
            Assert.AreEqual(socket + Vector3.right * shift * 0.5f, connector.position);
            Assert.GreaterOrEqual(connector.size.y, shift);
            Assert.AreEqual(side == LookSide.Plant ? ShapeKind.Segment : ShapeKind.Block, connector.shape.kind);
            Assert.GreaterOrEqual(LookMeasure.OutlineReach(parts, _vocabulary.Unit(side)),
                LookComposer.AccessoryClearance(side, _vocabulary));
            Assert.AreEqual(5, parts.headStarts.Count);
        }

        [TestCase(LookSide.Plant)]
        [TestCase(LookSide.Stone)]
        public void Layout_SimpleMiniHead_KeepsItsAuthoredPlacement(LookSide side)
        {
            AddMiniHead();
            // Its authored miniature already clears either side's outline.
            _vocabulary.accessories[AccessoryKind.MiniHead].miniHeadAt = Vector3.right * 1.2f;
            UnitChannels channels = RenderTestAssets.CreateChannels(side, HeadKind.Bud,
                accessory: AccessoryKind.MiniHead);
            channels.accessoryHead = HeadKind.Bud;
            PartList parts = LookComposer.Layout(channels, _vocabulary);
            Vector3 socket = UnitSockets.Place(channels, _vocabulary).neck;

            Assert.AreEqual(socket + Vector3.right * 0.45f, parts.Source(parts.accessoryStart).position);
            Assert.IsFalse(Array.Exists(parts.ToArray(), p => p.id == "AccessorySupport"));
        }

        [TestCase(LookSide.Plant)]
        [TestCase(LookSide.Stone)]
        public void Compose_CenteredRing_RemainsOnItsSocketWithoutARightSupport(LookSide side)
        {
            _vocabulary.Layout.extendAccessorySupports = true;
            LookPart ring = Part("Collar", PartRole.Accessory, ShapeProfile.Ring());
            ring.primitive = Primitive.Torus;
            ring.size = new Vector3(0.9f, 0.13f, 0.9f);
            _vocabulary.accessories[AccessoryKind.SmallTorus] = new LookVocabulary.AccessoryEntry
            {
                socket = AccessorySocket.NeckOrbit, isCentered = true,
                plant = new[] { ring }, stone = new[] { ring }
            };
            CreatureGrammarPreset preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
            preset.vocabulary = _vocabulary;
            preset.side = side;
            preset.accessory = AccessoryKind.SmallTorus;
            UnitChannels channels = preset.Channels();
            PartList parts = LookComposer.Layout(channels, _vocabulary);

            Assert.AreEqual(UnitSockets.Place(channels, _vocabulary).neck,
                parts.Source(parts.accessoryStart).position);
            Assert.IsFalse(Array.Exists(parts.ToArray(), p => p.id == "AccessorySupport"));
            Assert.IsEmpty(CreatureGrammarValidator.Validate(preset));
            Assert.NotNull(Track(preset.Compose()));
            // The collar has an open ring profile with positive visible dimensions, not a hidden marker.
            Assert.AreEqual(ShapeKind.Ring, parts.Source(parts.accessoryStart).shape.kind);
            Assert.Greater(parts.Source(parts.accessoryStart).size.x,
                _vocabulary.stems[StemBand.Steady].thickness * 2f);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(0f)]
        [TestCase(-1f)]
        public void Compose_InvalidLayout_RejectsBeforeConstructingGeometry(float width)
        {
            _vocabulary.Layout.limbWidth = width;
            LogAssert.Expect(LogType.Error, "[LookComposer] Invalid layout, shape profile or selected band dimensions.");

            Assert.IsNull(Compose(LookSide.Stone));
        }

        [Test]
        public void Compose_AttachedBlade_UsesItsAnchorsAfterProfileEdits()
        {
            _vocabulary.heads[HeadKind.Bud].plant = new[]
            {
                new LookPart
                {
                    id = "Blade", primitive = Primitive.Leaf, shape = ShapeProfile.Leaf(0.75f, 1.3f),
                    pivot = ShapeAnchor.Bottom, position = new Vector3(0.2f, 0f, 0f),
                    size = new Vector3(0.5f, 1.2f, 0.25f), role = PartRole.Head, colour = ColourRole.Body
                },
                new LookPart
                {
                    id = "Accent", primitive = Primitive.Sphere, shape = ShapeProfile.Bulb(),
                    pivot = ShapeAnchor.Bottom, attachTo = "Blade", attachAt = ShapeAnchor.Top,
                    size = Vector3.one * 0.15f, role = PartRole.Tip, colour = ColourRole.Accent
                }
            };
            CreatureRecipe recipe = Compose(LookSide.Plant);
            CreaturePart blade = Array.Find(recipe.parts, p => p.id == "Blade");
            CreaturePart accent = Array.Find(recipe.parts, p => p.id == "Accent");
            Vector3 basePoint = recipe.parts[0].localPosition + blade.localPosition
                + Vector3.Scale(blade.dimensions, ProceduralShapeMeshes.Anchor(blade.shape, ShapeAnchor.Bottom));
            Vector3 expectedBase = recipe.neckLocal + Vector3.right * (0.2f * _vocabulary.Unit(LookSide.Plant));
            Vector3 topPoint = blade.localPosition
                + Vector3.Scale(blade.dimensions, ProceduralShapeMeshes.Anchor(blade.shape, ShapeAnchor.Top));
            Vector3 accentBase = accent.localPosition
                + Vector3.Scale(accent.dimensions, ProceduralShapeMeshes.Anchor(accent.shape, ShapeAnchor.Bottom));

            Assert.Less(Vector3.Distance(expectedBase, basePoint), 0.00001f);
            Assert.Less(Vector3.Distance(topPoint, accentBase), 0.00001f);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Compose_EmptyBody_RejectsBeforeReadingItsSocket(bool nullArray)
        {
            _vocabulary.bodies[MassBand.Light].plant = nullArray ? null : Array.Empty<LookPart>();
            LogAssert.Expect(LogType.Error, "[LookComposer] The fragment has no parts.");

            Assert.IsNull(Compose(LookSide.Plant));
        }

        [Test]
        public void Validate_InvalidAttachment_StopsStudioAndRuntimeComposition()
        {
            _vocabulary.heads[HeadKind.Bud].plant[0].attachTo = "Missing";
            CreatureGrammarPreset preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
            preset.vocabulary = _vocabulary;
            preset.side = LookSide.Plant;
            preset.head = HeadKind.Bud;
            StringAssert.Contains("earlier unique active part", string.Join(" ", CreatureGrammarValidator.Validate(preset)));
            Assert.IsNull(preset.Compose());
            LogAssert.Expect(LogType.Error,
                "[LookComposer] Part 'Tip' must attach to an earlier unique active part in its fragment: 'Missing'.");
            Assert.IsNull(Compose(LookSide.Plant));
        }

        [Test]
        public void Validate_InvalidProfile_ReportsItInStudio()
        {
            ShapeProfile invalid = ShapeProfile.Bulb();
            invalid.fullness = float.NaN;
            _vocabulary.heads[HeadKind.Bud].plant[0].shape = invalid;
            CreatureGrammarPreset preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
            preset.vocabulary = _vocabulary;
            preset.side = LookSide.Plant;
            preset.head = HeadKind.Bud;

            StringAssert.Contains("invalid part data", string.Join(" ", CreatureGrammarValidator.Validate(preset)));
        }

        [Test]
        public void Validate_InvalidRootProportion_ReportsItInStudio()
        {
            _vocabulary.roots[ReachBand.Short].jointScale = float.NaN;
            CreatureGrammarPreset preset = Track(ScriptableObject.CreateInstance<CreatureGrammarPreset>());
            preset.vocabulary = _vocabulary;
            preset.side = LookSide.Plant;
            preset.head = HeadKind.Bud;
            preset.reach = ReachBand.Short;

            StringAssert.Contains("root profiles", string.Join(" ", CreatureGrammarValidator.Validate(preset)));
        }
    }
}
