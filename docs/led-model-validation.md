# Cree LED JB2835BWT-G — first real-world SPICE validation

The test-board LED model was supplied separately as `JSeries-SPICE-JB2835BWT-G.txt`. **Do not commit or redistribute the manufacturer's model text** without confirming permission; keep it in a local, user-selected model directory.

## What the supplied file contains

- Manufacturer/model: Cree LED `JB2835BWT-G`
- SPICE definition: `.MODEL JB2835BWT-G D` (diode model, not a subcircuit)
- `IS=9.8487e-31`, `N=1.5601`, `RS=0.86341`
- `Tnom=25`, `EG=2.7421`, `XTI=20.442`
- Manufacturer comments: binning current 0.055 A; maximum current 0.48 A

The values describe **electrical diode behavior**; the file does not specify luminous flux, optical efficiency, junction-to-board thermal resistance, or a complete driver circuit. The manufacturer maximum is a model comment, not a simulator-enforced current limit.

## Initial manual ngspice bench

The following **test circuit is our own**, not manufacturer model data. Save it as `led-dc-bench.cir` next to the supplied model text, and run `ngspice -b led-dc-bench.cir` after installing ngspice separately.

```spice
Cree JB2835BWT-G DC validation bench
.include JSeries-SPICE-JB2835BWT-G.txt
V1 supply 0 DC 5
R1 supply led_anode 36
D1 led_anode 0 JB2835BWT-G
.control
op
print v(led_anode) i(v1)
dc V1 0 6 0.1
.endc
.end
```

The 36-ohm resistor is an illustrative test fixture, **not a recommended LED driver design**. `i(v1)` follows SPICE's voltage-source current sign convention and is normally negative for a source delivering power. Verify that the installed ngspice version accepts the model's `mfg=Cree LED` annotation; if it reports a parsing error, report the error rather than silently changing manufacturer model parameters.

## Next integration steps

1. Allow the viewer to browse for a local `.txt`, `.lib`, or `.model` file without copying it into the repository.
2. Parse model declarations and display model name/type, parameter list and source path.
3. Map `JB2835BWT-G` to actual LED reference designators and schematic pin polarity. The model name alone is insufficient to identify PCB instances.
4. Run an isolated ngspice DC bench and capture raw output, diagnostics and exit code.
5. Validate current and voltage against known analytical values and user measurements; then use terminal currents as inputs to the copper solver.
6. Report any mismatch between schematic LED types, model mappings and physical PCB population.

Do not claim this test exercises PCB current flow; it validates only a circuit-model input path.
