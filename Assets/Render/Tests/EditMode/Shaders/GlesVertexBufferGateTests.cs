using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace HealerLike.Render.Shaders
{

public class GlesVertexBufferGateTests
{
    [Test]
    public void Targets_CoverTheThreeShadersWithVertexStorageReads()
    {
        string[] paths = GlesVertexBufferGate.targets.Select(t => t.path).ToArray();

        CollectionAssert.IsSubsetOf(new[]
        {
            "Assets/Render/Shaders/GroundSimulation.shader",
            "Assets/Render/Shaders/Look.shader",
            "Assets/Render/Shaders/GrassRing.shader"
        }, paths);
    }

    [Test]
    public void VariantsOf_CompilesNoKeywordThenEachKeywordAlone()
    {
        List<string[]> variants = GlesVertexBufferGate.VariantsOf(new[] { "A", "B" }, null);

        Assert.AreEqual(3, variants.Count);
        CollectionAssert.IsEmpty(variants[0]);
        CollectionAssert.AreEqual(new[] { "A" }, variants[1]);
        CollectionAssert.AreEqual(new[] { "B" }, variants[2]);
    }

    [Test]
    public void VariantsOf_ExtraSets_KeepOnlyDeclaredKeywordsAndSkipRepeats()
    {
        List<string[]> variants = GlesVertexBufferGate.VariantsOf(new[] { "A", "B" }, new[]
        {
            new[] { "A", "B" },
            new[] { "A", "UNDECLARED" },
            new[] { "UNDECLARED" },
            new[] { "A", "B" }
        });

        Assert.AreEqual(4, variants.Count);
        CollectionAssert.AreEqual(new[] { "A", "B" }, variants[3]);
    }

    [Test]
    public void Record_SameBlockAcrossVariants_IsOneFindingListingEveryVariant()
    {
        List<GlesVertexBufferGate.Finding> findings = new List<GlesVertexBufferGate.Finding>();
        GlslStorageBlock block = new GlslStorageBlock(GlslStage.Vertex, "_HLZones");

        GlesVertexBufferGate.Record(findings, "Ring.shader", "P", 0, block, "<none>");
        GlesVertexBufferGate.Record(findings, "Ring.shader", "P", 0, block, "INSTANCING_ON");
        GlesVertexBufferGate.Record(findings, "Ring.shader", "P", 0, block, "INSTANCING_ON");

        Assert.AreEqual(1, findings.Count);
        CollectionAssert.AreEqual(new[] { "<none>", "INSTANCING_ON" }, findings[0].variants);
    }

    [Test]
    public void Record_DifferentPassOrStage_AreSeparateFindings()
    {
        List<GlesVertexBufferGate.Finding> findings = new List<GlesVertexBufferGate.Finding>();

        GlesVertexBufferGate.Record(findings, "S", "P0", 0, new GlslStorageBlock(GlslStage.Vertex, "B"), "v");
        GlesVertexBufferGate.Record(findings, "S", "P3", 3, new GlslStorageBlock(GlslStage.Vertex, "B"), "v");
        GlesVertexBufferGate.Record(findings, "S", "P0", 0, new GlslStorageBlock(GlslStage.Fragment, "B"), "v");

        Assert.AreEqual(3, findings.Count);
    }

    [Test]
    public void ExitCode_VertexBlockFails_FragmentBlockAloneIsClean()
    {
        GlesVertexBufferGate.Result fragmentOnly = new GlesVertexBufferGate.Result();
        GlesVertexBufferGate.Record(fragmentOnly.otherBlocks, "S", "P", 0,
            new GlslStorageBlock(GlslStage.Fragment, "_HLZones"), "v");
        GlesVertexBufferGate.Result vertex = new GlesVertexBufferGate.Result();
        GlesVertexBufferGate.Record(vertex.vertexBlocks, "S", "P", 0,
            new GlslStorageBlock(GlslStage.Vertex, "_HLZones"), "v");

        Assert.AreEqual(GlesVertexBufferGate.exitClean, fragmentOnly.exitCode);
        Assert.AreEqual(GlesVertexBufferGate.exitViolation, vertex.exitCode);
    }

    [Test]
    public void ExitCode_AnyBlindSpot_OverridesACleanOrViolatingResult()
    {
        GlesVertexBufferGate.Result result = new GlesVertexBufferGate.Result();
        result.blindSpots.Add("compile failed");

        Assert.AreEqual(GlesVertexBufferGate.exitBlind, result.exitCode);
        GlesVertexBufferGate.Record(result.vertexBlocks, "S", "P", 0, new GlslStorageBlock(GlslStage.Vertex, "B"), "v");
        Assert.AreEqual(GlesVertexBufferGate.exitBlind, result.exitCode);
    }

    [Test]
    public void LooksLikeGlsl_EmptyOrBytecodeText_IsNotGlsl()
    {
        Assert.IsFalse(GlesVertexBufferGate.LooksLikeGlsl(""));
        Assert.IsFalse(GlesVertexBufferGate.LooksLikeGlsl("DXBC\nRDEF"));
        Assert.IsTrue(GlesVertexBufferGate.LooksLikeGlsl("#ifdef VERTEX\n#version 310 es\nvoid main()\n{}\n#endif\n"));
    }
}

}
