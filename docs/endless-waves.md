# Endless wave generation

Read [WaveConfig.cs](../Samples/Config/WaveConfig.cs), [DifficultyProfile.cs](../Samples/Config/DifficultyProfile.cs), then [EndlessWaveGenerator.cs](../Samples/Waves/EndlessWaveGenerator.cs).

The full runtime plays six authored waves directly from the selected difficulty. Only later waves go through the generator. Health, preparation time and enemy-count progression extend the final authored-wave delta. Enemy-count growth has an additional multiplier.

Spawn groups form assaults with multiple timed rows. The starting lane rotates so a profile using fewer than four simultaneous lanes still exercises every arena entrance over time. Every tenth wave is a boss wave, with the boss lane rotating as well.

Bosses from wave 20 onward increase later enemy counts and assault rows. A bounded growth-step count and capped row width limit parts of that escalation. Boss health uses wider arithmetic and clamps the final result before returning it.

## Caller contract

The caller supplies a valid six-wave profile and a one-based wave number beyond the authored range. The generator does not validate null profiles or perform the authored-wave dispatch itself. The full `WaveDirector` owns that dispatch and limits simultaneous living enemies separately from the planned total.

## Review topics

- Scheduling is deterministic and does not instantiate enemies.
- Authored early-wave data remains untouched by endless generation.
- Rotating lane offsets avoids permanently starving a lane.
- Extreme wave numbers still deserve boundary tests: some extrapolation happens in `int` arithmetic before later clamps, and enormous planned counts could allocate large spawn-group arrays.
- The constants are balance decisions, not a claim that late endless mode is balanced on every difficulty.
