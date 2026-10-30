using System.Collections.Generic;
using Project.Combat;
using UnityEngine;

namespace Project.Traps
{
    [DisallowMultipleComponent]
    public class RotatingSpikeTrap : MonoBehaviour
    {
        [Header("Rotation Settings")]
        [Tooltip("Rotation speed in degrees per second")]
        [SerializeField] private float rotationSpeed = 120f;
        [Tooltip("Local axis to rotate around")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private bool reverseDirection = false;

        [Header("Damage Settings")]
        [Tooltip("Damage dealt on contact")]
        [SerializeField] private float contactDamage = 25f;
        [Tooltip("Knockback force pushing player away")]
        [SerializeField] private float knockbackForce = 8.5f;
        [Tooltip("Cooldown between damage ticks to the same target")]
        [SerializeField] private float damageCooldown = 0.5f;
        [Tooltip("Proximity radius around pillar center for spike contact damage")]
        [SerializeField] private float spikeDamageRadius = 1.3f;
        [SerializeField] private Vector3 centerOffset = new Vector3(0f, 0.8f, 0f);

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip hitSound;
        private AudioSource audioSource;

        private readonly Dictionary<IDamageable, float> lastDamageTimes = new Dictionary<IDamageable, float>();

        private void Awake()
        {
            // Safety: if this component is attached to a child of another RotatingSpikeTrap, remove it so it doesn't fight the parent
            if (transform.parent != null && transform.parent.GetComponentInParent<RotatingSpikeTrap>() != null)
            {
                Debug.LogWarning($"[RotatingSpikeTrap] Removing duplicate RotatingSpikeTrap on child '{gameObject.name}' of '{transform.parent.name}'");
                Destroy(this);
                return;
            }

            FixAndEnsureColliders();
            SetupAudio();
        }

        private void FixAndEnsureColliders()
        {
            // Remove broken MeshColliders with missing mesh
            var meshCols = GetComponentsInChildren<MeshCollider>(true);
            foreach (var mc in meshCols)
            {
                if (mc.sharedMesh == null)
                {
                    Destroy(mc);
                }
            }

            // Calculate visual bounds
            Bounds totalBounds = new Bounds(transform.position, Vector3.zero);
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                totalBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    totalBounds.Encapsulate(renderers[i].bounds);
                }
                centerOffset = transform.InverseTransformPoint(totalBounds.center);
                spikeDamageRadius = Mathf.Max(totalBounds.extents.x, totalBounds.extents.z) * 1.15f;
            }

            // Ensure a solid physical collider exists on root matching the pillar bounds
            var col = GetComponent<Collider>();
            if (col == null)
            {
                float scaleX = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x));
                float scaleY = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
                float scaleZ = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.z));
                float maxHorizontalScale = Mathf.Max(scaleX, scaleZ);

                var cap = gameObject.AddComponent<CapsuleCollider>();
                cap.center = centerOffset;
                cap.radius = (spikeDamageRadius * 0.8f) / maxHorizontalScale;
                cap.height = Mathf.Max(1.8f, totalBounds.size.y) / scaleY;
            }
        }

        private void SetupAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && hitSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
                audioSource.clip = hitSound;
            }
        }

        private void Update()
        {
            // 1. Continuous Rotation
            float dir = reverseDirection ? -1f : 1f;
            transform.Rotate(rotationAxis * (rotationSpeed * dir * Time.deltaTime), Space.Self);

            // 2. Proximity Damage Detection (Guaranteed detection even without rigidbodies)
            CheckSpikeProximity();
        }

        private void CheckSpikeProximity()
        {
            Vector3 worldCenter = transform.TransformPoint(centerOffset);
            Collider[] hits = Physics.OverlapSphere(worldCenter, spikeDamageRadius);

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    ApplyDamage(damageable, hit.ClosestPoint(worldCenter), worldCenter);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision.collider, collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
        }

        private void OnCollisionStay(Collision collision)
        {
            HandleCollision(collision.collider, collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleCollision(other, other.ClosestPoint(transform.position));
        }

        private void OnTriggerStay(Collider other)
        {
            HandleCollision(other, other.ClosestPoint(transform.position));
        }

        private void HandleCollision(Collider other, Vector3 hitPoint)
        {
            if (other.transform == transform || other.transform.IsChildOf(transform)) return;

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                Vector3 worldCenter = transform.TransformPoint(centerOffset);
                ApplyDamage(damageable, hitPoint, worldCenter);
            }
        }

        private void ApplyDamage(IDamageable target, Vector3 hitPoint, Vector3 worldCenter)
        {
            if (lastDamageTimes.TryGetValue(target, out float lastTime))
            {
                if (Time.time < lastTime + damageCooldown) return;
            }

            lastDamageTimes[target] = Time.time;

            // Knockback direction directly away from pillar center
            Vector3 knockbackDir = hitPoint - worldCenter;
            knockbackDir.y = 0.35f;
            knockbackDir.Normalize();

            target.TakeDamage(new DamageInfo(
                contactDamage,
                hitPoint,
                knockbackDir,
                knockbackForce,
                gameObject
            ));

            if (audioSource != null && hitSound != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.15f);
                audioSource.PlayOneShot(hitSound);
            }

            // Debug.Log($"[RotatingSpikeTrap] Dealt {contactDamage} damage to player!");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Vector3 worldCenter = transform.TransformPoint(centerOffset);
            Gizmos.DrawWireSphere(worldCenter, spikeDamageRadius);
        }
    }
}
