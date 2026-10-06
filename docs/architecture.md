# Architecture overview

This overview describes the full project's architecture. Only the linked samples are included in this repository.

## Run ownership

The scene-local `GameRoot` composes per-run services and controls the run state: Boot → DifficultySelection → Tutorial → Playing → Victory / Defeat. It constructs research, rune, economy and statistics services, binds them to scene components and disposes their subscriptions. It enables player systems according to run state and whether the smith is downed.

The game uses explicit serialized scene references and feature namespaces such as `Forgefall.Waves`, `Forgefall.Forge` and `Forgefall.Combat`. The original project currently has no assembly-definition boundaries; runtime and editor code use Unity's default assemblies. The small assembly definitions in this extract are packaging for review and tests, not an architectural change to the game.

## Data and behavior

Difficulty and authored waves live in ScriptableObjects. The run resolves a difficulty at startup. [DifficultyProfile](../Samples/Config/DifficultyProfile.cs) validates the required six-wave scenario; [WaveConfig](../Samples/Config/WaveConfig.cs) represents spawn groups and their scheduling data.

[EndlessWaveGenerator](../Samples/Waves/EndlessWaveGenerator.cs) operates on that data without spawning GameObjects. The separate `WaveDirector` owns runtime timing, active enemies, preparation holds and wave transitions. Enemy-count generation and the runtime concurrent-enemy cap are separate concerns.

[RelayConnectivity](../Samples/Forge/RelayConnectivity.cs) computes power from source coordinates, radii and an arena predicate. `BuildRing` applies results to the actual buildings, recomputing on topology changes instead of every frame. Placement previews invoke the same algorithm with a virtual source, without committing the source to the game.

[Health](../Samples/Combat/Health.cs) owns health mutation and damage attribution. Run-specific effects provide its damage filter rather than embedding Forge research rules in the common component.

## Authoritative events

The runtime wave director emits a single wave-completion event for post-wave ore and wave-clear Ether. Tracked enemy defeats during an active run supply the kill fact used by Forge Heat, elite trophies, boss rewards and rune seals. Views observe state and gameplay events instead of granting rewards themselves.

Resource transfer commits both inventory and Forge stocks before notifying observers, with guards against synchronous re-entry. That transfer system is described here but is not part of the published source selection.

## Tradeoffs worth discussing

- Scene composition keeps object ownership explicit but the full `GameRoot` coordinates many features; splitting its responsibilities is a reasonable future review topic.
- The full project uses default Unity assemblies. Feature-level assembly boundaries would require a separate dependency review.
- Wave progression is deterministic and easy to inspect, but balance and large-number behavior need explicit limits and playtesting.
- Power resolution favors simple, testable graph logic over a more complex spatial index. Its scaling cost matters if relay counts grow substantially.
- AI assistance was part of development. The portfolio documents observable behavior, source provenance and validation scope rather than claiming wholly unaided implementation.
