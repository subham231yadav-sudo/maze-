using Project.Player;
using UnityEngine;

namespace Project.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class FinishLineTrigger : MonoBehaviour
    {
        public static FinishLineTrigger Instance { get; private set; }

        [Header("Beacon & Visual Settings")]
        [SerializeField] private Color beaconColor = new Color(0.2f, 0.9f, 1f, 0.85f); // Vibrant Cyan/Gold
        [SerializeField] private float beamHeight = 55f;
        [SerializeField] private bool hasTriggered = false;

        private BoxCollider boxCollider;
        private ParticleSystem celebrationParticles;
        private Light beaconLight;

        public bool HasTriggered => hasTriggered;

        private void Awake()
        {
            Instance = this;
            SetupCollider();
            BuildVisuals();
        }

        private void Start()
        {
            SetupCollider();

            // Ensure MazeVictoryUI exists in the scene
            if (FindFirstObjectByType<MazeVictoryUI>() == null)
            {
                var victoryManagerObj = new GameObject("MazeVictoryManager");
                victoryManagerObj.AddComponent<MazeVictoryUI>();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            // 1. Locate Finish Line in the loaded scene
            var finishLine = FindFirstObjectByType<FinishLineTrigger>();
            if (finishLine == null)
            {
                var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var t in allTransforms)
                {
                    string lower = t.name.ToLower();
                    if (lower == "finish line" || lower == "finishline" || lower.Contains("finish line") || t.CompareTag("Finish"))
                    {
                        finishLine = t.gameObject.AddComponent<FinishLineTrigger>();
                        break;
                    }
                }
            }

            // 2. Ensure Victory UI exists
            if (FindFirstObjectByType<MazeVictoryUI>() == null)
            {
                var victoryMgr = new GameObject("MazeVictoryManager");
                victoryMgr.AddComponent<MazeVictoryUI>();
            }

            // 3. Ensure Canvases have high quality scaler
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    UIThemeHelper.ApplyHighQualityCanvasSettings(scaler);
                }
            }
        }

        private void Update()
        {
            if (hasTriggered) return;

            // Animate beacon light pulse
            if (beaconLight != null)
            {
                beaconLight.intensity = 2.5f + Mathf.PingPong(Time.time * 2.2f, 1.5f);
            }

            // Proximity fallback check in case physics layers miss trigger
            var player = FindFirstObjectByType<FirstPersonMovement>();
            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist <= 2.2f)
                {
                    var health = player.GetComponent<PlayerHealth>();
                    TriggerVictory(health);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            // Detect player through Tag, Movement, or Health component
            if (other.CompareTag("Player") ||
                other.GetComponentInParent<FirstPersonMovement>() != null ||
                other.GetComponentInParent<PlayerHealth>() != null)
            {
                var health = other.GetComponentInParent<PlayerHealth>();
                TriggerVictory(health);
            }
        }

        private void TriggerVictory(PlayerHealth playerHealth)
        {
            if (hasTriggered) return;
            hasTriggered = true;

            // 1. Play celebration particles
            if (celebrationParticles != null)
            {
                celebrationParticles.Play();
            }

            // 2. Notify Victory UI
            var victoryUI = MazeVictoryUI.Instance ?? FindFirstObjectByType<MazeVictoryUI>();
            if (victoryUI != null)
            {
                victoryUI.TriggerVictory(playerHealth);
            }
            else
            {
                var newVictoryObj = new GameObject("MazeVictoryManager");
                victoryUI = newVictoryObj.AddComponent<MazeVictoryUI>();
                victoryUI.TriggerVictory(playerHealth);
            }

            Debug.Log("[FinishLineTrigger] PLAYER WON! Reached the Finish Line and Escaped the Maze!");
        }

        public void SetupCollider()
        {
            boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
            }
            boxCollider.isTrigger = true;

            // Ensure reasonable trigger volume
            Vector3 size = boxCollider.size;
            if (size.x < 3f) size.x = 5f;
            if (size.y < 2.5f) size.y = 3.5f;
            if (size.z < 2f) size.z = 3f;
            boxCollider.size = size;

            if (boxCollider.center.y < 0.5f)
            {
                boxCollider.center = new Vector3(boxCollider.center.x, 1.5f, boxCollider.center.z);
            }
        }

        private void BuildVisuals()
        {
            // Avoid duplicating visuals if already created
            if (transform.Find("ExitBeaconBeam") != null) return;

            // ==========================================
            // 1. TOWERING SKY BEACON BEAM (Visible above walls)
            // ==========================================
            GameObject beamObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beamObj.name = "ExitBeaconBeam";
            beamObj.transform.SetParent(transform, false);
            beamObj.transform.localPosition = new Vector3(0f, beamHeight * 0.5f, 0f);
            beamObj.transform.localScale = new Vector3(0.65f, beamHeight * 0.5f, 0.65f);

            // Remove cylinder physics collider
            var beamCollider = beamObj.GetComponent<Collider>();
            if (beamCollider != null) Destroy(beamCollider);

            // Material for glowing beam
            var beamRenderer = beamObj.GetComponent<Renderer>();
            if (beamRenderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                               Shader.Find("Mobile/Unlit (Supports Lightmap)") ??
                               Shader.Find("Unlit/Color");
                var mat = new Material(shader);
                mat.color = beaconColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", beaconColor);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", beaconColor * 2.5f);
                beamRenderer.material = mat;
            }

            // ==========================================
            // 2. POINT LIGHT AT EXIT
            // ==========================================
            GameObject lightObj = new GameObject("BeaconLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);

            beaconLight = lightObj.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = beaconColor;
            beaconLight.range = 14f;
            beaconLight.intensity = 3.5f;

            // ==========================================
            // 3. GROUND GLOWING PAD (Exit Zone)
            // ==========================================
            GameObject padObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            padObj.name = "ExitGroundPad";
            padObj.transform.SetParent(transform, false);
            padObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            padObj.transform.localScale = new Vector3(4.5f, 0.05f, 2.5f);

            var padCollider = padObj.GetComponent<Collider>();
            if (padCollider != null) Destroy(padCollider);

            var padRenderer = padObj.GetComponent<Renderer>();
            if (padRenderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                var padMat = new Material(shader);
                Color padColor = new Color(0.2f, 0.95f, 0.5f, 0.7f); // Emerald Neon
                padMat.color = padColor;
                if (padMat.HasProperty("_BaseColor")) padMat.SetColor("_BaseColor", padColor);
                padRenderer.material = padMat;
            }

            // ==========================================
            // 4. CELEBRATION CONFETTI / SPARKS PARTICLE SYSTEM
            // ==========================================
            GameObject psObj = new GameObject("CelebrationParticles");
            psObj.transform.SetParent(transform, false);
            psObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            celebrationParticles = psObj.AddComponent<ParticleSystem>();
            var main = celebrationParticles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 4f;
            main.startLifetime = 3.5f;
            main.startSpeed = 8f;
            main.startSize = 0.25f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.2f, 1f), new Color(0.2f, 0.95f, 1f, 1f));
            main.gravityModifier = 0.35f;

            var emission = celebrationParticles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0.0f, 60),
                new ParticleSystem.Burst(0.4f, 40),
                new ParticleSystem.Burst(0.8f, 50)
            });

            var shape = celebrationParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 1.5f;
        }
    }
}
