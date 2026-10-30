# Architecture

## Product objective

A read-only, portable Windows application that loads DipTrace 5.3+ PCB and schematic exports and ultimately displays **numerically calculated** current density and voltage drop on actual PCB copper. Standalone operation is mandatory; DipTrace Tools → Plugins is an optional launcher, not a dependency.

## Components and ownership

| Module | Implementation | Responsibility | State |
| --- | --- | --- | --- |
| Desktop viewer | C# / .NET 8 WPF | Open files, render layers, diagnostics and result overlays | M2 preview |
| DipTrace import | C# core library | Validate XML, parse PCB geometry and electrical connectivity | Partial |
| Plugin bridge | C# launcher | Snapshot host exchange XML and launch portable viewer, without writeback | Scaffold; host untested |
| Circuit simulation adapter | C# orchestration + external process | Generate/accept SPICE netlists, invoke ngspice, parse results and errors | Planned |
| Circuit solver | **ngspice** (third-party) | Nonlinear DC operating point, diode/LED models, later transients | Not integrated |
| PCB copper solver | **Our C++20 native DLL** | Copper resistance, network discretization, current conservation, voltage drop and current density | ABI stub only |
| Validation and reports | Managed + native tests | Reproducible fixtures, convergence warnings, numerical and measurement comparisons | Planned |

**Decision:** Do not build a general-purpose SPICE engine. Use independently obtained ngspice for circuit behavior, and implement the spatial PCB copper solver ourselves. Do not assume the DipTrace-bundled ngspice DLL is a supported public API or may be redistributed.

## Data flow

```text
DipTrace 5.3+ export
  ├── .dipxml PCB: outline, layers, nets, traces, pads, vias, pours
  ├── .dchxml schematic: devices, pins, models, connectivity
  └── .cir SPICE netlist (initial recommended circuit input)
                │
                ▼
        Read-only import + validation
                │
                ├── PCB geometry + connectivity ────────────┐
                │                                           │
                └── SPICE netlist → ngspice adapter          │
                                   │                         │
                              Terminal V / I                │
                                   │                         │
                                   └─────────┬───────────────┘
                                             ▼
                                  C++20 copper network
                                             │
                                Node V, edge I, J, I²R
                                             │
                                             ▼
                                 WPF overlays and reports
```

## Simulation phases

1. **Circuit:** Import or generate a valid SPICE netlist. Run ngspice as an isolated external executable for the initial implementation. Collect operating-point voltages and branch currents with explicit units and error reporting. Evaluate the ngspice shared library only after the external process path is tested.
2. **Mapping:** Resolve schematic pins, component pad IDs, and PCB nets. Flag unresolved/ambiguous mappings; never fabricate current injection points.
3. **Copper:** Build a layer-aware resistive mesh with copper thickness, temperature-dependent resistivity, vias, contacts, finite jumper resistance and geometry-dependent cross-sections.
4. **Coupling:** Apply circuit terminal currents/voltages as boundary conditions. Where copper drop changes nonlinear circuit behavior, use iterative co-simulation with convergence checks rather than claiming a single-pass result is exact.
5. **Rendering:** Present voltage, current density and dissipated power on the physical copper with legends, assumptions, missing-model warnings and selectable nets.

A schematic-only SPICE solution **cannot** infer current density in a copper pour. Conversely, a copper-only network cannot predict nonlinear LED current sharing without electrical models.

## Geometry fidelity and numerical validation

- Parse and render board arcs, pads, vias, actual poured copper fill regions and clearance voids; don't treat a pour boundary as solid copper.
- Distinguish trace geometry from electrical connectivity; honor net IDs and layer transitions.
- Normalize DipTrace units before calculation and expose assumptions about thickness, resistivity and temperature.
- Validate resistance against `R = ρL/(wt)` for straight traces, and verify Kirchhoff conservation for parallel paths.
- Include regression cases for series/parallel LEDs, jumper population variants, missing models, open nets and near-zero-ohm elements.
- No current-flow animations or heatmaps without computed results.

## Packaging and deployment

- Target Windows x64 with a self-contained .NET 8 WPF publish.
- Keep the plugin bridge in its own `bridge/` folder; never merge two self-contained .NET publishes.
- Preserve a no-admin, standalone executable workflow.
- Native DLL and future ngspice dependencies require explicit deployment tests and license review before bundling.
- XML import must remain read-only and reject DTD/external entities and oversized inputs.

## Release acceptance criteria

A testable **geometry preview** release requires: passing managed build, unit tests, C++ build, WPF startup smoke test, a downloadable `DipTraceSpice-win-x64` artifact, and correct rendering of a representative DipTrace 5.3 PCB. This does **not** imply a working SPICE simulator.

An **electrical simulation** release additionally requires: validated ngspice integration, model error reporting, a passing DC reference suite, PCB terminal mapping, and copper solver results that satisfy conservation and analytical benchmarks.

## Research and decisions

See [SPICE research](spice-research.md) for DipTrace forum findings, ngspice integration tradeoffs and licensing considerations.
