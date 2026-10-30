# M2 geometry preview — release test plan

This is a **viewer preview**, not an electrical simulator. A successful CI build does not establish PCB rendering accuracy.

## Automated CI gates

- [ ] Windows managed solution builds in Release.
- [ ] xUnit tests pass (including synthetic DipTrace XML parser regression).
- [ ] C++20 native DLL compiles.
- [ ] Self-contained Windows x64 viewer and isolated plugin bridge publish.
- [ ] WPF runtime dependencies exist in the packaged artifact.
- [ ] Packaged viewer remains running for the startup smoke test.
- [ ] `DipTraceSpice-win-x64` artifact is downloadable.

## Manual acceptance test (no administrator permissions)

1. Download the artifact from a **successful** [CI run](https://github.com/jn90z/diptrace-spice-simulator/actions).
2. Extract the full ZIP to a user-writable directory. Run `DipTraceSpice.App.exe`.
3. Confirm the window opens without requiring .NET installation.
4. Open a known DipTrace 5.3 `.dipxml` board. Confirm board outline, visible trace segments and component centers against DipTrace PCB Layout.
5. Toggle top and bottom copper independently. Confirm no cross-layer traces are incorrectly hidden or shown.
6. Toggle component centers. Confirm reference labels follow the component locations.
7. Use mouse wheel zoom and **Fit Board**. Confirm no crash, clipping or distorted board proportions.
8. Open a `.dchxml` schematic. Confirm it is recognized and clearly states schematic rendering is not yet supported.
9. Open an unrelated XML or malformed file. Confirm a useful error and no crash.
10. Close and relaunch the application.

## Known limitations — do not mistake these for valid results

- Curved outline segments are currently rendered as straight lines.
- Pads, vias, actual copper pour fills and clearance voids are not rendered.
- Component centers are not physical component footprints.
- Schematic rendering, ngspice integration, electrical connectivity analysis and current-density solving are not implemented.
- Direct DipTrace Tools → Plugins integration is not verified.

## Release criteria

Publish an M2 **preview** only after automated gates pass and a representative board is visually inspected against DipTrace. Do not advertise voltage, current or thermal calculations until validated solvers exist.

Record the exact CI run, commit SHA, DipTrace version, tested board features and any visual mismatches in the release notes. Never upload proprietary customer PCB data into a public issue or repository.
