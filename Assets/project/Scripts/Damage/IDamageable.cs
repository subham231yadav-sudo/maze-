using UnityEngine;

namespace Project.Combat
{
    public struct DamageInfo
    {
        public float Amount;
        public Vector3 HitPoint;
        public Vector3 HitDirection;
        public float KnockbackForce;
        public GameObject Instigator;

        public DamageInfo(float amount, Vector3 hitPoint = default, Vector3 hitDirection = default, float knockbackForce = 0f, GameObject instigator = null)
        {
            Amount = amount;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            KnockbackForce = knockbackForce;
            Instigator = instigator;
        }
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo damage);
        bool IsDead { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
    }
}
