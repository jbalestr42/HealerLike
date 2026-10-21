using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace HealerLike.Render.Shaders
{

public class GlslStorageBlocksTests
{
    // The shape HLSLcc gives a linked GLES program: both stages in one text, nested conditionals inside each
    const string linkedProgram =
        "#ifdef VERTEX\n" +
        "#version 310 es\n" +
        "#if HLSLCC_ENABLE_UNIFORM_BUFFERS\n" +
        "#define UNITY_UNIFORM\n" +
        "#endif\n" +
        "struct _HLGroundStamps_type { uint[8] value; };\n" +
        "layout(std430, binding = 0) readonly buffer _HLGroundStamps {\n" +
        "    _HLGroundStamps_type _HLGroundStamps_buf[];\n" +
        "};\n" +
        "void main() {}\n" +
        "#endif\n" +
        "#ifdef FRAGMENT\n" +
        "#version 310 es\n" +
        "layout(std430, binding = 1) readonly buffer _HLZones {\n" +
        "    uint _HLZones_buf[];\n" +
        "};\n" +
        "void main() {}\n" +
        "#endif\n";

    [Test]
    public void Find_LinkedProgram_AttributesEachBlockToTheStageSectionDeclaringIt()
    {
        List<GlslStorageBlock> blocks = GlslStorageBlocks.Find(linkedProgram, GlslStage.Vertex);

        CollectionAssert.AreEquivalent(new[]
        {
            new GlslStorageBlock(GlslStage.Vertex, "_HLGroundStamps"),
            new GlslStorageBlock(GlslStage.Fragment, "_HLZones")
        }, blocks);
    }

    [Test]
    public void Find_FragmentOnlyBlock_IsNeverAttributedToTheVertexStage()
    {
        string program = "#ifdef VERTEX\n#version 310 es\nvoid main() {}\n#endif\n" +
                         "#ifdef FRAGMENT\n#version 310 es\nreadonly buffer _HLZones { uint b[]; };\nvoid main() {}\n#endif\n";

        List<GlslStorageBlock> blocks = GlslStorageBlocks.Find(program, GlslStage.Vertex);

        CollectionAssert.AreEqual(new[] { new GlslStorageBlock(GlslStage.Fragment, "_HLZones") }, blocks);
    }

    [Test]
    public void Find_SameBlockInBothStages_ReportsItOncePerStage()
    {
        string program = "#ifdef VERTEX\nbuffer A { uint a[]; };\nbuffer A { uint a[]; };\n#endif\n" +
                         "#ifdef FRAGMENT\nbuffer A { uint a[]; };\n#endif\n";

        List<GlslStorageBlock> blocks = GlslStorageBlocks.Find(program, GlslStage.Vertex);

        CollectionAssert.AreEquivalent(new[]
        {
            new GlslStorageBlock(GlslStage.Vertex, "A"),
            new GlslStorageBlock(GlslStage.Fragment, "A")
        }, blocks);
    }

    [Test]
    public void Find_TextOutsideAnyStageSection_BelongsToTheRequestedStage()
    {
        string program = "#version 310 es\nlayout(std430, binding = 3) buffer Seeds\n{\n uint s[];\n};\nvoid main() {}\n";

        CollectionAssert.AreEqual(new[] { new GlslStorageBlock(GlslStage.Vertex, "Seeds") },
            GlslStorageBlocks.Find(program, GlslStage.Vertex));
        CollectionAssert.AreEqual(new[] { new GlslStorageBlock(GlslStage.Fragment, "Seeds") },
            GlslStorageBlocks.Find(program, GlslStage.Fragment));
    }

    [Test]
    public void Find_AllQualifierForms_AreStorageBlocks()
    {
        string program = "#ifdef VERTEX\n" +
                         "buffer Plain { uint a[]; };\n" +
                         "layout(std430, binding = 0) readonly buffer ReadOnly { uint b[]; };\n" +
                         "layout(std430) coherent restrict writeonly buffer Written{ uint c[]; };\n" +
                         "#endif\n";

        List<GlslStorageBlock> blocks = GlslStorageBlocks.Find(program, GlslStage.Fragment);

        CollectionAssert.AreEqual(new[]
        {
            new GlslStorageBlock(GlslStage.Vertex, "Plain"),
            new GlslStorageBlock(GlslStage.Vertex, "ReadOnly"),
            new GlslStorageBlock(GlslStage.Vertex, "Written")
        }, blocks);
    }

    [Test]
    public void Find_UniformBlocksCommentsAndLookalikeIdentifiers_AreNotStorageBlocks()
    {
        string program = "#ifdef VERTEX\n" +
                         "layout(std140) uniform UnityPerDraw { vec4 unity_ObjectToWorld[4]; };\n" +
                         "// readonly buffer Commented { uint a[]; };\n" +
                         "/* buffer Hidden {\n uint b[]; }; */\n" +
                         "uniform vec4 _HLbuffer;\n" +
                         "vec4 u_xlat_buffer = vec4(0.0);\n" +
                         "uint my_buffer { };\n" +
                         "#endif\n";

        Assert.IsEmpty(GlslStorageBlocks.Find(program, GlslStage.Vertex));
    }

    [Test]
    public void Split_NestedConditionalsInsideAStage_StayInThatStage()
    {
        string program = "#ifdef VERTEX\n#if A\n#ifdef FRAGMENT\nbuffer Inner { uint a[]; };\n#endif\n#endif\n" +
                         "buffer After { uint b[]; };\n#endif\n";

        List<GlslStorageBlock> blocks = GlslStorageBlocks.Find(program, GlslStage.Fragment);

        CollectionAssert.AreEqual(new[]
        {
            new GlslStorageBlock(GlslStage.Vertex, "Inner"),
            new GlslStorageBlock(GlslStage.Vertex, "After")
        }, blocks);
    }

    [Test]
    public void Split_UnterminatedStageSection_KeepsItsText()
    {
        List<GlslStorageBlock> blocks = GlslStorageBlocks.Find("#ifdef VERTEX\nbuffer Cut { uint a[]; };\n",
            GlslStage.Fragment);

        CollectionAssert.AreEqual(new[] { new GlslStorageBlock(GlslStage.Vertex, "Cut") }, blocks);
    }

    [Test]
    public void Find_EmptyOrNull_FindsNothing()
    {
        Assert.IsEmpty(GlslStorageBlocks.Find(null, GlslStage.Vertex));
        Assert.IsEmpty(GlslStorageBlocks.Find("", GlslStage.Vertex));
    }

    [Test]
    public void StageOfSection_MapsOnlyVertexAndFragmentToTheirStages()
    {
        Assert.AreEqual(GlslStage.Vertex, GlslStorageBlocks.StageOfSection("VERTEX"));
        Assert.AreEqual(GlslStage.Fragment, GlslStorageBlocks.StageOfSection("FRAGMENT"));
        Assert.AreEqual(GlslStage.Other, GlslStorageBlocks.StageOfSection("GEOMETRY"));
    }

    [Test]
    public void TextOf_BinaryHeaderAhead_KeepsTheGlslReadable()
    {
        byte[] glsl = Encoding.ASCII.GetBytes("#ifdef VERTEX\r\nbuffer B { uint a[]; };\r\n#endif\r\n");
        byte[] data = new byte[glsl.Length + 6];
        new byte[] { 0x00, 0xff, 0x03, 0x80, 0x01, 0x00 }.CopyTo(data, 0);
        glsl.CopyTo(data, 6);

        string text = GlslStorageBlocks.TextOf(data);

        StringAssert.DoesNotContain("\r", text);
        CollectionAssert.AreEqual(new[] { new GlslStorageBlock(GlslStage.Vertex, "B") },
            GlslStorageBlocks.Find(text, GlslStage.Fragment));
        Assert.AreEqual("", GlslStorageBlocks.TextOf(null));
    }
}

}
