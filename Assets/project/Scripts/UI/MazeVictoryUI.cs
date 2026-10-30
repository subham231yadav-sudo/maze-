using System.Collections;
using Project.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.UI
{
    [DisallowMultipleComponent]
    public class MazeVictoryUI : MonoBehaviour
    {
        public static MazeVictoryUI Instance { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioClip customVictorySound;

        // Runtime state
        private bool isVictoryTriggered = false;
        private float mazeStartTime = 0f;
        private float finalEscapeTime = 0f;

        // UI references
        private Canvas targetCanvas;
        private GameObject victoryRoot;
        private CanvasGroup victoryGroup;
        private RectTransform victoryCardRect;
        private Text titleText;
        private Text subtitleText;
        private Text timeStatText;
        private Text healthStatText;
        private Text damageStatText;
        private Text rankStatText;
        private Image[] starImages = new Image[3];
        private AudioSource audioSource;

        public bool IsVictory => isVictoryTriggered;
        public float ElapsedTime => isVictoryTriggered ? finalEscapeTime : Mathf.Max(0f, Time.time - mazeStartTime);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            mazeStartTime = Time.time;
            BuildVictoryUI();
        }

        private void Update()
        {
            if (isVictoryTriggered)
            {
                // Instant quick-restart via keyboard
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    OnPlayAgainClicked();
                }
            }
        }

        public void TriggerVictory(PlayerHealth playerHealth)
        {
            if (isVictoryTriggered) return;
            isVictoryTriggered = true;
            finalEscapeTime = Time.time - mazeStartTime;

            // 1. Freeze player movement & camera look
            var movement = FindFirstObjectByType<FirstPersonMovement>();
            if (movement != null) movement.enabled = false;

            var look = FindFirstObjectByType<FirstPersonLook>();
            if (look != null) look.enabled = false;

            var rb = movement != null ? movement.GetComponent<Rigidbody>() : null;
            if (rb != null) rb.linearVelocity = Vector3.zero;

            // 2. Unlock & reveal mouse cursor for UI buttons
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 3. Play victory fanfare audio
            AudioClip clip = customVictorySound != null ? customVictorySound : UIThemeHelper.GetVictoryFanfareClip();
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, 0.95f);
            }

            // 4. Hide in-game HUD elements to keep victory screen clean
            var healthUI = FindFirstObjectByType<PlayerHealthUI>();
            if (healthUI != null)
            {
                var bar = GameObject.Find("HealthBarContainer");
                if (bar != null) bar.SetActive(false);
            }

            var objUI = FindFirstObjectByType<MazeObjectiveUI>();
            if (objUI != null)
            {
                var pill = GameObject.Find("PersistentObjectivePill");
                if (pill != null) pill.SetActive(false);
            }

            // 5. Populate Victory Stats
            PopulateStats(playerHealth);

            // 6. Launch animated victory reveal
            StartCoroutine(VictorySequenceRoutine());
        }

        private void PopulateStats(PlayerHealth playerHealth)
        {
            // Time formatting MM:SS
            int minutes = Mathf.FloorToInt(finalEscapeTime / 60f);
            int seconds = Mathf.FloorToInt(finalEscapeTime % 60f);
            if (timeStatText != null)
            {
                timeStatText.text = $"{minutes:00}:{seconds:00}";
            }

            // Health stats
            float curHealth = playerHealth != null ? playerHealth.CurrentHealth : 100f;
            float maxHealth = playerHealth != null ? playerHealth.MaxHealth : 100f;
            float healthPct = Mathf.Clamp01(curHealth / Mathf.Max(1f, maxHealth));

            if (healthStatText != null)
            {
                healthStatText.text = $"{Mathf.CeilToInt(curHealth)} / {Mathf.CeilToInt(maxHealth)} HP";
            }

            float damageTaken = Mathf.Max(0f, maxHealth - curHealth);
            if (damageStatText != null)
            {
                damageStatText.text = damageTaken <= 0.1f ? "0 HP (Flawless!)" : $"{Mathf.CeilToInt(damageTaken)} HP Taken";
            }

            // Calculate Star Rating
            int stars = 3;
            string rankTitle = "FLAWLESS ESCAPE";
            if (healthPct < 0.4f || finalEscapeTime > 240f)
            {
                stars = 1;
                rankTitle = "SURVIVOR";
            }
            else if (healthPct < 0.85f || finalEscapeTime > 120f)
            {
                stars = 2;
                rankTitle = "SKILLED RUNNER";
            }

            if (rankStatText != null)
            {
                rankStatText.text = rankTitle;
            }

            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].color = (i < stars) ? UIThemeHelper.GoldAccent : new Color(0.3f, 0.35f, 0.45f, 0.4f);
                    starImages[i].transform.localScale = Vector3.zero; // Animate in later
                }
            }
        }

        private IEnumerator VictorySequenceRoutine()
        {
            if (victoryRoot == null || victoryGroup == null) yield break;

            victoryRoot.SetActive(true);
            victoryGroup.alpha = 0f;

            if (victoryCardRect != null)
            {
                victoryCardRect.localScale = Vector3.one * 0.85f;
            }

            // Smooth fade-in & elastic card pop
            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth ease-out back
                float ease = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);

                victoryGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                if (victoryCardRect != null)
                {
                    victoryCardRect.localScale = Vector3.LerpUnclamped(Vector3.one * 0.85f, Vector3.one, ease);
                }
                yield return null;
            }

            victoryGroup.alpha = 1f;
            if (victoryCardRect != null) victoryCardRect.localScale = Vector3.one;

            // Sequential star pop-in animation
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null && starImages[i].color.a > 0.5f)
                {
                    yield return new WaitForSecondsRealtime(0.12f);
                    StartCoroutine(PopStarRoutine(starImages[i].transform));
                }
            }
        }

        private IEnumerator PopStarRoutine(Transform starTr)
        {
            float elapsed = 0f;
            float duration = 0.28f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
                float scale = (t < 0.7f) ? Mathf.Lerp(0f, 1.35f, t / 0.7f) : Mathf.Lerp(1.35f, 1f, (t - 0.7f) / 0.3f);
                starTr.localScale = Vector3.one * scale;
                yield return null;
            }
            starTr.localScale = Vector3.one;
        }

        public void OnPlayAgainClicked()
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(UIThemeHelper.GetButtonClickClip(), 0.8f);
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void OnQuitClicked()
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(UIThemeHelper.GetButtonClickClip(), 0.8f);
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BuildVictoryUI()
        {
            // Find or create PlayerHUD_Canvas
            targetCanvas = FindFirstObjectByType<Canvas>();
            if (targetCanvas == null)
            {
                var canvasObj = new GameObject("PlayerHUD_Canvas");
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 100;

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                UIThemeHelper.ApplyHighQualityCanvasSettings(scaler);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                var scaler = targetCanvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    UIThemeHelper.ApplyHighQualityCanvasSettings(scaler);
                }
            }

            // Ensure EventSystem exists so buttons are interactable
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            Font font = UIThemeHelper.GetCleanFont();
            Sprite cardSprite = UIThemeHelper.GetRoundedCardSprite();
            Sprite pillSprite = UIThemeHelper.GetPillSprite();
            Sprite starSprite = UIThemeHelper.GetStarSprite();

            // ==========================================
            // 1. FULLSCREEN BACKDROP
            // ==========================================
            victoryRoot = new GameObject("MazeVictoryScreen");
            victoryRoot.transform.SetParent(targetCanvas.transform, false);

            var rootRect = victoryRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            victoryGroup = victoryRoot.AddComponent<CanvasGroup>();

            var bgImg = victoryRoot.AddComponent<Image>();
            bgImg.color = UIThemeHelper.DarkBackdrop;

            // ==========================================
            // 2. MAIN VICTORY CARD (Glassmorphic Window)
            // ==========================================
            GameObject cardObj = new GameObject("VictoryCard");
            cardObj.transform.SetParent(victoryRoot.transform, false);
            victoryCardRect = cardObj.AddComponent<RectTransform>();
            victoryCardRect.anchorMin = new Vector2(0.5f, 0.5f);
            victoryCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            victoryCardRect.pivot = new Vector2(0.5f, 0.5f);
            victoryCardRect.sizeDelta = new Vector2(740f, 540f);

            var cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = cardSprite;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = UIThemeHelper.GlassCardBg;

            // Subtle glowing outline
            var cardOutline = cardObj.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.25f, 0.7f, 1f, 0.45f);
            cardOutline.effectDistance = new Vector2(2f, 2f);

            // ==========================================
            // 3. TOP GLOWING BADGE
            // ==========================================
            GameObject badgeObj = new GameObject("Badge");
            badgeObj.transform.SetParent(cardObj.transform, false);
            var badgeRect = badgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 1f);
            badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 1f);
            badgeRect.anchoredPosition = new Vector2(0f, -24f);
            badgeRect.sizeDelta = new Vector2(260f, 36f);

            var badgeImg = badgeObj.AddComponent<Image>();
            badgeImg.sprite = pillSprite;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = new Color(0.1f, 0.6f, 0.9f, 0.25f);

            GameObject badgeTxtObj = new GameObject("Text");
            badgeTxtObj.transform.SetParent(badgeObj.transform, false);
            var btRect = badgeTxtObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;

            var badgeTxt = badgeTxtObj.AddComponent<Text>();
            badgeTxt.font = font;
            badgeTxt.fontSize = 15;
            badgeTxt.fontStyle = FontStyle.Bold;
            badgeTxt.alignment = TextAnchor.MiddleCenter;
            badgeTxt.color = UIThemeHelper.CyanAccent;
            badgeTxt.text = "✦  MAZE ESCAPED  ✦";

            // ==========================================
            // 4. MAIN HEADLINE & SUBTITLE
            // ==========================================
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -68f);
            titleRect.sizeDelta = new Vector2(680f, 64f);

            titleText = titleObj.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 48;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = UIThemeHelper.GoldAccent;
            titleText.text = "VICTORY!";

            var titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            titleShadow.effectDistance = new Vector2(2.5f, -2.5f);

            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(cardObj.transform, false);
            var subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0f, -128f);
            subRect.sizeDelta = new Vector2(660f, 28f);

            subtitleText = subObj.AddComponent<Text>();
            subtitleText.font = font;
            subtitleText.fontSize = 17;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = UIThemeHelper.TextSecondary;
            subtitleText.text = "You reached the exit and survived all deadly traps!";

            // ==========================================
            // 5. THREE STAR RATING
            // ==========================================
            GameObject starsRow = new GameObject("StarsRow");
            starsRow.transform.SetParent(cardObj.transform, false);
            var starsRect = starsRow.AddComponent<RectTransform>();
            starsRect.anchorMin = new Vector2(0.5f, 1f);
            starsRect.anchorMax = new Vector2(0.5f, 1f);
            starsRect.pivot = new Vector2(0.5f, 1f);
            starsRect.anchoredPosition = new Vector2(0f, -164f);
            starsRect.sizeDelta = new Vector2(220f, 54f);

            float[] starXs = { -65f, 0f, 65f };
            float[] starScales = { 46f, 56f, 46f }; // Center star slightly bigger

            for (int i = 0; i < 3; i++)
            {
                GameObject starObj = new GameObject($"Star_{i}");
                starObj.transform.SetParent(starsRow.transform, false);
                var sRect = starObj.AddComponent<RectTransform>();
                sRect.anchorMin = new Vector2(0.5f, 0.5f);
                sRect.anchorMax = new Vector2(0.5f, 0.5f);
                sRect.anchoredPosition = new Vector2(starXs[i], 0f);
                sRect.sizeDelta = new Vector2(starScales[i], starScales[i]);

                starImages[i] = starObj.AddComponent<Image>();
                starImages[i].sprite = starSprite;
                starImages[i].color = UIThemeHelper.GoldAccent;
            }

            // ==========================================
            // 6. STATS DASHBOARD (2x2 Grid)
            // ==========================================
            GameObject statsGrid = new GameObject("StatsGrid");
            statsGrid.transform.SetParent(cardObj.transform, false);
            var sgRect = statsGrid.AddComponent<RectTransform>();
            sgRect.anchorMin = new Vector2(0.5f, 0.5f);
            sgRect.anchorMax = new Vector2(0.5f, 0.5f);
            sgRect.anchoredPosition = new Vector2(0f, -34f);
            sgRect.sizeDelta = new Vector2(640f, 140f);

            // Stat Box 1: Time
            timeStatText = CreateStatCard(statsGrid.transform, new Vector2(-165f, 36f), "⏱ ESCAPE TIME", "01:24", font, cardSprite);
            // Stat Box 2: Health
            healthStatText = CreateStatCard(statsGrid.transform, new Vector2(165f, 36f), "♥ REMAINING HEALTH", "100 / 100 HP", font, cardSprite);
            // Stat Box 3: Damage
            damageStatText = CreateStatCard(statsGrid.transform, new Vector2(-165f, -36f), "🛡 DAMAGE TAKEN", "0 HP (Flawless!)", font, cardSprite);
            // Stat Box 4: Rank
            rankStatText = CreateStatCard(statsGrid.transform, new Vector2(165f, -36f), "★ PERFORMANCE", "FLAWLESS ESCAPE", font, cardSprite);

            // ==========================================
            // 7. ACTION BUTTONS
            // ==========================================
            GameObject playBtnObj = CreateButton(cardObj.transform, new Vector2(-130f, -205f), new Vector2(230f, 50f),
                "PLAY AGAIN", UIThemeHelper.EmeraldGreen, Color.black, font, pillSprite, OnPlayAgainClicked);

            GameObject quitBtnObj = CreateButton(cardObj.transform, new Vector2(130f, -205f), new Vector2(230f, 50f),
                "QUIT GAME", new Color(0.2f, 0.25f, 0.35f, 0.85f), Color.white, font, pillSprite, OnQuitClicked);

            // Keyboard shortcut hint
            GameObject hintObj = new GameObject("ShortcutHint");
            hintObj.transform.SetParent(cardObj.transform, false);
            var hRect = hintObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 0f);
            hRect.anchorMax = new Vector2(0.5f, 0f);
            hRect.pivot = new Vector2(0.5f, 0f);
            hRect.anchoredPosition = new Vector2(0f, 14f);
            hRect.sizeDelta = new Vector2(400f, 22f);

            var hintText = hintObj.AddComponent<Text>();
            hintText.font = font;
            hintText.fontSize = 13;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new Color(0.6f, 0.65f, 0.75f, 0.65f);
            hintText.text = "Press [Space] or [R] to Play Again";

            victoryRoot.SetActive(false);
        }

        private Text CreateStatCard(Transform parent, Vector2 pos, string label, string defaultValue, Font font, Sprite cardSprite)
        {
            GameObject statBox = new GameObject($"StatBox_{label}");
            statBox.transform.SetParent(parent, false);
            var rect = statBox.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(300f, 62f);

            var img = statBox.AddComponent<Image>();
            img.sprite = cardSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.12f, 0.15f, 0.22f, 0.85f);

            // Header label (small)
            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(statBox.transform, false);
            var lRect = lblObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0f, 0.55f);
            lRect.anchorMax = new Vector2(1f, 1f);
            lRect.sizeDelta = new Vector2(-24f, 0f);
            var lText = lblObj.AddComponent<Text>();
            lText.font = font;
            lText.fontSize = 12;
            lText.fontStyle = FontStyle.Bold;
            lText.alignment = TextAnchor.MiddleLeft;
            lText.color = UIThemeHelper.TextSecondary;
            lText.text = label;

            // Value label (bold & crisp)
            GameObject valObj = new GameObject("Value");
            valObj.transform.SetParent(statBox.transform, false);
            var vRect = valObj.AddComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0f, 0f);
            vRect.anchorMax = new Vector2(1f, 0.62f);
            vRect.sizeDelta = new Vector2(-24f, 0f);
            var vText = valObj.AddComponent<Text>();
            vText.font = font;
            vText.fontSize = 20;
            vText.fontStyle = FontStyle.Bold;
            vText.alignment = TextAnchor.MiddleLeft;
            vText.color = UIThemeHelper.TextPrimary;
            vText.text = defaultValue;

            return vText;
        }

        private GameObject CreateButton(Transform parent, Vector2 pos, Vector2 size, string label, Color bgColor, Color textColor, Font font, Sprite pillSprite, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = btnObj.AddComponent<Image>();
            img.sprite = pillSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.35f);
            colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.25f);
            colors.colorMultiplier = 1f;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            // Add audio hover/click listeners
            var trigger = btnObj.AddComponent<EventTrigger>();
            var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            hoverEntry.callback.AddListener((_) =>
            {
                if (audioSource != null) audioSource.PlayOneShot(UIThemeHelper.GetButtonHoverClip(), 0.5f);
            });
            trigger.triggers.Add(hoverEntry);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            var txt = textObj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 17;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = textColor;
            txt.text = label;

            return btnObj;
        }
    }
}
