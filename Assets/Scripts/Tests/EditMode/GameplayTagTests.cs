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
    public void IsDescendantOf_BeyondSearchLimit_ReturnsFalse()
    {
        // searchLimit defaults to 4: root -> t1 -> t2 -> t3 -> t4 -> t5, so t5 is 5 levels below root
        GameplayTag root = CreateTag("Root");
        GameplayTag t1 = CreateTag("T1", root);
        GameplayTag t2 = CreateTag("T2", t1);
        GameplayTag t3 = CreateTag("T3", t2);
        GameplayTag t4 = CreateTag("T4", t3);
        GameplayTag t5 = CreateTag("T5", t4);

        Assert.IsFalse(t5.IsDescendantOf(root));
    }

    [Test]
    public void IsDescendantOf_WithinSearchLimit_ReturnsTrue()
    {
        GameplayTag root = CreateTag("Root");
        GameplayTag t1 = CreateTag("T1", root);
        GameplayTag t2 = CreateTag("T2", t1);
        GameplayTag t3 = CreateTag("T3", t2);
        GameplayTag t4 = CreateTag("T4", t3);

        Assert.IsTrue(t4.IsDescendantOf(root));
    }
}
