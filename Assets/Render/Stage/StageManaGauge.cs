using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // Presents the existing mana resource without changing its gameplay ownership.
    public sealed class StageManaGauge
    {
        VisualElement _track;
        VisualElement _fill;

        public void Update(VisualElement root, ResourceAttribute mana)
        {
            if (root == null)
            {
                return;
            }

            if (_track == null || !root.Contains(_track))
            {
                VisualElement section = root.Q("spell-section");
                Label value = root.Q<Label>("mana-value");
                if (section == null || value == null)
                {
                    return;
                }

                _track = new VisualElement { name = "render-mana-gauge", pickingMode = PickingMode.Ignore };
                _fill = new VisualElement { name = "render-mana-fill", pickingMode = PickingMode.Ignore };
                _track.Add(_fill);
                _track.Add(value);
                section.Insert(0, _track);
            }

            _track.style.display = mana != null ? DisplayStyle.Flex : DisplayStyle.None;
            float fraction = mana != null && mana.Max > 0f ? Mathf.Clamp01(mana.Value / mana.Max) : 0f;
            _fill.style.width = Length.Percent(fraction * 100f);
        }
    }
}
