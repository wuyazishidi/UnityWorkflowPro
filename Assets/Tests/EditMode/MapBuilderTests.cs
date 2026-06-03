using NUnit.Framework;
using UnityEngine;
using Game.World;

namespace Game.Tests.EditMode
{
    /// <summary>M2：地图边界生成的纯逻辑单测（spec 005）。</summary>
    public class MapBuilderTests
    {
        [Test]
        public void Border_Of3x3_Has8Cells_CenterOpen()
        {
            var walls = MapBuilder.GenerateBorderWalls(3, 3);
            Assert.AreEqual(8, walls.Count);
            Assert.IsFalse(walls.Contains(new Vector2Int(1, 1)), "中心应为开放");
            Assert.IsTrue(walls.Contains(new Vector2Int(0, 0)), "角应为墙");
            Assert.IsTrue(walls.Contains(new Vector2Int(2, 2)), "角应为墙");
        }

        [Test]
        public void Border_Count_Matches_Perimeter()
        {
            // 周长公式：2w + 2h - 4
            var walls = MapBuilder.GenerateBorderWalls(20, 14);
            Assert.AreEqual(2 * 20 + 2 * 14 - 4, walls.Count);
        }

        [Test]
        public void CellToWorld_CentersMap()
        {
            // 20×14：左下角 (0,0) 应映射到 (-9.5, -6.5)
            Vector2 w = MapBuilder.CellToWorld(new Vector2Int(0, 0), 20, 14);
            Assert.That(w.x, Is.EqualTo(-9.5f).Within(1e-4f));
            Assert.That(w.y, Is.EqualTo(-6.5f).Within(1e-4f));
        }
    }
}
