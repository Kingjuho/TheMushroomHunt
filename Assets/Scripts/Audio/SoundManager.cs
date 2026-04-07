using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum BgmId
{
    None = 0,
    TitleScene = 1,
    MainScene = 2,
    Guard = 3
}

public enum SfxId
{
    None = 0,
    MushroomHit = 1,
    MushroomHarvestComplete = 2,
    GuardSiren = 3
}

public sealed class SoundManager : MonoBehaviour
{
    [Serializable]
    private struct BgmClipSlot
    {
        public BgmId id;
        public AudioClip clip;
    }

    [Serializable]
    private struct SfxClipSlot
    {
        public SfxId id;
        public AudioClip clip;
    }

    public static SoundManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string mainSceneName = "MainScene";

    [Header("Volume")]
    [SerializeField][Range(0f, 1f)] private float bgmVolume = 0.5f;
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("BGM Slots")]
    [SerializeField] private BgmClipSlot[] bgmClips;

    [Header("SFX Slots")]
    [SerializeField] private SfxClipSlot[] sfxClips;

    private readonly Dictionary<BgmId, AudioClip> _bgmLookup = new Dictionary<BgmId, AudioClip>();
    private readonly Dictionary<SfxId, AudioClip> _sfxLookup = new Dictionary<SfxId, AudioClip>();

    private BgmId _currentBgmId = BgmId.None;

    private bool _isSceneLoadedSubscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // duplicate는 어떤 생명주기 훅도 더 진행하지 않게 막고 바로 정리
            enabled = false;
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildClipLookupTables();

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        ConfigureAudioSources();
    }

    private void OnEnable()
    {
        if (Instance != this)
        {
            return;
        }

        SubscribeSceneLoaded();
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        // 현재 씬에서 바로 플레이를 시작했을 때도 기본 BGM이 나오도록 보강
        PlaySceneDefaultBgm(SceneManager.GetActiveScene().name);
    }

    private void OnDisable()
    {
        UnsubscribeSceneLoaded();
    }

    private void OnDestroy()
    {
        UnsubscribeSceneLoaded();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void SubscribeSceneLoaded()
    {
        if (_isSceneLoadedSubscribed)
        {
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        _isSceneLoadedSubscribed = true;
    }

    private void UnsubscribeSceneLoaded()
    {
        if (!_isSceneLoadedSubscribed)
        {
            return;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        _isSceneLoadedSubscribed = false;
    }

    public void PlayBgm(BgmId id)
    {
        if (bgmSource == null)
        {
            return;
        }

        if (id == BgmId.None)
        {
            StopBgm();
            return;
        }

        AudioClip clip = GetBgmClip(id);

        if (clip == null)
        {
            return;
        }

        // 같은 곡을 이미 재생 중이면 중복 재시작하지 않음
        if (_currentBgmId == id && bgmSource.isPlaying && bgmSource.clip == clip)
        {
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();

        _currentBgmId = id;
    }

    public void StopBgm()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }

        _currentBgmId = BgmId.None;
    }

    public void PlaySfx(SfxId id)
    {
        if (sfxSource == null || id == SfxId.None)
        {
            return;
        }

        AudioClip clip = GetSfxClip(id);

        // SFX 에셋이 아직 없거나 슬롯이 비어 있어도 안전하게 무시
        if (clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneDefaultBgm(scene.name);
    }

    private void PlaySceneDefaultBgm(string sceneName)
    {
        if (string.Equals(sceneName, titleSceneName, StringComparison.Ordinal))
        {
            PlayBgm(BgmId.TitleScene);
            return;
        }

        if (string.Equals(sceneName, mainSceneName, StringComparison.Ordinal))
        {
            PlayBgm(BgmId.MainScene);
        }
    }

    private void ConfigureAudioSources()
    {
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
    }

    private void BuildClipLookupTables()
    {
        _bgmLookup.Clear();

        if (bgmClips != null)
        {
            for (int i = 0; i < bgmClips.Length; i++)
            {
                BgmClipSlot slot = bgmClips[i];

                if (slot.id == BgmId.None || slot.clip == null)
                {
                    continue;
                }

                _bgmLookup[slot.id] = slot.clip;
            }
        }

        _sfxLookup.Clear();

        if (sfxClips != null)
        {
            for (int i = 0; i < sfxClips.Length; i++)
            {
                SfxClipSlot slot = sfxClips[i];

                if (slot.id == SfxId.None || slot.clip == null)
                {
                    continue;
                }

                _sfxLookup[slot.id] = slot.clip;
            }
        }
    }

    private AudioClip GetBgmClip(BgmId id)
    {
        AudioClip clip;
        _bgmLookup.TryGetValue(id, out clip);
        return clip;
    }

    private AudioClip GetSfxClip(SfxId id)
    {
        AudioClip clip;
        _sfxLookup.TryGetValue(id, out clip);
        return clip;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (bgmSource == null)
        {
            Debug.LogWarning($"{nameof(SoundManager)}: bgmSource reference is missing.", this);
            isValid = false;
        }

        if (sfxSource == null)
        {
            Debug.LogWarning($"{nameof(SoundManager)}: sfxSource reference is missing.", this);
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(titleSceneName))
        {
            Debug.LogWarning($"{nameof(SoundManager)}: titleSceneName is empty.", this);
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogWarning($"{nameof(SoundManager)}: mainSceneName is empty.", this);
            isValid = false;
        }

        return isValid;
    }
}
