using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static HealerLike.Render.Stage.AStageRun;
using HealerLike.Render.Creatures;
using UnityEngine.SceneManagement;

namespace HealerLike.Render.Stage
{
    public class StagePresentationPortraits
    {
        readonly StageCaptureSession _session;
        readonly StagePresentationOutput _output;
        public StagePresentationPortraits(StageCaptureSession session, StagePresentationOutput output)
        {
            _session = session;
            _output = output;
        }

        public IEnumerator Portraits(string prefix)
        {
            StageInterface attachment = _session.manager.GetComponent<StageInterface>();
            _output.Check(attachment.portraits != null && ReferenceEquals(_session.actions.ui.iconProvider,
                attachment.portraits), "Toolkit has the Render-owned creature portrait provider");
            yield return _session.actions.PointerTap("party-button");
            yield return Wait(0.2f);
            List<Button> cards = _session.actions.Cards("party-list");
            _output.Check(cards.Count > 0, "Party has actual creature cards");
            VisualElement icon = cards[0].Q("card-icon");
            Texture2D cardPortrait = icon.resolvedStyle.backgroundImage.texture;
            _output.Check(icon.ClassListContains("creature-portrait") && cardPortrait != null
                && cardPortrait.name.StartsWith("Creature Portrait ") && cardPortrait.width == 256
                && cardPortrait.height == 256, "Party card shows the 256px creature screenshot");
            CheckPartyLabel(cards[0], "card-title", prefix);
            CheckPartyLabel(cards[0], "card-status", prefix);
            _output.ExportPortrait(cardPortrait, prefix + "-cached-portrait");
            List<Camera> portraitCameras = PortraitCameras();
            _output.Check(portraitCameras.Count == 1,
                "Exactly one runtime portrait capture camera, including hidden objects; observed "
                + portraitCameras.Count);
            Camera portraitCamera = portraitCameras[0];
            _output.Check(portraitCamera.transform.parent != null
                && portraitCamera.transform.parent.name == CreaturePortraitRenderer.RootName
                && !portraitCamera.enabled && !portraitCamera.gameObject.activeInHierarchy,
                "Owned portrait camera remains disabled and inactive outside capture");
            int captures = attachment.portraits.captureCount;
            yield return Wait(0.45f);
            _output.Check(attachment.portraits.captureCount == captures
                && icon.resolvedStyle.backgroundImage.texture == cardPortrait,
                "Repeated UI refresh reuses the captured texture");
            yield return _session.Capture(prefix + "-party");
            yield return _session.actions.SelectCardByTouch(cards[0].parent.Q<Button>("card-info"));
            yield return Wait(0.25f);
            VisualElement detail = _session.actions.root.Q("detail-icon");
            _output.Check(detail != null && detail.ClassListContains("creature-portrait")
                && detail.resolvedStyle.backgroundImage.texture == cardPortrait,
                "Expanded detail reuses the exact creature screenshot");
            _output.Check(_session.interaction.GetInteraction() == null,
                "Inspecting a creature does not begin placement");
            if (Screen.width == 844 && Screen.height == 390)
            {
                CheckDetailLabel("detail-title", prefix, true);
                CheckDetailLabel("detail-description", prefix, false);
            }

            yield return _session.Capture(prefix + "-detail");
            attachment.RefreshCreatureIcons();
            yield return Wait(0.2f);
            Texture2D refreshed = detail.resolvedStyle.backgroundImage.texture;
            _output.Check(refreshed != null && refreshed != cardPortrait
                && refreshed.name.StartsWith("Creature Portrait "), "Vocabulary refresh replaces the visible portrait");
            cardPortrait = refreshed;
            yield return _session.actions.PointerTap("detail-close-button");
            yield return Wait(0.2f);
            List<Button> spells = _session.actions.Cards("spell-list");
            _output.Check(spells.Count > 0, "Shipped spell cards provide non-creature coverage");
            VisualElement spellIcon = spells[0].Q("card-icon");
            _output.Check(spellIcon.resolvedStyle.backgroundImage.texture != null
                && !spellIcon.ClassListContains("creature-portrait"),
                "Non-creature spell keeps its normal icon fallback");
        }

        public void CheckPartyLabel(Button card, string name, string context)
        {
            Label label = card.Q<Label>(name);
            ScrollView scroll = card.GetFirstAncestorOfType<ScrollView>();
            VisualElement drawer = _session.actions.root.Q("party-panel");
            _output.Check(label != null && scroll != null && drawer != null, context + " party card provides "
                + name + " inside a ScrollView");
            Rect clip = Intersect(scroll.contentViewport.worldBound, _session.actions.root.worldBound);
            clip = Intersect(clip, drawer.worldBound);
            Rect bounds = label.worldBound;
            // IsVisible alone checks layout/display, not whether the scroll viewport actually clips the text.
            _output.manifest.checks.Add(context + " " + name + " bounds=" + bounds + ", visibleScrollClip=" + clip);
            _output.Check(StageInterfaceOutput.IsVisible(label) && !string.IsNullOrWhiteSpace(label.text)
                && clip.width > 0f && clip.height > 0f && bounds.xMin >= clip.xMin - 1f
                && bounds.yMin >= clip.yMin - 1f && bounds.xMax <= clip.xMax + 1f && bounds.yMax <= clip.yMax + 1f,
                context + " first party card " + name + " is fully inside the visible scroll viewport at "
                + Screen.width + "x" + Screen.height);
        }

        static Rect Intersect(Rect a, Rect b)
        {
            float x = Mathf.Max(a.xMin, b.xMin);
            float y = Mathf.Max(a.yMin, b.yMin);
            return new Rect(x, y, Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - x), Mathf.Max(0f, Mathf.Min(a.yMax,
                b.yMax) - y));
        }

        void CheckDetailLabel(string name, string context, bool requireFullHeight)
        {
            Label label = _session.actions.root.Q<Label>(name);
            ScrollView scroll = _session.actions.root.Q<ScrollView>("detail-scroll");
            VisualElement drawer = _session.actions.root.Q("detail-panel");
            _output.Check(label != null && scroll != null && drawer != null,
                context + " detail provides " + name + " inside its ScrollView");
            Rect clip = Intersect(scroll.contentViewport.worldBound, _session.actions.root.worldBound);
            clip = Intersect(clip, drawer.worldBound);
            Rect bounds = label.worldBound;
            float lineHeight = label.MeasureTextSize("Ag", 0f, VisualElement.MeasureMode.Undefined,
                0f, VisualElement.MeasureMode.Undefined).y;
            float requiredHeight = requireFullHeight ? bounds.height : Mathf.Min(bounds.height, lineHeight);
            _output.manifest.checks.Add(context + " " + name + " bounds=" + bounds + ", visibleScrollClip=" + clip
                + ", requiredVisibleHeight=" + requiredHeight);
            _output.Check(StageInterfaceOutput.IsVisible(label) && !string.IsNullOrWhiteSpace(label.text)
                && clip.width > 0f && clip.height > 0f && requiredHeight > 0f && bounds.xMin >= clip.xMin - 1f
                && bounds.xMax <= clip.xMax + 1f && bounds.yMin >= clip.yMin - 1f
                && bounds.yMin + requiredHeight <= clip.yMax + 1f,
                context + " detail " + name + " is readable inside the initial landscape scroll viewport");
        }

        public List<Camera> PortraitCameras()
        {
            List<Camera> cameras = new List<Camera>();
            // HideAndDontSave removes a camera internally from its Scene. Scene.IsValid therefore cannot
            // distinguish these owned runtime cameras from assets. Include hidden objects and reject assets.
            foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>())
            {
                if (camera.name != "Portrait Camera" || EditorUtility.IsPersistent(camera))
                {
                    continue;
                }

                cameras.Add(camera);
                _output.manifest.checks.Add("Observed portrait camera instance " + camera.GetEntityId()
                    + ", parent=" + (camera.transform.parent != null ? camera.transform.parent.name : "none")
                    + ", sceneValid=" + camera.gameObject.scene.IsValid() + ", flags=" + camera.gameObject.hideFlags
                    + ", enabled=" + camera.enabled + ", active=" + camera.gameObject.activeInHierarchy);
            }

            return cameras;
        }
    }
}
