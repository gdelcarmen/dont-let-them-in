using System;
using System.Collections;
using System.Collections.Generic;
using DontLetThemIn.Audio;
using DontLetThemIn.Core;
using DontLetThemIn.Visuals;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontLetThemIn.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        private const float ScrapTweenDuration = 0.24f;

        private Canvas _canvas;
        private Font _font;
        private bool _initialized;
        private int _integrityMax = 10;
        private int _displayedScrap;
        private Coroutine _scrapTweenRoutine;
        private Coroutine _breachFeedbackRoutine;

        private Text _scrapText;
        private Text _waveText;
        private Text _integrityText;
        private Text _statusText;
        private Text _floorText;
        private Text _prepCountdownText;
        private Text _waveCountdownText;
        private Button _restartButton;
        private Image _topBar;
        private Image _bottomBar;
        private Image _statusPanel;
        private Image _wavePulse;
        private Image _scrapIcon;
        private HorizontalLayoutGroup _integrityIconRow;
        private readonly List<Image> _integrityIcons = new();
        private GameObject _draftRoot;
        private GameObject _runEndRoot;
        private bool _waveStateActive;

        public event Action RestartRequested;

        public bool IsDraftPickVisible => _draftRoot != null && _draftRoot.activeSelf;

        public bool IsRunEndVisible => _runEndRoot != null && _runEndRoot.activeSelf;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildCanvas();
            BuildHud();
            ApplyTheme(VisualThemeRuntime.ActiveTheme);

            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(AudioManager.TryPlayUiButton);
            _restartButton.onClick.AddListener(() => RestartRequested?.Invoke());

            HideDraftPick();
            HideRunEndOverlay();
            HidePrepCountdown();
            HideWaveCountdown();
            SetStatus(string.Empty);
            SetRestartVisible(true);
            _initialized = true;
        }

        public void ApplyTheme(VisualTheme theme)
        {
            if (theme == null)
            {
                return;
            }

            Color hudBackground = theme.Ui.HudBackgroundColor;
            hudBackground.a = theme.Ui.HudBackgroundOpacity;
            _topBar.color = hudBackground;
            _bottomBar.color = new Color(hudBackground.r, hudBackground.g, hudBackground.b, hudBackground.a * 0.96f);
            _statusPanel.color = new Color(hudBackground.r, hudBackground.g, hudBackground.b, hudBackground.a * 0.86f);
            _wavePulse.color = new Color(theme.Threat.WindowGlowColor.r, theme.Threat.WindowGlowColor.g, theme.Threat.WindowGlowColor.b, 0.12f);
            _scrapIcon.color = theme.Ui.ScrapIconTint;

            foreach (Text text in _canvas.GetComponentsInChildren<Text>(true))
            {
                if (text == null)
                {
                    continue;
                }

                text.color = theme.Ui.HudTextColor;
            }
        }

        public void SetWaveStateActive(bool active)
        {
            if (_wavePulse == null)
            {
                return;
            }

            _waveStateActive = active;
        }

        private void Update()
        {
            if (_wavePulse != null)
            {
                float alpha = _waveStateActive
                    ? 0.2f + Mathf.PingPong(Time.unscaledTime * 0.22f, 0.12f)
                    : 0.08f;
                Color color = _wavePulse.color;
                color.a = alpha;
                _wavePulse.color = color;
            }
        }

        public void SetScrap(int scrap)
        {
            scrap = Mathf.Max(0, scrap);
            if (_scrapTweenRoutine != null)
            {
                StopCoroutine(_scrapTweenRoutine);
            }

            _scrapTweenRoutine = StartCoroutine(AnimateScrapCounter(_displayedScrap, scrap));
        }

        public void SetWave(int current, int total)
        {
            _waveText.text = $"WAVE {Mathf.Max(0, current)}/{Mathf.Max(1, total)}";
        }

        public void ShowWaveCountdown(int secondsRemaining)
        {
            _waveCountdownText.text = $"Threat spike in {Mathf.Max(0, secondsRemaining)}";
            _waveCountdownText.gameObject.SetActive(true);
        }

        public void HideWaveCountdown()
        {
            _waveCountdownText.gameObject.SetActive(false);
        }

        public void SetIntegrityMax(int integrityMax)
        {
            _integrityMax = Mathf.Max(1, integrityMax);
            RebuildIntegrityIcons();
        }

        public void SetIntegrity(int integrity)
        {
            int clamped = Mathf.Clamp(integrity, 0, _integrityMax);
            _integrityText.text = $"{clamped}/{_integrityMax}";
            RebuildIntegrityIcons();

            for (int i = 0; i < _integrityIcons.Count; i++)
            {
                bool intact = i < clamped;
                _integrityIcons[i].color = intact
                    ? new Color(0.96f, 0.82f, 0.52f, 1f)
                    : new Color(0.32f, 0.28f, 0.24f, 0.92f);
            }
        }

        public void SetStatus(string status)
        {
            _statusText.text = status ?? string.Empty;
        }

        public void SetFloorName(string floorName)
        {
            _floorText.text = string.IsNullOrWhiteSpace(floorName) ? "Ground Floor" : floorName;
        }

        public void SetRestartVisible(bool visible)
        {
            _restartButton.gameObject.SetActive(visible);
        }

        public void PlaySafeRoomBreachFeedback()
        {
            if (_breachFeedbackRoutine != null)
            {
                StopCoroutine(_breachFeedbackRoutine);
            }

            _breachFeedbackRoutine = StartCoroutine(BreachFeedbackRoutine());
        }

        public void ShowPrepCountdown(int secondsRemaining)
        {
            _prepCountdownText.text = $"PREP PHASE  {Mathf.Max(0, secondsRemaining)}";
            _prepCountdownText.gameObject.SetActive(true);
        }

        public void HidePrepCountdown()
        {
            _prepCountdownText.gameObject.SetActive(false);
        }

        public void ShowDraftPick(Action<int> onCardSelected, bool autoSelect = false)
        {
            ShowDraftPick(Array.Empty<DraftOffer>(), onCardSelected, autoSelect);
        }

        public void ShowDraftPick(IReadOnlyList<DraftOffer> offers, Action<int> onCardSelected, bool autoSelect = false)
        {
            EnsureDraftOverlay();
            _draftRoot.SetActive(true);

            if (autoSelect)
            {
                onCardSelected?.Invoke(0);
                _draftRoot.SetActive(false);
                return;
            }

            int count = Mathf.Clamp(offers != null ? offers.Count : 3, 1, 4);
            for (int i = 0; i < 4; i++)
            {
                Transform card = _draftRoot.transform.Find($"Card_{i}");
                if (card == null)
                {
                    continue;
                }

                bool active = i < count;
                card.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                DraftOffer offer = offers != null && i < offers.Count ? offers[i] : null;
                ConfigureDraftCard(card, offer, i, onCardSelected);
            }
        }

        public void HideDraftPick()
        {
            if (_draftRoot != null)
            {
                _draftRoot.SetActive(false);
            }
        }

        public void ShowRunEndOverlay(bool survived, int floorsCleared, int totalKills, int totalScrapEarned, Action onReturnToMenu)
        {
            ShowRunEndOverlay(survived, floorsCleared, totalKills, totalScrapEarned, "N/A", 0, 0, 0, onReturnToMenu);
        }

        public void ShowRunEndOverlay(bool survived, int floorsCleared, int totalKills, int totalScrapEarned, string bestDefense, Action onReturnToMenu)
        {
            ShowRunEndOverlay(survived, floorsCleared, totalKills, totalScrapEarned, bestDefense, 0, 0, 0, onReturnToMenu);
        }

        public void ShowRunEndOverlay(
            bool survived,
            int floorsCleared,
            int totalKills,
            int totalScrapEarned,
            string bestDefense,
            int salvageEarned,
            int totalSalvage,
            int highestLoop,
            Action onReturnToMenu)
        {
            EnsureRunEndOverlay();
            _runEndRoot.SetActive(true);
            Text title = _runEndRoot.transform.Find("Title")?.GetComponent<Text>();
            if (title != null)
            {
                title.text = survived ? "HOUSE SECURED" : "THEY GOT IN";
            }

            Text summary = _runEndRoot.transform.Find("Summary")?.GetComponent<Text>();
            if (summary != null)
            {
                string loopLine = highestLoop > 0 ? $"\nHighest Loop: {highestLoop}" : string.Empty;
                summary.text =
                    $"Final Wave Defense Report\n\n" +
                    $"Floors Cleared: {floorsCleared}/3\n" +
                    $"Aliens Eliminated: {totalKills}\n" +
                    $"Scrap Earned: {totalScrapEarned}\n" +
                    $"Salvage Gained: {Mathf.Max(0, salvageEarned)}\n" +
                    $"Salvage Bank: {Mathf.Max(0, totalSalvage)}\n" +
                    $"MVP Defense: {bestDefense}{loopLine}";
            }

            BindOverlayButton(_runEndRoot.transform.Find("ReturnButton")?.GetComponent<Button>(), "MAIN MENU", () =>
            {
                onReturnToMenu?.Invoke();
                if (SceneManager.GetActiveScene().name != "MainMenu")
                {
                    SceneManager.LoadScene("MainMenu");
                }
            });
        }

        public void HideRunEndOverlay()
        {
            if (_runEndRoot != null)
            {
                _runEndRoot.SetActive(false);
            }
        }

        private void BuildCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void BuildHud()
        {
            _topBar = CreatePanel("TopBar", _canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1080f, 120f));
            _bottomBar = CreatePanel("BottomBar", _canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1080f, 108f));
            _statusPanel = CreatePanel("StatusPanel", _canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(760f, 44f));
            _wavePulse = CreatePanel("WavePulse", _topBar.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(200f, 64f));
            _wavePulse.transform.SetAsFirstSibling();

            _scrapText = CreateText("ScrapText", _topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, 0f), new Vector2(240f, 48f), 28, TextAnchor.MiddleLeft);
            _waveText = CreateText("WaveText", _topBar.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(260f, 48f), 30, TextAnchor.MiddleCenter);
            _integrityText = CreateText("IntegrityText", _topBar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-66f, 16f), new Vector2(84f, 28f), 18, TextAnchor.MiddleRight);
            _floorText = CreateText("FloorText", _topBar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-58f, -18f), new Vector2(220f, 26f), 18, TextAnchor.MiddleRight);
            _statusText = CreateText("StatusText", _statusPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 36f), 18, TextAnchor.MiddleCenter);
            _prepCountdownText = CreateText("PrepCountdownText", _canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(560f, 64f), 34, TextAnchor.MiddleCenter);
            _waveCountdownText = CreateText("WaveCountdownText", _canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -192f), new Vector2(300f, 34f), 18, TextAnchor.MiddleCenter);

            _scrapIcon = CreateIcon("ScrapIcon", _topBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(28f, 28f));
            _integrityIconRow = CreateIntegrityRow(_topBar.transform);
            _restartButton = CreateButton("RestartButton", _canvas.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36f, 140f), new Vector2(160f, 44f), "TRY AGAIN");
        }

        private IEnumerator AnimateScrapCounter(int startValue, int endValue)
        {
            if (startValue == endValue)
            {
                _displayedScrap = endValue;
                _scrapText.text = endValue.ToString();
                _scrapTweenRoutine = null;
                yield break;
            }

            Vector3 startScale = _scrapText.transform.localScale;
            _scrapText.transform.localScale = Vector3.one * 1.1f;
            float elapsed = 0f;
            while (elapsed < ScrapTweenDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ScrapTweenDuration);
                _displayedScrap = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, t));
                _scrapText.text = _displayedScrap.ToString();
                yield return null;
            }

            _scrapText.transform.localScale = startScale;
            _displayedScrap = endValue;
            _scrapText.text = endValue.ToString();
            _scrapTweenRoutine = null;
        }

        private IEnumerator BreachFeedbackRoutine()
        {
            Vector3 startScale = _statusPanel.transform.localScale;
            Color original = _statusPanel.color;
            for (int i = 0; i < 2; i++)
            {
                _statusPanel.color = new Color(0.78f, 0.14f, 0.12f, 0.92f);
                _statusPanel.transform.localScale = Vector3.one * 1.03f;
                yield return new WaitForSecondsRealtime(0.12f);
                _statusPanel.color = original;
                _statusPanel.transform.localScale = startScale;
                yield return new WaitForSecondsRealtime(0.08f);
            }
        }

        private void RebuildIntegrityIcons()
        {
            if (_integrityIconRow == null)
            {
                return;
            }

            for (int i = _integrityIcons.Count; i < _integrityMax; i++)
            {
                Image icon = CreateIcon($"House_{i}", _integrityIconRow.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(18f, 18f));
                _integrityIcons.Add(icon);
            }

            for (int i = 0; i < _integrityIcons.Count; i++)
            {
                _integrityIcons[i].gameObject.SetActive(i < _integrityMax);
            }
        }

        private void EnsureDraftOverlay()
        {
            if (_draftRoot != null)
            {
                return;
            }

            _draftRoot = new GameObject("DraftPickOverlay");
            _draftRoot.transform.SetParent(_canvas.transform, false);
            RectTransform rootRect = _draftRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image background = _draftRoot.AddComponent<Image>();
            background.color = new Color(0.02f, 0.03f, 0.05f, 0.86f);

            CreateText("Title", _draftRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(640f, 60f), 36, TextAnchor.MiddleCenter).text = "Pick A Household Upgrade";
            for (int i = 0; i < 4; i++)
            {
                GameObject card = new($"Card_{i}");
                card.transform.SetParent(_draftRoot.transform, false);
                RectTransform rect = card.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-330f + (i * 220f), -12f);
                rect.sizeDelta = new Vector2(200f, 236f);
                Image image = card.AddComponent<Image>();
                image.color = new Color(0.1f, 0.12f, 0.16f, 0.96f);
                Button button = card.AddComponent<Button>();
                button.onClick.AddListener(AudioManager.TryPlayUiButton);

                CreateText("Title", card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(176f, 56f), 22, TextAnchor.UpperCenter);
                CreateText("Description", card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(176f, 92f), 15, TextAnchor.UpperCenter);
                CreateText("Meta", card.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(176f, 42f), 13, TextAnchor.MiddleCenter);
            }

            _draftRoot.SetActive(false);
        }

        private void ConfigureDraftCard(Transform cardTransform, DraftOffer offer, int index, Action<int> onCardSelected)
        {
            Text title = cardTransform.Find("Title")?.GetComponent<Text>();
            Text description = cardTransform.Find("Description")?.GetComponent<Text>();
            Text meta = cardTransform.Find("Meta")?.GetComponent<Text>();
            Image background = cardTransform.GetComponent<Image>();
            Button button = cardTransform.GetComponent<Button>();

            if (title != null)
            {
                title.text = offer?.Title ?? $"Option {index + 1}";
            }

            if (description != null)
            {
                description.text = offer?.Description ?? "Keep the house barely together.";
            }

            if (meta != null)
            {
                meta.text = offer != null ? offer.OfferType.ToString().ToUpperInvariant() : "PERK";
            }

            if (background != null)
            {
                background.color = offer != null
                    ? Color.Lerp(new Color(0.08f, 0.1f, 0.14f, 0.96f), offer.AccentColor, 0.22f)
                    : new Color(0.1f, 0.12f, 0.16f, 0.96f);
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(AudioManager.TryPlayUiButton);
                button.onClick.AddListener(() => onCardSelected?.Invoke(index));
            }
        }

        private void EnsureRunEndOverlay()
        {
            if (_runEndRoot != null)
            {
                return;
            }

            _runEndRoot = new GameObject("RunEndOverlay");
            _runEndRoot.transform.SetParent(_canvas.transform, false);
            RectTransform rootRect = _runEndRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image background = _runEndRoot.AddComponent<Image>();
            background.color = new Color(0.02f, 0.02f, 0.03f, 0.9f);

            Image panel = CreatePanel("Panel", _runEndRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 420f));
            panel.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);
            CreateText("Title", panel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(460f, 50f), 34, TextAnchor.MiddleCenter);
            CreateText("Summary", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(460f, 220f), 18, TextAnchor.UpperCenter);
            CreateButton("ReturnButton", panel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(240f, 44f), "MAIN MENU");
            _runEndRoot.SetActive(false);
        }

        private void BindOverlayButton(Button button, string label, Action action)
        {
            if (button == null)
            {
                return;
            }

            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(AudioManager.TryPlayUiButton);
            button.onClick.AddListener(() => action?.Invoke());
        }

        private Image CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject panel = new(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image image = panel.AddComponent<Image>();
            image.sprite = global::DontLetThemIn.RuntimeSpriteFactory.GetPaperSprite();
            return image;
        }

        private Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor anchor)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Text text = textObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Image CreateIcon(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject iconObject = new(name);
            iconObject.transform.SetParent(parent, false);
            RectTransform rect = iconObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image image = iconObject.AddComponent<Image>();
            image.sprite = global::DontLetThemIn.RuntimeSpriteFactory.GetCircleSprite();
            return image;
        }

        private HorizontalLayoutGroup CreateIntegrityRow(Transform parent)
        {
            GameObject row = new("IntegrityIcons");
            row.transform.SetParent(parent, false);
            RectTransform rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-56f, -10f);
            rect.sizeDelta = new Vector2(220f, 22f);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleRight;
            return layout;
        }

        private Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, string label)
        {
            GameObject buttonObject = new(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image image = buttonObject.AddComponent<Image>();
            image.sprite = global::DontLetThemIn.RuntimeSpriteFactory.GetPaperSprite();
            image.color = new Color(0.18f, 0.22f, 0.28f, 0.96f);
            Button button = buttonObject.AddComponent<Button>();
            Text text = CreateText("Label", buttonObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, 18, TextAnchor.MiddleCenter);
            text.text = label;
            return button;
        }
    }
}
