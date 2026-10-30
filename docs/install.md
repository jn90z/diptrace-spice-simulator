# Install and test the DipTrace plugin (Windows)

**Minimum host:** DipTrace 5.3. This is an early, read-only XML viewer/launcher, **not yet an electrical simulator**.

1. Open the repository's **Actions → CI** workflow and run it on `main` (or use the latest successful run).
2. Download the `DipTraceSpice-win-x64` artifact and unzip it.
3. Place the **entire** `DipTraceSpice` folder inside the DipTrace PCB plugins directory (the folder containing other PCB plugin folders). Check the installed DipTrace SDK for the exact directory on your machine.
4. Confirm `DipTraceSpice.Plugin.exe`, `DipTraceSpice.App.exe`, and `settings.xml` are together in that folder.
5. Restart DipTrace PCB Layout 5.3+, open a test PCB and select **Tools → Plugins → PCB Current Flow Simulator**.
6. The viewer should launch and display the exported XML filename and root element. **It will not yet draw copper or simulate current.**

## Why the launcher copies the file

DipTrace synchronously waits for the plugin executable and owns the temporary `plugin_exchange.xml`. The bridge first copies that exchange to a uniquely named local snapshot, launches the viewer with the snapshot path, then exits. The viewer never writes back to DipTrace.

Snapshots are stored under `%LOCALAPPDATA%\DipTraceSpiceSimulator\Snapshots`. Delete old snapshots manually as needed; automated cleanup is planned.

## Troubleshooting

- Missing Tools menu entry: confirm the folder, `settings.xml`, executable names and host version.
- Viewer does not open: run the plugin executable from a terminal with a known DipTrace XML file path; check whether Windows blocked downloaded executables.
- XML error: capture the root element and exception text, but **do not post proprietary board data**.
- If the DipTrace editor remains blocked: confirm the bridge exits; do not modify the exchange file.

## Current limitations

No geometry parser, electrical solver, C++ P/Invoke wiring, or packaged native DLL integration yet. CI builds both managed and native projects independently. The first integration test requires a real Windows DipTrace installation.
