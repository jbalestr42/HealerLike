using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // Adapt the live cards; their models and gameplay names remain owned by Toolkit.
    public sealed class StageIconLabels : System.IDisposable
    {
        readonly Dictionary<string, string> _names = new Dictionary<string, string>();

        public void Update(VisualElement root)
        {
            if (root == null) return;
            VisualElement hud = root.Q("hud-root");
            hud?.EnableInClassList("render-has-spells", root.Q("spell-list")?.Q<Button>(className: "data-card") != null);
            hud?.EnableInClassList("render-has-creatures", root.Q("party-list")?.Q<Button>(className: "data-card") != null);
            root.Q("party-list")?.Query<Button>(className: "data-card").ForEach(FormatCreature);
            root.Q("spell-list")?.Query<Button>(className: "data-card").ForEach(FormatSpell);
        }

        void FormatCreature(Button card)
        {
            PlaceLabel(card);
            if (!(card.userData is ToolkitCardModel model)) return;
            string name = model.title ?? "";
            if (!_names.TryGetValue(name, out string readable))
            {
                readable = Regex.Replace(name, "([A-Z])([A-Z][a-z])", "$1 $2");
                readable = Regex.Replace(readable, "([a-z0-9])([A-Z])", "$1 $2");
                _names.Add(name, readable);
            }
            Label label = card.Q<Label>("card-title");
            if (label != null) label.text = readable;
        }

        void FormatSpell(Button card) { PlaceLabel(card); }

        void PlaceLabel(Button card)
        {
            VisualElement icon = card.Q("card-icon");
            Label label = card.Q<Label>("card-title");
            if (icon == null || label == null) return;
            VisualElement art = icon.Q("render-card-art");
            if (art == null)
            {
                art = new VisualElement { name = "render-card-art", pickingMode = PickingMode.Ignore };
                art.AddToClassList("render-card-art");
                icon.Insert(0, art);
                ToolkitCooldown cooldown = icon.Q<ToolkitCooldown>();
                if (cooldown != null) art.Add(cooldown);
            }
            // Keep the provider-owned grammar icon, including later cache invalidations.
            if (icon.style.backgroundImage.value.texture != null)
                art.style.backgroundImage = icon.style.backgroundImage;
            icon.style.backgroundImage = StyleKeyword.None;
            if (label.parent != icon) icon.Add(label);
        }

        public void Dispose() { _names.Clear(); }
    }
}
