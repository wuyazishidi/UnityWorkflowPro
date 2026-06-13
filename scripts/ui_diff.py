# -*- coding: utf-8 -*-
"""
渲染图 vs Figma 真值图的分区域 MAE 报告（spec 004 Phase 1 的 -Verify 用）。
truth 由 figma-pull 导出的卡片节点（与渲染同框），故直接缩放对齐即可。

用法: python scripts/ui_diff.py <render.png> <truth.png>
"""
import sys, io


def main():
    if len(sys.argv) < 3:
        print("usage: ui_diff.py <render.png> <truth.png>"); return 2
    sys.stdout.reconfigure(encoding="utf-8")
    render_p, truth_p = sys.argv[1], sys.argv[2]
    try:
        from PIL import Image
        import numpy as np
    except Exception as e:
        print(f"[diff] 跳过（缺 PIL/numpy）: {e}"); return 0
    import os
    if not (os.path.exists(render_p) and os.path.exists(truth_p)):
        print(f"[diff] 跳过：缺图 render={os.path.exists(render_p)} truth={os.path.exists(truth_p)}"); return 0

    r = Image.open(render_p).convert("RGB")
    t = Image.open(truth_p).convert("RGB").resize(r.size, Image.LANCZOS)
    ra = np.asarray(r).astype(int); ta = np.asarray(t).astype(int)
    H, W, _ = ra.shape
    glob = float(np.abs(ra - ta).mean())
    print(f"[diff] 全局 MAE = {glob:.1f} (0-255，越小越像)")
    # 3x3 分区域，定位差异在哪块
    print("[diff] 分区域 MAE (3x3):")
    for gy in range(3):
        row = []
        for gx in range(3):
            y0, y1 = gy * H // 3, (gy + 1) * H // 3
            x0, x1 = gx * W // 3, (gx + 1) * W // 3
            m = float(np.abs(ra[y0:y1, x0:x1] - ta[y0:y1, x0:x1]).mean())
            row.append(f"{m:5.1f}")
        print("   " + " ".join(row))
    # 提示阈值（经验值，仅参考；角落因渲染背景色不同会偏大）
    if glob > 25:
        print("[diff] 注意：全局 MAE 偏大，建议人工核对 _render.png 与 truth.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
