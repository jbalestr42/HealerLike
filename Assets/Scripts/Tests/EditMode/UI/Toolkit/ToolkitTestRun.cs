using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Toolkit
{

    // A run for the Toolkit panels, without the singletons: a GameManager for the screens (the board open), a player
    // and an AscensionGameType put in a state by hand, none of them started
    public class ToolkitTestRun : IDisposable
    {
        public readonly GameObject root;
        public readonly GameManager game;
        public readonly PlayerBehaviour player;
        public readonly AscensionGameType ascension;
        public readonly ToolkitGameContext context;

        public ToolkitTestRun()
        {
            root = new GameObject("Toolkit test run");
            game = root.AddComponent<GameManager>();
            player = root.AddComponent<PlayerBehaviour>();
            ascension = root.AddComponent<AscensionGameType>();
            TestHelpers.SetPrivateField(ascension, "_game", game);
            TestHelpers.SetPrivateField(ascension, "_player", player);
            game.screens.Open(GameScreen.Game);
            context = new ToolkitGameContext { game = game, player = player, ascension = ascension };
        }

        public void SetState(AscensionGameType.State state)
        {
            TestHelpers.SetPrivateField(ascension, "_state", state);
        }

        // The event screen open with these choices
        public void ShowEvent(string title, string description, params EventChoice[] choices)
        {
            SetState(AscensionGameType.State.PlayEvent);
            ascension.ShowChoices(title, description, choices);
        }

        // The reward screen open with these choices
        public void ShowRewards(params RewardChoice[] choices)
        {
            SetState(AscensionGameType.State.SelectUpgrade);
            TestHelpers.SetPrivateField(ascension, "_rewardChoices", new List<RewardChoice>(choices));
            game.screens.Open(GameScreen.Reward);
        }

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
