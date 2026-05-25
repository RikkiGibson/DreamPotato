# Android Port Out-of-Scope Findings

This file records issues noticed during the Android production pass that are outside the Android-port code we are willing to edit without upstream owner approval.

## Build warning: malformed XML documentation in ImGui renderer

- File: `src/DreamPotato.MonoGame/UI/ImGuiRenderer.cs`
- Line: 20
- Evidence: Android build warns with `CS1570` because the XML doc text contains `FNA & MonoGame` without escaping the ampersand.
- Suggested owner fix: change `&` to `&amp;` or remove XML-doc parsing from that comment.

## Build warning: ambiguous XML cref for ImGui.Image

- File: `src/DreamPotato.MonoGame/UI/ImGuiRenderer.cs`
- Line: 112
- Evidence: Android build warns with `CS0419` because `<see cref="ImGui.Image" />` matches multiple overloads.
- Suggested owner fix: either remove the cref or point it at a specific overload.

## Build warning: unresolved XML cref for MinWidth

- File: `src/DreamPotato.MonoGame/Game1.cs`
- Line: 600
- Evidence: Android build warns with `CS1574` because `<see cref="MinWidth"/>` does not resolve.
- Suggested owner fix: update the cref to the real constant/member name or rewrite the summary without a cref.

## VMU save stream replacement may leave the previous stream undisposed

- File: `src/DreamPotato.Core/Vmu.cs`
- Lines: 166, 179
- Evidence: `LoadVmu` and `SaveVmuAs` assign new streams into `_cpu.VmuFileWriteStream`. The visible code does not dispose the previous stream before replacing it.
- Why it matters: repeated open/save-as flows may keep stale file handles alive longer than expected, especially on Windows where this can affect overwrites, deletes, or external tools.
- Suggested owner fix: define ownership for `_cpu.VmuFileWriteStream` and dispose the existing stream before replacing it, if no other component owns that lifetime.

## Gamepad remap popup reads gamepad index even when None can be selected

- File: `src/DreamPotato.MonoGame/UI/UserInterface.cs`
- Lines: 1736, 1745, 1910
- Evidence: the Gamepad Config UI allows `None`, represented by `InputMappings.GamePadIndex_None`, and later `LayoutEditButton` calls `GamePad.GetState(_mappingEditState.GamePadIndex)`.
- Why it matters: if MonoGame does not tolerate the sentinel value on every backend, opening the edit-button popup while `None` is selected could fail or behave inconsistently.
- Suggested owner fix: guard `LayoutEditButton` when the selected gamepad index is `InputMappings.GamePadIndex_None`.
