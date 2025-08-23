# Change Log

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
