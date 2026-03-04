using System;
using UnityEngine;
using UnityEngine.UI;

namespace DontLetThemIn.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        private Text _scrapText;
        private Text _waveText;
        private Text _integrityText;
        private Text _statusText;
        private Button _restartButton;

        public event Action RestartRequested;

        public void Initialize()
        {
            Canvas canvas = CreateCanvas();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _scrapText = CreateText("ScrapText", canvas.transform, font, TextAnchor.UpperLeft, new Vector2(20f, -20f));
            _waveText = CreateText("WaveText", canvas.transform, font, TextAnchor.UpperCenter, new Vector2(0f, -20f));
            _integrityText = CreateText("IntegrityText", canvas.transform, font, TextAnchor.UpperRight, new Vector2(-20f, -20f));

            _statusText = CreateText("StatusText", canvas.transform, font, TextAnchor.MiddleCenter, new Vector2(0f, -80f));
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.fontSize = 24;

            _restartButton = CreateButton(canvas.transform, font);
            _restartButton.onClick.AddListener(() => RestartRequested?.Invoke());
        }

        public void SetScrap(int scrap)
        {
            _scrapText.text = $"Scrap: {scrap}";
        }

        public void SetWave(int current, int total)
        {
            _waveText.text = $"Wave: {current}/{total}";
        }

        public void SetIntegrity(int integrity)
        {
            _integrityText.text = $"Integrity: {integrity}";
        }

        public void SetStatus(string status)
        {
            _statusText.text = status;
        }

        public void SetRestartVisible(bool visible)
        {
            _restartButton.gameObject.SetActive(visible);
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
            Vector2 anchoredPosition)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(350f, 50f);

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 28;
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
            else
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }

            rect.anchoredPosition = anchoredPosition;
            return text;
        }

        private static Button CreateButton(Transform parent, Font font)
        {
            GameObject buttonObject = new("RestartButton");
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 30f);
            rect.sizeDelta = new Vector2(220f, 60f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.25f, 0.95f);
            colors.pressedColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
            button.colors = colors;

            GameObject textObject = new("Text");
            textObject.transform.SetParent(buttonObject.transform, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = "Restart Run";
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
    }
}
