namespace DipTraceSpice.Core;

/// <summary>Read-only imported board snapshot. Geometry and electrical models will be added in later milestones.</summary>
public sealed record BoardSnapshot(string SourcePath, string RootElementName);
