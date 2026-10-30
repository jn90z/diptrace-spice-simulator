# DipTrace SPICE Simulator

**DipTrace 5.3+** | **Windows** | **C#/.NET 8 WPF + C++20** | **MIT**

An independent, open-source PCB analysis tool intended to display **calculated** current distribution, voltage drop, and power dissipation over actual DipTrace copper geometry.

> **Current status: M1 integration prototype.** A read-only DipTrace PCB plugin launcher and XML-opening WPF viewer have been scaffolded. The viewer does **not** render PCB copper yet, and no electrical simulation has been implemented or validated.

## Confirmed DipTrace 5.3 XML samples\n\nTwo local sample files from DipTrace **5.3.0.3** were inspected (not committed): `TEST_BOARD_GER.dipxml` has root `Source Type="DipTrace-PCB"`, `Units="inch"`, with board outline, two copper layers, nine component entries, two nets, and copper pours; `TEST_BOARD_SCH.dchxml` has root `Source Type="DipTrace-Schematic"`, seven component entries, three nets, and a Simulator section. XML extension support and root metadata display are implemented; geometry import and simulation remain pending. Do not upload private board designs to the public repository.\n\n## What is available now

- DipTrace PCB plugin manifest configured for full XML export and **no import/writeback**
- C# plugin bridge that copies the host's temporary exchange file before launching the viewer
- WPF window with XML file picker and secure XML root inspection
- C++20 native DLL stub exposing an ABI version function (not a solver)
- Windows GitHub Actions CI: .NET build, unit tests, managed app artifact and native DLL artifact
- Basic unit test and project documentation

## Install and test

Follow **[Windows installation and smoke test](docs/install.md)**. Download the `DipTraceSpice-win-x64` artifact from a successful GitHub Actions CI run. A passing build has **not yet been verified**.

## Architecture

```text
DipTrace PCB Layout 5.3+
  └─ Tools → Plugins → PCB Current Flow Simulator
       └─ DipTraceSpice.Plugin.exe (C#)
            ├─ Copy plugin_exchange.xml to a stable snapshot
            └─ Launch DipTraceSpice.App.exe (C# / WPF)
                 ├─ DipTrace XML importer and PCB viewer [planned]
                 └─ P/Invoke → C++20 native engine [planned]
                       ├─ Circuit / SPICE adapter [planned]
                       ├─ Copper resistive network [planned]
                       └─ Current-density solver [planned]
```

A live overlay inside DipTrace's own PCB canvas is **not** assumed to be supported. The initial UI is an independent window launched from DipTrace.

## Roadmap

- **M0 complete:** license, documentation, repository structure, initial CI
- **M1 in progress:** verify read-only plugin launch and exchange XML on real DipTrace 5.3
- **M2:** parse and render PCB outlines, layers, pads, traces, vias and copper pours
- **M3:** deterministic electrical network benchmarks and component models
- **M4:** spatial copper-current solver, validated against analytical and measured paths
- **M5:** accurate heatmaps, current arrows, units, warnings and reports
- **M6:** SPICE integration, LED models and population variants
- **Later:** thermal and 3D views

## Build locally

Install .NET 8 SDK, Visual Studio 2022 with .NET desktop development and C++ build tools, and CMake.

```powershell
dotnet restore DipTraceSpice.sln
dotnet build DipTraceSpice.sln -c Release
dotnet test DipTraceSpice.sln -c Release
cmake -S native -B native/build -A x64
cmake --build native/build --config Release
```

The WPF viewer and C++ DLL currently build separately. The DLL is not yet called by the viewer.

## Engineering and security notes

Simulation must use actual electrical models and copper geometry, not decorative current animations. Imported XML is untrusted; external entities are disabled. This tool is not certified for safety-critical or production-release verification. Do not commit proprietary DipTrace SDK materials or customer designs without redistribution rights.

## Documentation

- [Install and smoke test](docs/install.md)
- [Architecture](docs/architecture.md)
- [Development](docs/development.md)
- [Compatibility](docs/compatibility.md)
- [Native interop](docs/native-interop.md)
- [Contributing](CONTRIBUTING.md)

MIT licensed. Copyright (c) 2026 Joseph Niksa. Not affiliated with or endorsed by DipTrace/Novarm.
