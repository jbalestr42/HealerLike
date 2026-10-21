using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // Capture automation uses the same room button as a player. It never advances RunState directly.
    public static class StageMapActions
    {
        public static IEnumerator WaitForSelection(StageInterfaceActions actions)
        {
            float deadline = Time.realtimeSinceStartup + 12f;
            while (!StageInterfaceOutput.IsVisible(actions.root.Q("map-panel")))
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new InvalidOperationException("The run never offered its Toolkit expedition map.");
                }

                yield return null;
            }

            yield return AStageRun.Wait(0.25f);
        }

        public static Button ButtonFor(StageInterfaceActions actions, MapNode node)
        {
            return actions.root.Q<Button>("map-node-" + node.floor + "-" + node.column);
        }

        // preferFight picks the first Combat or Elite room when one is offered: an Event room parks the run on
        // Julien's event screen rather than the map, which a caller looking for combat cannot get past
        public static IEnumerator SelectFirst(StageInterfaceActions actions, bool useTouch, bool preferFight = false)
        {
            yield return WaitForSelection(actions);
            AscensionGameType ascension = UnityEngine.Object.FindAnyObjectByType<AscensionGameType>();
            if (ascension == null || ascension.run == null || ascension.run.GetAvailableNodes().Count == 0)
            {
                throw new InvalidOperationException("No available expedition room.");
            }

            MapNode node = ascension.run.GetAvailableNodes()[0];
            if (preferFight)
            {
                node = ascension.run.GetAvailableNodes().FirstOrDefault(available => available.type == MapNodeType.Combat
                    || available.type == MapNodeType.Elite) ?? node;
            }

            Button button = ButtonFor(actions, node);
            yield return actions.BringIntoView(button);
            if (useTouch)
            {
                yield return actions.PointerTap(button);
            }
            else
            {
                actions.Submit(button);
            }

            yield return AStageRun.Wait(0.5f);
            if (ascension.run.currentNode != node)
            {
                throw new InvalidOperationException("Toolkit room activation did not travel to the selected room.");
            }

            Debug.Log("[StageMapActions] Selected " + node + " through Toolkit "
                + (useTouch ? "multi-frame touch input" : "navigation submit event"));
        }

        public static IEnumerator EnterCombat(StageInterfaceActions actions)
        {
            for (int room = 0; room < 32; room++)
            {
                AscensionGameType ascension = UnityEngine.Object.FindAnyObjectByType<AscensionGameType>();
                if (LegacyUiReader.AscensionState(ascension) == AscensionGameType.State.WaitForRoundToStart)
                {
                    yield break;
                }

                if (StageInterfaceOutput.IsVisible(actions.root.Q("upgrade-panel")))
                {
                    yield return actions.SelectCardByTouch(actions.Cards("upgrade-list")[0]);
                    yield return AStageRun.Wait(0.3f);
                }
                else
                {
                    yield return SelectFirst(actions, true, true);
                }
            }

            throw new InvalidOperationException("No combat preparation reached after map progression.");
        }
    }
}
