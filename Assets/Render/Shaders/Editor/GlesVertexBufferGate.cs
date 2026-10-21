using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Shaders
{
    // OpenGL ES 3.1 guarantees zero shader storage blocks in the vertex stage, so a vertex shader that reads a
    // StructuredBuffer fails to link on many GLES phones. This compiles the listed shaders for GLES3x, reads the GLSL
    // the compiler produced and fails on any storage block the vertex stage declares. Fragment blocks are reported
    // for information only: fragment and compute stages are guaranteed at least four.
    //
    // Batchmode: Unity -batchmode -projectPath <p> -executeMethod HealerLike.Render.Shaders.GlesVertexBufferGate.Run
    //   [-glesGateReport <file>] [-glesGateDump <dir>]
    // Exit 0 clean, 1 a vertex storage block, 2 the check could not see the GLSL (compile error, unreadable output).
    public static class GlesVertexBufferGate
    {
        public struct Target
        {
            public string path;
            // Keyword sets compiled on top of the automatic ones (none, and each vertex keyword alone)
            public string[][] extraKeywordSets;

            public Target(string path, params string[][] extraKeywordSets)
            {
                this.path = path;
                this.extraKeywordSets = extraKeywordSets;
            }
        }

        // Add a shader here to put it under the gate.
        public static readonly Target[] targets =
        {
            new Target("Assets/Render/Shaders/GroundSimulation.shader"),
            new Target("Assets/Render/Shaders/Look.shader",
                new[] { "INSTANCING_ON", "HL_GRASS_INSTANCED" },
                new[] { "PROCEDURAL_INSTANCING_ON", "HL_GRASS_INSTANCED" }),
            new Target("Assets/Render/Shaders/GrassRing.shader"),
        };

        public const int exitClean = 0;
        public const int exitViolation = 1;
        public const int exitBlind = 2;

        public class Finding
        {
            public string shader;
            public string pass;
            public int passIndex;
            public GlslStage stage;
            public string block;
            public List<string> variants = new List<string>();
        }

        public class Result
        {
            public List<Finding> vertexBlocks = new List<Finding>();
            public List<Finding> otherBlocks = new List<Finding>();
            public List<string> blindSpots = new List<string>();
            public List<string> log = new List<string>();
            public int compiledVariants;

            public int exitCode
            {
                get
                {
                    if (blindSpots.Count > 0)
                    {
                        return exitBlind;
                    }

                    return vertexBlocks.Count > 0 ? exitViolation : exitClean;
                }
            }
        }

        [MenuItem("HealerLike/Render/GLES3 Vertex Storage Buffer Gate")]
        static void RunFromMenu()
        {
            Result result = Check(targets, null);
            Debug.Log(Report(result));
        }

        public static void Run()
        {
            int code = exitBlind;
            try
            {
                string reportPath = Argument("-glesGateReport") ?? "Logs/GlesVertexBufferGate.txt";
                Result result = Check(targets, Argument("-glesGateDump"));
                string report = Report(result);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
                File.WriteAllText(reportPath, report);
                Console.WriteLine(report);
                Debug.Log(report);
                code = result.exitCode;
            }
            catch (Exception exception)
            {
                Console.WriteLine("[GLES3 gate] crashed: " + exception);
                Debug.LogException(exception);
            }

            EditorApplication.Exit(code);
        }

        public static Result Check(IEnumerable<Target> checkedTargets, string dumpDirectory)
        {
            Result result = new Result();
            foreach (Target target in checkedTargets)
            {
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(target.path);
                if (shader == null)
                {
                    result.blindSpots.Add(target.path + ": no shader at this path");
                    continue;
                }

                ShaderData data = ShaderUtil.GetShaderData(shader);
                for (int sub = 0; sub < data.SubshaderCount; sub++)
                {
                    ShaderData.Subshader subshader = data.GetSubshader(sub);
                    for (int p = 0; p < subshader.PassCount; p++)
                    {
                        CheckPass(result, target, shader, sub, p, subshader.GetPass(p), dumpDirectory);
                    }
                }
            }

            return result;
        }

        static void CheckPass(Result result, Target target, Shader shader, int sub, int passIndex, ShaderData.Pass pass,
            string dumpDirectory)
        {
            string[] keywords = ShaderUtil.GetPassKeywords(shader, new PassIdentifier((uint)sub, (uint)passIndex),
                ShaderType.Vertex).Select(keyword => keyword.name).ToArray();
            string passName = string.IsNullOrEmpty(pass.Name) ? "<unnamed>" : pass.Name;
            result.log.Add(target.path + " pass " + passIndex + " " + passName + " vertex keywords: [" +
                           string.Join(" ", keywords) + "]");

            foreach (string[] variant in VariantsOf(keywords, target.extraKeywordSets))
            {
                string variantName = variant.Length == 0 ? "<none>" : string.Join("+", variant);
                // On GLES3x the vertex slot returns the whole linked program, each stage in its own "#ifdef VERTEX" /
                // "#ifdef FRAGMENT" section, and the fragment slot returns zero bytes (measured on 6000.6.0f1), so one
                // compile sees both stages and the sections tell them apart.
                ShaderData.VariantCompileInfo info = pass.CompileVariant(ShaderType.Vertex, variant,
                    ShaderCompilerPlatform.GLES3x, BuildTarget.Android);
                string where = target.path + " pass " + passIndex + " " + passName + " [" + variantName + "]";
                result.compiledVariants++;
                if (!info.Success)
                {
                    result.blindSpots.Add(where + ": compile failed: " + Messages(info));
                    continue;
                }

                string glsl = GlslStorageBlocks.TextOf(info.ShaderData);
                if (dumpDirectory != null)
                {
                    Directory.CreateDirectory(dumpDirectory);
                    File.WriteAllText(Path.Combine(dumpDirectory, Path.GetFileNameWithoutExtension(target.path) +
                        "_p" + passIndex + "_" + variantName.Replace('<', '_').Replace('>', '_') + ".glsl"), glsl);
                }

                if (!LooksLikeGlsl(glsl))
                {
                    result.blindSpots.Add(where + ": compiled " + (info.ShaderData?.Length ?? 0) +
                                          " bytes that do not read as GLSL");
                    continue;
                }

                // Text outside any stage section is attributed to the vertex stage, so an unexpected layout can only
                // over-report, never hide a vertex block.
                foreach (GlslStorageBlock block in GlslStorageBlocks.Find(glsl, GlslStage.Vertex))
                {
                    List<Finding> list = block.stage == GlslStage.Vertex ? result.vertexBlocks : result.otherBlocks;
                    Record(list, target.path, passName, passIndex, block, variantName);
                }
            }
        }

        public static bool LooksLikeGlsl(string text)
        {
            return text.Contains("#version") && text.Contains("main(");
        }

        // No keyword, each keyword alone, then the target's own combinations, without repeats.
        public static List<string[]> VariantsOf(string[] keywords, string[][] extraKeywordSets)
        {
            List<string[]> variants = new List<string[]> { new string[0] };
            foreach (string keyword in keywords)
            {
                variants.Add(new[] { keyword });
            }

            if (extraKeywordSets != null)
            {
                foreach (string[] extra in extraKeywordSets)
                {
                    // Only keywords the pass declares; the rest would silently compile the no keyword variant again.
                    string[] kept = extra.Where(keywords.Contains).ToArray();
                    if (kept.Length > 0 && !variants.Any(v => v.SequenceEqual(kept)))
                    {
                        variants.Add(kept);
                    }
                }
            }

            return variants;
        }

        public static void Record(List<Finding> list, string shader, string pass, int passIndex, GlslStorageBlock block,
            string variant)
        {
            Finding finding = list.FirstOrDefault(f =>
                f.shader == shader && f.passIndex == passIndex && f.stage == block.stage && f.block == block.name);
            if (finding == null)
            {
                finding = new Finding
                {
                    shader = shader, pass = pass, passIndex = passIndex, stage = block.stage, block = block.name
                };
                list.Add(finding);
            }

            if (!finding.variants.Contains(variant))
            {
                finding.variants.Add(variant);
            }
        }

        public static string Report(Result result)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("[GLES3 gate] GLES3x compiles: " + result.compiledVariants);
            foreach (string line in result.log)
            {
                report.AppendLine("[GLES3 gate]   " + line);
            }

            report.AppendLine("[GLES3 gate] VERTEX storage blocks (violations): " + result.vertexBlocks.Count);
            foreach (Finding finding in result.vertexBlocks)
            {
                report.AppendLine("[GLES3 gate]   VIOLATION " + Describe(finding));
            }

            report.AppendLine("[GLES3 gate] other-stage storage blocks (legal, not counted): " + result.otherBlocks.Count);
            foreach (Finding finding in result.otherBlocks)
            {
                report.AppendLine("[GLES3 gate]   allowed " + Describe(finding));
            }

            report.AppendLine("[GLES3 gate] blind spots: " + result.blindSpots.Count);
            foreach (string blind in result.blindSpots)
            {
                report.AppendLine("[GLES3 gate]   BLIND " + blind);
            }

            report.AppendLine("[GLES3 gate] exit " + result.exitCode);
            return report.ToString();
        }

        static string Describe(Finding finding)
        {
            return finding.shader + " pass " + finding.passIndex + " " + finding.pass + " " + finding.stage + " buffer " +
                   finding.block + " in " + string.Join(", ", finding.variants);
        }

        static string Messages(ShaderData.VariantCompileInfo info)
        {
            if (info.Messages == null || info.Messages.Length == 0)
            {
                return "no messages";
            }

            return string.Join(" | ", info.Messages.Select(m => m.severity + " " + m.message + " " + m.file + ":" + m.line));
        }

        static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
