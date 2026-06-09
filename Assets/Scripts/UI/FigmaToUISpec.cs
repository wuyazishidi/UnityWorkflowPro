using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 纯函数：把 Figma 文件 JSON（GET /v1/files/:key 的返回）里指定 Frame 转成 UISpec。
    /// 只读取我们需要的字段（type/name/absoluteBoundingBox/fills/characters/style/children）。
    /// 不依赖 Unity 资源；可 EditMode 测。图片填充(IMAGE fill)暂记为占位色，切图下载留待后续。
    /// </summary>
    public static class FigmaToUISpec
    {
        public static UISpecParseResult Convert(string figmaJson, string frameName)
        {
            var result = new UISpecParseResult();
            JObject root;
            try { root = JObject.Parse(figmaJson); }
            catch (Exception e) { result.Errors.Add("Figma JSON 解析失败: " + e.Message); return result; }

            var frame = FindFrame(root["document"], frameName);
            if (frame == null) { result.Errors.Add($"找不到 Frame '{frameName}'"); return result; }

            var fb = frame["absoluteBoundingBox"];
            if (fb == null) { result.Errors.Add("Frame 缺少 absoluteBoundingBox"); return result; }
            float fx = (float)fb["x"], fy = (float)fb["y"];
            int fw = Mathf.RoundToInt((float)fb["width"]);
            int fh = Mathf.RoundToInt((float)fb["height"]);

            var rootNode = new UINode
            {
                name = SafeName(frame, "Panel"),
                type = "Container",
                anchorPreset = "center",
                rect = new UIRect(0, 0, fw, fh),
            };

            // Frame 自身填充作为底板背景
            var frameFill = SolidFillHex(frame);
            if (frameFill != null)
            {
                rootNode.children.Add(new UINode
                {
                    name = "Bg",
                    type = "Image",
                    color = frameFill,
                    raycastTarget = false,
                    rect = new UIRect(0, 0, fw, fh),
                });
            }

            foreach (var child in Children(frame))
            {
                var node = MapNode(child, fx, fy);
                if (node != null) rootNode.children.Add(node);
            }

            var spec = new UISpec
            {
                schemaVersion = 1,
                referenceWidth = fw,
                referenceHeight = fh,
                rootName = rootNode.name,
                root = rootNode,
            };

            var errors = UISpecValidator.Validate(spec);
            if (errors.Count > 0) { result.Errors.AddRange(errors); return result; }

            result.Ok = true;
            result.Spec = spec;
            return result;
        }

        private static UINode MapNode(JToken n, float fx, float fy)
        {
            if (n == null) return null;
            var type = (string)n["type"];
            if (type == null) return null;
            if (Hidden(n)) return null;

            var rect = RectOf(n, fx, fy);
            if (rect == null) return null;

            switch (type)
            {
                case "TEXT":
                    return new UINode
                    {
                        name = SafeName(n, "Text"),
                        type = "Text",
                        rect = rect,
                        text = TextOf(n),
                    };

                case "RECTANGLE":
                case "VECTOR":
                case "ELLIPSE":
                    return new UINode
                    {
                        name = SafeName(n, "Image"),
                        type = "Image",
                        rect = rect,
                        color = SolidFillHex(n) ?? "#FFFFFF",
                    };

                case "FRAME":
                case "GROUP":
                case "COMPONENT":
                case "INSTANCE":
                {
                    // 有填充 → 当作图片块（按钮底/卡片）；否则纯容器
                    var fill = SolidFillHex(n);
                    var node = new UINode
                    {
                        name = SafeName(n, "Group"),
                        type = fill != null ? "Image" : "Container",
                        rect = rect,
                        color = fill,
                    };
                    foreach (var c in Children(n))
                    {
                        var cn = MapNode(c, fx, fy);
                        if (cn != null) node.children.Add(cn);
                    }
                    return node;
                }

                default:
                    return null;
            }
        }

        // ---------- helpers ----------

        private static JToken FindFrame(JToken node, string frameName)
        {
            if (node == null) return null;
            if ((string)node["type"] == "FRAME" && (string)node["name"] == frameName)
                return node;
            foreach (var c in Children(node))
            {
                var found = FindFrame(c, frameName);
                if (found != null) return found;
            }
            return null;
        }

        private static IEnumerable<JToken> Children(JToken node)
        {
            if (node?["children"] is JArray arr) return arr;
            return System.Array.Empty<JToken>();
        }

        private static bool Hidden(JToken n)
        {
            var v = n["visible"];
            return v != null && v.Type == JTokenType.Boolean && !(bool)v;
        }

        private static UIRect RectOf(JToken n, float fx, float fy)
        {
            var b = n["absoluteBoundingBox"];
            if (b == null || b.Type == JTokenType.Null) return null;
            return new UIRect((float)b["x"] - fx, (float)b["y"] - fy, (float)b["width"], (float)b["height"]);
        }

        private static string SolidFillHex(JToken n)
        {
            if (!(n["fills"] is JArray fills)) return null;
            foreach (var f in fills)
            {
                if ((string)f["type"] != "SOLID") continue;
                if (f["visible"] != null && f["visible"].Type == JTokenType.Boolean && !(bool)f["visible"]) continue;
                var c = f["color"];
                if (c == null) continue;
                int r = To255(c["r"]), g = To255(c["g"]), b = To255(c["b"]);
                return $"#{r:X2}{g:X2}{b:X2}";
            }
            return null;
        }

        private static UIText TextOf(JToken n)
        {
            var style = n["style"];
            float fontSize = style?["fontSize"] != null ? (float)style["fontSize"] : 16f;
            string align = AlignOf(style);
            string color = SolidFillHex(n) ?? "#1F2A37";
            return new UIText
            {
                content = (string)n["characters"] ?? "",
                fontSize = fontSize,
                color = color,
                alignment = align,
            };
        }

        private static string AlignOf(JToken style)
        {
            var h = (string)style?["textAlignHorizontal"];
            switch (h)
            {
                case "LEFT": return "Left";
                case "RIGHT": return "Right";
                case "CENTER":
                default: return "Center";
            }
        }

        private static int To255(JToken c)
        {
            if (c == null) return 255;
            return Mathf.Clamp(Mathf.RoundToInt((float)c * 255f), 0, 255);
        }

        private static string SafeName(JToken n, string fallback)
        {
            var name = (string)n["name"];
            return string.IsNullOrWhiteSpace(name) ? fallback : name;
        }
    }
}
