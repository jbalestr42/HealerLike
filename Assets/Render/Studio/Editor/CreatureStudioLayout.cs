using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Studio.Editor
{
    // Owns the creature workbench panes and their preview/inspector resources.
    public class CreatureStudioLayout : System.IDisposable
    {
        static readonly Color accent = new Color(0.75f, 0.68f, 1f);
        static readonly Color highlight = new Color(0.22f, 0.18f, 0.32f);
        static readonly Color smallText = new Color(0.62f, 0.68f, 0.75f);
        static readonly Color cardText = new Color(0.85f, 0.88f, 0.94f);
        static readonly float libraryWidth = 210f;
        static readonly float inspectorWidth = 370f;
        static readonly float top = 77f;

        readonly StudioStyles _styles = new StudioStyles();
        readonly CreatureLibraryPane _library = new CreatureLibraryPane();
        readonly CreaturePartsInspector _parts = new CreaturePartsInspector();
        readonly CreatureGrammarInspector _grammarInspector = new CreatureGrammarInspector();
        readonly CreatureViewport _viewport = new CreatureViewport();
        readonly CreatureStudioHeader _header = new CreatureStudioHeader();
        readonly CreatureRosterPane _roster = new CreatureRosterPane();
        CreatureStudioWindow _window;

        public StudioStyles styles { get { return _styles; } }
        public CreatureLibraryPane library { get { return _library; } }
        public CreaturePartsInspector parts { get { return _parts; } }
        public CreatureGrammarInspector grammarInspector { get { return _grammarInspector; } }
        public CreatureViewport viewport { get { return _viewport; } }
        public CreatureRosterPane roster { get { return _roster; } }

        public void Init(CreatureStudioWindow window)
        {
            _window = window;
            _viewport.Init(window);
            _header.Init(window);
            _library.Init(window);
            _parts.Init(window);
            _grammarInspector.Init(window);
        }

        public void Dispose()
        {
            _roster.Dispose();
            _viewport.Dispose();
        }

        public void Draw()
        {
            if (!_styles.isReady)
            {
                _styles.Init(accent, highlight, smallText, cardText);
            }

            _viewport.HandleShortcuts();
            StudioStyles.DrawBackground(_window.position);
            _header.Draw(_window.position);
            float height = _window.position.height - 87f;
            if (_window.isRosterMode)
            {
                _roster.Draw(new Rect(10f, top, _window.position.width - 20f, height));
                return;
            }

            Rect library = new Rect(10f, top, libraryWidth, height);
            Rect inspector = new Rect(_window.position.width - inspectorWidth - 10f, top, inspectorWidth, height);
            if (_window.isGrammarMode)
            {
                _library.DrawGrammar(library);
            }
            else
            {
                _library.DrawParts(library);
            }

            _viewport.Draw(new Rect(libraryWidth + 18f, top,
                _window.position.width - libraryWidth - inspectorWidth - 36f,
                height));
            if (_window.isGrammarMode)
            {
                _grammarInspector.Draw(inspector);
            }
            else
            {
                _parts.Draw(inspector);
            }

            if (GUI.changed)
            {
                _window.Repaint();
            }
        }
    }
}
