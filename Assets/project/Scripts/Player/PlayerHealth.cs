using System;
using System.Collections;
using Project.Combat;
using UnityEngine;

namespace Project.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float invulnerabilityDuration = 0.4f;

        [Header("Respawn")]
        [SerializeField] private bool autoRespawn = true;
        [SerializeField] private float respawnDelay = 2.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip deathSound;
        private AudioSource audioSource;

        private Rigidbody rb;
        private FirstPersonMovement movement;
        private FirstPersonLook look;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float lastDamageTime = -999f;
        private bool isDead = false;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;

        public event Action<float, float> OnHealthChanged; // (current, max)
        public event Action<DamageInfo> OnDamageTaken;
        public event Action OnDeath;
        public event Action OnRespawn;

        private void Awake()
        {
            currentHealth = maxHealth;
            rb = GetComponent<Rigidbody>();
            movement = GetComponent<FirstPersonMovement>();
            look = GetComponentInChildren<FirstPersonLook>();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D player audio
            }

            // Save initial spawn transform
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (isDead) return;
            if (Time.time < lastDamageTime + invulnerabilityDuration) return;

            lastDamageTime = Time.time;
            currentHealth = Mathf.Max(0f, currentHealth - damage.Amount);

            // Apply Knockback
            ApplyKnockback(damage.HitDirection, damage.KnockbackForce);

            // Play Sound
            PlayHurtSound();

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamageTaken?.Invoke(damage);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void ApplyKnockback(Vector3 direction, float force)
        {
            if (force <= 0f) return;

            if (direction == Vector3.zero)
            {
                direction = -transform.forward;
            }

            direction.Normalize();
            direction.y = Mathf.Max(direction.y, 0.35f); // Ensure slight upward arc

            if (rb != null && !rb.isKinematic)
            {
                // In Unity 6, linearVelocity is preferred
                rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.2f, 0f, rb.linearVelocity.z * 0.2f);
                rb.AddForce(direction * force, ForceMode.Impulse);
            }
        }

        private void PlayHurtSound()
        {
            if (hurtSound != null && audioSource != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(hurtSound);
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (deathSound != null && audioSource != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(deathSound);
            }

            // Freeze controls
            if (movement != null) movement.enabled = false;
            if (look != null) look.enabled = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }

            OnDeath?.Invoke();

            if (autoRespawn)
            {
                StartCoroutine(RespawnRoutine());
            }
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            Respawn();
        }

        public void Respawn()
        {
            isDead = false;
            currentHealth = maxHealth;

            // Teleport to spawn
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Re-enable controls
            if (movement != null) movement.enabled = true;
            if (look != null) look.enabled = true;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnRespawn?.Invoke();
        }

        public void SetSpawnPoint(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = rotation;
        }

        public void Heal(float amount)
        {
            if (isDead) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
