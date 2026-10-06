# Health and damage attribution

Read [CombatDamageSource.cs](../Samples/Combat/CombatDamageSource.cs), then [Health.cs](../Samples/Combat/Health.cs).

The component handles initialization, damage, restoration, death and selected changes to maximum health. Damage requests can carry a source identity, allowing the game's statistics to distinguish player damage and different turret branches.

A single optional `IncomingDamageFilter` lets the owning system transform raw incoming damage before health changes. The reusable component therefore does not need to know the research or rune rules of each actor.

## Notification order

`ApplyDamage` updates current health and marks death before invoking observers. It then emits health-changed, damaged and attributed-damage events, followed by the death event when applicable. It reports the amount actually removed, so overkill does not inflate damage credit.

Restoration does not revive a dead actor. `Revive` is a separate operation used by the smith's downed/respawn lifecycle; the Forge's destruction remains terminal in the full game's ownership rules.

The damage-source enum has append-only identities because the full project records those identities in statistics and metrics.

## Review topics

The component supports synchronous callbacks but does not guard against observers re-entering health mutation or throwing exceptions. Those are useful design questions for a reviewer. Maximum-health and incoming-damage arithmetic also merit boundary tests if inputs can approach integer limits.
