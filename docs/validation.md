# Sample setup and validation

## Reading the extract

No Unity installation is needed to review the source on GitHub. The seven production files are copied unchanged from the private project's runtime code. Their hashes and source paths are in [source-manifest.json](source-manifest.json).

## Running the focused graph tests

Use a fresh Unity 6000.6.0f1 project with Unity Test Framework 1.8.0 available. This repository is not itself a complete Unity project.

1. Copy `Samples/` and `Tests/` into `Assets/ForgefallPortfolio/` in the fresh project, retaining their directory structure.
2. Let Unity import the runtime and Editor test assemblies.
3. Open Window → General → Test Runner, choose EditMode and run `RelayConnectivityTests`.

The runtime sample assembly has no dependency on original game scenes or third-party plugins. The test assembly references the runtime sample and Unity Test Framework. Avoid importing the extract alongside the original Forgefall source, because their types have identical namespaces and names.

## What the test fixture covers

Overlap versus edge adjacency versus non-contact; a disconnected relay cycle; alternative routes after bridge removal; connected-source coverage counts; and a destroyed Forge supplying no power.

The first three graph checks are adapted from `Tools/Validation/RelayConnectivityChecks.cs` in the original project, with scene and runtime integration checks removed. Coverage counts and the dead-Forge case are additional focused tests. This is a small behavioral fixture, not a complete regression suite for the game.

## Evidence scope

The original project's September 11 report recorded the graph checks and Play Mode integration checks passing, including bridge sale/reconnection and cooldown preservation. The October 5 Web report recorded a successful local release build and browser validation. Those are historical results from the full game.

The portfolio export checks verify unchanged source bytes, the publication file inventory, documentation/image links, the absence of full-project assets and a scan for credential-like material. The new packaged EditMode fixture has not been executed in Unity as part of this export; historical full-project results must not be read as a pass for this fixture.
