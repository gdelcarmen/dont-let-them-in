using System.Collections;
using DontLetThemIn.Grid;
using DontLetThemIn.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DontLetThemIn.Visuals
{
    public sealed class VisualPresentationController : MonoBehaviour
    {
        private Canvas _canvas;
        private Image _screenFade;
        private Image _waveBanner;
        private Text _waveBannerText;
        private Volume _volume;
        private Bloom _bloom;
        private Vignette _vignette;
        private ColorAdjustments _colorAdjustments;
        private FilmGrain _filmGrain;
        private Coroutine _waveBannerRoutine;
        private Coroutine _floorFadeRoutine;

        public VisualTheme ActiveTheme { get; private set; }

        public void Initialize(VisualTheme theme, Camera camera, HUDController hud)
        {
            ActiveTheme = theme != null ? theme : VisualThemeRuntime.ActiveTheme;
            VisualThemeRuntime.SetActiveTheme(ActiveTheme);
            EnsureCanvas(hud);
            EnsurePostProcessing(camera);
            ApplyThemeToCamera(camera);
        }

        public void ApplyTheme(VisualTheme theme, Camera camera, HUDController hud)
        {
            Initialize(theme, camera, hud);
            hud?.ApplyTheme(ActiveTheme);
            ApplyPostProcessing();
        }

        public void PlayWaveStartBanner(int wave, Color accent)
        {
            if (_waveBannerText == null || _waveBanner == null)
            {
                return;
            }

            if (_waveBannerRoutine != null)
            {
                StopCoroutine(_waveBannerRoutine);
            }

            _waveBannerRoutine = StartCoroutine(WaveBannerRoutine(wave, accent));
        }

        public void PlayFloorTransition(string label)
        {
            if (_floorFadeRoutine != null)
            {
                StopCoroutine(_floorFadeRoutine);
            }

            _floorFadeRoutine = StartCoroutine(FloorFadeRoutine(label));
        }

        public void FlashAlert(Color color, float peakAlpha = 0.32f, float duration = 0.26f)
        {
            if (_screenFade == null)
            {
                return;
            }

            StartCoroutine(AlertFlashRoutine(color, peakAlpha, duration));
        }

        private void EnsureCanvas(HUDController hud)
        {
            _canvas = hud != null ? hud.GetComponent<Canvas>() : null;
            if (_canvas == null)
            {
                _canvas = FindFirstObjectByType<Canvas>();
            }

            if (_canvas == null)
            {
                return;
            }

            Transform fadeTransform = _canvas.transform.Find("VisualScreenFade");
            if (fadeTransform == null)
            {
                GameObject fadeObject = new("VisualScreenFade");
                fadeObject.transform.SetParent(_canvas.transform, false);
                RectTransform rect = fadeObject.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                _screenFade = fadeObject.AddComponent<Image>();
                _screenFade.raycastTarget = false;
                _screenFade.color = new Color(0f, 0f, 0f, 0f);
            }
            else
            {
                _screenFade = fadeTransform.GetComponent<Image>();
            }

            Transform bannerTransform = _canvas.transform.Find("WaveBanner");
            if (bannerTransform == null)
            {
                GameObject bannerObject = new("WaveBanner");
                bannerObject.transform.SetParent(_canvas.transform, false);
                RectTransform rect = bannerObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 80f);
                rect.sizeDelta = new Vector2(520f, 96f);
                _waveBanner = bannerObject.AddComponent<Image>();
                _waveBanner.sprite = global::DontLetThemIn.RuntimeSpriteFactory.GetPaperSprite();
                _waveBanner.color = new Color(0.05f, 0.08f, 0.12f, 0f);
                _waveBanner.raycastTarget = false;

                GameObject label = new("Label");
                label.transform.SetParent(bannerObject.transform, false);
                RectTransform labelRect = label.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                _waveBannerText = label.AddComponent<Text>();
                _waveBannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _waveBannerText.fontSize = 40;
                _waveBannerText.alignment = TextAnchor.MiddleCenter;
                _waveBannerText.color = new Color(1f, 1f, 1f, 0f);
                _waveBannerText.text = "WAVE 1";
            }
            else
            {
                _waveBanner = bannerTransform.GetComponent<Image>();
                _waveBannerText = bannerTransform.Find("Label")?.GetComponent<Text>();
            }
        }

        private void EnsurePostProcessing(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            _volume = camera.GetComponent<Volume>();
            if (_volume == null)
            {
                _volume = camera.gameObject.AddComponent<Volume>();
                _volume.isGlobal = true;
                _volume.priority = 100f;
            }

            if (_volume.profile == null)
            {
                _volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            }

            if (!_volume.profile.TryGet(out _bloom))
            {
                _bloom = _volume.profile.Add<Bloom>(true);
            }

            if (!_volume.profile.TryGet(out _vignette))
            {
                _vignette = _volume.profile.Add<Vignette>(true);
            }

            if (!_volume.profile.TryGet(out _colorAdjustments))
            {
                _colorAdjustments = _volume.profile.Add<ColorAdjustments>(true);
            }

            if (!_volume.profile.TryGet(out _filmGrain))
            {
                _filmGrain = _volume.profile.Add<FilmGrain>(true);
            }

            ApplyPostProcessing();
        }

        private void ApplyPostProcessing()
        {
            if (ActiveTheme == null)
            {
                return;
            }

            _bloom.intensity.Override(ActiveTheme.Atmosphere.BloomIntensity);
            _bloom.threshold.Override(0.9f);
            _vignette.intensity.Override(ActiveTheme.Atmosphere.FogVignetteIntensity);
            _vignette.smoothness.Override(0.72f);
            _colorAdjustments.colorFilter.Override(Color.Lerp(ActiveTheme.Atmosphere.ShadowColor, ActiveTheme.Atmosphere.MidtoneColor, 0.6f));
            _colorAdjustments.postExposure.Override(0.12f);
            _colorAdjustments.saturation.Override(-8f);
            _filmGrain.intensity.Override(ActiveTheme.Atmosphere.FilmGrainIntensity);
            _filmGrain.response.Override(0.65f);
        }

        private void ApplyThemeToCamera(Camera camera)
        {
            if (camera == null || ActiveTheme == null)
            {
                return;
            }

            camera.backgroundColor = Color.Lerp(ActiveTheme.Threat.AlienLightColor, Color.black, 0.8f);
            camera.allowHDR = true;
            camera.allowMSAA = false;
            ApplyPostProcessing();
        }

        private IEnumerator WaveBannerRoutine(int wave, Color accent)
        {
            _waveBannerText.text = $"WAVE {wave}";
            Color bannerColor = Color.Lerp(ActiveTheme.Ui.HudBackgroundColor, accent, 0.28f);
            Color textColor = Color.Lerp(ActiveTheme.Ui.HudTextColor, accent, 0.25f);

            const float duration = 1.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Sin(t * Mathf.PI);
                _waveBanner.color = new Color(bannerColor.r, bannerColor.g, bannerColor.b, alpha * 0.92f);
                _waveBannerText.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
                yield return null;
            }

            _waveBanner.color = new Color(_waveBanner.color.r, _waveBanner.color.g, _waveBanner.color.b, 0f);
            _waveBannerText.color = new Color(_waveBannerText.color.r, _waveBannerText.color.g, _waveBannerText.color.b, 0f);
            _waveBannerRoutine = null;
        }

        private IEnumerator FloorFadeRoutine(string label)
        {
            Color fadeColor = new(0.01f, 0.02f, 0.04f, 0f);
            float elapsed = 0f;
            while (elapsed < 0.22f)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeColor.a = Mathf.Lerp(0f, 0.78f, elapsed / 0.22f);
                _screenFade.color = fadeColor;
                yield return null;
            }

            if (_waveBannerText != null && !string.IsNullOrWhiteSpace(label))
            {
                _waveBannerText.text = label;
                _waveBannerText.color = new Color(ActiveTheme.Ui.HudTextColor.r, ActiveTheme.Ui.HudTextColor.g, ActiveTheme.Ui.HudTextColor.b, 1f);
            }

            yield return new WaitForSecondsRealtime(0.16f);

            elapsed = 0f;
            while (elapsed < 0.28f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / 0.28f;
                fadeColor.a = Mathf.Lerp(0.78f, 0f, t);
                _screenFade.color = fadeColor;
                if (_waveBannerText != null)
                {
                    Color textColor = _waveBannerText.color;
                    textColor.a = Mathf.Lerp(1f, 0f, t);
                    _waveBannerText.color = textColor;
                }
                yield return null;
            }

            _floorFadeRoutine = null;
        }

        private IEnumerator AlertFlashRoutine(Color color, float peakAlpha, float duration)
        {
            if (_screenFade == null)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Sin(t * Mathf.PI) * peakAlpha;
                _screenFade.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            _screenFade.color = new Color(color.r, color.g, color.b, 0f);
        }
    }
}
