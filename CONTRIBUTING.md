# Contributing

Thanks for helping build DipTrace SPICE Simulator.

## Workflow
1. Open an issue describing the bug or proposed feature.
2. Create a topic branch from `main`.
3. Keep changes focused and include tests for parsers, numerical calculations and geometry transforms.
4. Open a pull request describing the change and validation performed.

## Engineering standards
- Minimum DipTrace version: **5.3**.
- Target .NET 8 and Windows initially.
- Use SI units internally and label all displayed units.
- Keep parsing, numerical simulation and rendering separately testable.
- Include analytic reference results and tolerances for every numerical test.
- Never describe placeholder arrows or heatmaps as simulation output.
- Reject or warn on incomplete copper geometry, floating networks and unsupported models.
- Treat imported XML as untrusted input; prohibit external entity resolution.
- Keep the DipTrace plugin read-only unless explicit writeback is designed and reviewed.

## Intellectual property
Do not upload customer PCB designs, proprietary libraries, unlicensed datasheets or redistributed DipTrace SDK files without permission. Synthetic fixtures are preferred. Check third-party license compatibility before adding dependencies.
