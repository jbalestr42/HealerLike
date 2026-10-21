using NUnit.Framework;

namespace UI.Toolkit
{
    public class ToolkitRichTextTests
    {
        [Test]
        public void Normalise_Empty_ReturnsEmpty()
        {
            string input = "";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("", result);
        }

        [Test]
        public void Normalise_PlainText_ReturnsSameInstance()
        {
            string input = "Heal a single ally for 35 HP";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_ComparisonOperators_TreatedAsContent()
        {
            string input = "HP < 5 and 3 > 1";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_Bold_PassesThrough()
        {
            string input = "<b>Rest</b>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_Italic_PassesThrough()
        {
            string input = "<i>A quiet place</i>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_Underline_PassesThrough()
        {
            string input = "<u>link</u>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_ColorHex_PassesThrough()
        {
            string input = "<color=#FFD966><b>Stats</b></color>\n";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_ColorQuotedName_PassesThrough()
        {
            string input = "Cost: <color=\"blue\">5</color> mana";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_ColorHexWithAlpha_PassesThrough()
        {
            string input = "<color=#008080ff>(150% HealPower)</color>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_ColorUnquotedName_GetsQuotes()
        {
            string input = "<color=green>5</color>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<color=\"green\">5</color>", result);
        }

        [Test]
        public void Normalise_ColorQuotedHex_LosesQuotes()
        {
            string input = "<color=\"#008080ff\">x</color>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<color=#008080ff>x</color>", result);
        }

        [Test]
        public void Normalise_ColorShortForm_PassesThrough()
        {
            string input = "<#FF0000>x</color>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_ColorBadHex_StripsTag()
        {
            string input = "<color=#12>x</color>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_ColorEmptyValue_StripsTag()
        {
            string input = "<color=>x</color>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_ColorNoValue_StripsTag()
        {
            string input = "<color>x</color>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_SizePercent_PassesThrough()
        {
            string input = "<b>Rest</b>\n<size=75%>Heal</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_SizeEm_PassesThrough()
        {
            string input = "<size=1.5em>x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_SizePixels_PassesThrough()
        {
            string input = "<size=12px>x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_SizeRelative_PassesThroughWithoutBase()
        {
            string input = "<size=+6><b>Title</b></size>\n";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_SizeRelativeNegative_PassesThroughWithoutBase()
        {
            string input = "<size=-2>x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_SizeRelative_ResolvesWithBase()
        {
            string input = "<size=+6><b>Title</b></size>";

            string result = ToolkitRichText.Normalise(input, 16f);

            Assert.AreEqual("<size=22px><b>Title</b></size>", result);
        }

        [Test]
        public void Normalise_SizeRelativeNegative_ResolvesWithBase()
        {
            string input = "<size=-4>x</size>";

            string result = ToolkitRichText.Normalise(input, 16f);

            Assert.AreEqual("<size=12px>x</size>", result);
        }

        [Test]
        public void Normalise_SizeRelativeDecimal_ResolvesWithBase()
        {
            string input = "<size=+6.5>x</size>";

            string result = ToolkitRichText.Normalise(input, 16f);

            Assert.AreEqual("<size=22.5px>x</size>", result);
        }

        [Test]
        public void Normalise_SizeRelativeBelowOne_ClampsToOnePixel()
        {
            string input = "<size=-40>x</size>";

            string result = ToolkitRichText.Normalise(input, 16f);

            Assert.AreEqual("<size=1px>x</size>", result);
        }

        [Test]
        public void Normalise_SizeAbsolute_GetsPixelUnit()
        {
            string input = "<size=22><b>Name</b></size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<size=22px><b>Name</b></size>", result);
        }

        [Test]
        public void Normalise_SizeAbsoluteDecimal_GetsPixelUnit()
        {
            string input = "<size=22.5>x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<size=22.5px>x</size>", result);
        }

        [Test]
        public void Normalise_SizeAbsoluteCommaDecimal_BecomesDot()
        {
            string input = "<size=22,5>x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<size=22.5px>x</size>", result);
        }

        [Test]
        public void Normalise_SizeAbsoluteSpacer_GetsPixelUnit()
        {
            string input = "<size=6>\n</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<size=6px>\n</size>", result);
        }

        [Test]
        public void Normalise_SizeAbsolute_IgnoresBase()
        {
            string input = "<size=22>x</size>";

            string result = ToolkitRichText.Normalise(input, 16f);

            Assert.AreEqual("<size=22px>x</size>", result);
        }

        [Test]
        public void Normalise_SizeQuoted_LosesQuotes()
        {
            string input = "<size=\"12\">x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<size=12px>x</size>", result);
        }

        [Test]
        public void Normalise_SizeNotANumber_StripsTag()
        {
            string input = "<size=big>x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_SizeExponent_StripsTag()
        {
            string input = "<size=1E+07>x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_SizeTrailingSeparator_StripsTag()
        {
            string input = "<size=22.>x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_SizeNoValue_StripsTag()
        {
            string input = "<size>x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_SizeEmptyValue_StripsTag()
        {
            string input = "<size=>x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_Mark_PassesThrough()
        {
            string input = "<mark=#ffff00aa>x</mark>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_MarkWithoutValue_StripsTag()
        {
            string input = "<mark>x</mark>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_BoldWithValue_StripsTag()
        {
            string input = "<b=1>x</b>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_UpperCaseTagNames_AreMatched()
        {
            string input = "<B>x</B>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_LineBreak_PassesThrough()
        {
            string input = "a<br>b";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_SpriteAndAlign_PassThrough()
        {
            string input = "<sprite=0> <align=center>x</align>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_UnsupportedTags_AreStrippedKeepingText()
        {
            string input = "a<page>b<material=2>c</material>d<scale=2>e";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("abcde", result);
        }

        [Test]
        public void Normalise_Nested_PassesThrough()
        {
            string input = "<b><i>x</i></b>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_NestedSizeBoldColor_NormalisesOnlyTheSize()
        {
            string input = "<size=22><b>Name</b></size>\n<color=#9AA3B2>Ally · in battle</color>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<size=22px><b>Name</b></size>\n<color=#9AA3B2>Ally · in battle</color>", result);
        }

        [Test]
        public void Normalise_Unclosed_IsClosedAtTheEnd()
        {
            string input = "<b>bold";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<b>bold</b>", result);
        }

        [Test]
        public void Normalise_UnclosedNested_IsClosedInnermostFirst()
        {
            string input = "<b><color=#FFFFFF>x";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<b><color=#FFFFFF>x</color></b>", result);
        }

        [Test]
        public void Normalise_UnclosedAfterRewrite_IsClosed()
        {
            string input = "<size=22>x";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<size=22px>x</size>", result);
        }

        [Test]
        public void Normalise_MisnestedClosers_AreRepaired()
        {
            string input = "<b><i>x</b>y</i>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<b><i>x</i></b>y", result);
        }

        [Test]
        public void Normalise_StrayCloser_IsStripped()
        {
            string input = "x</b>y";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("xy", result);
        }

        [Test]
        public void Normalise_OpenBracketWithoutClose_IsContent()
        {
            string input = "a <b c";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_DoubleOpenBracket_FirstIsContent()
        {
            string input = "<<b>x</b>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_EmptyTag_IsContent()
        {
            string input = "a <> b";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_NameInAngleBrackets_IsContent()
        {
            string input = "You meet <Skeleton> ahead";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_NameWithSpaceInAngleBrackets_IsContent()
        {
            string input = "<Fire Mage> joins";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_NameInsideRealTags_IsContent()
        {
            string input = "<b>You meet <Skeleton></b>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_HashNumberInAngleBrackets_IsContent()
        {
            string input = "rank <#1> of 3";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_TooDeep_DropsTheExcessOpeners()
        {
            string input = string.Concat(System.Linq.Enumerable.Repeat("<b>", 20)) + "x";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual(string.Concat(System.Linq.Enumerable.Repeat("<b>", 16)) + "x" + string.Concat(System.Linq.Enumerable.Repeat("</b>", 16)), result);
        }

        [Test]
        public void Normalise_EntityHeader_NormalisesTheSpacerOnly()
        {
            string input = "<size=6>\n</size><color=#FFD966><b>Mana</b></color>\n<color=#9AA3B2>none</color>\n";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<size=6px>\n</size><color=#FFD966><b>Mana</b></color>\n<color=#9AA3B2>none</color>\n", result);
        }

        [Test]
        public void Normalise_SkillDescription_PassesThrough()
        {
            string input = "Heal for <color=\"green\">[35x1]</color> <color=#008080ff>(150% HealPower)</color> every 3s\n\nCost: <color=\"blue\">5</color> mana\nCooldown: <color=\"yellow\">8s</color>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_StripSizes_Absolute_RemovesTagKeepsText()
        {
            string input = "<size=22><b>Zealot</b></size>";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("<b>Zealot</b>", result);
        }

        [Test]
        public void Normalise_StripSizes_Relative_RemovesTagKeepsText()
        {
            string input = "<size=+6><b>Zealot</b></size>";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("<b>Zealot</b>", result);
        }

        [Test]
        public void Normalise_StripSizes_NegativeRelative_RemovesTagKeepsText()
        {
            string input = "<size=-2>x</size>";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_StripSizes_Percent_RemovesTagKeepsText()
        {
            string input = "<size=75%>x</size>";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_StripSizes_Spacer_KeepsTheNewline()
        {
            string input = "<size=6>\n</size>x";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("\nx", result);
        }

        [Test]
        public void Normalise_StripSizes_UpperCase_RemovesTag()
        {
            string input = "<SIZE=6>\n</SIZE>x";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("\nx", result);
        }

        [Test]
        public void Normalise_StripSizes_Malformed_RemovesTag()
        {
            string input = "<size=big>x</size>";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_StripSizes_NoValue_RemovesTag()
        {
            string input = "<size>x</size>";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_StripSizes_StrayCloser_RemovesTag()
        {
            string input = "x</size>y";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("xy", result);
        }

        [Test]
        public void Normalise_StripSizes_Unclosed_AddsNoCloser()
        {
            string input = "<size=22>x";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_StripSizes_IgnoresBase()
        {
            string input = "<size=+6>x</size>";

            string result = ToolkitRichText.Normalise(input, 16f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("x", result);
        }

        [Test]
        public void Normalise_StripSizes_OtherTags_PassThrough()
        {
            string input = "<color=#9AA3B2>(base 10)</color> <b>12</b> <i>note</i>";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_StripSizes_EntityTitle_KeepsBoldAndSecondLine()
        {
            string input = "<size=22><b>Name</b></size>\n<color=#9AA3B2>Ally · in battle</color>";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("<b>Name</b>\n<color=#9AA3B2>Ally · in battle</color>", result);
        }

        [Test]
        public void Normalise_StripSizes_NoSizeTag_ReturnsSameInstance()
        {
            string input = "<b>Rest</b>\n<size-like> text";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_StripSizes_AbsentByDefault_SizeSurvives()
        {
            string input = "<size=22>x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<size=22px>x</size>", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_AtStart_BecomeNonBreaking()
        {
            string input = "    x";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("\u00A0\u00A0\u00A0\u00A0x", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_AfterNewline_BecomeNonBreaking()
        {
            string input = "a\n  b";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("a\n\u00A0\u00A0b", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_BehindOpeningTag_BecomeNonBreaking()
        {
            string input = "Damage: <b>12</b>\n<color=#9AA3B2>    +2 · Hexer</color>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("Damage: <b>12</b>\n<color=#9AA3B2>\u00A0\u00A0\u00A0\u00A0+2 · Hexer</color>", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_BehindTwoTags_BecomeNonBreaking()
        {
            string input = "<color=#9AA3B2><i>  x</i></color>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<color=#9AA3B2><i>\u00A0\u00A0x</i></color>", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_BetweenTags_BecomeNonBreaking()
        {
            string input = "<b></b>  <i>x</i>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<b></b>\u00A0\u00A0<i>x</i>", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_BehindRewrittenTag_BecomeNonBreaking()
        {
            string input = "<size=22>  x</size>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("<size=22px>\u00A0\u00A0x</size>", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_BehindStrippedSize_BecomeNonBreaking()
        {
            string input = "<size=22>  x</size>";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual("\u00A0\u00A0x", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_SpacesOnlyLine_BecomeNonBreaking()
        {
            string input = "a\n   \nb";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("a\n\u00A0\u00A0\u00A0\nb", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_InsideALine_AreNotTouched()
        {
            string input = "Max HP:  100";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_LeadingSpaces_AfterTextAndTag_AreNotTouched()
        {
            string input = "a <b>  x</b>  y";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_LeadingSpaces_AfterContentLookingTag_AreNotTouched()
        {
            string input = "<Skeleton>  x";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_LeadingSpaces_Trailing_AreNotTouched()
        {
            string input = "x  \ny ";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_LeadingSpaces_OnlyTheLeadingRun_IsConverted()
        {
            string input = "  a  b";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("\u00A0\u00A0a  b", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_PlainLeadingSpacesOption_ReturnsSameInstance()
        {
            string input = "<color=#9AA3B2>    +2 · Hexer</color>\n  x";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.PlainLeadingSpaces);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_LeadingSpaces_PlainLeadingSpacesWithStrip_OnlyStrips()
        {
            string input = "<size=6>  x</size>";

            string result = ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes | ToolkitRichTextOptions.PlainLeadingSpaces);

            Assert.AreEqual("  x", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_AfterLineBreakTag_BecomeNonBreaking()
        {
            string input = "a<br>  b";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreEqual("a<br>\u00A0\u00A0b", result);
        }

        [Test]
        public void Normalise_LeadingSpaces_AfterSprite_AreNotTouched()
        {
            string input = "<sprite=0>  x";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_LeadingSpaces_AlreadyNonBreaking_ReturnsSameInstance()
        {
            string input = "\u00A0\u00A0\u00A0\u00A0x\n<b>\u00A0y</b>";

            string result = ToolkitRichText.Normalise(input);

            Assert.AreSame(input, result);
        }

        [Test]
        public void Normalise_Null_ReturnsEmpty()
        {
            string result = ToolkitRichText.Normalise(null);

            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void Normalise_CalledTwice_DoesNotLeakScratchState()
        {
            string input = "<size=22,5><b>x</b>\n<color=green>y";

            string first = ToolkitRichText.Normalise(input);
            string second = ToolkitRichText.Normalise(input);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Normalise_OwnOutput_IsUnchanged()
        {
            string once = ToolkitRichText.Normalise("<size=22,5><b>x</b>\n<color=green>y<i>z</b>");

            string twice = ToolkitRichText.Normalise(once);

            Assert.AreSame(once, twice);
        }

        [Test]
        public void Normalise_OptionsOverload_MatchesTheThreeArgumentForm()
        {
            string input = "<size=22><b>x</b></size>";

            string result = ToolkitRichText.Normalise(input, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual(ToolkitRichText.Normalise(input, 0f, ToolkitRichTextOptions.StripSizes), result);
        }

        [Test]
        public void Normalise_NullWithOptions_ReturnsEmpty()
        {
            string result = ToolkitRichText.Normalise(null, ToolkitRichTextOptions.StripSizes);

            Assert.AreEqual(string.Empty, result);
        }
    }
}
