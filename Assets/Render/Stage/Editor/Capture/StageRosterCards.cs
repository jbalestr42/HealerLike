using System.Collections.Generic;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    // The roster cards a player can drag onto the board. The party panel sets no status text, so a label reading
    // "Deploy" can never match. The card model's canDrag is the one statement of "deployable": it is false for a
    // unit already on the board and outside preparation.
    public static class StageRosterCards
    {
        public const string ListName = "party-list";

        public static bool IsDeployable(Button card)
        {
            if (card == null || !card.enabledInHierarchy)
            {
                return false;
            }

            ToolkitCardModel model = card.userData as ToolkitCardModel;
            return model != null && model.canDrag;
        }

        public static List<Button> Deployable(List<Button> cards)
        {
            return cards.FindAll(IsDeployable);
        }

        public static List<Button> Deployable(StageInterfaceActions actions)
        {
            return Deployable(actions.Cards(ListName));
        }
    }
}
