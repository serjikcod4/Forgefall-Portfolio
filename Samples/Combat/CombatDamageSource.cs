namespace Forgefall.Combat
{
    public enum CombatDamageSource
    {
        Unattributed,
        Player,
        BoltT1,
        RepeaterT2,
        LancerT2,
        // Stage 6B/6C-2: appended, never inserted -- RunStatistics/RunMetrics key their
        // per-source ledgers by this enum and existing entries must keep their
        // serialized integer values.
        MarksmanT2,
        BastionT1,
        BreakerT2,
        SiegeMasterT2,
        // Stage 6D: appended, never inserted. RunStatistics/RunMetrics serialize
        // these enum identities in their per-source ledgers.
        AnchorT1,
        UndertowT2,
        RiptideT2,
        // Stage 6E: appended, never inserted. Mortar damage, kills and T2
        // investments keep independent ledger identities.
        MortarT1,
        ShrapnelT2,
        PayloadT2
    }
}
