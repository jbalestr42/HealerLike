using NUnit.Framework;

namespace Game.Map
{

public class MapNodeTests
{
    [Test]
    public void Connect_LinksBothWays()
    {
        MapNode node = new MapNode(0, 1, MapNodeType.Combat);
        MapNode next = new MapNode(1, 2, MapNodeType.Rest);

        node.Connect(next);

        Assert.IsTrue(node.IsConnectedTo(next));
        Assert.IsFalse(next.IsConnectedTo(node));
        CollectionAssert.AreEqual(new[] { next }, node.next);
        CollectionAssert.AreEqual(new[] { node }, next.previous);
    }

    [Test]
    public void Connect_Twice_DoesNotDuplicateTheEdge()
    {
        MapNode node = new MapNode(0, 1, MapNodeType.Combat);
        MapNode next = new MapNode(1, 1, MapNodeType.Combat);

        node.Connect(next);
        node.Connect(next);

        Assert.AreEqual(1, node.next.Count);
        Assert.AreEqual(1, next.previous.Count);
    }
}

}
