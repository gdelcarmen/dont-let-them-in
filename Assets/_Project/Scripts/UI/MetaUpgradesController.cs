using System.Collections.Generic;
using DontLetThemIn.Audio;
using DontLetThemIn.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontLetThemIn.UI
{
    public sealed class MetaUpgradesController : MonoBehaviour
    {
        private sealed class UpgradeUiRow
        {
            public MetaUpgradeId Id;
            public Text CostText;
            public Button PurchaseButton;
            public Text PurchaseLabel;
        }

        private readonly List<UpgradeUiRow> _rows = new();
        private Font _font;
        private Text _salvageBalanceText;
        private MetaProgressionSaveData _saveData;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureController()
        {
            if (SceneManager.GetActiveScene().name != "MetaUpgrades")
            {
                return;
            }

            if (Object.FindFirstObjectByType<MetaUpgradesController>() != null)
            {
                return;
            }

            GameObject root = GameObject.Find("MetaUpgradesRoot");
            if (root == null)
            {
                root = new GameObject("MetaUpgradesRoot");
            }

            root.AddComponent<MetaUpgradesController>();
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name != "MetaUpgrades")
            {
                return;
            }

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _saveData = MetaProgressionService.Reload();
            BuildUi();
            RefreshUi();
        }

        private void BuildUi()
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

            CreateText(
                root,
                "TitleText",
                "META UPGRADES",
                52,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -50f),
                new Vector2(840f, 72f),
                new Color(0.95f, 0.9f, 0.74f, 1f));

            _salvageBalanceText = CreateText(
                root,
                "SalvageBalanceText",
                string.Empty,
                28,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -102f),
                new Vector2(840f, 42f),
                new Color(0.9f, 0.92f, 0.96f, 1f));

            GameObject scrollObject = FindOrCreate(root, "UpgradesScroll");
            RectTransform scrollRect = EnsureRect(scrollObject);
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.anchoredPosition = new Vector2(0f, -36f);
            scrollRect.sizeDelta = new Vector2(980f, 480f);

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            if (scroll == null)
            {
                scroll = scrollObject.AddComponent<ScrollRect>();
            }

            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            GameObject viewport = FindOrCreate(scrollRect, "Viewport");
            RectTransform viewportRect = EnsureRect(viewport);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage == null)
            {
                viewportImage = viewport.AddComponent<Image>();
            }

            viewportImage.color = new Color(0f, 0f, 0f, 0.06f);
            Mask mask = viewport.GetComponent<Mask>();
            if (mask == null)
            {
                mask = viewport.AddComponent<Mask>();
            }

            mask.showMaskGraphic = false;

            GameObject content = FindOrCreate(viewportRect, "Content");
            RectTransform contentRect = EnsureRect(content);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = new Vector2(0f, 0f);
            contentRect.sizeDelta = new Vector2(950f, 900f);

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            foreach (Transform child in contentRect)
            {
                Destroy(child.gameObject);
            }

            _rows.Clear();
            IReadOnlyList<MetaUpgradeDefinition> catalog = MetaProgressionService.UpgradeCatalog;
            for (int i = 0; i < catalog.Count; i++)
            {
                MetaUpgradeDefinition definition = catalog[i];
                UpgradeUiRow row = BuildUpgradeRow(contentRect, definition, i);
                _rows.Add(row);
            }

            float contentHeight = Mathf.Max(440f, catalog.Count * 102f + 40f);
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);

            Button backButton = CreateButton(
                root,
                "BackButton",
                "Back to Menu",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 18f),
                new Vector2(280f, 64f));
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        }

        private UpgradeUiRow BuildUpgradeRow(RectTransform parent, MetaUpgradeDefinition definition, int index)
        {
            GameObject card = new($"Upgrade_{definition.Id}");
            card.transform.SetParent(parent, false);
            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0f, 1f);
            cardRect.anchorMax = new Vector2(0f, 1f);
            cardRect.pivot = new Vector2(0f, 1f);
            cardRect.anchoredPosition = new Vector2(18f, -18f - index * 102f);
            cardRect.sizeDelta = new Vector2(910f, 88f);

            Image background = card.AddComponent<Image>();
            background.color = new Color(0.15f, 0.18f, 0.24f, 0.95f);

            CreateText(
                cardRect,
                "Name",
                definition.Name,
                23,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(16f, -10f),
                new Vector2(420f, 30f),
                Color.white);

            CreateText(
                cardRect,
                "Description",
                definition.Description,
                15,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(16f, -38f),
                new Vector2(620f, 38f),
                new Color(0.84f, 0.88f, 0.94f, 0.95f));

            Text costText = CreateText(
                cardRect,
                "Cost",
                string.Empty,
                16,
                TextAnchor.MiddleRight,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-246f, 0f),
                new Vector2(180f, 24f),
                new Color(0.94f, 0.84f, 0.38f, 1f));

            Button purchase = CreateButton(
                cardRect,
                "PurchaseButton",
                "Purchase",
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-106f, 0f),
                new Vector2(190f, 52f));

            Text purchaseLabel = purchase.transform.Find("Text")?.GetComponent<Text>();
            purchase.onClick.RemoveAllListeners();
            purchase.onClick.AddListener(() => TryPurchase(definition.Id));

            return new UpgradeUiRow
            {
                Id = definition.Id,
                CostText = costText,
                PurchaseButton = purchase,
                PurchaseLabel = purchaseLabel
            };
        }

        private void TryPurchase(MetaUpgradeId id)
        {
            if (MetaProgressionService.TryPurchaseUpgrade(id, out _))
            {
                _saveData = MetaProgressionService.Reload();
                RefreshUi();
            }
        }

        private void RefreshUi()
        {
            _saveData ??= MetaProgressionService.Load();
            if (_salvageBalanceText != null)
            {
                _salvageBalanceText.text = $"Salvage Points: {_saveData.SalvagePoints}";
            }

            IReadOnlyList<MetaUpgradeDefinition> catalog = MetaProgressionService.UpgradeCatalog;
            foreach (UpgradeUiRow row in _rows)
            {
                if (row == null)
                {
                    continue;
                }

                MetaUpgradeDefinition definition = null;
                for (int i = 0; i < catalog.Count; i++)
                {
                    if (catalog[i].Id == row.Id)
                    {
                        definition = catalog[i];
                        break;
                    }
                }

                if (definition == null)
                {
                    continue;
                }

                bool purchased = MetaProgressionService.IsUpgradePurchased(_saveData, row.Id);
                bool canAfford = _saveData.SalvagePoints >= definition.Cost;
                if (row.CostText != null)
                {
                    row.CostText.text = $"Cost: {definition.Cost} SP";
                }

                if (row.PurchaseButton != null)
                {
                    row.PurchaseButton.interactable = !purchased && canAfford;
                    Image buttonImage = row.PurchaseButton.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = purchased
                            ? new Color(0.2f, 0.48f, 0.26f, 0.95f)
                            : canAfford
                                ? new Color(0.22f, 0.38f, 0.62f, 0.95f)
                                : new Color(0.24f, 0.24f, 0.24f, 0.88f);
                    }
                }

                if (row.PurchaseLabel != null)
                {
                    row.PurchaseLabel.font = _font;
                    row.PurchaseLabel.fontSize = 20;
                    row.PurchaseLabel.alignment = TextAnchor.MiddleCenter;
                    row.PurchaseLabel.color = Color.white;
                    row.PurchaseLabel.text = purchased
                        ? "Purchased"
                        : canAfford
                            ? "Purchase"
                            : "Insufficient SP";
                }
            }
        }

        private Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.name = "MetaUpgradesCanvas";
                return canvas;
            }

            GameObject canvasObject = new("MetaUpgradesCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private Text CreateText(
            RectTransform parent,
            string objectName,
            string content,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            GameObject textObject = FindOrCreate(parent, objectName);
            RectTransform rect = EnsureRect(textObject);
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
            text.color = color;
            text.text = content;
            return text;
        }

        private Button CreateButton(
            RectTransform parent,
            string objectName,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject buttonObject = FindOrCreate(parent, objectName);
            RectTransform rect = EnsureRect(buttonObject);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }

            image.color = new Color(0.22f, 0.38f, 0.62f, 0.95f);

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            button.onClick.RemoveListener(AudioManager.TryPlayUiButton);
            button.onClick.AddListener(AudioManager.TryPlayUiButton);

            Text labelText = buttonObject.transform.Find("Text")?.GetComponent<Text>();
            if (labelText == null)
            {
                GameObject textObject = new("Text");
                textObject.transform.SetParent(buttonObject.transform, false);
                labelText = textObject.AddComponent<Text>();
            }

            RectTransform textRect = labelText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 4f);
            textRect.offsetMax = new Vector2(-4f, -4f);
            labelText.font = _font;
            labelText.text = label;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.fontSize = 22;
            return button;
        }

        private static GameObject FindOrCreate(Transform parent, string objectName)
        {
            Transform existing = parent.Find(objectName);
            GameObject target = existing != null ? existing.gameObject : new GameObject(objectName);
            if (existing == null)
            {
                target.transform.SetParent(parent, false);
            }

            return target;
        }

        private static RectTransform EnsureRect(GameObject gameObject)
        {
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }

            return rect;
        }
    }
}
