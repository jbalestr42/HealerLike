using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Icons
{
    public class DataIconSourceTests
    {
        class UnregisteredData
        {
            public string title = "A field does not opt into presentation";
        }

        class Source : IGameDataSource
        {
            public object sourceData { get; set; }
        }

        [Test]
        public void Label_NullSource_MatchesDescriptorFallback()
        {
            Assert.AreEqual(DataIconDescriptor.From(null).label, DataIconSource.Label(null));
        }

        [Test]
        public void From_UnknownTypeWithoutContract_UsesDeterministicTypeName()
        {
            DataIconDescriptor descriptor = DataIconDescriptor.From(new UnregisteredData());

            Assert.AreEqual(nameof(UnregisteredData), descriptor.label);
            Assert.AreEqual(DataIconKind.Data, descriptor.kind);
            Assert.IsNull(DataIconService.TryGetAuthoredSprite(new UnregisteredData()));
        }

        [Test]
        public void From_UnknownAssetWithoutContract_UsesObjectName()
        {
            DataIconCatalog asset = ScriptableObject.CreateInstance<DataIconCatalog>();
            try
            {
                asset.name = "Custom catalog";
                Assert.AreEqual(asset.name, DataIconDescriptor.From(asset).label);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Unwrap_NullData_PreservesOwnerFallback()
        {
            Source source = new Source();

            Assert.AreSame(source, DataIconSource.Unwrap(source));
            Assert.AreEqual(nameof(Source), DataIconDescriptor.From(source).label);
        }

        [Test]
        public void From_ReplacedData_UsesCurrentIdentityWithoutCachingSourceState()
        {
            Source source = new Source { sourceData = new BaseItemData { name = "Old" } };
            DataIconDescriptor previous = DataIconDescriptor.From(source);
            BaseItemData current = new BaseItemData { name = "Current" };
            source.sourceData = current;

            DataIconDescriptor descriptor = DataIconDescriptor.From(source);

            Assert.AreEqual(DataIconDescriptor.From(current).key, descriptor.key);
            Assert.AreNotEqual(previous.key, descriptor.key);
        }
    }
}
