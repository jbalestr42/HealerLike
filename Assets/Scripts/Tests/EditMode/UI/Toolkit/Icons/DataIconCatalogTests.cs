using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Icons
{

public class DataIconCatalogTests
{
    DataIconCatalog _catalog;
    readonly List<Object> _objects = new List<Object>();

    ObjectType CreateTracked<ObjectType>() where ObjectType : ScriptableObject
    {
        ObjectType created = ScriptableObject.CreateInstance<ObjectType>();
        _objects.Add(created);
        return created;
    }

    Texture2D CreateTexture()
    {
        Texture2D texture = new Texture2D(16, 16);
        _objects.Add(texture);
        return texture;
    }

    DataIconCatalogEntry AddEntry(Object source, Texture2D generated, Texture2D artworkOverride)
    {
        DataIconCatalogEntry entry = new DataIconCatalogEntry();
        entry.source = source;
        entry.generated = generated;
        entry.artworkOverride = artworkOverride;
        _catalog.entries.Add(entry);
        return entry;
    }

    [SetUp]
    public void SetUp()
    {
        _catalog = CreateTracked<DataIconCatalog>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object tracked in _objects)
        {
            Object.DestroyImmediate(tracked);
        }

        _objects.Clear();
    }

    [Test]
    public void FindDescriptor_RuntimeItemOfFactory_ReturnsFactoryArtwork()
    {
        ItemFactory factory = CreateTracked<ItemFactory>();
        factory.data = new ItemData { name = "Ward" };
        Texture2D artwork = CreateTexture();
        AddEntry(factory, null, artwork);

        Texture2D found = _catalog.FindDescriptor(DataIconDescriptor.From(factory.GetItem()));

        Assert.AreSame(artwork, found);
    }

    [Test]
    public void Find_NoOverride_ReturnsGenerated()
    {
        EntityData source = CreateTracked<EntityData>();
        Texture2D generated = CreateTexture();
        AddEntry(source, generated, null);

        Texture2D found = _catalog.Find(source);

        Assert.AreSame(generated, found);
    }

    [Test]
    public void Find_OverrideSet_ReturnsOverride()
    {
        EntityData source = CreateTracked<EntityData>();
        Texture2D artwork = CreateTexture();
        AddEntry(source, CreateTexture(), artwork);

        Texture2D found = _catalog.Find(source);

        Assert.AreSame(artwork, found);
    }

    [Test]
    public void Find_NullSource_ReturnsNull()
    {
        AddEntry(CreateTracked<EntityData>(), CreateTexture(), null);

        Texture2D found = _catalog.Find(null);

        Assert.IsNull(found);
    }

    [Test]
    public void FindDescriptor_EmptyMatchingEntry_StillFindsLaterArtwork()
    {
        ItemFactory first = CreateTracked<ItemFactory>();
        first.data = new ItemData { name = "Ward" };
        ItemFactory second = CreateTracked<ItemFactory>();
        second.data = first.data;
        Texture2D artwork = CreateTexture();
        AddEntry(first, null, null);
        AddEntry(second, null, artwork);

        Texture2D found = _catalog.FindDescriptor(DataIconDescriptor.From(first.GetItem()));

        Assert.AreSame(artwork, found);
    }

    [Test]
    public void Find_EmptyMatchingEntry_StillFindsLaterTexture()
    {
        EntityData source = CreateTracked<EntityData>();
        Texture2D generated = CreateTexture();
        AddEntry(source, null, null);
        AddEntry(source, generated, null);

        Texture2D found = _catalog.Find(source);

        Assert.AreSame(generated, found);
    }
}
}
