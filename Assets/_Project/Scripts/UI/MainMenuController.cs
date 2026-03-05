using System.Collections.Generic;
using DontLetThemIn.Audio;
using DontLetThemIn.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontLetThemIn.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string campaignSceneName = "GameScene";
        [SerializeField] private string metaUpgradesSceneName = "MetaUpgrades";

        private readonly Dictionary<CampaignTier, Button> _tierButtons = new();
        private Font _font;
        private CampaignTier _selectedTier = CampaignTier.Normal;
        private MetaProgressionSaveData _metaProgression;
        private Text _tierInfoText;
        private Button _campaignButton;
        private Button _endlessButton;
        private Button _metaUpgradesButton;

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
            _metaProgression = MetaProgressionService.Reload();
            RunLaunchConfig.ResetToDefaults();
            BuildMenuUi();
            SelectTier(CampaignTier.Normal);
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

            Text tierHeader = FindOrCreateText(
                root,
                "TierHeader",
                "Campaign Tier",
                24,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f),
                new Vector2(420f, 40f));
            tierHeader.color = new Color(0.88f, 0.9f, 0.95f, 1f);

            _tierInfoText = FindOrCreateText(
                root,
                "TierInfoText",
                string.Empty,
                16,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -12f),
                new Vector2(680f, 28f));
            _tierInfoText.color = new Color(0.82f, 0.84f, 0.9f, 0.95f);

            EnsureTierButton(root, CampaignTier.Normal, "Normal", new Vector2(-220f, -54f));
            EnsureTierButton(root, CampaignTier.Infestation, "Infestation", new Vector2(0f, -54f));
            EnsureTierButton(root, CampaignTier.Swarm, "Swarm", new Vector2(220f, -54f));

            _campaignButton = EnsureMenuButton(
                root,
                "CampaignButton",
                "Campaign",
                new Vector2(0f, -140f),
                new Vector2(290f, 72f),
                onClick: StartCampaign);

            _endlessButton = EnsureMenuButton(
                root,
                "EndlessButton",
                "Endless",
                new Vector2(0f, -226f),
                new Vector2(290f, 72f),
                onClick: StartEndless);

            _metaUpgradesButton = EnsureMenuButton(
                root,
                "MetaUpgradesButton",
                "Meta Upgrades",
                new Vector2(0f, -312f),
                new Vector2(290f, 72f),
                onClick: OpenMetaUpgrades);

            RefreshTierVisuals();
            RefreshModeButtons();
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

        private Button EnsureMenuButton(
            RectTransform parent,
            string objectName,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            UnityEngine.Events.UnityAction onClick)
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

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(AudioManager.TryPlayUiButton);
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            Text labelText = EnsureButtonLabel(buttonObject.transform);
            labelText.text = label;
            return button;
        }

        private Button EnsureTierButton(RectTransform parent, CampaignTier tier, string label, Vector2 anchoredPosition)
        {
            Button button = EnsureMenuButton(
                parent,
                $"{tier}TierButton",
                label,
                anchoredPosition,
                new Vector2(190f, 56f),
                () => SelectTier(tier));
            _tierButtons[tier] = button;
            return button;
        }

        private static Text EnsureButtonLabel(Transform buttonTransform)
        {
            Transform textTransform = buttonTransform.Find("Text");
            Text text = textTransform != null ? textTransform.GetComponent<Text>() : null;
            if (text == null)
            {
                GameObject textObject = new("Text");
                textObject.transform.SetParent(buttonTransform, false);
                text = textObject.AddComponent<Text>();
            }

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(0f, 4f);
            textRect.offsetMax = new Vector2(0f, -4f);
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 24;
            return text;
        }

        private static void SetButtonVisual(Button button, bool interactable, bool highlighted)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = highlighted
                    ? new Color(0.28f, 0.36f, 0.5f, 0.96f)
                    : interactable
                        ? new Color(0.23f, 0.24f, 0.26f, 0.95f)
                        : new Color(0.17f, 0.17f, 0.17f, 0.85f);
            }

            Text label = button.transform.Find("Text")?.GetComponent<Text>();
            if (label != null)
            {
                label.color = interactable
                    ? Color.white
                    : new Color(0.72f, 0.72f, 0.72f, 0.85f);
            }
        }

        private void SelectTier(CampaignTier tier)
        {
            if (!MetaProgressionService.IsTierUnlocked(tier))
            {
                return;
            }

            _selectedTier = tier;
            RefreshTierVisuals();
        }

        private void RefreshTierVisuals()
        {
            foreach (KeyValuePair<CampaignTier, Button> pair in _tierButtons)
            {
                CampaignTier tier = pair.Key;
                Button button = pair.Value;
                bool unlocked = MetaProgressionService.IsTierUnlocked(tier);
                bool selected = tier == _selectedTier;
                SetButtonVisual(button, unlocked, selected);

                Text label = button != null ? button.transform.Find("Text")?.GetComponent<Text>() : null;
                if (label != null)
                {
                    string baseName = tier.ToString();
                    label.text = unlocked ? baseName : $"{baseName} (Locked)";
                    label.font = _font;
                    label.fontSize = 19;
                }
            }

            if (_tierInfoText != null)
            {
                int highestUnlocked = MetaProgressionService.GetHighestTierUnlocked();
                string highestName = ((CampaignTier)highestUnlocked).ToString();
                _tierInfoText.text = $"Selected: {_selectedTier}  |  Highest Unlocked: {highestName}";
            }
        }

        private void RefreshModeButtons()
        {
            if (_metaProgression == null)
            {
                _metaProgression = MetaProgressionService.Load();
            }

            SetButtonVisual(_campaignButton, interactable: true, highlighted: false);
            SetButtonVisual(_endlessButton, interactable: _metaProgression.EndlessUnlocked, highlighted: false);
            SetButtonVisual(_metaUpgradesButton, interactable: true, highlighted: false);

            Text endlessLabel = _endlessButton != null ? _endlessButton.transform.Find("Text")?.GetComponent<Text>() : null;
            if (endlessLabel != null)
            {
                endlessLabel.font = _font;
                endlessLabel.text = _metaProgression.EndlessUnlocked
                    ? "Endless"
                    : "Endless (Locked)";
            }

            Text metaLabel = _metaUpgradesButton != null ? _metaUpgradesButton.transform.Find("Text")?.GetComponent<Text>() : null;
            if (metaLabel != null)
            {
                metaLabel.font = _font;
                metaLabel.text = "Meta Upgrades";
            }
        }

        private void StartCampaign()
        {
            RunLaunchConfig.ConfigureCampaign(_selectedTier);
            SceneManager.LoadScene(campaignSceneName);
        }

        private void StartEndless()
        {
            if (_metaProgression == null || !_metaProgression.EndlessUnlocked)
            {
                return;
            }

            RunLaunchConfig.ConfigureEndless(_selectedTier);
            SceneManager.LoadScene(campaignSceneName);
        }

        private void OpenMetaUpgrades()
        {
            SceneManager.LoadScene(metaUpgradesSceneName);
        }
    }
}
