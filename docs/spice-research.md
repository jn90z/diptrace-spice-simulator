# SPICE engine research and architecture decision

Research date: 2026-10-30. Scope: publicly indexed DipTrace forum discussions and official documentation. The forum's search endpoint was not directly retrievable, so this is a **targeted review**, not an exhaustive scrape.

## Evidence from DipTrace and its forum

1. **DipTrace already has built-in ngspice-based schematic simulation.** The official tutorial describes assigning SPICE models and running the simulator under Tools → SPICE Simulator. It also documents exporting a `.cir` netlist to an external simulator such as LTspice.
   - https://www.diptrace.com/support/tutorials/tutorial_html/
   - https://www.diptrace.com/support/tutorials/help_html/
2. **SPICE models and pin mappings are a frequent pain point.** A May 2026 forum thread reports a successful SPICE netlist export but a simulator error saying no circuits were loaded. DipTrace support attributes the issue to incorrect model data.
   - https://diptrace.com/forum/viewtopic.php?t=16791
3. **Users ask how to import vendor PSpice/LTspice models.** A 2022 forum thread asks about ngspice-compatible model sources and importing TI models.
   - https://diptrace.com/forum/viewtopic.php?t=15019
4. **Older workflows exported to LTspice.** Historical forum discussion documents netlist export and external simulation; it does not imply a schematic-to-schematic conversion.
   - https://diptrace.com/forum/viewtopic.php?t=43
5. **External DLL packaging can fail.** A 2022 DipTrace forum post reports an ngspice DLL load error; this is a reminder to test runtime dependencies, not evidence of a current DipTrace 5.3 defect.
   - https://diptrace.com/forum/viewtopic.php?t=14748
6. **Official ngspice provides shared-library integration documentation.**
   - https://ngspice.sourceforge.io/docs.html

## Decision

Use **ngspice as the planned circuit simulator**, not a custom general-purpose SPICE implementation. Keep a separate **in-house C++20 copper network solver** for physical PCB current flow, and a C# WPF frontend.

### Phased integration

1. Implement reliable DipTrace geometry and net connectivity parsing.
2. Accept an exported `.cir` file as the initial circuit simulation input. Report missing/invalid models and mapping errors.
3. Prototype an external ngspice process adapter (simpler to diagnose and isolate), then evaluate the ngspice shared library for a tighter UI integration.
4. Add a validated DC operating-point workflow that maps component terminal currents into the PCB copper network.
5. Solve copper resistance and current distribution using a sparse numerical network. Compare results against analytical traces, Kirchhoff checks and measured boards.
6. Consider iterative circuit/copper co-simulation if voltage drop materially affects LED or driver operating points.

### Why not depend on DipTrace's private ngspice DLL?

We should not assume DipTrace exposes its internal simulator as a supported plugin API, or redistribute DLLs from the installed product. Use an independently installed or explicitly licensed ngspice distribution instead.

### Licensing

The project remains MIT. **ngspice has mixed licensing across its source files**; before bundling any ngspice binaries, inspect the applicable license texts and redistribution obligations for the specific build. Do not assume all ngspice code is MIT, or that the external process approach automatically removes all distribution obligations.

Source: https://github.com/ngspice/ngspice/blob/master/COPYING

### Testing targets

- Netlist with known DC resistive divider.
- Two parallel branches with known current split.
- LED diode model and current-limiting resistor.
- Invalid or missing SPICE model produces an actionable error, never fabricated results.
- Copper trace resistance matches `R = ρL/(wt)` under documented assumptions.
- Current conservation at every node and layer/via transitions.
- Zero-ohm jumpers modeled as finite, controlled resistance rather than an undefined singularity.
