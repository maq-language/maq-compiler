---
title: Project dependencies
---

# Project dependencies

> [!NOTE]
> This page is generated automatically from the MSBuild project graph. Do not edit it manually.

Source solution: `Maq.slnx`

**Projects**: 3
**Project references**: 1

```mermaid
flowchart LR
    p_eea2a3d88b2c["Maq.Source"]
    p_d3dd1b19677d["Maq.Source.Tests"]
    p_5e81d3e4ddc2["Maq.Docs.DependencyGraph"]

    p_d3dd1b19677d --> p_eea2a3d88b2c
```

## Projects

| Project | Path | Dependencies |
| --- | --- | ---: |
| Maq.Source | `src/Maq.Source/Maq.Source.csproj` | 0 |
| Maq.Source.Tests | `tests/Maq.Source.Tests/Maq.Source.Tests.csproj` | 1 |
| Maq.Docs.DependencyGraph | `tools/Docs.DependencyGraph/Maq.Docs.DependencyGraph.csproj` | 0 |
