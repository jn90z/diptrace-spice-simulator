# Portable testing without administrator permissions

No installation into Program Files is required to test the standalone viewer.

## Run the Windows application
1. Open GitHub **Actions → CI**, select a successful run, and download `DipTraceSpice-win-x64`.
2. Extract the archive into a user-writable folder, e.g. `%USERPROFILE%\Downloads\DipTraceSpice`.
3. Run `DipTraceSpice.App.exe`. If your company uses application control, obtain IT approval rather than attempting to bypass it.
4. Select **Open DipTrace XML…** and choose an exported XML file.

**Expected today:** the application reports the XML file name and root element. It does not yet render PCB geometry or calculate current.

## Launch the plugin bridge manually
From PowerShell in the extracted folder:

```powershell
.\DipTraceSpice.Plugin.exe "C:\path\to\your\export.xml"
```

The bridge copies the file to a stable snapshot in `%LOCALAPPDATA%\DipTraceSpiceSimulator\Snapshots` and starts the viewer. This validates the launcher without needing to register it in DipTrace.

## Export a board for testing
Use DipTrace 5.3's documented PCB XML export route. The exact menu location depends on the installed version; consult the host's documentation. Avoid uploading confidential board files to public GitHub issues.

## What this does not validate
Standalone testing does **not** prove DipTrace detects the plugin manifest, passes the expected command-line arguments, or allows the plugin to launch from the Tools menu. Those require an actual host integration test.

## Future deployment
The target is a self-contained, user-writable, portable application. Direct Tools-menu registration is optional and may require IT approval or access to the plugin installation directory. Do not change system folder permissions to work around organization policy.
