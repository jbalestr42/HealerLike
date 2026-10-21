using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using static HealerLike.Render.Stage.AStageRun;

namespace HealerLike.Render.Stage
{
    public sealed class StageCompactLayout
    {
        readonly StageCaptureSession _s;
        public StageCompactLayout(StageCaptureSession session) { _s = session; }
        public IEnumerator Check()
        {
            int longNames = 0;
            foreach (Button button in _s.actions.Cards("spell-list"))
            {
                Label label = button.Q<Label>("card-title");
                Vector2 measured = label.MeasureTextSize(label.text, label.contentRect.width,
                    VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                _s.output.Check(measured.y <= label.contentRect.height + 1,
                    "Actual spell name fits its compact label: " + label.text);
                if (label.text.Length > 12)
                {
                    longNames++;
                }
            }
            // The class this run starts with carries four spells, one of them long; the fit check above is only
            // worth something while at least one name has to wrap
            _s.output.Check(longNames >= 1, "An authored multiword spell name exercises a two-line label: " + longNames);
            ScrollView spells = _s.actions.root.Q<ScrollView>("spell-list");
            Rect mana = _s.actions.root.Q("mana-value").worldBound;
            Button first = _s.actions.Cards("spell-list")[0];
            float overflow = spells.contentContainer.layout.width - spells.contentViewport.layout.width;
            if (overflow > 1f)
            {
                yield return new StageCompactGestures(_s).Scroll(first);
                _s.output.Check(spells.scrollOffset.x > 0,
                    "Actual spell overflow scrolls through Toolkit pointer movement");
                _s.output.Check(mana == _s.actions.root.Q("mana-value").worldBound,
                    "Global mana remains fixed while spell row scrolls");
            }
            else
            {
                _s.output.manifest.checks.Add("Spell row does not overflow with " + _s.actions.Cards("spell-list").Count
                    + " spells (" + overflow.ToString("0.#") + " px); overflow scroll not exercised");
            }

            yield return _s.Capture("01c-long-spell-overflow");
            yield return _s.actions.BringIntoView(first);
            yield return Wait(0.1f);
        }
    }
}
