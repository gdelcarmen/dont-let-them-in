using System;
using System.Collections;
using System.Collections.Generic;
using DontLetThemIn.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontLetThemIn.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private Text _scrapText;
        [SerializeField] private Text _waveText;
        [SerializeField] private Text _integrityText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _restartButton;

        private Text _floorText;
        private Text _prepCountdownText;
        private Text _waveCountdownText;
        private Text _integrityBarLabel;
        private Image _integrityBarFill;
        private Image _scrapIcon;

        private GameObject _draftRoot;
        private GameObject _runEndRoot;
        private Font _font;
        private bool _initialized;
        private bool _draftSelectionLocked;

        private int _integrityMax = 10;
        private int _displayedScrap;
        private Coroutine _scrapTweenRoutine;

        public event Action RestartRequested;

        public bool IsDraftPickVisible => _draftRoot != null && _draftRoot.activeSelf;

        public bool IsRunEndVisible => _runEndRoot != null && _runEndRoot.activeSelf;

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            if (!TryBindExistingHud())
            {
                Canvas canvas = CreateCanvas();
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                _scrapText = CreateText("ScrapText", canvas.transform, _font, TextAnchor.UpperLeft, new Vector2(56f, -20f), new Vector2(340f, 50f), 28);
                _waveText = CreateText("WaveText", canvas.transform, _font, TextAnchor.UpperCenter, new Vector2(0f, -20f), new Vector2(360f, 50f), 28);
                _integrityText = CreateText("IntegrityText", canvas.transform, _font, TextAnchor.UpperRight, new Vector2(-20f, -20f), new Vector2(350f, 50f), 26);
                _statusText = CreateText("StatusText", canvas.transform, _font, TextAnchor.MiddleCenter, new Vector2(0f, -84f), new Vector2(980f, 60f), 24);
                _restartButton = CreateButton(canvas.transform, _font, "RestartButton", "Restart Run", new Vector2(0f, 30f), new Vector2(220f, 60f));
            }
            else
            {
                _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            EnsureExtendedHudElements();

            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(() => RestartRequested?.Invoke());

            HideWaveCountdown();
            HideDraftPick();
            HideRunEndOverlay();
            HidePrepCountdown();
            SetStatus(string.Empty);
            _initialized = true;
        }

        public void SetScrap(int scrap)
        {
            int clamped = Mathf.Max(0, scrap);
            if (_scrapTweenRoutine != null)
            {
                StopCoroutine(_scrapTweenRoutine);
            }

            _scrapTweenRoutine = StartCoroutine(AnimateScrapCounter(_displayedScrap, clamped));
        }

        public void SetWave(int current, int total)
        {
            if (_waveText == null)
            {
                return;
            }

            int clampedCurrent = Mathf.Max(0, current);
            int clampedTotal = Mathf.Max(1, total);
            _waveText.text = $"Wave {clampedCurrent}/{clampedTotal}";
        }

        public void ShowWaveCountdown(int secondsRemaining)
        {
            if (_waveCountdownText == null)
            {
                return;
            }

            int clamped = Mathf.Max(0, secondsRemaining);
            _waveCountdownText.text = $"Next wave in {clamped}...";
            _waveCountdownText.gameObject.SetActive(true);
        }

        public void HideWaveCountdown()
        {
            if (_waveCountdownText != null)
            {
                _waveCountdownText.gameObject.SetActive(false);
            }
        }

        public void SetIntegrityMax(int integrityMax)
        {
            _integrityMax = Mathf.Max(1, integrityMax);
        }

        public void SetIntegrity(int integrity)
        {
            int clamped = Mathf.Max(0, integrity);
            if (_integrityText != null)
            {
                _integrityText.text = $"Integrity: {clamped}/{_integrityMax}";
            }

            if (_integrityBarFill != null)
            {
                _integrityBarFill.fillAmount = Mathf.Clamp01(clamped / (float)_integrityMax);
            }

            if (_integrityBarLabel != null)
            {
                float percent = Mathf.Clamp01(clamped / (float)_integrityMax) * 100f;
                _integrityBarLabel.text = $"{percent:0}%";
            }
        }

        public void SetStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status ?? string.Empty;
            }
        }

        public void SetFloorName(string floorName)
        {
            if (_floorText != null)
            {
                _floorText.text = string.IsNullOrWhiteSpace(floorName) ? "Floor" : floorName;
            }
        }

        public void SetRestartVisible(bool visible)
        {
            if (_restartButton != null)
            {
                _restartButton.gameObject.SetActive(visible);
            }
        }

        public void ShowPrepCountdown(int secondsRemaining)
        {
            if (_prepCountdownText == null)
            {
                return;
            }

            _prepCountdownText.text = $"PREP PHASE - {Mathf.Max(0, secondsRemaining)} seconds";
            _prepCountdownText.gameObject.SetActive(true);
        }

        public void HidePrepCountdown()
        {
            if (_prepCountdownText != null)
            {
                _prepCountdownText.gameObject.SetActive(false);
            }
        }

        public void ShowDraftPick(Action<int> onCardSelected, bool autoSelect = false)
        {
            ShowDraftPick(Array.Empty<DraftOffer>(), onCardSelected, autoSelect);
        }

        public void ShowDraftPick(IReadOnlyList<DraftOffer> offers, Action<int> onCardSelected, bool autoSelect = false)
        {
            EnsureDraftOverlay();
            if (_draftRoot == null)
            {
                return;
            }

            _draftSelectionLocked = false;
            _draftRoot.SetActive(true);

            if (autoSelect)
            {
                onCardSelected?.Invoke(0);
                _draftRoot.SetActive(false);
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                DraftOffer offer = offers != null && i < offers.Count ? offers[i] : null;
                ConfigureDraftCard(i, offer, onCardSelected);
            }
        }

        public void HideDraftPick()
        {
            if (_draftRoot != null)
            {
                _draftRoot.SetActive(false);
            }
        }

        public void ShowRunEndOverlay(
            bool survived,
            int floorsCleared,
            int totalKills,
            int totalScrapEarned,
            Action onReturnToMenu)
        {
            ShowRunEndOverlay(survived, floorsCleared, totalKills, totalScrapEarned, "N/A", onReturnToMenu);
        }

        public void ShowRunEndOverlay(
            bool survived,
            int floorsCleared,
            int totalKills,
            int totalScrapEarned,
            string bestDefense,
            Action onReturnToMenu)
        {
            EnsureRunEndOverlay();
            if (_runEndRoot == null)
            {
                return;
            }

            _runEndRoot.SetActive(true);

            Text title = _runEndRoot.transform.Find("Title")?.GetComponent<Text>();
            Text summary = _runEndRoot.transform.Find("Summary")?.GetComponent<Text>();
            Button button = _runEndRoot.transform.Find("ReturnButton")?.GetComponent<Button>();

            if (title != null)
            {
                title.text = survived ? "YOU SURVIVED!" : "THEY GOT IN.";
            }

            if (summary != null)
            {
                summary.text =
                    $"Floors Cleared: {floorsCleared}/3\n" +
                    $"Aliens Killed: {totalKills}\n" +
                    $"Scrap Earned: {totalScrapEarned}\n" +
                    $"Best Defense: {bestDefense}";
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    onReturnToMenu?.Invoke();
                    if (SceneManager.GetActiveScene().name != "MainMenu")
                    {
                        SceneManager.LoadScene("MainMenu");
                    }
                });
            }
        }

        public void HideRunEndOverlay()
        {
            if (_runEndRoot != null)
            {
                _runEndRoot.SetActive(false);
            }
        }

        private IEnumerator AnimateScrapCounter(int startValue, int endValue)
        {
            if (_scrapText == null)
            {
                _displayedScrap = endValue;
                yield break;
            }

            if (startValue == endValue)
            {
                _displayedScrap = endValue;
                _scrapText.text = $"Scrap: {endValue}";
                _scrapTweenRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            const float duration = 0.28f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int value = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, t));
                _displayedScrap = value;
                _scrapText.text = $"Scrap: {value}";
                yield return null;
            }

            _displayedScrap = endValue;
            _scrapText.text = $"Scrap: {endValue}";
            _scrapTweenRoutine = null;
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new("HUDCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            TextAnchor anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = anchor;

            if (anchor == TextAnchor.UpperLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else if (anchor == TextAnchor.UpperCenter)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
            }
            else if (anchor == TextAnchor.UpperRight)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            rect.anchoredPosition = anchoredPosition;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            Font font,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject buttonObject = new(name);
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.28f, 0.28f, 0.28f, 0.95f);
            colors.pressedColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
            button.colors = colors;

            GameObject textObject = new("Text");
            textObject.transform.SetParent(buttonObject.transform, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 22;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        private bool TryBindExistingHud()
        {
            _scrapText ??= FindTextByName("ScrapText");
            _waveText ??= FindTextByName("WaveText");
            _integrityText ??= FindTextByName("IntegrityText");
            _statusText ??= FindTextByName("StatusText");
            _floorText ??= FindTextByName("FloorText");
            _prepCountdownText ??= FindTextByName("PrepCountdownText");
            _waveCountdownText ??= FindTextByName("WaveCountdownText");

            if (_restartButton == null)
            {
                foreach (Button button in GetComponentsInChildren<Button>(true))
                {
                    if (button != null && button.name == "RestartButton")
                    {
                        _restartButton = button;
                        break;
                    }
                }
            }

            return _scrapText != null &&
                   _waveText != null &&
                   _integrityText != null &&
                   _statusText != null &&
                   _restartButton != null;
        }

        private Text FindTextByName(string objectName)
        {
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                if (text != null && text.name == objectName)
                {
                    return text;
                }
            }

            return null;
        }

        private void EnsureExtendedHudElements()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInChildren<Canvas>(true);
            }

            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            if (_scrapText != null)
            {
                _displayedScrap = ParseScrapValue(_scrapText.text);
                EnsureScrapIcon(_scrapText.transform);
            }

            if (_floorText == null)
            {
                _floorText = CreateText(
                    "FloorText",
                    canvas.transform,
                    _font,
                    TextAnchor.UpperRight,
                    new Vector2(-20f, -56f),
                    new Vector2(360f, 34f),
                    20);
            }

            if (_prepCountdownText == null)
            {
                _prepCountdownText = CreateText(
                    "PrepCountdownText",
                    canvas.transform,
                    _font,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f),
                    new Vector2(920f, 72f),
                    38);
            }

            if (_waveCountdownText == null)
            {
                _waveCountdownText = CreateText(
                    "WaveCountdownText",
                    canvas.transform,
                    _font,
                    TextAnchor.UpperCenter,
                    new Vector2(0f, -54f),
                    new Vector2(420f, 34f),
                    18);
            }

            EnsureIntegrityBar(canvas.transform);
            _prepCountdownText.gameObject.SetActive(false);
            _waveCountdownText.gameObject.SetActive(false);
        }

        private void EnsureScrapIcon(Transform scrapTransform)
        {
            if (_scrapIcon != null || scrapTransform == null)
            {
                return;
            }

            Transform existing = scrapTransform.Find("ScrapIcon");
            GameObject iconObject = existing != null ? existing.gameObject : new GameObject("ScrapIcon");
            if (existing == null)
            {
                iconObject.transform.SetParent(scrapTransform, false);
            }

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            if (iconRect == null)
            {
                iconRect = iconObject.AddComponent<RectTransform>();
            }

            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-36f, 0f);
            iconRect.sizeDelta = new Vector2(18f, 18f);

            _scrapIcon = iconObject.GetComponent<Image>();
            if (_scrapIcon == null)
            {
                _scrapIcon = iconObject.AddComponent<Image>();
            }

            _scrapIcon.color = new Color(0.95f, 0.8f, 0.32f, 1f);
        }

        private void EnsureIntegrityBar(Transform canvasTransform)
        {
            Transform root = canvasTransform.Find("IntegrityBarRoot");
            if (root == null)
            {
                GameObject rootObject = new("IntegrityBarRoot");
                rootObject.transform.SetParent(canvasTransform, false);

                RectTransform rootRect = rootObject.AddComponent<RectTransform>();
                rootRect.anchorMin = new Vector2(1f, 1f);
                rootRect.anchorMax = new Vector2(1f, 1f);
                rootRect.pivot = new Vector2(1f, 1f);
                rootRect.anchoredPosition = new Vector2(-20f, -84f);
                rootRect.sizeDelta = new Vector2(250f, 24f);

                Image background = rootObject.AddComponent<Image>();
                background.color = new Color(0.15f, 0.18f, 0.22f, 0.92f);

                GameObject fillObject = new("Fill");
                fillObject.transform.SetParent(rootObject.transform, false);
                RectTransform fillRect = fillObject.AddComponent<RectTransform>();
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(1f, 1f);
                fillRect.offsetMin = new Vector2(3f, 3f);
                fillRect.offsetMax = new Vector2(-3f, -3f);
                _integrityBarFill = fillObject.AddComponent<Image>();
                _integrityBarFill.type = Image.Type.Filled;
                _integrityBarFill.fillMethod = Image.FillMethod.Horizontal;
                _integrityBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                _integrityBarFill.color = new Color(0.28f, 0.82f, 0.4f, 1f);

                GameObject labelObject = new("Label");
                labelObject.transform.SetParent(rootObject.transform, false);
                RectTransform labelRect = labelObject.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                _integrityBarLabel = labelObject.AddComponent<Text>();
                _integrityBarLabel.font = _font;
                _integrityBarLabel.fontSize = 14;
                _integrityBarLabel.alignment = TextAnchor.MiddleCenter;
                _integrityBarLabel.color = Color.white;
            }
            else
            {
                _integrityBarFill = root.Find("Fill")?.GetComponent<Image>();
                _integrityBarLabel = root.Find("Label")?.GetComponent<Text>();
            }
        }

        private void EnsureDraftOverlay()
        {
            if (_draftRoot != null)
            {
                return;
            }

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInChildren<Canvas>(true);
            }

            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            _draftRoot = new GameObject("DraftPickOverlay");
            _draftRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = _draftRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image background = _draftRoot.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.84f);

            CreateText(
                "Title",
                _draftRoot.transform,
                _font,
                TextAnchor.UpperCenter,
                new Vector2(0f, -70f),
                new Vector2(800f, 64f),
                44).text = "Draft Pick";

            for (int i = 0; i < 3; i++)
            {
                GameObject card = new($"Card_{i}");
                card.transform.SetParent(_draftRoot.transform, false);
                RectTransform cardRect = card.AddComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = new Vector2((i - 1) * 280f, -6f);
                cardRect.sizeDelta = new Vector2(240f, 290f);
                Image cardBackground = card.AddComponent<Image>();
                cardBackground.color = new Color(0.16f, 0.2f, 0.28f, 0.95f);

                GameObject icon = new("CategoryIcon");
                icon.transform.SetParent(card.transform, false);
                RectTransform iconRect = icon.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 1f);
                iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.anchoredPosition = new Vector2(12f, -12f);
                iconRect.sizeDelta = new Vector2(22f, 22f);
                icon.AddComponent<Image>();

                CreateText(
                    "CardTitle",
                    card.transform,
                    _font,
                    TextAnchor.UpperCenter,
                    new Vector2(0f, -18f),
                    new Vector2(206f, 48f),
                    21);

                CreateText(
                    "CardBody",
                    card.transform,
                    _font,
                    TextAnchor.UpperCenter,
                    new Vector2(0f, -76f),
                    new Vector2(206f, 140f),
                    16);

                Button button = CreateButton(
                    card.transform,
                    _font,
                    "Button",
                    "Select",
                    new Vector2(0f, 18f),
                    new Vector2(170f, 48f));

                RectTransform buttonRect = button.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.5f, 0f);
                buttonRect.anchorMax = new Vector2(0.5f, 0f);
                buttonRect.pivot = new Vector2(0.5f, 0f);
            }

            _draftRoot.SetActive(false);
        }

        private void ConfigureDraftCard(int index, DraftOffer offer, Action<int> onCardSelected)
        {
            Transform card = _draftRoot.transform.Find($"Card_{index}");
            if (card == null)
            {
                return;
            }

            Text title = card.Find("CardTitle")?.GetComponent<Text>();
            Text body = card.Find("CardBody")?.GetComponent<Text>();
            Image icon = card.Find("CategoryIcon")?.GetComponent<Image>();
            Button button = card.Find("Button")?.GetComponent<Button>();

            if (title != null)
            {
                title.text = offer != null ? offer.Title : $"Option {index + 1}";
            }

            if (body != null)
            {
                body.text = offer != null
                    ? offer.Description
                    : "No draft data available.";
            }

            if (icon != null)
            {
                icon.color = offer != null
                    ? offer.AccentColor
                    : new Color(0.7f, 0.7f, 0.7f, 0.8f);
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                int captured = index;
                button.onClick.AddListener(() =>
                {
                    if (_draftSelectionLocked)
                    {
                        return;
                    }

                    _draftSelectionLocked = true;
                    StartCoroutine(DraftSelectRoutine(card, () =>
                    {
                        onCardSelected?.Invoke(captured);
                        _draftRoot.SetActive(false);
                    }));
                });
            }
        }

        private IEnumerator DraftSelectRoutine(Transform card, Action onComplete)
        {
            if (card == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 start = card.localScale;
            Vector3 peak = start * 1.08f;
            float elapsed = 0f;
            const float duration = 0.11f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                card.localScale = Vector3.Lerp(start, peak, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                card.localScale = Vector3.Lerp(peak, start, t);
                yield return null;
            }

            card.localScale = start;
            onComplete?.Invoke();
        }

        private void EnsureRunEndOverlay()
        {
            if (_runEndRoot != null)
            {
                return;
            }

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInChildren<Canvas>(true);
            }

            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            _runEndRoot = new GameObject("RunEndOverlay");
            _runEndRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = _runEndRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image background = _runEndRoot.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.86f);

            Text title = CreateText(
                "Title",
                _runEndRoot.transform,
                _font,
                TextAnchor.UpperCenter,
                new Vector2(0f, -90f),
                new Vector2(900f, 72f),
                52);
            title.text = "YOU SURVIVED!";

            Text summary = CreateText(
                "Summary",
                _runEndRoot.transform,
                _font,
                TextAnchor.MiddleCenter,
                new Vector2(0f, -26f),
                new Vector2(760f, 210f),
                28);
            summary.text = string.Empty;

            Button returnButton = CreateButton(
                _runEndRoot.transform,
                _font,
                "ReturnButton",
                "Return to Menu",
                new Vector2(0f, 54f),
                new Vector2(320f, 72f));
            RectTransform buttonRect = returnButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);

            GameObject adObject = new("AdPlaceholder");
            adObject.transform.SetParent(_runEndRoot.transform, false);
            RectTransform adRect = adObject.AddComponent<RectTransform>();
            adRect.anchorMin = new Vector2(0.5f, 0f);
            adRect.anchorMax = new Vector2(0.5f, 0f);
            adRect.pivot = new Vector2(0.5f, 0f);
            adRect.anchoredPosition = new Vector2(0f, 132f);
            adRect.sizeDelta = new Vector2(560f, 90f);
            Image adImage = adObject.AddComponent<Image>();
            adImage.color = new Color(0.12f, 0.12f, 0.12f, 0.55f);

            Text adText = CreateText(
                "Label",
                adObject.transform,
                _font,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(520f, 60f),
                20);
            adText.text = "Ad Placeholder";
            adText.color = new Color(0.88f, 0.88f, 0.88f, 0.75f);

            _runEndRoot.SetActive(false);
        }

        private static int ParseScrapValue(string scrapText)
        {
            if (string.IsNullOrWhiteSpace(scrapText))
            {
                return 0;
            }

            int colonIndex = scrapText.LastIndexOf(':');
            if (colonIndex < 0 || colonIndex + 1 >= scrapText.Length)
            {
                return 0;
            }

            if (int.TryParse(scrapText[(colonIndex + 1)..].Trim(), out int parsed))
            {
                return Mathf.Max(0, parsed);
            }

            return 0;
        }
    }
}
