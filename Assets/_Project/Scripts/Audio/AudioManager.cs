using UnityEngine;

namespace DontLetThemIn.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        [Header("Optional Assignments")]
        [SerializeField] private AudioClip gameplayMusic;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        private AudioClip _trapClip;
        private AudioClip _alienDeathClip;
        private AudioClip _shotgunClip;
        private AudioClip _roombaLoopClip;
        private AudioClip _dogBarkClip;
        private AudioClip _uiClickClip;
        private AudioClip _waveStartClip;
        private AudioClip _powerSurgeClip;

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    EnsureExists();
                }

                return _instance;
            }
        }

        public AudioClip RoombaLoopClip => _roombaLoopClip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureExists();
        }

        public static void EnsureExists()
        {
            if (!Application.isPlaying)
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioManager>();
                    _instance?.InitializeIfNeeded();
                }

                return;
            }

            if (_instance != null)
            {
                return;
            }

            _instance = FindFirstObjectByType<AudioManager>();
            if (_instance != null)
            {
                _instance.InitializeIfNeeded();
                return;
            }

            GameObject root = new("AudioManager");
            _instance = root.AddComponent<AudioManager>();
            _instance.InitializeIfNeeded();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                SafeDestroy(gameObject);
                return;
            }

            _instance = this;
            InitializeIfNeeded();
        }

        private void InitializeIfNeeded()
        {
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (_musicSource == null)
            {
                _musicSource = gameObject.GetComponent<AudioSource>();
                if (_musicSource == null)
                {
                    _musicSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (_sfxSource == null)
            {
                GameObject sfxObject = transform.Find("SfxSource")?.gameObject;
                if (sfxObject == null)
                {
                    sfxObject = new GameObject("SfxSource");
                    sfxObject.transform.SetParent(transform, false);
                }

                _sfxSource = sfxObject.GetComponent<AudioSource>();
                if (_sfxSource == null)
                {
                    _sfxSource = sfxObject.AddComponent<AudioSource>();
                }
            }

            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = 0.25f;

            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;
            _sfxSource.spatialBlend = 0f;
            _sfxSource.volume = 0.75f;

            EnsureClips();
            StartMusicIfNeeded();
        }

        public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || _sfxSource == null)
            {
                return;
            }

            float previousPitch = _sfxSource.pitch;
            _sfxSource.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            _sfxSource.pitch = previousPitch;
        }

        public static void TryPlayTrapTrigger() => Instance?.PlaySfx(Instance._trapClip, 0.8f, Random.Range(0.95f, 1.08f));

        public static void TryPlayAlienDeath() => Instance?.PlaySfx(Instance._alienDeathClip, 0.7f, Random.Range(0.9f, 1.12f));

        public static void TryPlayShotgunFire() => Instance?.PlaySfx(Instance._shotgunClip, 0.82f, Random.Range(0.94f, 1.03f));

        public static void TryPlayDogBark() => Instance?.PlaySfx(Instance._dogBarkClip, 0.7f, Random.Range(0.88f, 1.08f));

        public static void TryPlayUiButton() => Instance?.PlaySfx(Instance._uiClickClip, 0.55f, Random.Range(0.95f, 1.05f));

        public static void TryPlayWaveStart() => Instance?.PlaySfx(Instance._waveStartClip, 0.7f, 1f);

        public static void TryPlayPowerSurge() => Instance?.PlaySfx(Instance._powerSurgeClip, 0.78f, 1f);

        public static AudioSource AttachRoombaLoopSource(Transform host)
        {
            if (host == null || Instance == null || Instance._roombaLoopClip == null)
            {
                return null;
            }

            AudioSource source = host.GetComponent<AudioSource>();
            if (source == null)
            {
                source = host.gameObject.AddComponent<AudioSource>();
            }

            source.clip = Instance._roombaLoopClip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.25f;
            if (Application.isPlaying && !source.isPlaying)
            {
                source.Play();
            }

            return source;
        }

        private void StartMusicIfNeeded()
        {
            if (!Application.isPlaying || _musicSource == null || _musicSource.isPlaying)
            {
                return;
            }

            _musicSource.clip = gameplayMusic != null ? gameplayMusic : CreateSilentMusicPlaceholder();
            _musicSource.Play();
        }

        private void EnsureClips()
        {
            _trapClip ??= CreateChirpClip("Sfx_TrapTrigger", 980f, 0.14f, 0.28f, descending: true);
            _alienDeathClip ??= CreateChirpClip("Sfx_AlienDeath", 420f, 0.18f, 0.24f, descending: true);
            _shotgunClip ??= CreateNoiseBurstClip("Sfx_Shotgun", 0.12f, 0.38f);
            _roombaLoopClip ??= CreateHumLoopClip("Sfx_RoombaLoop", 112f, 2.2f, 0.15f);
            _dogBarkClip ??= CreateDoubleToneClip("Sfx_DogBark", 390f, 0.18f, 0.28f);
            _uiClickClip ??= CreateChirpClip("Sfx_UIClick", 1320f, 0.06f, 0.16f, descending: true);
            _waveStartClip ??= CreateChirpClip("Sfx_WaveStart", 420f, 0.42f, 0.22f, rising: true);
            _powerSurgeClip ??= CreateNoiseBurstClip("Sfx_PowerSurge", 0.34f, 0.34f);
        }

        private static AudioClip CreateSilentMusicPlaceholder()
        {
            const int sampleRate = 44100;
            int lengthSamples = sampleRate * 4;
            AudioClip clip = AudioClip.Create("Music_Placeholder", lengthSamples, 1, sampleRate, false);
            clip.SetData(new float[lengthSamples], 0);
            return clip;
        }

        private static AudioClip CreateChirpClip(
            string name,
            float frequency,
            float duration,
            float volume,
            bool descending = false,
            bool rising = false)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(64, Mathf.RoundToInt(sampleRate * Mathf.Max(0.03f, duration)));
            float[] samples = new float[sampleCount];
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float targetFrequency = frequency;
                if (descending)
                {
                    targetFrequency = Mathf.Lerp(frequency * 1.35f, frequency * 0.55f, t);
                }
                else if (rising)
                {
                    targetFrequency = Mathf.Lerp(frequency * 0.75f, frequency * 1.6f, t);
                }

                phase += (2f * Mathf.PI * targetFrequency) / sampleRate;
                samples[i] = Mathf.Sin(phase) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateDoubleToneClip(string name, float baseFrequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(64, Mathf.RoundToInt(sampleRate * Mathf.Max(0.05f, duration)));
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                float envelope = Mathf.Pow(Mathf.Clamp01(1f - t), 0.45f);
                float frequency = t < 0.45f ? baseFrequency : baseFrequency * 1.35f;
                float toneA = Mathf.Sin(2f * Mathf.PI * frequency * t);
                float toneB = Mathf.Sin(2f * Mathf.PI * (frequency * 0.5f) * t);
                samples[i] = (toneA * 0.7f + toneB * 0.3f) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateNoiseBurstClip(string name, float duration, float volume)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(64, Mathf.RoundToInt(sampleRate * Mathf.Max(0.05f, duration)));
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                float envelope = Mathf.Pow(Mathf.Clamp01(1f - t), 1.2f);
                float noise = (Random.value * 2f - 1f) * 0.55f;
                float lowTone = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.22f;
                samples[i] = (noise + lowTone) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateHumLoopClip(string name, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(64, Mathf.RoundToInt(sampleRate * Mathf.Max(0.4f, duration)));
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                float wobble = 1f + Mathf.Sin(t * Mathf.PI * 6f) * 0.05f;
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t * wobble);
                float harmonic = Mathf.Sin(2f * Mathf.PI * frequency * 2f * t) * 0.2f;
                samples[i] = (tone * 0.8f + harmonic * 0.2f) * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void SafeDestroy(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
