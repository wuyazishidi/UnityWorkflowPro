---
description: 运行"完成的定义"门禁——编译闸门 + EditMode 测试，报告是否全绿
argument-hint: (无需参数)
---

执行 Definition of Done 门禁（对照 `specs/constitution.md` 第三条）。要求 Unity 编辑器已打开本工程。

依次运行并如实报告结果：

1. **编译闸门**：
   ```powershell
   powershell -ExecutionPolicy Bypass -Command "& '.\Packages\cn.etetet.yiuimcp\Config\compile-unity-flow.ps1' -Force 0 -NoWait 1"
   ```
   期望 `Result: Success, No errors!`。

2. **EditMode 测试**：
   ```powershell
   powershell -ExecutionPolicy Bypass -Command "& '.\scripts\dod.ps1'"
   ```
   （该脚本会先编译、再经 YIUIMCP 触发 `YIUIMCP/Run EditMode Tests` 并断言 `result=PASS`。）

3. 汇总：编译是否 Success、测试 passed/failed 数。**只要编译失败或 failed>0，就明确判定"未完成 (NOT DONE)"并给出失败输出**；全绿才判 DONE。

注意：若 Unity 未打开或 `.port`/服务异常导致超时，不要伪造通过，明确标注门禁未能执行。
