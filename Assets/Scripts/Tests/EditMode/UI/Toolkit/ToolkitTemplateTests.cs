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
            Assert.AreEqual(PickingMode.Position, root.Q("party-panel").pickingMode);
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
            Assert.IsTrue(root.Q("party-close-button").hierarchy.parent.ClassListContains("section-heading-actions"));
            Assert.IsTrue(root.Q("detail-close-button").hierarchy.parent.ClassListContains("section-heading-actions"));
            Assert.IsTrue(root.Q("resume-button").hierarchy.parent.ClassListContains("dialog-content"));
            Assert.IsTrue(root.Q("restart-button").hierarchy.parent.ClassListContains("dialog-content"));
            Assert.IsTrue(root.Q("upgrade-title").hierarchy.parent.ClassListContains("dialog-content"));
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
