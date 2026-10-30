using System.Collections;
using Project.Combat;
using Project.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("Custom UI References (Optional - auto-created if empty)")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Text healthText;
        [SerializeField] private Image damageFlashImage;
        [SerializeField] private GameObject deathScreenRoot;

        [Header("Visual Settings")]
        [SerializeField] private Color fullHealthColor = new Color(0.2f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.2f, 0.15f, 1f);
        [SerializeField] private Color damageFlashColor = new Color(0.85f, 0.05f, 0.05f, 0.45f);

        private PlayerHealth playerHealth;
        private Coroutine flashCoroutine;

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            EnsureUIExists();

            if (GetComponent<MazeObjectiveUI>() == null)
            {
                gameObject.AddComponent<MazeObjectiveUI>();
            }
        }

        private void Start()
        {
            EnsureUIExists();
            if (playerHealth != null)
            {
                HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
        }

        private void Update()
        {
            // Fallback safety: keep UI synchronized and visible
            if (healthSlider == null || canvas == null)
            {
                EnsureUIExists();
            }

            if (playerHealth != null && healthSlider != null)
            {
                float targetPct = Mathf.Clamp01(playerHealth.CurrentHealth / Mathf.Max(1f, playerHealth.MaxHealth));
                if (Mathf.Abs(healthSlider.value - targetPct) > 0.001f)
                {
                    HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
                }
            }
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += HandleHealthChanged;
                playerHealth.OnDamageTaken += HandleDamageTaken;
                playerHealth.OnDeath += HandleDeath;
                playerHealth.OnRespawn += HandleRespawn;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandleHealthChanged;
                playerHealth.OnDamageTaken -= HandleDamageTaken;
                playerHealth.OnDeath -= HandleDeath;
                playerHealth.OnRespawn -= HandleRespawn;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            float pct = Mathf.Clamp01(current / Mathf.Max(1f, max));
            if (healthSlider != null)
            {
                healthSlider.value = pct;
            }

            if (healthFillImage != null)
            {
                healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, pct);
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        private void HandleDamageTaken(DamageInfo info)
        {
            if (damageFlashImage != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(DamageFlashRoutine());
            }
        }

        private IEnumerator DamageFlashRoutine()
        {
            damageFlashImage.color = damageFlashColor;
            damageFlashImage.gameObject.SetActive(true);

            float elapsed = 0f;
            float duration = 0.45f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(damageFlashColor.a, 0f, elapsed / duration);
                damageFlashImage.color = new Color(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, alpha);
                yield return null;
            }

            damageFlashImage.gameObject.SetActive(false);
        }

        private void HandleDeath()
        {
            if (deathScreenRoot != null)
            {
                deathScreenRoot.SetActive(true);
            }
        }

        private void HandleRespawn()
        {
            if (deathScreenRoot != null)
            {
                deathScreenRoot.SetActive(false);
            }
            if (playerHealth != null)
            {
                HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
        }

        public void EnsureUIExists()
        {
            // 1. Find or create Canvas
            if (canvas == null)
            {
                var existingCanvas = GameObject.Find("PlayerHUD_Canvas");
                if (existingCanvas != null)
                {
                    canvas = existingCanvas.GetComponent<Canvas>();
                }

                if (canvas == null)
                {
                    var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (allCanvases.Length > 0)
                    {
                        canvas = allCanvases[0];
                    }
                }

                if (canvas == null)
                {
                    GameObject canvasObj = new GameObject("PlayerHUD_Canvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100;

                    var scaler = canvasObj.AddComponent<CanvasScaler>();
                    UIThemeHelper.ApplyHighQualityCanvasSettings(scaler);
                    canvasObj.AddComponent<GraphicRaycaster>();
                }
                else
                {
                    var existingScaler = canvas.GetComponent<CanvasScaler>();
                    if (existingScaler != null)
                    {
                        UIThemeHelper.ApplyHighQualityCanvasSettings(existingScaler);
                    }
                }
            }

            canvas.enabled = true;
            canvas.sortingOrder = 100; // Always on top

            Font font = UIThemeHelper.GetCleanFont();
            Sprite cardSprite = UIThemeHelper.GetRoundedCardSprite();
            Sprite pillSprite = UIThemeHelper.GetPillSprite();

            // 2. Find or create Damage Flash Overlay
            Transform flashTr = canvas.transform.Find("DamageFlashOverlay");
            if (flashTr != null)
            {
                damageFlashImage = flashTr.GetComponent<Image>();
            }
            else
            {
                GameObject flashObj = new GameObject("DamageFlashOverlay");
                flashObj.transform.SetParent(canvas.transform, false);
                var flashRect = flashObj.AddComponent<RectTransform>();
                flashRect.anchorMin = Vector2.zero;
                flashRect.anchorMax = Vector2.one;
                flashRect.sizeDelta = Vector2.zero;

                damageFlashImage = flashObj.AddComponent<Image>();
                damageFlashImage.color = new Color(0.8f, 0f, 0f, 0f);
                damageFlashImage.raycastTarget = false;
                flashObj.SetActive(false);
            }

            // 3. Find or create Health Bar Container
            Transform containerTr = canvas.transform.Find("HealthBarContainer");
            if (containerTr == null)
            {
                GameObject barContainer = new GameObject("HealthBarContainer");
                barContainer.transform.SetParent(canvas.transform, false);
                var containerRect = barContainer.AddComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(0f, 0f);
                containerRect.anchorMax = new Vector2(0f, 0f);
                containerRect.pivot = new Vector2(0f, 0f);
                containerRect.anchoredPosition = new Vector2(36f, 36f);
                containerRect.sizeDelta = new Vector2(280f, 36f);

                // Background
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(barContainer.transform, false);
                var bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                var bgImg = bgObj.AddComponent<Image>();
                bgImg.sprite = cardSprite;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = UIThemeHelper.GlassCardBg;

                // Accent Border
                var outline = bgObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.25f, 0.6f, 0.85f, 0.45f);
                outline.effectDistance = new Vector2(1.5f, 1.5f);

                // Health Icon Label
                GameObject iconObj = new GameObject("HealthIcon");
                iconObj.transform.SetParent(barContainer.transform, false);
                var iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0f);
                iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(12f, 0f);
                iconRect.sizeDelta = new Vector2(24f, 0f);
                var iconTxt = iconObj.AddComponent<Text>();
                iconTxt.font = font;
                iconTxt.fontSize = 20;
                iconTxt.alignment = TextAnchor.MiddleCenter;
                iconTxt.color = new Color(0.98f, 0.28f, 0.32f, 1f);
                iconTxt.text = "♥";

                // Slider
                GameObject sliderObj = new GameObject("HealthSlider");
                sliderObj.transform.SetParent(barContainer.transform, false);
                var sliderRect = sliderObj.AddComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(0f, 0f);
                sliderRect.anchorMax = new Vector2(1f, 1f);
                sliderRect.pivot = new Vector2(0.5f, 0.5f);
                sliderRect.anchoredPosition = new Vector2(16f, 0f);
                sliderRect.sizeDelta = new Vector2(-44f, -10f);

                healthSlider = sliderObj.AddComponent<Slider>();
                healthSlider.minValue = 0f;
                healthSlider.maxValue = 1f;
                healthSlider.value = 1f;
                healthSlider.interactable = false;

                // Fill Area
                GameObject fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(sliderObj.transform, false);
                var fillAreaRect = fillArea.AddComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.sizeDelta = Vector2.zero;

                // Fill
                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                var fillRect = fill.AddComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = Vector2.zero;

                healthFillImage = fill.AddComponent<Image>();
                healthFillImage.sprite = pillSprite;
                healthFillImage.type = Image.Type.Sliced;
                healthFillImage.color = fullHealthColor;
                healthSlider.fillRect = fillRect;

                // Text Label: 100 / 100
                GameObject textObj = new GameObject("HealthText");
                textObj.transform.SetParent(barContainer.transform, false);
                var textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                healthText = textObj.AddComponent<Text>();
                healthText.alignment = TextAnchor.MiddleCenter;
                healthText.font = font;
                healthText.fontSize = 17;
                healthText.fontStyle = FontStyle.Bold;
                healthText.color = Color.white;
                healthText.text = "100 / 100";

                var txtShadow = textObj.AddComponent<Shadow>();
                txtShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
                txtShadow.effectDistance = new Vector2(1.5f, -1.5f);

                barContainer.SetActive(true);
            }
            else
            {
                containerTr.gameObject.SetActive(true);
                healthSlider = containerTr.GetComponentInChildren<Slider>(true);
                if (healthSlider != null && healthSlider.fillRect != null)
                {
                    healthFillImage = healthSlider.fillRect.GetComponent<Image>();
                }
                var texts = containerTr.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.name == "HealthText") healthText = t;
                }
            }

            // 4. Find or create Death Screen
            Transform deathTr = canvas.transform.Find("DeathScreen");
            if (deathTr != null)
            {
                deathScreenRoot = deathTr.gameObject;
            }
            else
            {
                GameObject deathObj = new GameObject("DeathScreen");
                deathObj.transform.SetParent(canvas.transform, false);
                var deathRect = deathObj.AddComponent<RectTransform>();
                deathRect.anchorMin = Vector2.zero;
                deathRect.anchorMax = Vector2.one;
                deathRect.sizeDelta = Vector2.zero;

                var deathBg = deathObj.AddComponent<Image>();
                deathBg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);

                GameObject deathTextObj = new GameObject("DeathMessage");
                deathTextObj.transform.SetParent(deathObj.transform, false);
                var msgRect = deathTextObj.AddComponent<RectTransform>();
                msgRect.anchorMin = new Vector2(0.2f, 0.4f);
                msgRect.anchorMax = new Vector2(0.8f, 0.6f);
                msgRect.sizeDelta = Vector2.zero;

                var msgText = deathTextObj.AddComponent<Text>();
                msgText.alignment = TextAnchor.MiddleCenter;
                msgText.font = font;
                msgText.fontSize = 48;
                msgText.fontStyle = FontStyle.Bold;
                msgText.color = new Color(0.95f, 0.15f, 0.15f, 1f);
                msgText.text = "YOU DIED\n<size=22><color=#cccccc>Respawning...</color></size>";

                deathScreenRoot = deathObj;
                deathObj.SetActive(false);
            }
        }
    }
}
