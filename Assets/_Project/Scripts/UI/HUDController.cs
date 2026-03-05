using System;
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
        private GameObject _draftRoot;
        private GameObject _runEndRoot;
        private Font _font;
        private bool _initialized;

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

                _scrapText = CreateText("ScrapText", canvas.transform, _font, TextAnchor.UpperLeft, new Vector2(20f, -20f), new Vector2(350f, 50f), 28);
                _waveText = CreateText("WaveText", canvas.transform, _font, TextAnchor.UpperCenter, new Vector2(0f, -20f), new Vector2(350f, 50f), 28);
                _integrityText = CreateText("IntegrityText", canvas.transform, _font, TextAnchor.UpperRight, new Vector2(-20f, -20f), new Vector2(350f, 50f), 28);
                _statusText = CreateText("StatusText", canvas.transform, _font, TextAnchor.MiddleCenter, new Vector2(0f, -80f), new Vector2(900f, 60f), 24);
                _restartButton = CreateButton(canvas.transform, _font, "RestartButton", "Restart Run", new Vector2(0f, 30f), new Vector2(220f, 60f));
            }
            else
            {
                _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            EnsureExtraHudElements();

            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(() => RestartRequested?.Invoke());
            HideDraftPick();
            HideRunEndOverlay();
            HidePrepCountdown();
            _initialized = true;
        }

        public void SetScrap(int scrap)
        {
            if (_scrapText != null)
            {
                _scrapText.text = $"Scrap: {scrap}";
            }
        }

        public void SetWave(int current, int total)
        {
            if (_waveText != null)
            {
                _waveText.text = $"Wave: {current}/{total}";
            }
        }

        public void SetIntegrity(int integrity)
        {
            if (_integrityText != null)
            {
                _integrityText.text = $"Integrity: {integrity}";
            }
        }

        public void SetStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        public void SetFloorName(string floorName)
        {
            if (_floorText != null)
            {
                _floorText.text = floorName;
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
            EnsureDraftOverlay();
            if (_draftRoot == null)
            {
                return;
            }

            _draftRoot.SetActive(true);
            if (autoSelect)
            {
                onCardSelected?.Invoke(0);
                _draftRoot.SetActive(false);
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                Transform buttonTransform = _draftRoot.transform.Find($"Card_{i}/Button");
                if (buttonTransform == null)
                {
                    continue;
                }

                Button button = buttonTransform.GetComponent<Button>();
                if (button == null)
                {
                    continue;
                }

                int captured = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    onCardSelected?.Invoke(captured);
                    _draftRoot.SetActive(false);
                });
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
                summary.text = $"Floors Cleared: {floorsCleared}\nAliens Killed: {totalKills}\nScrap Earned: {totalScrapEarned}";
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

        private void EnsureExtraHudElements()
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

            if (_floorText == null)
            {
                _floorText = CreateText(
                    "FloorText",
                    canvas.transform,
                    _font,
                    TextAnchor.UpperCenter,
                    new Vector2(0f, -56f),
                    new Vector2(480f, 42f),
                    22);
            }

            if (_prepCountdownText == null)
            {
                _prepCountdownText = CreateText(
                    "PrepCountdownText",
                    canvas.transform,
                    _font,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f),
                    new Vector2(900f, 70f),
                    38);
            }

            _prepCountdownText.gameObject.SetActive(false);
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
            background.color = new Color(0f, 0f, 0f, 0.8f);

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
                cardRect.anchoredPosition = new Vector2((i - 1) * 260f, -10f);
                cardRect.sizeDelta = new Vector2(220f, 260f);
                Image cardBg = card.AddComponent<Image>();
                cardBg.color = new Color(0.16f, 0.2f, 0.28f, 0.95f);

                CreateText(
                    "CardTitle",
                    card.transform,
                    _font,
                    TextAnchor.UpperCenter,
                    new Vector2(0f, -20f),
                    new Vector2(190f, 44f),
                    24).text = $"Option {i + 1}";

                CreateText(
                    "CardBody",
                    card.transform,
                    _font,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 28f),
                    new Vector2(180f, 110f),
                    16).text = "Placeholder draft card.\nStage 6 will add full effects.";

                Button button = CreateButton(
                    card.transform,
                    _font,
                    "Button",
                    "Select",
                    new Vector2(0f, 18f),
                    new Vector2(160f, 48f));
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.5f, 0f);
                buttonRect.anchorMax = new Vector2(0.5f, 0f);
                buttonRect.pivot = new Vector2(0.5f, 0f);
            }

            _draftRoot.SetActive(false);
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
                new Vector2(0f, -20f),
                new Vector2(700f, 180f),
                30);
            summary.text = string.Empty;

            Button returnButton = CreateButton(
                _runEndRoot.transform,
                _font,
                "ReturnButton",
                "Return to Menu",
                new Vector2(0f, 44f),
                new Vector2(320f, 72f));
            RectTransform buttonRect = returnButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);

            _runEndRoot.SetActive(false);
        }
    }
}
