using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Icons
{
    public class DataIconCatalogCompatibilityTests
    {
        [Test]
        public void Descriptor_ShippedCatalog_PreservesEveryBakedIdentity()
        {
            DataIconCatalog catalog = Resources.Load<DataIconCatalog>(DataIconService.CatalogResourcePath);
            Assert.IsNotNull(catalog);
            Assert.IsNotEmpty(catalog.entries);
            foreach (DataIconCatalogEntry entry in catalog.entries)
            {
                Assert.IsNotNull(entry);
                Assert.IsTrue(entry.source, "A catalog source is missing: " + entry.fingerprint);
                DataIconDescriptor descriptor = DataIconDescriptor.From(entry.source);
                Assert.AreEqual(entry.fingerprint, "3:" + descriptor.kind + ":" + descriptor.key,
                    entry.source.name);
            }
        }
    }
}
