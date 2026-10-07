using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Toolkit
{

    // Every screen canvas of Julien's UI, in the scenes the game ships, is either replaced by a Toolkit panel or
    // deliberately left on screen. A screen he adds lands in neither and fails here with its type name
    public class ToolkitScreenCensusTests
    {
        enum Disposition
        {
            // A Toolkit panel draws this screen, so ToolkitLegacyCanvases hides his canvas
            Replaced,
            // No Toolkit counterpart: his canvas must stay visible and clickable or the run gets stuck
            Exempt,
        }

        // The registry is the point of this test: it is the list a human has to touch when Julien adds a screen.
        // Adding a line means deciding, for his new view, whether a Toolkit panel draws it (then write that panel
        // first) or whether his canvas stays (then make ToolkitLegacyCanvases leave it alone). A test that merely
        // passed after a new view would let that view vanish behind the Toolkit with no choice on screen, which
        // is what happened to EventView. Keys are the owning view's type name, or the menu controller's for the
        // menu canvas that has no view.
        static readonly Dictionary<string, Disposition> Registry = new Dictionary<string, Disposition>
        {
            { nameof(GameView), Disposition.Replaced },
            { nameof(UpgradeView), Disposition.Replaced },
            { nameof(MapView), Disposition.Replaced },
            { nameof(GameOverView), Disposition.Replaced },
            { nameof(EventView), Disposition.Exempt },
            { nameof(MainMenu), Disposition.Replaced },
        };

        static readonly string[] Scenes = { "Assets/Scenes/Main.unity", "Assets/Scenes/MenuScene.unity" };

        Scene _scene;
        bool _opened;

        [TearDown]
        public void TearDown()
        {
            if (_opened && _scene.IsValid())
            {
                EditorSceneManager.CloseScene(_scene, true);
            }

            _opened = false;
        }

        // Additive, so the scene the Editor has open stays; a scene already open is used as it is and left open
        Scene Open(string path)
        {
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded)
            {
                _opened = false;
                return loaded;
            }

            _opened = true;
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        static List<T> FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToList();
        }

        // The roots ToolkitLegacyCanvases scopes its pass to: his UIManager, or the menu controllers without one
        static List<GameObject> LegacyRoots(Scene scene)
        {
            List<UIManager> managers = FindInScene<UIManager>(scene);
            if (managers.Count > 0)
            {
                return managers.Select(manager => manager.gameObject).ToList();
            }

            return FindInScene<MainMenu>(scene).Select(menu => menu.gameObject).ToList();
        }

        // The view a canvas belongs to: the AView above it, or else the single AView inside it. A canvas holding
        // no view is named by the menu controller above it, or by its path when nothing claims it
        static string Owner(Canvas canvas)
        {
            AView view = canvas.GetComponentInParent<AView>(true);
            if (view == null)
            {
                AView[] inside = canvas.GetComponentsInChildren<AView>(true);
                if (inside.Length == 1)
                {
                    view = inside[0];
                }
                else if (inside.Length > 1)
                {
                    return string.Join("+", inside.Select(child => child.GetType().Name).Distinct().OrderBy(name => name));
                }
            }

            if (view != null)
            {
                return view.GetType().Name;
            }

            MainMenu menu = canvas.GetComponentInParent<MainMenu>(true);
            return menu != null ? nameof(MainMenu) : $"no view at {Path(canvas.transform)}";
        }

        static string Path(Transform transform)
        {
            return transform.parent == null ? transform.name : $"{Path(transform.parent)}/{transform.name}";
        }

        [TestCaseSource(nameof(Scenes))]
        public void EveryScreenCanvas_IsReplacedOrExempt_AndTheHidingPassAgrees(string path)
        {
            _scene = Open(path);
            Assert.IsTrue(_scene.IsValid() && _scene.isLoaded, $"{path} did not open.");

            List<GameObject> roots = LegacyRoots(_scene);
            Assert.IsNotEmpty(roots, $"{path} has neither a UIManager nor a MainMenu: the census would see nothing.");
            List<Canvas> canvases = roots
                .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                .Where(canvas => canvas.renderMode != RenderMode.WorldSpace)
                .Distinct()
                .ToList();
            Assert.IsNotEmpty(canvases, $"{path} has no screen canvas under its UI root: the census would see nothing.");

            List<string> unregistered = canvases
                .Select(Owner)
                .Where(owner => !Registry.ContainsKey(owner))
                .Distinct()
                .ToList();
            Assert.IsEmpty(unregistered,
                $"{path}: Julien's UI has screen(s) the Toolkit registry does not know: {string.Join(", ", unregistered)}. "
                + "Decide for each whether a Toolkit panel replaces it or whether his canvas stays, and add it to "
                + "ToolkitScreenCensusTests.Registry.");

            Dictionary<Canvas, bool> before = canvases.ToDictionary(canvas => canvas, canvas => canvas.enabled);
            ToolkitGameContext context = new ToolkitGameContext();
            context.ui = FindInScene<UIManager>(_scene).FirstOrDefault();
            context.isMenu = FindInScene<GameManager>(_scene).Count == 0;
            ToolkitLegacyCanvases legacyCanvases = new ToolkitLegacyCanvases();
            List<string> drift = new List<string>();
            try
            {
                legacyCanvases.Hide(context);
                foreach (Canvas canvas in canvases)
                {
                    string owner = Owner(canvas);
                    if (Registry[owner] == Disposition.Exempt && canvas.enabled != before[canvas])
                    {
                        drift.Add($"{owner} is exempt but the hiding pass hid {Path(canvas.transform)}");
                    }
                    else if (Registry[owner] == Disposition.Replaced && canvas.enabled)
                    {
                        drift.Add($"{owner} is replaced but the hiding pass left {Path(canvas.transform)} on screen");
                    }
                }
            }
            finally
            {
                legacyCanvases.Restore();
            }

            Assert.IsEmpty(drift, $"{path}: the registry and ToolkitLegacyCanvases disagree: {string.Join("; ", drift)}");
            CollectionAssert.AreEqual(before.Values, canvases.Select(canvas => canvas.enabled),
                "Restore puts every canvas back as the scene had it.");
        }

        [Test]
        public void Registry_CoversEveryViewTypeOfTheGame()
        {
            // A view type that exists in his code but in neither scene still has to be decided
            List<string> views = typeof(AView).Assembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(AView)) && !type.IsAbstract)
                .Select(type => type.Name)
                .Where(name => !Registry.ContainsKey(name))
                .ToList();

            Assert.IsEmpty(views, $"View type(s) missing from ToolkitScreenCensusTests.Registry: {string.Join(", ", views)}");
        }
    }
}
