using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DiscJockey.Audio;
using DiscJockey.Audio.Data;
using DiscJockey.Data;
using DiscJockey.Input;
using DiscJockey.Patches;
using DiscJockey.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DiscJockey.Managers;

public class UIManager : MonoBehaviour
{
    private const float TrackNameScrollSpeed = 30.0f;
    private static bool _hasInitialized;

    public static UIManager Instance;
    public TextMeshProUGUI VersionText;
    public Image OverlayBackgroundImage;
    public Button PreviousButton;
    public Button NextButton;
    public Button StopButton;
    public Button PlayButton;
    public Slider TrackProgressSlider;
    public TextMeshProUGUI TrackProgressText;
    public TextMeshProUGUI TrackName;
    public TextMeshProUGUI TrackOwnerName;
    public GameObject TracklistContainer;
    public GameObject TrackButtonPrefab;
    public Button CloseButton;
    public GameObject DiscJockeyUIPanel;
    public Button ShuffleButton;
    public Button SequentialButton;
    public Button RepeatButton;
    public Slider VolumeSlider;
    public Button AddTrackButton;
    public ScrollRect TrackListScrollRect;
    public TextMeshProUGUI CreditsText;
    public RectTransform TrackNameContainerTransform;
    public RectTransform TrackNameTextTransform;
    private readonly List<TrackListButton> _trackListButtonInstances = new();

    private TrackListButton _pendingTrackButton;
    private Coroutine _trackNameScrollRoutine;
    private CancellationTokenSource _trackNameScrollRoutineCancellationSource;

    public bool UIPanelActive => DiscJockeyUIPanel.activeSelf;

    public void Awake()
    {
        if (_hasInitialized) return;
        if (Instance == null) Instance = this;
        DiscJockeyPlugin.LogInfo("DiscJockeyUIManager<Awake>: Called");
        InitUIEvents();
        DiscJockeyUIPanel.SetActive(false);
        RegisterEventListeners();
        ApplyConfig();
        _trackNameScrollRoutineCancellationSource = new CancellationTokenSource();
        DiscJockeyPlugin.LogInfo("DiscJockeyUIManager<Awake>: Initialized");
        _hasInitialized = true;
    }

    public void Update()
    {
        if (!BoomboxManager.IsLookingAtOrHoldingBoombox) return;
        var activeBoombox = BoomboxManager.LookedAtOrHeldBoombox;

        if (Keyboard.current.escapeKey.wasPressedThisFrame && UIPanelActive) HideUIPanel();

        if (activeBoombox.IsPlaying)
        {
            TrackProgressSlider.SetValueWithoutNotify(activeBoombox.CurrentTrackProgress);
            TrackProgressSlider.maxValue = activeBoombox.CurrentTrackLength;
            var lengthTimespan = TimeSpan.FromSeconds(activeBoombox.CurrentTrackLength);
            var lengthString = $"{lengthTimespan.Minutes:D2}:{lengthTimespan.Seconds:D2}";
            var currentTimespan = TimeSpan.FromSeconds(TrackProgressSlider.value);
            var currentTime = $"{currentTimespan.Minutes:D2}:{currentTimespan.Seconds:D2}";
            TrackProgressText.text = $"{currentTime}/{lengthString}";
        }
    }

    public void RemoveTrackListButton(TrackListButton trackListButton) =>
        _trackListButtonInstances.Remove(trackListButton);

    private void ApplyConfig()
    {
        if (DiscJockeyConfig.LocalConfig.DisableCreditsText) CreditsText.gameObject.SetActive(false);

        var configInterfaceTransparency = ConfigValidator.ValidateInterfaceTransparency();
        var configInterfaceColour = ConfigValidator.ValidateInterfaceColour();
        OverlayBackgroundImage.color = new Color(configInterfaceColour.r, configInterfaceColour.g,
            configInterfaceColour.b, configInterfaceTransparency);

        VolumeSlider.value = DiscJockeyConfig.LocalConfig.DefaultVolume;
    }

    public void RegisterEventListeners()
    {
        AudioManager.OnVolumeChanged += OnVolumeChanged;
        AudioManager.OnTrackAddedToTrackList += OnTrackAddedToTracklist;
        DJNetworkManager.OnAudioStreamTransmitStarted += OnAudioPlaybackStarted;
        DJNetworkManager.OnAudioStreamPlaybackStopped += OnAudioPlaybackStopped;
        GameNetworkManagerPatches.OnDisconnect += OnDisconnect;
        AudioManager.TrackList.OnTracklistSorted += SortTrackList;
    }

    public void UnregisterEventListeners()
    {
        AudioManager.OnVolumeChanged -= OnVolumeChanged;
        AudioManager.OnTrackAddedToTrackList -= OnTrackAddedToTracklist;
        DJNetworkManager.OnAudioStreamTransmitStarted -= OnAudioPlaybackStarted;
        DJNetworkManager.OnAudioStreamPlaybackStopped -= OnAudioPlaybackStopped;
        GameNetworkManagerPatches.OnDisconnect -= OnDisconnect;
        AudioManager.TrackList.OnTracklistSorted -= SortTrackList;
    }

    private void OnVolumeChanged(ulong networkedBoomboxId, float volume)
    {
        if (BoomboxManager.LookedAtOrHeldBoomboxIsNot(networkedBoomboxId))
        {
            return;
        }
        
        VolumeSlider.SetValueWithoutNotify(volume);
    }

    private void OnTrackAddedToTracklist(Track track)
    {
        AddButtonToTrackList(track);

        if (LocalPlayerHelper.Player != null)
            track.TakeOwnership(LocalPlayerHelper.Player.playerClientId,
                GameUtils.GetPlayerName(LocalPlayerHelper.Player.playerClientId));

        SortTrackList();
    }

    private void OnDisconnect()
    {
        if (_pendingTrackButton != null) Destroy(_pendingTrackButton.gameObject);
        if(UIPanelActive) HideUIPanel();
        _pendingTrackButton = null;
    }

    private void OnAudioPlaybackStarted(ulong senderClientId, ulong networkedBoomboxId, TrackMetadata trackMetadata,
        AudioFormat audioFormat)
    {
        if (BoomboxManager.LookedAtOrHeldBoomboxIsNot(networkedBoomboxId))
        {
            return;
        }
        
        TrackOwnerName.text = senderClientId == LocalPlayerHelper.Player.playerClientId
            ? "From Your Tracklist"
            : $"From {trackMetadata.OwnerName}'s Tracklist";
        TrackName.text = trackMetadata.Name;

        StopButton.gameObject.SetActive(true);
        PlayButton.gameObject.SetActive(false);
        StopTrackScrollEffect();
        StartTrackScrollEffect();
    }

    private void OnAudioPlaybackStopped(ulong senderClientId, ulong networkedBoomboxId)
    {
        if (BoomboxManager.LookedAtOrHeldBoomboxIsNot(networkedBoomboxId))
        {
            return;
        }
        
        DiscJockeyPlugin.LogInfo("DiscJockeyUIManager<OnStopTrack>: Stopping track");
        TrackProgressSlider.value = 0;
        TrackProgressSlider.minValue = 0;
        TrackProgressSlider.maxValue = 0;
        TrackProgressText.text = string.Empty;
        TrackName.text = "No Track Selected";
        TrackOwnerName.text = string.Empty;
        StopButton.gameObject.SetActive(false);
        PlayButton.gameObject.SetActive(true);
        StopTrackScrollEffect();
    }

    private void AddButtonToTrackList(Track track)
    {
        DiscJockeyPlugin.LogInfo(
            $"DiscJockeyUIManager<AddButtonToTrackList>: Adding button for track {track}");

        var trackListButton = TrackListButton.InstantiateAsNormal(TracklistContainer.transform, track);
        _trackListButtonInstances.Add(trackListButton);
        SortTrackList();
    }

    public void DisableAddTrackButton()
    {
        AddTrackButton.gameObject.SetActive(false);
    }

    public void ToggleUIPanel()
    {
        if (UIPanelActive)
            HideUIPanel();
        else
            ShowUIPanel();
    }

    public void ShowUIPanel()
    {
        InputManager.DisablePlayerInteractions();
        DiscJockeyUIPanel.SetActive(true);
        VolumeSlider.SetValueWithoutNotify(BoomboxManager.LookedAtOrHeldBoombox.Volume);
    }

    public void HideUIPanel()
    {
        InputManager.EnablePlayerInteractions();
        DiscJockeyUIPanel.SetActive(false);
    }

    private void SortTrackList()
    {
        var buttonsReadyForSorting =
            _trackListButtonInstances.Where(button => button.IsReadyForSorting)
                .OrderBy(button => button.Track.IndexInTracklist).ToList();

        foreach (var button in buttonsReadyForSorting)
        {
            button.transform.SetSiblingIndex(button.Track.IndexInTracklist);
        }

        if (_pendingTrackButton != null) _pendingTrackButton.transform.SetAsFirstSibling();
    }

    private void StartTrackScrollEffect()
    {
        Canvas.ForceUpdateCanvases();
        var trackTextWidth = TrackNameTextTransform.rect.width;
        var trackContainerWidth = TrackNameContainerTransform.rect.width;
        if (trackTextWidth <= trackContainerWidth)
        {
            return;
        }

        var diff = trackContainerWidth - trackTextWidth;
        _trackNameScrollRoutine = StartCoroutine(ScrollTrackName(diff, _trackNameScrollRoutineCancellationSource.Token));
    }

    private void StopTrackScrollEffect()
    {
        if (_trackNameScrollRoutine != null)
        {
            _trackNameScrollRoutineCancellationSource.Cancel();
            _trackNameScrollRoutineCancellationSource.Dispose();
            _trackNameScrollRoutineCancellationSource = new CancellationTokenSource();
            StopCoroutine(_trackNameScrollRoutine);
        }

        TrackNameTextTransform.anchoredPosition = new Vector2(0, TrackNameTextTransform.anchoredPosition.y);
    }

    private void OnNextButtonClicked()
    {
        if (!BoomboxManager.IsLookingAtOrHoldingBoombox) return;

        if (BoomboxManager.LookedAtOrHeldBoombox.LocalClientOwnsCurrentTrack)
        {
            AudioManager.RequestPlayTrack(
                AudioManager.TrackList.GetNextTrack(
                    BoomboxManager.LookedAtOrHeldBoombox.CurrentTrackIndexInOwnersTracklist,
                    BoomboxManager.LookedAtOrHeldBoombox.BoomboxPlaybackMode,
                    true));
        }
        else
        {
            AudioManager.RequestPlayTrack(AudioManager.TrackList.GetTrackAtIndex(0));
        }
    }

    private void OnPreviousButtonClicked()
    {
        if (!BoomboxManager.IsLookingAtOrHoldingBoombox) return;

        if (BoomboxManager.LookedAtOrHeldBoombox.LocalClientOwnsCurrentTrack)
        {
            AudioManager.RequestPlayTrack(
                AudioManager.TrackList.GetPreviousTrack(
                    BoomboxManager.LookedAtOrHeldBoombox.CurrentTrackIndexInOwnersTracklist));
        }
        else
        {
            AudioManager.RequestPlayTrack(AudioManager.TrackList.GetTrackAtIndex(0));
        }
    }

    private void OnSequentialPlaybackModeButtonClicked()
    {
        if (!BoomboxManager.IsLookingAtOrHoldingBoombox) return;

        DJNetworkManager.Instance.RequestBoomboxPlaybackModeChangeServerRpc(
            BoomboxManager.LookedAtOrHeldBoombox.NetworkedBoomboxId, BoomboxPlaybackMode.Shuffle);
        ShuffleButton.gameObject.SetActive(true);
        SequentialButton.gameObject.SetActive(false);
    }

    private void OnShufflePlaybackModeButtonClicked()
    {
        if (!BoomboxManager.IsLookingAtOrHoldingBoombox) return;

        DJNetworkManager.Instance.RequestBoomboxPlaybackModeChangeServerRpc(
            BoomboxManager.LookedAtOrHeldBoombox.NetworkedBoomboxId, BoomboxPlaybackMode.Repeat);
        RepeatButton.gameObject.SetActive(true);
        ShuffleButton.gameObject.SetActive(false);
    }

    private void OnRepeatPlaybackModeButtonClicked()
    {
        if (!BoomboxManager.IsLookingAtOrHoldingBoombox) return;

        DJNetworkManager.Instance.RequestBoomboxPlaybackModeChangeServerRpc(
            BoomboxManager.LookedAtOrHeldBoombox.NetworkedBoomboxId, BoomboxPlaybackMode.Sequential);
        SequentialButton.gameObject.SetActive(true);
        RepeatButton.gameObject.SetActive(false);
    }

    private void OnStopButtonClicked()
    {
        AudioManager.RequestStopTrack();
    }

    private void OnPlayButtonClicked()
    {
        AudioManager.RequestPlayTrack(AudioManager.TrackList.GetRandomTrack());
    }

    private void OnVolumeSliderChanged(float value)
    {
        if (!BoomboxManager.IsLookingAtOrHoldingBoombox) return;
        AudioManager.RequestVolumeChange(value, true);
    }

    private IEnumerator ScrollTrackName(float toPosition, CancellationToken ctx)
    {
        yield return new WaitForSeconds(2.0f);
        while (TrackNameTextTransform.anchoredPosition.x > toPosition && !ctx.IsCancellationRequested)
        {
            var anchoredPosition = TrackNameTextTransform.anchoredPosition;
            anchoredPosition = new Vector2(anchoredPosition.x - TrackNameScrollSpeed * Time.deltaTime,
                anchoredPosition.y);
            TrackNameTextTransform.anchoredPosition = anchoredPosition;

            if (TrackNameTextTransform.anchoredPosition.x <= toPosition)
            {
                yield return new WaitForSeconds(2.0f);
                TrackNameTextTransform.anchoredPosition = new Vector2(0, TrackNameTextTransform.anchoredPosition.y);
                yield return new WaitForSeconds(2.0f);
            }

            yield return null;
        }
    }

    private void InitUIEvents()
    {
        NextButton.onClick.AddListener(OnNextButtonClicked);
        PreviousButton.onClick.AddListener(OnPreviousButtonClicked);
        StopButton.onClick.AddListener(OnStopButtonClicked);
        PlayButton.onClick.AddListener(OnPlayButtonClicked);
        CloseButton.onClick.AddListener(HideUIPanel);
        SequentialButton.onClick.AddListener(OnSequentialPlaybackModeButtonClicked);
        ShuffleButton.onClick.AddListener(OnShufflePlaybackModeButtonClicked);
        RepeatButton.onClick.AddListener(OnRepeatPlaybackModeButtonClicked);
        VolumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
        AddTrackButton.onClick.AddListener(AddPendingTrackButton);
    }

    public void AddPendingTrackButton()
    {
        if (_pendingTrackButton != null) return;

        _pendingTrackButton = TrackListButton.InstantiateAsPendingInput(TracklistContainer.transform);
        _pendingTrackButton.transform.SetAsFirstSibling();
        _trackListButtonInstances.Add(_pendingTrackButton);
        TrackListScrollRect.normalizedPosition = new Vector2(0, 1);

        _pendingTrackButton.OnAddTrackCancelled += () =>
        {
            _trackListButtonInstances.Remove(_pendingTrackButton);
            _pendingTrackButton = null;
        };

        _pendingTrackButton.OnDownloadTaskFailed += () =>
        {
            _trackListButtonInstances.Remove(_pendingTrackButton);
            _pendingTrackButton = null;
        };

        _pendingTrackButton.OnDestroyed += () =>
        {
            _trackListButtonInstances.Remove(_pendingTrackButton);
            _pendingTrackButton = null;
            SortTrackList();
        };
    }


    public void SetTrackName(string trackName)
    {
        TrackName.text = trackName;
    }
}