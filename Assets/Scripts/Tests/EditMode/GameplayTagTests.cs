using NUnit.Framework;
using UnityEngine;

public class GameplayTagTests
{
    static GameplayTag CreateTag(string name, GameplayTag parent = null)
    {
        GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
        tag.name = name;
        if (parent != null)
        {
            TestHelpers.SetPrivateField(tag, "_parent", parent);
        }
        return tag;
    }

    [Test]
    public void IsDescendantOf_DirectParent_ReturnsTrue()
    {
        GameplayTag parent = CreateTag("Parent");
        GameplayTag child = CreateTag("Child", parent);

        Assert.IsTrue(child.IsDescendantOf(parent));
    }

    [Test]
    public void IsDescendantOf_Grandparent_ReturnsTrue()
    {
        GameplayTag grandparent = CreateTag("Grandparent");
        GameplayTag parent = CreateTag("Parent", grandparent);
        GameplayTag child = CreateTag("Child", parent);

        Assert.IsTrue(child.IsDescendantOf(grandparent));
    }

    [Test]
    public void IsDescendantOf_UnrelatedTag_ReturnsFalse()
    {
        GameplayTag a = CreateTag("A");
        GameplayTag b = CreateTag("B");

        Assert.IsFalse(a.IsDescendantOf(b));
    }

    [Test]
    public void IsDescendantOf_Self_ReturnsFalse()
    {
        GameplayTag tag = CreateTag("Tag");

        Assert.IsFalse(tag.IsDescendantOf(tag));
    }

    [Test]
    public void IsDescendantOf_RootTag_HasNoParent()
    {
        GameplayTag root = CreateTag("Root");

        Assert.IsNull(root.parent);
    }

    [Test]
    public void IsDescendantOf_TenLevelsDeep_ReturnsTrue()
    {
        GameplayTag root = CreateTag("Root");
        GameplayTag tag = root;
        for (int i = 1; i <= 10; i++)
        {
            tag = CreateTag($"T{i}", tag);
        }

        Assert.IsTrue(tag.IsDescendantOf(root));
    }

    [Test]
    public void IsDescendantOf_CircularParentChain_ReturnsFalseInsteadOfLoopingForever()
    {
        GameplayTag a = CreateTag("A");
        GameplayTag b = CreateTag("B", a);
        TestHelpers.SetPrivateField(a, "_parent", b);
        GameplayTag other = CreateTag("Other");

        Assert.IsFalse(a.IsDescendantOf(other));
    }
}
