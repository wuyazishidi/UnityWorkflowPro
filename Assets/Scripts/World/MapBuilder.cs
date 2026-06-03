using System.Collections.Generic;
using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// 运行时生成一张矩形地图：地板 + 四周带碰撞体的墙。
    /// 墙体坐标计算抽为纯函数 <see cref="GenerateBorderWalls"/>，便于 EditMode 单测。
    /// </summary>
    public static class MapBuilder
    {
        /// <summary>纯函数：返回 width×height 网格的边界格坐标（含四边与四角）。</summary>
        public static List<Vector2Int> GenerateBorderWalls(int width, int height)
        {
            var cells = new List<Vector2Int>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool isBorder = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                    if (isBorder) cells.Add(new Vector2Int(x, y));
                }
            }
            return cells;
        }

        /// <summary>格坐标 → 居中世界坐标（地图中心对齐原点）。</summary>
        public static Vector2 CellToWorld(Vector2Int cell, int width, int height)
        {
            return new Vector2(cell.x - (width - 1) * 0.5f, cell.y - (height - 1) * 0.5f);
        }

        /// <summary>运行时实例化地板与墙体（带 BoxCollider2D）。</summary>
        public static void Build(int width, int height, Sprite square)
        {
            var root = new GameObject("Map");

            // 地板（背景，无碰撞）
            var floor = new GameObject("Floor");
            floor.transform.SetParent(root.transform);
            var fsr = floor.AddComponent<SpriteRenderer>();
            fsr.sprite = square;
            fsr.color = new Color(0.22f, 0.24f, 0.30f);
            fsr.sortingOrder = -10;
            floor.transform.localScale = new Vector3(width, height, 1f);
            floor.transform.position = Vector3.zero;

            // 墙体
            foreach (var cell in GenerateBorderWalls(width, height))
            {
                var wall = new GameObject($"Wall_{cell.x}_{cell.y}");
                wall.transform.SetParent(root.transform);
                var wsr = wall.AddComponent<SpriteRenderer>();
                wsr.sprite = square;
                wsr.color = new Color(0.45f, 0.45f, 0.5f);
                wsr.sortingOrder = 0;
                Vector2 wp = CellToWorld(cell, width, height);
                wall.transform.position = new Vector3(wp.x, wp.y, 0f);
                wall.AddComponent<BoxCollider2D>();
            }
        }
    }
}
