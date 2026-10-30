# DipTrace SPICE Simulator

**DipTrace 5.3+ · Windows · C#/.NET 8 WPF + C++20 · MIT**

A portable, read-only PCB viewer and future electrical-analysis tool for DipTrace designs. The goal is to display **calculated** voltage drop and current density on the physical PCB, not decorative current animations.

> **Project status — M2: early PCB geometry viewer.** Standalone startup and DipTrace XML recognition have been user-tested. The initial outline/trace viewer has been user-tested; the new copper-fill/silkscreen upgrade requires CI and visual verification. No electrical solver or SPICE integration is functional yet.

## Current capabilities

| Feature | Status |
| --- | --- |
| Portable Windows WPF application | Verified on user's PC |
| Open `.dipxml` PCB and `.dchxml` schematic | Verified |
| DipTrace 5.3.0.3 XML metadata | Verified |
| Board outline, straight trace segments, component centers | Verified visually on test board |
| Layer toggles, fit-to-board and wheel zoom | Implemented; testing pending |
| Copper fill scanline preview and top silkscreen text | Implemented; visual verification pending
| Pads, vias, exact pour polygons/clearances, curved outlines, pan | Planned |
| Native C++20 DLL scaffold | Builds independently; no simulation |
| ngspice integration | Planned; not bundled or called |
| Copper current-density solver | Planned |
| Direct Tools → Plugins launch | Not yet verified |

## Getting started without administrator permissions

1. Visit [GitHub Actions](https://github.com/jn90z/diptrace-spice-simulator/actions) and select a successful **CI** run.
2. Download the `DipTraceSpice-win-x64` artifact.
3. Extract the **entire archive** to a writable folder.
4. Run `DipTraceSpice.App.exe`.
5. Open a DipTrace PCB XML (`.dipxml`) or schematic XML (`.dchxml`).

For plugin-bridge testing and troubleshooting, see [Portable testing](docs/portable-testing.md). Direct integration with DipTrace's Tools menu requires a separate host test.

## How simulation will work

We will **not** write a new general-purpose SPICE engine.

- **ngspice (planned, external dependency):** nonlinear device and circuit analysis using models/netlists; not currently integrated.
- **Our C++20 solver (planned):** geometry-aware resistive copper network, current sharing, voltage drop and current-density calculations.
- **Our C# WPF viewer:** DipTrace XML import, layer rendering, net selection, results and diagnostics.

DipTrace already includes ngspice-based schematic simulation and can export SPICE `.cir` netlists. Our distinctive feature is relating circuit currents to the **actual copper geometry**. We must validate connectivity and solver accuracy before reporting physical results.

See [SPICE research and architecture decision](docs/spice-research.md).

## Architecture

```text
DipTrace 5.3+ (.dipxml / .dchxml / optional .cir)
               |
        C# WPF viewer
        |             |
  XML geometry    Circuit adapter [planned]
  importer        |
        |          ngspice [planned]
        |             |
        +-- C++20 copper-network solver [planned]
                     |
          Voltage/current-density results [planned]
```

The application is an independent window. A live overlay inside DipTrace's PCB canvas is not assumed to be supported.

## Build from source

Prerequisites: Visual Studio 2022 with .NET desktop development and C++ tools, .NET 8 SDK, CMake.

```powershell
dotnet restore DipTraceSpice.sln
dotnet build DipTraceSpice.sln -c Release
dotnet test DipTraceSpice.sln -c Release
cmake -S native -B native/build -A x64
cmake --build native/build --config Release
```

GitHub Actions builds the managed application and native stub independently, executes unit tests and performs a packaged WPF startup smoke test.

## Milestones

- **M0 — Repository and CI:** complete.
- **M1 — Portable app and XML import:** verified; direct DipTrace plugin launch pending.
- **M2 — PCB geometry viewer:** in progress.
- **M3 — Circuit model and ngspice adapter:** planned.
- **M4 — Copper resistive network and validation:** planned.
- **M5 — Current-density heatmaps and reports:** planned.
- **M6 — Advanced LED population variants and transient analysis:** planned.

## Documentation

- [Portable installation and testing](docs/portable-testing.md)
- [DipTrace plugin installation](docs/install.md)
- [SPICE research and architecture decision](docs/spice-research.md)
- [Architecture](docs/architecture.md)
- [Development](docs/development.md)
- [Compatibility](docs/compatibility.md)
- [Native interoperability](docs/native-interop.md)
- [Contributing](CONTRIBUTING.md)

**Security:** Do not commit confidential PCB designs, proprietary DipTrace SDK files, or third-party component models without redistribution permission. Imported XML is untrusted. This software is not certified for safety-critical design verification.

MIT licensed. Copyright © 2026 Joseph Niksa. Not affiliated with or endorsed by DipTrace / Novarm.
