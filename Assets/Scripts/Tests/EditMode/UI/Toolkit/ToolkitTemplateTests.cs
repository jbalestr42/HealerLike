using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;

namespace UI.Toolkit
{

    public class ToolkitTemplateTests
    {
        [Test]
        public void Validate_ComposedPlayerLayout_PreservesControlsAndPickingBoundaries()
        {
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            Assert.IsTrue(ToolkitLayoutContract.Validate(root));
            ToolkitTemplates.PreparePicking(root);
            Assert.AreEqual(PickingMode.Ignore, root.Q("world-space").pickingMode);
            Assert.AreEqual(PickingMode.Ignore, root.Q("field-toolbar").pickingMode);
            Assert.AreEqual(PickingMode.Ignore, root.Q("party-panel").pickingMode);
            Assert.AreEqual(PickingMode.Position, root.Q("pause-panel").pickingMode);
            Assert.AreEqual(PickingMode.Ignore, root.Q("detail-actions").pickingMode);
            TemplateContainer actions = root.Q<TemplateContainer>("detail-actions");
            DropdownField targeting = root.Q<DropdownField>("detail-targeting");
            string ancestry = DescribeHierarchy(targeting) + " | Action content: "
                + DescribeHierarchy(actions.contentContainer);
            Assert.AreSame(actions, targeting.parent, ancestry);
            Assert.AreSame(actions.Q("action-section-content"), actions.contentContainer, ancestry);
            Assert.AreSame(actions.contentContainer, targeting.hierarchy.parent, ancestry);
            Assert.IsTrue(root.Q("inventory-actions").Contains(root.Q("inventory-equip-button")));
            Assert.IsNull(root.Q("party-close-button"));
            Assert.IsTrue(root.Q("detail-panel").Contains(root.Q("detail-close-button")));
            Assert.IsTrue(root.Q("resume-button").hierarchy.parent.ClassListContains("dialog-content"));
            Assert.IsTrue(root.Q("restart-button").hierarchy.parent.ClassListContains("dialog-content"));
            Assert.IsTrue(root.Q("upgrade-title").hierarchy.parent.ClassListContains("dialog-content"));
        }

        // The start screen: the title and one line of copy, nothing that repeats it, over a backdrop it lets
        // through; Start above Sandbox in the same actions; its sheet on the HUD root so it wins over the stage's
        static readonly string MenuSheet = "Assets/Resources/UI/Toolkit/GameUI.Menu.uss";
        static readonly string MenuVeil = "Assets/Resources/UI/Toolkit/Menu/MenuVeil.png";

        [Test]
        public void Validate_MenuScreen_TitleAndOneLineOfCopyOverAnOpenBackdrop()
        {
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            Assert.IsTrue(ToolkitLayoutContract.Validate(root));
            ToolkitTemplates.PreparePicking(root);

            VisualElement menu = root.Q("menu-panel");
            Assert.AreEqual("A healer's\njourney", root.Q<Label>("menu-title").text);
            string tagline = root.Q<Label>("menu-tagline").text;
            StringAssert.DoesNotContain("\n", tagline);
            List<string> copy = menu.Query<Label>().ToList().ConvertAll(label => label.text);
            Assert.AreEqual(3, copy.Count, string.Join(" | ", copy));
            Assert.AreEqual(PickingMode.Ignore, menu.pickingMode, "Touches through the menu reach the meadow.");
            Assert.IsTrue(root.Q("menu-hero").ClassListContains("menu-hero"));
        }

        [Test]
        public void Validate_MenuActions_PrimaryStartBeforeSecondarySandbox()
        {
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();

            VisualElement actions = root.Q("encounter-actions");
            Button start = root.Q<Button>("start-button");
            Button sandbox = root.Q<Button>("sandbox-button");
            Assert.AreSame(actions, start.parent);
            Assert.AreSame(actions, sandbox.parent);
            Assert.Less(actions.IndexOf(start), actions.IndexOf(sandbox));
            Assert.IsTrue(start.ClassListContains("button--primary"));
            Assert.IsFalse(sandbox.ClassListContains("button--primary"));
        }

        [Test]
        public void MenuSheet_OnTheHudRoot_StylesTheMenuAndTheClassScreen()
        {
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(MenuSheet);
            Assert.IsNotNull(sheet, MenuSheet);

            Assert.IsTrue(root.Q("hud-root").styleSheets.Contains(sheet),
                "On the HUD root it outranks the stage's document-level HUD sheet.");
            string text = File.ReadAllText(MenuSheet);
            StringAssert.Contains("#hud-root.game-ui.is-menu", text);
            StringAssert.Contains(".class-list .data-card__details", text);
            StringAssert.Contains("Coiny-Regular-UI.ttf", text);
            Assert.IsTrue(root.Q("class-panel").Q(className: "class-dialog") != null, "The class screen opts in.");
        }

        // A 4 px wide ramp compressed in 4x4 blocks bands into visible steps; it ships uncompressed and unmipped
        [Test]
        public void MenuVeil_Import_IsUncompressedWithoutMips()
        {
            TextureImporter importer = AssetImporter.GetAtPath(MenuVeil) as TextureImporter;
            Assert.IsNotNull(importer, MenuVeil);

            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);
        }

        static string DescribeHierarchy(VisualElement element)
        {
            System.Text.StringBuilder path = new System.Text.StringBuilder();
            while (element != null)
            {
                if (path.Length > 0)
                {
                    path.Append(" <- ");
                }
                path.Append(element.GetType().Name).Append("(").Append(element.name).Append(")");
                element = element.parent;
            }
            return path.ToString();
        }

        [Test]
        public void Validate_MissingRequiredAction_ReportsFailureWithoutRepairingTree()
        {
            VisualElement root = Resources.Load<VisualTreeAsset>("UI/Toolkit/GameUI").CloneTree();
            root.Q("detail-targeting").RemoveFromHierarchy();
            LogAssert.Expect(LogType.Error, "[ToolkitTemplates] Required DropdownField 'detail-targeting' is missing.");
            Assert.IsFalse(ToolkitLayoutContract.Validate(root));
            Assert.IsNull(root.Q("detail-targeting"));
        }

        [Test]
        public void TryClone_AuthoredRootHasWrongType_RefusesBinding()
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>("UI/Toolkit/DataCard");
            LogAssert.Expect(LogType.Error, "[ToolkitTemplates] Required Button 'card-shell' is missing.");
            Assert.IsFalse(ToolkitTemplates.TryClone(template, "card-shell", out Button root));
            Assert.IsNull(root);
        }

        [Test]
        public void TryClone_TemplateOwnStylesheet_PreservesItOnExtractedRoot()
        {
            string folder = "Assets/ToolkitTemplateTest-" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            try
            {
                File.WriteAllText(folder + "/Card.uss", ".card { color: red; }");
                File.WriteAllText(
                    folder + "/Card.uxml",
                    "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"><Style src=\"Card.uss\"/>"
                        + "<ui:VisualElement name=\"card\" class=\"card\"/></ui:UXML>"
                );
                AssetDatabase.ImportAsset(folder + "/Card.uss", ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(folder + "/Card.uxml", ImportAssetOptions.ForceSynchronousImport);
                VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(folder + "/Card.uxml");
                StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(folder + "/Card.uss");
                Assert.IsTrue(ToolkitTemplates.TryClone(template, "card", out VisualElement root));
                Assert.IsTrue(root.styleSheets.Contains(style));
                Assert.IsNull(root.parent);
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
