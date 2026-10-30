# Development guide

## Prerequisites
- Windows 10/11
- .NET 8 SDK
- DipTrace 5.3 or newer for integration tests
- Visual Studio 2022 or another compatible .NET IDE

## Getting started
The repository is at **M0: scaffolding**. No buildable application has been added yet.

Planned commands once the solution exists:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

## Plugin integration
Use the official DipTrace 5.3 plugin documentation to create the PCB plugin directory and settings.xml. Do not assume undocumented in-process rendering or live edit hooks. The bridge should accept the XML exchange path as a command-line argument and pass it to the GUI without modifying the input.

## Test strategy
Start with synthetic boards: single resistor, series chain, two parallel copper paths, vias between layers, and disconnected/floating nets. Compare computed results against analytic calculations and, later, bench measurements.

## Releases
Initial releases will be pre-release Windows builds until geometry parsing and solver accuracy are validated.
