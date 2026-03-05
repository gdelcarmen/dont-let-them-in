using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontLetThemIn.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string campaignSceneName = "GameScene";

        private Font _font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureController()
        {
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                return;
            }

            if (Object.FindFirstObjectByType<MainMenuController>() != null)
            {
                return;
            }

            GameObject root = GameObject.Find("MainMenuRoot");
            if (root == null)
            {
                root = new GameObject("MainMenuRoot");
            }

            root.AddComponent<MainMenuController>();
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                return;
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildMenuUi();
        }

        private void BuildMenuUi()
        {
            Canvas canvas = FindOrCreateCanvas();
            if (canvas == null)
            {
                return;
            }

            RectTransform root = canvas.transform as RectTransform;
            if (root == null)
            {
                return;
            }

            Text title = FindOrCreateText(
                root,
                "TitleText",
                "DON'T LET THEM IN",
                64,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 145f),
                new Vector2(950f, 90f));
            title.color = new Color(0.96f, 0.84f, 0.67f, 1f);
            title.fontStyle = FontStyle.Bold;

            Text subtitle = FindOrCreateText(
                root,
                "SubtitleText",
                "Home Alone. In space. Sort of.",
                28,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 82f),
                new Vector2(900f, 48f));
            subtitle.color = new Color(0.92f, 0.92f, 0.92f, 1f);

            EnsureButton(
                root,
                "CampaignButton",
                "Campaign",
                new Vector2(0f, -26f),
                new Vector2(290f, 72f),
                interactable: true,
                onClick: () => SceneManager.LoadScene(campaignSceneName),
                showComingSoon: false);

            EnsureButton(
                root,
                "EndlessButton",
                "Endless",
                new Vector2(0f, -114f),
                new Vector2(290f, 72f),
                interactable: false,
                onClick: null,
                showComingSoon: true);

            EnsureButton(
                root,
                "MetaUpgradesButton",
                "Meta Upgrades",
                new Vector2(0f, -202f),
                new Vector2(290f, 72f),
                interactable: false,
                onClick: null,
                showComingSoon: true);
        }

        private Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.name = "MainMenuCanvas";
                return canvas;
            }

            GameObject canvasObject = new("MainMenuCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private Text FindOrCreateText(
            RectTransform parent,
            string objectName,
            string content,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            Transform existing = parent.Find(objectName);
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
            if (existing == null)
            {
                textObject.transform.SetParent(parent, false);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            if (text == null)
            {
                text = textObject.AddComponent<Text>();
            }

            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = content;
            return text;
        }

        private void EnsureButton(
            RectTransform parent,
            string objectName,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            bool interactable,
            UnityEngine.Events.UnityAction onClick,
            bool showComingSoon)
        {
            Transform existing = parent.Find(objectName);
            GameObject buttonObject = existing != null ? existing.gameObject : new GameObject(objectName);
            if (existing == null)
            {
                buttonObject.transform.SetParent(parent, false);
            }

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = buttonObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }

            image.color = interactable
                ? new Color(0.23f, 0.24f, 0.26f, 0.95f)
                : new Color(0.17f, 0.17f, 0.17f, 0.85f);

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            button.interactable = interactable;

            Transform textTransform = buttonObject.transform.Find("Text");
            Text text = textTransform != null ? textTransform.GetComponent<Text>() : null;
            if (text == null)
            {
                GameObject textObject = new("Text");
                textObject.transform.SetParent(buttonObject.transform, false);
                text = textObject.AddComponent<Text>();
            }

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(0f, 4f);
            textRect.offsetMax = new Vector2(0f, -4f);
            text.font = _font;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 26;
            text.color = interactable ? Color.white : new Color(0.75f, 0.75f, 0.75f, 0.9f);
            text.text = label;

            Transform badgeTransform = buttonObject.transform.Find("ComingSoon");
            Text badge = badgeTransform != null ? badgeTransform.GetComponent<Text>() : null;
            if (showComingSoon)
            {
                if (badge == null)
                {
                    GameObject badgeObject = new("ComingSoon");
                    badgeObject.transform.SetParent(buttonObject.transform, false);
                    badge = badgeObject.AddComponent<Text>();
                }

                RectTransform badgeRect = badge.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(1f, 0.5f);
                badgeRect.anchorMax = new Vector2(1f, 0.5f);
                badgeRect.pivot = new Vector2(1f, 0.5f);
                badgeRect.anchoredPosition = new Vector2(-8f, -20f);
                badgeRect.sizeDelta = new Vector2(124f, 24f);
                badge.font = _font;
                badge.fontSize = 13;
                badge.alignment = TextAnchor.MiddleRight;
                badge.color = new Color(0.95f, 0.8f, 0.35f, 0.95f);
                badge.text = "Coming Soon";
            }
            else if (badge != null)
            {
                badge.gameObject.SetActive(false);
            }
        }
    }
}
