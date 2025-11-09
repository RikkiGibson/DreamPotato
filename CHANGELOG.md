# Change Log

## [0.1.0-beta-4] - 2025-10-29
- Allow using VMUs in both slot 1 and slot 2
    - VMU-to-VMU communication (https://github.com/RikkiGibson/DreamPotato/issues/6) is **not yet implemented**, but this gets us much closer to being able to do it.
    - When Slot 1+2 are both used, a "secondary VMU" will also be displayed. It has its own menu and can independently open/save files, pause/resume, save/load state, handle key bindings, etc.
    - Connecting both the slot 1 and 2 VMUs to Flycast works in the latest [Flycast dev build](https://flyinghead.github.io/flycast-builds/#dev) (from 2025-10-28 or later).
    - The default "Arrow keys" bindings were changed so that they do not overlap with the WASD bindings, to make the primary and secondary VMU easier to use at the same time.
    - Multiple gamepads/selecting a gamepad per-VMU is not yet supported.
- Add Linux build.
- Mac build is also available directly from the main branch CI run, but, it has issues with macOS quarantining it. I don't have code signing/notarizing working.
- Fix a bug where deleting the most recently opened VMU file would cause the emulator to crash on startup.
- Fix a bug where the VMU contents would become corrupted after using "Save As" while docked.
- Preserve the docked/ejected state between runs (https://github.com/RikkiGibson/DreamPotato/issues/8)
- Allow saving/loading state while docked
    - Saving state while docked can be useful before loading rewards from a minigame into a Dreamcast game, just in case something goes wrong.
    - Note that the docked/ejected state is restored when loading state, based on the save-state contents.
    - Loading DreamPotato state while docked also causes Flycast to behave as if the memory card was removed and re-inserted.

## [0.1.0-beta-3] - 2025-10-05

- Support resizing the window
- Add setting "Preserve Aspect Ratio" to control whether content is stretched when resizing
- Add menu commands to set integer-multiple window size

## [0.1.0-beta-2] - 2025-10-03

- Efficiency improvements
- Prompt before exiting/closing VMU when unsaved changes are present.
    - This generally only happens when a .vms file, or no file, is being used.
- Automatically reopen the most recently opened file on startup.
- Flycast connections are now established faster.
- DreamPotato can now open VMU/VMS files from the Windows File Explorer
    - Generally, if an "Open" command in DreamPotato works with the file, then right click -> Open With and selecting DreamPotato.exe will also work.

## [0.1.0-beta-1] - 2025-08-30

- Fix a bug where loading state could lead to corrupting VMU content on disk.
    - VMU file is now replaced on disk when loading state.
- Add "Undo Load State" command.
- Disable commands which shouldn't be used while docked (connected to Dreamcast controller).
- Add keyboard and gamepad mapping UI
- Use a more conventional default button mapping for gamepads
- Fix some Dreamcast I/O bugs.
- Indicate when Dreamcast is writing data to VMU.
- Show indicator when the current VMU has unsaved changes (when it is not being auto-saved).
- Show a message when loading state fails.
