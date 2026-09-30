using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HealerLike.Render.Shaders
{
    public enum GlslStage
    {
        Vertex,
        Fragment,
        Other
    }

    public struct GlslStorageBlock
    {
        public GlslStage stage;
        public string name;

        public GlslStorageBlock(GlslStage stage, string name)
        {
            this.stage = stage;
            this.name = name;
        }

        public override string ToString()
        {
            return stage + " " + name;
        }
    }

    // Finds the shader storage blocks ("buffer" interface blocks) a compiled GLSL program declares, and which stage
    // declares each. Unity's GLES output is either one stage alone or several stages in one text, each wrapped in a
    // top level "#ifdef VERTEX" / "#ifdef FRAGMENT" section; text outside any such section belongs to the stage the
    // caller asked the compiler for.
    public static class GlslStorageBlocks
    {
        static readonly Regex blockPattern = new Regex(
            @"(?<![\w.])(?:layout\s*\([^)]*\)\s*)?(?:(?:readonly|writeonly|coherent|volatile|restrict|highp|mediump|lowp)\s+)*buffer\s+([A-Za-z_]\w*)\s*\{",
            RegexOptions.Compiled);
        static readonly Regex sectionOpen = new Regex(@"^\s*#\s*(?:ifdef\s+(\w+)|if\s+defined\s*\(\s*(\w+)\s*\)\s*$)");
        static readonly Regex conditionalOpen = new Regex(@"^\s*#\s*if");
        static readonly Regex conditionalClose = new Regex(@"^\s*#\s*endif\b");

        public static List<GlslStorageBlock> Find(string glsl, GlslStage requestedStage)
        {
            List<GlslStorageBlock> blocks = new List<GlslStorageBlock>();
            if (string.IsNullOrEmpty(glsl))
            {
                return blocks;
            }

            foreach (KeyValuePair<GlslStage, string> section in Split(StripComments(glsl), requestedStage))
            {
                foreach (Match match in blockPattern.Matches(section.Value))
                {
                    GlslStorageBlock block = new GlslStorageBlock(section.Key, match.Groups[1].Value);
                    if (!blocks.Contains(block))
                    {
                        blocks.Add(block);
                    }
                }
            }

            return blocks;
        }

        public static GlslStage StageOfSection(string define)
        {
            switch (define)
            {
                case "VERTEX":
                    return GlslStage.Vertex;
                case "FRAGMENT":
                    return GlslStage.Fragment;
                default:
                    return GlslStage.Other;
            }
        }

        // Pairs of stage and the text belonging to it, in order. Only a top level #ifdef naming a stage opens a
        // section; conditionals nested inside it stay part of its text.
        public static List<KeyValuePair<GlslStage, string>> Split(string glsl, GlslStage requestedStage)
        {
            List<KeyValuePair<GlslStage, string>> sections = new List<KeyValuePair<GlslStage, string>>();
            StringBuilder outside = new StringBuilder();
            StringBuilder inside = null;
            GlslStage insideStage = requestedStage;
            int depth = 0;
            int sectionDepth = -1;

            foreach (string line in glsl.Split('\n'))
            {
                if (inside == null)
                {
                    Match open = sectionOpen.Match(line);
                    string define = open.Success ? (open.Groups[1].Success ? open.Groups[1].Value : open.Groups[2].Value) : null;
                    if (depth == 0 && define != null && IsStageDefine(define))
                    {
                        inside = new StringBuilder();
                        insideStage = StageOfSection(define);
                        sectionDepth = depth;
                        depth++;
                        continue;
                    }
                }

                if (conditionalOpen.IsMatch(line))
                {
                    depth++;
                }
                else if (conditionalClose.IsMatch(line))
                {
                    depth--;
                    if (inside != null && depth == sectionDepth)
                    {
                        sections.Add(new KeyValuePair<GlslStage, string>(insideStage, inside.ToString()));
                        inside = null;
                        continue;
                    }
                }

                (inside ?? outside).Append(line).Append('\n');
            }

            if (inside != null)
            {
                sections.Add(new KeyValuePair<GlslStage, string>(insideStage, inside.ToString()));
            }

            if (outside.ToString().Trim().Length > 0)
            {
                sections.Insert(0, new KeyValuePair<GlslStage, string>(requestedStage, outside.ToString()));
            }

            return sections;
        }

        static bool IsStageDefine(string define)
        {
            return define == "VERTEX" || define == "FRAGMENT" || define == "GEOMETRY" || define == "HULL" ||
                   define == "DOMAIN";
        }

        public static string StripComments(string glsl)
        {
            string noBlocks = Regex.Replace(glsl, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            return Regex.Replace(noBlocks, @"//[^\n]*", "");
        }

        // The compiled program blob can carry a binary header ahead of the GLSL; keep only printable text and line
        // breaks so the parser sees the source.
        public static string TextOf(byte[] data)
        {
            if (data == null)
            {
                return "";
            }

            StringBuilder text = new StringBuilder(data.Length);
            foreach (byte b in data)
            {
                if (b == '\n' || b == '\t' || (b >= 32 && b < 127))
                {
                    text.Append((char)b);
                }
                else if (b == '\r')
                {
                    continue;
                }
                else
                {
                    text.Append('\n');
                }
            }

            return text.ToString();
        }
    }
}
