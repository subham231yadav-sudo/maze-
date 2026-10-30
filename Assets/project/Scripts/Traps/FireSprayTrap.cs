using System.Collections;
using System.Collections.Generic;
using Project.Combat;
using UnityEngine;

namespace Project.Traps
{
    [DisallowMultipleComponent]
    public class FireSprayTrap : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("How long the fire sprays each cycle (seconds)")]
        [SerializeField] private float sprayDuration = 2.0f;
        [Tooltip("Interval between sprays (seconds) - 'every 2 seconds'")]
        [SerializeField] private float intervalBetweenSprays = 2.0f;
        [SerializeField] private float initialDelay = 0.0f;

        [Header("Damage Settings")]
        [SerializeField] private float damagePerTick = 20f;
        [SerializeField] private float damageTickRate = 0.35f;
        [SerializeField] private float knockbackForce = 5.0f;
        [SerializeField] private float flameRange = 4.5f;
        [SerializeField] private float flameRadius = 1.2f;

        [Header("References")]
        [SerializeField] private GameObject flameEffectObject;
        [SerializeField] private AudioSource flameAudio;
        [SerializeField] private AudioClip flameLoopClip;

        private ParticleSystem[] particleSystems;
        private Transform flameTransform;
        private bool isSpraying = false;
        private float lastTickTime = 0f;

        public bool IsSpraying => isSpraying;

        private void Awake()
        {
            InitializeFlameEffect();
            InitializeAudio();
        }

        private void Start()
        {
            StartCoroutine(SprayLoopRoutine());
        }

        private void InitializeFlameEffect()
        {
            // Disable or destroy CFX_AutoDestructShuriken so particle is NEVER destroyed
            var autoDestructs = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in autoDestructs)
            {
                if (mb != null && mb.GetType().Name.Contains("AutoDestruct"))
                {
                    Destroy(mb);
                }
            }

            // Find flame effect object
            if (flameEffectObject == null)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    if (child.name.Contains("Flame") || child.name.Contains("WFX") || child.GetComponentInChildren<ParticleSystem>(true) != null)
                    {
                        flameEffectObject = child.gameObject;
                        break;
                    }
                }
            }

            if (flameEffectObject != null)
            {
                flameTransform = flameEffectObject.transform;
                particleSystems = flameEffectObject.GetComponentsInChildren<ParticleSystem>(true);
            }
            else
            {
                particleSystems = GetComponentsInChildren<ParticleSystem>(true);
                if (particleSystems.Length > 0)
                {
                    flameTransform = particleSystems[0].transform;
                }
                else
                {
                    flameTransform = transform;
                }
            }

            // Prepare all particle systems
            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                {
                    if (ps == null) continue;
                    var main = ps.main;
                    main.loop = true;
                    main.playOnAwake = false;

                    // Ensure emission is initially off
                    var emission = ps.emission;
                    emission.enabled = false;
                    ps.Play();
                }
            }
        }

        private void InitializeAudio()
        {
            if (flameAudio == null)
            {
                flameAudio = GetComponent<AudioSource>();
            }

            if (flameAudio == null && flameLoopClip != null)
            {
                flameAudio = gameObject.AddComponent<AudioSource>();
                flameAudio.playOnAwake = false;
                flameAudio.loop = true;
                flameAudio.spatialBlend = 1f;
                flameAudio.minDistance = 2f;
                flameAudio.maxDistance = 25f;
                flameAudio.clip = flameLoopClip;
            }
        }

        private IEnumerator SprayLoopRoutine()
        {
            if (initialDelay > 0f)
            {
                yield return new WaitForSeconds(initialDelay);
            }

            while (true)
            {
                // SPRAY FIRE
                SetFireActive(true);
                yield return new WaitForSeconds(sprayDuration);

                // STOP FIRE (PAUSE 2 SECONDS)
                SetFireActive(false);
                yield return new WaitForSeconds(intervalBetweenSprays);
            }
        }

        public void SetFireActive(bool active)
        {
            isSpraying = active;

            if (flameEffectObject != null)
            {
                flameEffectObject.SetActive(active);
            }

            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                {
                    if (ps == null) continue;
                    var emission = ps.emission;
                    emission.enabled = active;

                    if (active)
                    {
                        if (!ps.isPlaying) ps.Play();
                    }
                    else
                    {
                        ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
            }

            if (flameAudio != null)
            {
                if (active) flameAudio.Play();
                else flameAudio.Stop();
            }

            // Debug.Log($"[FireSprayTrap] Fire is {(active ? "SPRAYING 🔥" : "OFF ⏸")}");
        }

        private void Update()
        {
            if (!isSpraying) return;

            // Direct Physics Overlap Check in front of the flame nozzle
            if (Time.time >= lastTickTime + damageTickRate)
            {
                lastTickTime = Time.time;
                CheckAndDamageTargets();
            }
        }

        private void CheckAndDamageTargets()
        {
            Vector3 startPos = flameTransform != null ? flameTransform.position : transform.position;
            Vector3 forwardDir = flameTransform != null ? flameTransform.forward : transform.forward;

            // Sample capsule/spheres along the flame path
            int steps = 4;
            float stepDistance = flameRange / steps;

            for (int i = 1; i <= steps; i++)
            {
                Vector3 sampleCenter = startPos + forwardDir * (stepDistance * i);
                float radius = Mathf.Lerp(flameRadius * 0.6f, flameRadius, (float)i / steps);

                Collider[] colliders = Physics.OverlapSphere(sampleCenter, radius);
                foreach (var col in colliders)
                {
                    // Ignore self
                    if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

                    var damageable = col.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        damageable.TakeDamage(new DamageInfo(
                            damagePerTick,
                            col.ClosestPoint(sampleCenter),
                            forwardDir,
                            knockbackForce,
                            gameObject
                        ));
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isSpraying ? new Color(1f, 0.2f, 0.1f, 0.7f) : new Color(1f, 0.6f, 0.1f, 0.35f);
            Vector3 startPos = flameTransform != null ? flameTransform.position : transform.position;
            Vector3 forwardDir = flameTransform != null ? flameTransform.forward : transform.forward;

            int steps = 4;
            float stepDistance = flameRange / steps;
            for (int i = 1; i <= steps; i++)
            {
                Vector3 sampleCenter = startPos + forwardDir * (stepDistance * i);
                float radius = Mathf.Lerp(flameRadius * 0.6f, flameRadius, (float)i / steps);
                Gizmos.DrawWireSphere(sampleCenter, radius);
            }
        }
    }
}
