# Issue #1 Resolution

## Summary
Issue #1 has been resolved. The Windows installer project using WiX Toolset v6.0.2 has been successfully integrated into the AudioVisualizer solution.

## What Was Done
1. Created `installer/AudioVisualizer.Installer.wixproj` - WiX project for building MSI installer
2. Created `installer/Package.wxs` - MSI package definition with installation directories, Start Menu shortcut, and ARP metadata
3. Created `installer/License.rtf` - License agreement for the installer UI
4. Added installer project to `AudioVisualizer.slnx` solution
5. Created `AudioVisualizer.Linux.slnf` solution filter for Linux CI builds (excludes installer)
6. Updated `Jenkinsfile` to use Linux solution filter
7. Created `Jenkinsfile.Windows` for Windows-specific builds with MSI installer
8. Updated `README.md` with installer build instructions

## Key Decisions
- WiX Toolset v6.0.2 (not v7) to avoid explicit EULA requirement
- Self-contained, single-file `win-x64` publish (no .NET runtime prerequisite)
- Per-machine package with `WixUI_InstallDir` UI extension
- Installer builds only on Windows (WiX limitation confirmed via #4381)
- UpgradeCode fixed at `B8C8D1F6-26DB-4EE4-AC54-907294B7C911`

## Build Instructions
### On Windows:
```powershell
# Build everything including MSI
dotnet build AudioVisualizer.slnx --configuration Release

# Build only the installer
dotnet build installer/AudioVisualizer.Installer.wixproj --configuration Release
```

### On Linux (CI):
```bash
# Build without installer (WiX not available)
dotnet build AudioVisualizer.Linux.slnf --configuration Release
```

## Verification
- ✅ Installer project builds successfully on Windows
- ✅ Administrative install extracts `AudioVisualizer.exe` successfully
- ✅ Linux CI builds with solution filter
- ✅ All tests pass (42 passed)

## Documentation
See `README.md` for complete installation and build instructions.
