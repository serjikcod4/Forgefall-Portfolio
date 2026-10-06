using System;
using UnityEngine;

namespace Forgefall.Combat
{
    // Returns the amount of damage that should actually be applied, given the
    // raw incoming amount, its source, and the current/max health at the
    // moment of the hit. Null (the default) leaves ApplyDamage unmodified.
    public delegate int IncomingDamageFilter(int amount, CombatDamageSource source, int currentHealth, int maxHealth);

    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 100;
        private int currentHealth;
        private bool initialized;
        private bool maximumConfigured;
        private bool damageEnabled = true;
        private IncomingDamageFilter incomingDamageFilter;

        public int MaxHealth => Mathf.Max(1, maxHealth);
        public int CurrentHealth => initialized ? currentHealth : MaxHealth;
        public bool IsDead { get; private set; }
        public bool DamageEnabled => damageEnabled;

        public event Action<int, int> Changed;
        public event Action<int> Damaged;
        public event Action<CombatDamageSource, int, bool> DamageApplied;
        public event Action Died;

        private void Awake()
        {
            if (initialized) return;
            currentHealth = MaxHealth;
            IsDead = false;
            initialized = true;
        }

        public bool Damage(int amount) => ApplyDamage(amount) > 0;

        public int ApplyDamage(int amount) => ApplyDamage(amount, CombatDamageSource.Unattributed);

        public int ApplyDamage(int amount, CombatDamageSource source)
        {
            if (!initialized) Awake();
            if (amount <= 0 || IsDead || !damageEnabled) return 0;

            if (incomingDamageFilter != null)
            {
                amount = incomingDamageFilter(amount, source, currentHealth, MaxHealth);
                if (amount <= 0) return 0;
            }

            int previous = currentHealth;
            currentHealth = Mathf.Clamp(previous - amount, 0, MaxHealth);
            int applied = previous - currentHealth;
            bool died = currentHealth == 0;
            if (died) IsDead = true;
            Changed?.Invoke(currentHealth, MaxHealth);
            Damaged?.Invoke(applied);
            DamageApplied?.Invoke(source, applied, died);
            if (died) Died?.Invoke();

            return applied;
        }

        public int Restore(int amount)
        {
            if (!initialized) Awake();
            if (amount <= 0 || IsDead || currentHealth >= MaxHealth) return 0;

            int previous = currentHealth;
            currentHealth = Mathf.Clamp(previous + amount, 0, MaxHealth);
            int restored = currentHealth - previous;
            Changed?.Invoke(currentHealth, MaxHealth);
            return restored;
        }

        // Economy v0.3 Stage 7B: the smith is downed and respawns. Only SmithVitals calls this;
        // the Forge's death stays terminal (nothing revives it).
        public bool Revive()
        {
            if (!initialized) Awake();
            if (!IsDead) return false;
            IsDead = false;
            currentHealth = MaxHealth;
            Changed?.Invoke(currentHealth, MaxHealth);
            return true;
        }

        public bool TrySetMaximumBeforeGameplay(int maximum)
        {
            if (!initialized) Awake();
            if (maximumConfigured || maximum <= 0 || IsDead || currentHealth != MaxHealth) return false;
            maximumConfigured = true;
            maxHealth = maximum;
            currentHealth = MaxHealth;
            Changed?.Invoke(currentHealth, MaxHealth);
            return true;
        }

        // Stage 7C Iron Lungs: raises the maximum mid-run and heals by the added amount. Never
        // lowers it; ignored while dead (Revive restores the raised maximum).
        public bool TryRaiseMaximum(int maximum)
        {
            if (!initialized) Awake();
            if (maximum <= MaxHealth) return false;
            int added = maximum - MaxHealth;
            maxHealth = maximum;
            if (!IsDead) currentHealth = Mathf.Clamp(currentHealth + added, 0, MaxHealth);
            Changed?.Invoke(currentHealth, MaxHealth);
            return true;
        }

        // Stage 7E-1 Glass Forge: lowers the maximum mid-run; current health is clamped to it.
        // Never raises it; ignored while dead.
        public bool TryLowerMaximum(int maximum)
        {
            if (!initialized) Awake();
            if (IsDead || maximum < 1 || maximum >= MaxHealth) return false;
            maxHealth = maximum;
            currentHealth = Mathf.Min(currentHealth, MaxHealth);
            Changed?.Invoke(currentHealth, MaxHealth);
            return true;
        }

        public void SetDamageEnabled(bool enabled) => damageEnabled = enabled;

        // Installs (or clears, with null) the single owner allowed to transform
        // incoming damage before it reduces health. Reusable across any Health
        // instance; unset by default so existing behavior (e.g. enemies) is
        // unaffected.
        public void SetIncomingDamageFilter(IncomingDamageFilter filter) => incomingDamageFilter = filter;
    }
}
