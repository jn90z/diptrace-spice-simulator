# Architecture (proposed)

## Goal
A Windows GUI that can launch from DipTrace PCB Layout 5.3+ and can also open exported PCB XML directly.

## Modules
- **Plugin bridge:** accepts the DipTrace-provided XML path and hands a snapshot to the app. Initial integration must not write back.
- **DipTrace importer:** version-aware XML parser with geometry and connectivity validation.
- **Core:** immutable board model, netlist, material properties and units.
- **Rendering:** independent 2D renderer for outlines, pads, traces, vias and copper pours.
- **Simulation:** DC component models plus a copper resistance network. SPICE adapter and copper solver are distinct.
- **Reporting:** solver assumptions, convergence, model coverage, numerical warnings and exportable results.

## Numerical model
A schematic-only SPICE solution cannot infer spatial current density. Build a resistive copper discretization (including thickness, temperature-dependent resistivity, layer transitions, and contact approximations). Apply circuit boundary conditions and solve node potentials. Recover edge currents, current density and Joule losses. Validate against simple analytic conductors and parallel-path benchmarks before any board-level claims.

## Integration boundary
The documented DipTrace plugin mechanism launches an external executable and passes XML exchange data. A live overlay inside DipTrace's native canvas is **not** assumed available. A standalone viewer launched from the Tools/Plugins menu is the baseline.

## Risks
Copper pour serialization, connectivity edge cases, mesh size/performance, component model accuracy, schematic-to-PCB mapping, solver convergence, and DipTrace patch-version differences.

## Security
Read-only import by default, XML parser hardened against XXE, bounded file sizes, and no execution of content from board files.
