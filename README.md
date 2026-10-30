# DipTrace SPICE Simulator

An open-source, Windows desktop application and DipTrace PCB Layout plugin for visualizing **electrical current distribution on real PCB copper geometry**.

> **Status:** Planning / initial repository setup. No validated electrical solver or PCB viewer has been released.

## Compatibility

- **DipTrace:** 5.3 or newer (minimum supported version)
- **OS:** Windows 10/11 (initial target)
- **Runtime / development:** .NET 8 SDK
- **Language:** C#
- **License:** MIT

This is an independent community project, not an official DipTrace product or endorsement.

## Vision

Launch from **DipTrace PCB Layout → Tools → Plugins**, or open a DipTrace XML export directly. View the actual PCB, select nets and layers, assign electrical sources and component models, run a DC analysis, and overlay calculated voltage, current, copper current density and dissipated power.

The UI must never present animated arrows or color maps as physical results until a numerical solver has produced validated values.

## Architecture

1. **DipTrace bridge** — external executable launched with a DipTrace XML exchange file, read-only by default.
2. **XML importer / domain model** — PCB outline, layers, pads, vias, traces, pours, components, nets and material parameters.
3. **2D viewer** — pan, zoom, net highlighting, layer controls and inspection.
4. **Circuit solver adapter** — component-level DC operating point (ngspice is a candidate, subject to dependency/license review).
5. **Copper network solver** — resistive network derived from geometry, with appropriate source/load boundary conditions.
6. **Visualization** — voltage and current-density maps, current arrows, hotspot inspection and reports.

The electrical solver and renderer must remain independent of DipTrace-specific parsing to permit deterministic testing.

## Initial milestones

- **M0:** Repository setup, architecture, CI, documentation and licensing.
- **M1:** Confirm DipTrace 5.3 plugin launch contract using a minimal read-only executable.
- **M2:** Import and accurately render a representative DipTrace 5.3 PCB XML file.
- **M3:** Resolve nets and components; run a simple, benchmarked DC resistor-network analysis.
- **M4:** Solve copper resistance/current sharing and compare against analytic and measured cases.
- **M5:** Overlay numerical current/voltage results on the PCB with traceable units and warnings.
- **M6:** Integrate SPICE models, LED strings and population variants.
- **Later:** Thermal analysis, 3D visualization and optional deeper editor integration.

## Proposed repository layout

```text
src/
  DipTraceSpice.App/          # Windows GUI
  DipTraceSpice.Plugin/       # DipTrace executable bridge
  DipTraceSpice.Core/         # Domain model
  DipTraceSpice.DipTrace/     # XML importer
  DipTraceSpice.Simulation/   # Solvers and adapters
  DipTraceSpice.Rendering/    # PCB renderer
tests/
  DipTraceSpice.Tests/
docs/
  architecture.md
  development.md
  compatibility.md
examples/                     # Synthetic, redistributable test fixtures
.github/workflows/
```

Folders are planned, not implemented yet.

## Safety and numerical validity

This is an engineering analysis aid, **not** a certified electrical or thermal verification tool. Never use unvalidated results as the sole basis for PCB safety, current ratings, or production release. Preserve original PCB files; do not silently change board geometry or connectivity. Flag missing geometry, model assumptions, unconstrained nets, and incomplete copper pours.

## Development

See [Development](docs/development.md), [Architecture](docs/architecture.md), and [Compatibility](docs/compatibility.md). Contributions are welcome; see [CONTRIBUTING.md](CONTRIBUTING.md).

## Third-party software

DipTrace is a product of Novarm Limited. DipTrace names and trademarks belong to their respective owners. Do not commit proprietary DipTrace SDK materials or customer board files unless redistribution is authorized. Third-party solver licenses must be reviewed before bundling.

## License

MIT — see [LICENSE](LICENSE).
