using System.Collections;
using Project.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Player
{
    [DisallowMultipleComponent]
    public class MazeObjectiveUI : MonoBehaviour
    {
        [Header("Announcement Settings")]
        [SerializeField] private string mainTitle = "ESCAPE THE MAZE";
        [SerializeField] private string subtitle = "Find the exit alive • Watch out for fire & rotating spike traps!";
        [SerializeField] private float displayDuration = 4.5f;
        [SerializeField] private float fadeDuration = 0.6f;

        [Header("Colors & Styling")]
        [SerializeField] private Color titleColor = new Color(1f, 0.85f, 0.25f, 1f); // Vibrant Gold
        [SerializeField] private Color subtitleColor = new Color(0.9f, 0.9f, 0.95f, 0.95f);
        [SerializeField] private Color badgeColor = new Color(1f, 0.45f, 0.15f, 1f); // Flame Orange
        [SerializeField] private Color bannerBgColor = new Color(0.08f, 0.08f, 0.12f, 0.88f);

        // UI references
        private Canvas targetCanvas;
        private GameObject announcementRoot;
        private CanvasGroup announcementGroup;
        private Text titleText;
        private Text subtitleText;
        private GameObject persistentPillRoot;
        private Text persistentPillText;

        private PlayerHealth playerHealth;

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            BuildObjectiveUI();
        }

        private void Start()
        {
            ShowIntroAnnouncement();
        }

        private void Update()
        {
            if (persistentPillRoot != null && persistentPillRoot.activeSelf && persistentPillText != null)
            {
                if (MazeVictoryUI.Instance != null && MazeVictoryUI.Instance.IsVictory)
                {
                    persistentPillRoot.SetActive(false);
                    return;
                }

                float elapsed = MazeVictoryUI.Instance != null ? MazeVictoryUI.Instance.ElapsedTime : Time.timeSinceLevelLoad;
                int mins = Mathf.FloorToInt(elapsed / 60f);
                int secs = Mathf.FloorToInt(elapsed % 60f);
                string timeStr = $"{mins:00}:{secs:00}";

                if (FinishLineTrigger.Instance != null)
                {
                    float dist = Vector3.Distance(transform.position, FinishLineTrigger.Instance.transform.position);
                    if (dist < 10f)
                    {
                        persistentPillText.text = $"⏱ {timeStr}  •  ✦ REACH THE EXIT! ✦";
                        persistentPillText.color = UIThemeHelper.EmeraldGreen;
                    }
                    else
                    {
                        persistentPillText.text = $"⏱ {timeStr}  •  ✦ Find Exit  •  📍 {Mathf.RoundToInt(dist)}m";
                        persistentPillText.color = titleColor;
                    }
                }
                else
                {
                    persistentPillText.text = $"⏱ {timeStr}  •  ✦ OBJECTIVE: Escape the Maze";
                    persistentPillText.color = titleColor;
                }
            }
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnRespawn += HandlePlayerRespawn;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnRespawn -= HandlePlayerRespawn;
            }
        }

        private void HandlePlayerRespawn()
        {
            ShowAnnouncement("DON'T GIVE UP", "Find the exit alive • Escape the maze!");
        }

        public void ShowIntroAnnouncement()
        {
            ShowAnnouncement(mainTitle, subtitle);
        }

        public void ShowAnnouncement(string title, string sub)
        {
            if (titleText != null) titleText.text = title;
            if (subtitleText != null) subtitleText.text = sub;

            StopAllCoroutines();
            StartCoroutine(AnnouncementRoutine());
        }

        private IEnumerator AnnouncementRoutine()
        {
            if (announcementRoot == null || announcementGroup == null) yield break;

            announcementRoot.SetActive(true);
            announcementGroup.alpha = 0f;

            // Fade in + subtle scale
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.92f;
            Vector3 endScale = Vector3.one;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                // Smooth easing
                float ease = Mathf.SmoothStep(0f, 1f, t);
                announcementGroup.alpha = ease;
                announcementRoot.transform.localScale = Vector3.Lerp(startScale, endScale, ease);
                yield return null;
            }

            announcementGroup.alpha = 1f;
            announcementRoot.transform.localScale = endScale;

            // Hold display
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float ease = Mathf.SmoothStep(0f, 1f, t);
                announcementGroup.alpha = 1f - ease;
                announcementRoot.transform.localScale = Vector3.Lerp(endScale, startScale, ease);
                yield return null;
            }

            announcementGroup.alpha = 0f;
            announcementRoot.SetActive(false);

            // Ensure persistent objective pill is active
            if (persistentPillRoot != null)
            {
                persistentPillRoot.SetActive(true);
            }
        }

        private void BuildObjectiveUI()
        {
            // Find or create Canvas
            targetCanvas = FindFirstObjectByType<Canvas>();
            if (targetCanvas == null)
            {
                var canvasObj = new GameObject("PlayerHUD_Canvas");
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 90;

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

            Font font = UIThemeHelper.GetCleanFont();
            Sprite cardSprite = UIThemeHelper.GetRoundedCardSprite();
            Sprite pillSprite = UIThemeHelper.GetPillSprite();

            // ==========================================
            // 1. CENTER INTRO ANNOUNCEMENT BANNER
            // ==========================================
            if (announcementRoot == null)
            {
                announcementRoot = new GameObject("MazeEscapeAnnouncement");
                announcementRoot.transform.SetParent(targetCanvas.transform, false);

                var rect = announcementRoot.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.72f);
                rect.anchorMax = new Vector2(0.5f, 0.72f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(740f, 185f);

                announcementGroup = announcementRoot.AddComponent<CanvasGroup>();

                // Dark Glass Backdrop with rounded corners
                var bgImg = announcementRoot.AddComponent<Image>();
                bgImg.sprite = cardSprite;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = UIThemeHelper.GlassCardBg;

                var outline = announcementRoot.AddComponent<Outline>();
                outline.effectColor = new Color(0.2f, 0.65f, 1f, 0.45f);
                outline.effectDistance = new Vector2(1.5f, 1.5f);

                // Category Badge: [ MISSION OBJECTIVE ]
                GameObject badgeObj = new GameObject("CategoryBadge");
                badgeObj.transform.SetParent(announcementRoot.transform, false);
                var badgeRect = badgeObj.AddComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(0.5f, 1f);
                badgeRect.anchorMax = new Vector2(0.5f, 1f);
                badgeRect.pivot = new Vector2(0.5f, 1f);
                badgeRect.anchoredPosition = new Vector2(0f, -14f);
                badgeRect.sizeDelta = new Vector2(250f, 28f);

                var badgeImg = badgeObj.AddComponent<Image>();
                badgeImg.sprite = pillSprite;
                badgeImg.type = Image.Type.Sliced;
                badgeImg.color = new Color(1f, 0.45f, 0.15f, 0.22f);

                GameObject badgeTextObj = new GameObject("BadgeText");
                badgeTextObj.transform.SetParent(badgeObj.transform, false);
                var btRect = badgeTextObj.AddComponent<RectTransform>();
                btRect.anchorMin = Vector2.zero;
                btRect.anchorMax = Vector2.one;
                btRect.sizeDelta = Vector2.zero;

                var badgeTxt = badgeTextObj.AddComponent<Text>();
                badgeTxt.font = font;
                badgeTxt.fontSize = 13;
                badgeTxt.alignment = TextAnchor.MiddleCenter;
                badgeTxt.fontStyle = FontStyle.Bold;
                badgeTxt.color = badgeColor;
                badgeTxt.text = "✦  MISSION OBJECTIVE  ✦";

                // Main Title: ESCAPE THE MAZE
                GameObject titleObj = new GameObject("MainTitle");
                titleObj.transform.SetParent(announcementRoot.transform, false);
                var titleRect = titleObj.AddComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 0.32f);
                titleRect.anchorMax = new Vector2(1f, 0.82f);
                titleRect.pivot = new Vector2(0.5f, 0.5f);
                titleRect.sizeDelta = Vector2.zero;

                titleText = titleObj.AddComponent<Text>();
                titleText.font = font;
                titleText.fontSize = 46;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.fontStyle = FontStyle.Bold;
                titleText.color = titleColor;
                titleText.text = mainTitle;

                // Crisp Shadow for high readability
                var shadow = titleObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
                shadow.effectDistance = new Vector2(2f, -2f);

                // Subtitle: Find the exit alive...
                GameObject subObj = new GameObject("Subtitle");
                subObj.transform.SetParent(announcementRoot.transform, false);
                var subRect = subObj.AddComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0f, 0f);
                subRect.anchorMax = new Vector2(1f, 0.35f);
                subRect.pivot = new Vector2(0.5f, 0.5f);
                subRect.sizeDelta = Vector2.zero;

                subtitleText = subObj.AddComponent<Text>();
                subtitleText.font = font;
                subtitleText.fontSize = 17;
                subtitleText.alignment = TextAnchor.MiddleCenter;
                subtitleText.color = subtitleColor;
                subtitleText.text = subtitle;

                announcementRoot.SetActive(false);
            }

            // ==========================================
            // 2. PERSISTENT TOP OBJECTIVE PILL
            // ==========================================
            if (persistentPillRoot == null)
            {
                persistentPillRoot = new GameObject("PersistentObjectivePill");
                persistentPillRoot.transform.SetParent(targetCanvas.transform, false);

                var pillRect = persistentPillRoot.AddComponent<RectTransform>();
                pillRect.anchorMin = new Vector2(0.5f, 1f);
                pillRect.anchorMax = new Vector2(0.5f, 1f);
                pillRect.pivot = new Vector2(0.5f, 1f);
                pillRect.anchoredPosition = new Vector2(0f, -20f);
                pillRect.sizeDelta = new Vector2(440f, 42f);

                var pillBg = persistentPillRoot.AddComponent<Image>();
                pillBg.sprite = pillSprite;
                pillBg.type = Image.Type.Sliced;
                pillBg.color = UIThemeHelper.GlassCardBg;

                var pillOutline = persistentPillRoot.AddComponent<Outline>();
                pillOutline.effectColor = new Color(0.2f, 0.6f, 0.9f, 0.4f);
                pillOutline.effectDistance = new Vector2(1.5f, 1.5f);

                GameObject pillTextObj = new GameObject("PillText");
                pillTextObj.transform.SetParent(persistentPillRoot.transform, false);
                var ptRect = pillTextObj.AddComponent<RectTransform>();
                ptRect.anchorMin = Vector2.zero;
                ptRect.anchorMax = Vector2.one;
                ptRect.sizeDelta = Vector2.zero;

                persistentPillText = pillTextObj.AddComponent<Text>();
                persistentPillText.font = font;
                persistentPillText.fontSize = 16;
                persistentPillText.alignment = TextAnchor.MiddleCenter;
                persistentPillText.fontStyle = FontStyle.Bold;
                persistentPillText.color = titleColor;
                persistentPillText.text = "⏱ 00:00  •  ✦ GOAL: Escape the Maze";

                var txtShadow = pillTextObj.AddComponent<Shadow>();
                txtShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
                txtShadow.effectDistance = new Vector2(1.5f, -1.5f);

                persistentPillRoot.SetActive(true);
            }
        }
    }
}
