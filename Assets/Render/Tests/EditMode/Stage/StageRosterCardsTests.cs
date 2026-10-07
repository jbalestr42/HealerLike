using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace HealerLike.Render.Stage
{
    public class StageRosterCardsTests
    {
        static Button Card(bool canDrag, string status = "")
        {
            return new Button { userData = new ToolkitCardModel { key = "card", canDrag = canDrag, status = status } };
        }

        [Test]
        public void IsDeployable_CardModelCanDrag_True()
        {
            Assert.That(StageRosterCards.IsDeployable(Card(true)), Is.True);
        }

        [Test]
        public void IsDeployable_CardModelCannotDrag_False()
        {
            Assert.That(StageRosterCards.IsDeployable(Card(false)), Is.False);
        }

        [Test]
        public void IsDeployable_StatusSaysDeployButCardCannotDrag_False()
        {
            Assert.That(StageRosterCards.IsDeployable(Card(false, "Deploy")), Is.False);
        }

        [Test]
        public void IsDeployable_CanDragWithEmptyStatus_True()
        {
            Assert.That(StageRosterCards.IsDeployable(Card(true, "")), Is.True);
        }

        [Test]
        public void IsDeployable_DisabledCard_False()
        {
            Button card = Card(true);
            card.SetEnabled(false);

            Assert.That(StageRosterCards.IsDeployable(card), Is.False);
        }

        [Test]
        public void IsDeployable_CardWithoutModel_False()
        {
            Assert.That(StageRosterCards.IsDeployable(new Button()), Is.False);
        }

        [Test]
        public void IsDeployable_NullCard_False()
        {
            Assert.That(StageRosterCards.IsDeployable(null), Is.False);
        }

        [Test]
        public void Deployable_MixedCards_KeepsOnlyDraggableCardsInOrder()
        {
            Button first = Card(true);
            Button deployed = Card(false);
            Button second = Card(true);

            List<Button> found = StageRosterCards.Deployable(new List<Button> { first, deployed, second });

            Assert.That(found, Is.EqualTo(new List<Button> { first, second }));
        }
    }
}
