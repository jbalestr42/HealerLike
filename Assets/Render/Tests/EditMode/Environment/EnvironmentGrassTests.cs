using System.Linq;
using System.Text.RegularExpressions;
using HealerLike.Render.Grass;
using HealerLike.Render.Stage;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace HealerLike.Render.Environment
{
    public class EnvironmentGrassTests
    {
        [Test]
        public void StripsTileTheRingWithoutTouchingTheGrid()
        {
            Rect grid = new Rect(-8f, -8f, 16f, 16f);

            Rect[] strips = EnvironmentGrass.Strips(grid, 16f);

            Assert.AreEqual(4, strips.Length);
            float area = 0f;
            for (int i = 0; i < strips.Length; i++)
            {
                area += strips[i].width * strips[i].height;
                Assert.IsFalse(strips[i].Overlaps(grid), "strip " + i);
                for (int j = i + 1; j < strips.Length; j++)
                {
                    Assert.IsFalse(strips[i].Overlaps(strips[j]), i + "/" + j);
                }
            }
            Assert.AreEqual(48 * 48 - 16 * 16, area, 0.001f);
            Assert.AreEqual(new Rect(-24f, 8f, 48f, 16f), strips[0]);
        }

        [Test]
        public void BudgetIsAreaTimesDensityCappedByTheGrassMaximum()
        {
            Assert.AreEqual(48 * 16 * 64, EnvironmentGrass.Budget(new Rect(0f, 0f, 48f, 16f), 64f));
            Assert.AreEqual(GrassLayout.MaxBudget, EnvironmentGrass.Budget(new Rect(0f, 0f, 1000f, 1000f), 64f));
            Assert.AreEqual(0, EnvironmentGrass.Budget(new Rect(0f, 0f, 4f, 4f), -1f));
        }

        [Test]
        public void Strips_ZeroRing_LogsAndReturnsNoStrips()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"^\[EnvironmentGrass\] Rejected ring width"));

            Rect[] strips = EnvironmentGrass.Strips(new Rect(0f, 0f, 1f, 1f), 0f);

            Assert.IsEmpty(strips);
        }

        [Test]
        public void BandsTileTheAnnulusInWholeCellsWithoutTouchingTheGrid()
        {
            Rect grid = new Rect(-8f, -8f, 16f, 16f);

            RingStrip[] strips = EnvironmentGrass.Bands(grid);

            float reach = EnvironmentGrass.DefaultWidths.Sum();
            float area = 0f;
            Assert.AreEqual(24f, reach);
            Rect outer = new Rect(grid.xMin - reach, grid.yMin - reach,
                grid.width + 2f * reach, grid.height + 2f * reach);
            for (int i = 0; i < strips.Length; i++)
            {
                Rect rect = strips[i].rect;
                area += rect.width * rect.height;
                Assert.IsFalse(rect.Overlaps(grid), "strip " + i);
                bool isInside = rect.xMin >= outer.xMin && rect.yMin >= outer.yMin && rect.xMax <= outer.xMax
                    && rect.yMax <= outer.yMax;
                Assert.IsTrue(isInside, "strip " + i);
                Assert.AreEqual(Mathf.Round(rect.width), rect.width);
                Assert.AreEqual(Mathf.Round(rect.height), rect.height);
                Assert.AreEqual(Mathf.Round(rect.x), rect.x);
                Assert.AreEqual(Mathf.Round(rect.y), rect.y);
                for (int j = i + 1; j < strips.Length; j++)
                {
                    Assert.IsFalse(rect.Overlaps(strips[j].rect), i + "/" + j);
                }
            }
            Assert.AreEqual(outer.width * outer.height - grid.width * grid.height, area, 0.001f);
            Assert.That(outer.yMin, Is.LessThan(-12.6f));
        }

        [Test]
        public void BandBudgetsStayUnderTheCapAndDensityFallsOutward()
        {
            RingStrip[] strips = EnvironmentGrass.Bands(new Rect(-8f, -8f, 16f, 16f));

            foreach (RingStrip strip in strips)
            {
                Assert.That(strip.budget, Is.LessThanOrEqualTo(GrassLayout.MaxBudget));
                Assert.AreEqual(Mathf.RoundToInt(strip.rect.width * strip.rect.height * strip.density), strip.budget);
                Assert.AreEqual(EnvironmentGrass.BoardDensity * EnvironmentGrass.DefaultFractions[strip.band],
                    strip.density, 0.001f);
            }
            for (int band = 1; band < EnvironmentGrass.DefaultWidths.Length; band++)
            {
                float outerMax = strips.Where(s => s.band == band).Max(s => s.density);
                float innerMin = strips.Where(s => s.band == band - 1).Min(s => s.density);
                Assert.That(outerMax, Is.LessThan(innerMin));
            }
            bool isNearBoard = strips.Where(s => s.band == 0)
                .All(s => s.rect.xMin >= -11f && s.rect.xMax <= 11f && s.rect.yMin >= -11f && s.rect.yMax <= 11f);
            Assert.That(isNearBoard);
        }

        [Test]
        public void OverBudgetStripsAreSplitWithoutLosingDensity()
        {
            // One 16-wide band at full board density: top and bottom are 48 x 16 x 256 = two budgets each
            RingStrip[] strips = EnvironmentGrass.Bands(new Rect(-8f, -8f, 16f, 16f), new float[] { 16f },
                new float[] { 1f }, 256f);

            Assert.That(strips.Length, Is.GreaterThan(4));
            Assert.AreEqual((48 * 48 - 16 * 16) * 256, strips.Sum(s => s.budget));
            foreach (RingStrip strip in strips)
            {
                Assert.That(strip.budget, Is.LessThanOrEqualTo(GrassLayout.MaxBudget));
            }

            // An odd split still yields whole cells and no loss
            RingStrip[] odd = EnvironmentGrass.Bands(new Rect(0f, 0f, 5f, 3f), new float[] { 7f },
                new float[] { 1f }, 3000f);

            foreach (RingStrip strip in odd)
            {
                Assert.AreEqual(Mathf.Round(strip.rect.width), strip.rect.width);
                Assert.That(strip.budget, Is.LessThanOrEqualTo(GrassLayout.MaxBudget));
            }
            Assert.AreEqual((19 * 17 - 5 * 3) * 3000, odd.Sum(s => s.budget));
        }

        [Test]
        public void BandsRejectInvalidInput()
        {
            Rect grid = new Rect(-8f, -8f, 16f, 16f);
            Rect halfCellGrid = new Rect(0f, 0f, 2.5f, 3f);
            Regex rejected = new Regex(@"^\[EnvironmentGrass\] (Rejected|Widths)");
            for (int i = 0; i < 8; i++)
            {
                LogAssert.Expect(LogType.Error, rejected);
            }

            Assert.IsEmpty(EnvironmentGrass.Bands(grid, null, new float[] { 1f }, 256f));
            Assert.IsEmpty(EnvironmentGrass.Bands(grid, new float[] { 3f, 5f }, new float[] { 1f }, 256f));
            Assert.IsEmpty(EnvironmentGrass.Bands(grid, new float[0], new float[0], 256f));
            Assert.IsEmpty(EnvironmentGrass.Bands(grid, new float[] { 2.5f }, new float[] { 1f }, 256f));
            Assert.IsEmpty(EnvironmentGrass.Bands(grid, new float[] { 0f }, new float[] { 1f }, 256f));
            Assert.IsEmpty(EnvironmentGrass.Bands(grid, new float[] { 3f }, new float[] { 0f }, 256f));
            Assert.IsEmpty(EnvironmentGrass.Bands(grid, new float[] { 3f }, new float[] { 1f }, float.NaN));
            Assert.IsEmpty(EnvironmentGrass.Bands(halfCellGrid, new float[] { 3f }, new float[] { 1f }, 256f));
        }

        [Test]
        public void InitCopiesTheTemplateOncePerBandStripAndUpdatesThemWithoutZones()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Zone buffers need a graphics device; run with Metal.");
            }

            GameObject go = new GameObject("RingGrass");
            GameObject zonesGo = new GameObject("RingZones");
            GameObject managerGo = new GameObject("RingManager");
            ZoneRegistry zones = zonesGo.AddComponent<ZoneRegistry>();
            try
            {
                GameObject templateGo = new GameObject("GrassStrip");
                templateGo.transform.SetParent(go.transform, false);
                templateGo.SetActive(false);
                GrassField template = templateGo.AddComponent<GrassField>();
                EnvironmentGrass grass = go.AddComponent<EnvironmentGrass>();
                TestHelpers.SetPrivateField(grass, "_stripTemplate", template);
                zones.Init();
                RenderManager manager = managerGo.AddComponent<RenderManager>();
                Rect board = new Rect(-8f, -8f, 16f, 16f);
                float boardDensity = GrassLayout.DefaultBudget / (16f * 16f);
                RingStrip[] bands = EnvironmentGrass.Bands(board, EnvironmentGrass.DefaultWidths, EnvironmentGrass.DefaultFractions, boardDensity);

                grass.Init(board, 1f, 0.5f, null, zones, manager);
                grass.UpdateStrips(zones);

                Assert.AreEqual(bands.Length, grass.strips.Count);
                for (int i = 0; i < bands.Length; i++)
                {
                    Assert.AreEqual(bands[i].budget, grass.strips[i].bladeBudget);
                    Assert.AreEqual(0, grass.strips[i].activeZoneCount);
                    Assert.AreSame(go.transform, grass.strips[i].transform.parent);
                }
                Assert.IsFalse(templateGo.activeSelf);
            }
            finally
            {
                zones.Release();
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(zonesGo);
                Object.DestroyImmediate(managerGo);
            }
        }

        [Test]
        public void UpdateStripsWithoutInitOrRegistryIsSafe()
        {
            GameObject go = new GameObject("RingGrass");
            try
            {
                EnvironmentGrass grass = go.AddComponent<EnvironmentGrass>();

                Assert.DoesNotThrow(() =>
                {
                    grass.UpdateStrips(null);
                    TestHelpers.InvokePrivate(grass, "OnDisable");
                });
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
