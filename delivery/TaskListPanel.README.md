# TaskListPanel â€” UI delivery package (for YC-Ego)

## Import
1. Import `TaskListPanel.unitypackage` into the YC-Ego project (exported with .meta -> GUID-faithful).
2. First time only: Window > TextMeshPro > Import TMP Essential Resources
   (binding.json sets needsTmpEssentials=true as a reminder).

## How to bind (read only `TaskListPanel.binding.json` + the prefab; never reference this project)
Each bindable element has: `key` (stable handle), `path` (transform path inside the prefab, root excluded),
`type` (component kind), `text` (current text, for human cross-check).

Naming convention â€” every bindable node name carries a TYPE SUFFIX (also present in `path`):
`_Btn` (Button), `_InputField` (InputField), `_Dropdown` (Dropdown), `_Text` (Text).
`key` is that suffixed name camelized (e.g. `Return_Btn` -> `returnBtn`): it ENCODES the type and
never collides across types, so binding by `key` is stable and self-documenting.

Bind = look up by `key` -> locate via `path` -> GetComponent of `type` -> wire your event. e.g.
    root.transform.Find(e.path).GetComponent<Button>().onClick.AddListener(OnConfirm);

Flags to honor:
- `keyAuto:true`        the Figma node name was non-ASCII; `key` is a `<type><index>` fallback (unstable).
                        Rename the node in Figma to an ASCII name and re-publish for a stable `key`.
- `nameTypeMismatch:true` the node is named like an interactive (e.g. `_Btn`) but is NOT that component
                        (the interactive was not generated as its matching component). Do not bind it;
                        report back to the design/translator side to fix bindability.

Human-readable element table: `TaskListPanel.binding.md`.

## Contract
Schema v1.0. Full definition: specs/005-ui-binding-contract.md (in the producer project).
The consumer depends only on the binding.json FORMAT â€” never on this project's code/pipeline.
