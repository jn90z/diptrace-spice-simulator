# Compatibility policy

- Minimum supported DipTrace version: **5.3**.
- Target platform for first release: Windows 10/11, x64.
- Target runtime: .NET 8.
- Newer DipTrace 5.3.x builds are intended to be supported; test them before claiming verified compatibility.
- XML parser must report unsupported or unknown constructs rather than silently discard electrically significant geometry.
- Native DipTrace XML and plugin exchange XML are both planned input routes.
- First plugin is read-only: no changes written to the active DipTrace project.

## Verification checklist
- [ ] Confirm settings.xml configuration against the installed DipTrace 5.3 SDK.
- [ ] Confirm plugin process arguments and lifecycle.
- [ ] Validate board outline, coordinate units and layer transforms.
- [ ] Validate nets, vias, pads and traces.
- [ ] Validate copper pours including compressed fill representations.
- [ ] Validate population variants and electrical connectivity.
- [ ] Regression test on multiple DipTrace 5.3 patch versions.

Official reference: https://www.diptrace.com/support/tutorials/
