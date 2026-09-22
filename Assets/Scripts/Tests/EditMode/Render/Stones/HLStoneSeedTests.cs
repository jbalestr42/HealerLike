using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneSeedTests
    {
        [TestCase(0,0,0,1251341186u)]
        [TestCase(1,2,3,1379180702u)]
        [TestCase(-1,-2,-3,1862517154u)]
        [TestCase(-2147483648,2147483647,-1,1668951140u)]
        public void PinnedWordFold(int seed,int x,int y,uint expected) => Assert.AreEqual(expected,HLStoneSeed.ForCell(seed,new Vector2Int(x,y)));
        [Test] public void SaltIsSingleFoldAndTraversalIndependent()
        {
            Assert.AreEqual(67918732u,HLStoneSeed.ForPart(2166136261u,1));
            uint a=HLStoneSeed.ForCell(3,new Vector2Int(-5,4));
            HLStoneSeed.ForCell(3,new Vector2Int(4,-5));
            Assert.AreEqual(a,HLStoneSeed.ForCell(3,new Vector2Int(-5,4)));
            Assert.AreNotEqual(a,HLStoneSeed.ForCell(4,new Vector2Int(-5,4)));
        }
    }
}
