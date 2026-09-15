# Maq Compiler

![Status](https://img.shields.io/badge/status-early%20development-orange)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![C%23](https://img.shields.io/badge/C%23-compiler-239120)
![Tests](https://img.shields.io/badge/tests-MSTest-5C2D91)
![Docs](https://img.shields.io/badge/docs-DocFX-2F80ED)
![License](https://img.shields.io/badge/license-public%20domain-blue)

> [!IMPORTANT] This project is **not finished yet**. As such,
> **contributions will not be accepted**. This is to prevent too much
> noise from interfering with the team's development process.
> However, the entire source code is released into the **public
> domain**, so you are free to make your own modifications by forking
> the repo.  For critical security-related concerns, please email
> [maq] *at* [alexover.dev], but please note that the mailbox is not
> automatically managed and a response is not guaranteed.

## Repository map

The repository is organized by meaningful compiler boundaries rather
than implementation classes. This means that classes that have high
coupling are generally placed within the same assembly, and classes
are only split into separate assemblies when large sections of code
are reused across the solution.

> [!NOTE]
> This section is unfinished.

## Requirements

Development currently targets **.NET 10**.

Check the installed SDK with:

```sh
dotnet --version
```

## Build

From the repository root:

```sh
dotnet restore
dotnet build
```

For a release build:

```sh
dotnet build --configuration Release
```

## Tests

Maq uses **MSTest** with Microsoft Testing Platform.

Run the complete test suite from the repository root:

```sh
dotnet test
```

Run a single test project:

```sh
dotnet test tests/Maq.Source.Tests/Maq.Source.Tests.csproj
```

Run tests without rebuilding after a successful build:

```sh
dotnet test --no-build
```

Tests are organized around compiler boundaries rather than implementation classes. Unit tests should lock down behavior at the smallest useful public or internal boundary, while `Maq.IntegrationTests` is reserved for behavior that crosses multiple compiler layers.

## Documentation

The documentation is built with **DocFX**.

### Read online

The public documentation is hosted at:

**https://maq-language.github.io/maq-compiler/**

No local SDK checkout is required, so it remains the easiest way to browse the language and compiler documentation from another machine — including, ideally, a remote cabin.

### Run locally

Restore the repository's .NET tools:

```sh
dotnet tool restore
```

Then build and serve the documentation:

```sh
dotnet tool run docfx docs/docfx.json --serve
```

DocFX serves the site locally at:

```text
http://localhost:8080
```

Leave that process running while editing documentation; refresh the browser after rebuilding when necessary.

To build the documentation without starting the local server:

```sh
dotnet tool run docfx docs/docfx.json
```

The generated site is written to the output directory configured by `docs/docfx.json`.

## Contributions

External contributions are **not currently being accepted**.

That includes:

- pull requests;
- feature requests;
- bug reports through public issues;
- design proposals submitted through the issue tracker; and
- requests to reserve or coordinate work.

This is temporary and intentional. Maq is still at a stage where foundational decisions are being revised quickly, and the project will benefit more from a small, coherent development loop than from optimizing for community process prematurely.

You are still free to fork the repository, experiment with it, modify it, or build your own work from it.

## Public domain

Maq Compiler is released into the **public domain** and is available for any use.

You may use, copy, modify, distribute, study, embed, fork, or build upon the project without needing permission from the Maq team.

See [`LICENSE`](LICENSE) for the repository's exact public-domain dedication.

## Name

**Maq** is short for **macro**.

The project's mascot is a monkey named **Maq**. 🐒

Happy hacking!
