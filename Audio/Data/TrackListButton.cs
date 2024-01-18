using System;
using System.Collections;
using System.Threading;
using DiscJockey.Data;
using DiscJockey.Events;
using DiscJockey.Managers;
using DiscJockey.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiscJockey.Audio.Data;

public class TrackListButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum ButtonState
    {
        Default,
        PendingInput,
        Downloading,
        Playing
    }

    public Button TrackButton;
    public GameObject URLInputContainer;
    public TMP_InputField URLInputField;
    public GameObject AddTrackButtonContainer;
    public Button AddTrackSaveButton;
    public Button AddTrackCancelButton;
    public TextMeshProUGUI TrackNameText;
    public TextMeshProUGUI TrackIndicatorText;
    public RectTransform TrackNameTextTransform;
    public RectTransform TrackNameContainerTransform;
    public Slider DownloadProgressSlider;
    public GameObject TrackContentContainer;
    public Button DeleteButton;
    
    private string _activeDownloadUrl;
    private ButtonState _buttonState = ButtonState.Default;
    
    private Coroutine _trackNameScrollRoutine;
    private CancellationTokenSource _trackNameScrollRoutineCancellationSource;
    private const string PlayingIndicator = ">";
    private const string DownloadingIndicator = "<";
    private const float TrackNameScrollSpeed = 30.0f;
    
    public Track Track;
    
    private string _trackName => Track != null ? $"{Track.Audio.Name}" : string.Empty;
    private string _trackIndexString => Track != null ? $"{Track.IndexInTracklist + 1}." : string.Empty;

    public bool IsReadyForSorting => Track != null && _buttonState != ButtonState.Downloading;

    private void Awake()
    {
        _trackNameScrollRoutineCancellationSource = new CancellationTokenSource();
        TrackIndicatorText.horizontalAlignment = HorizontalAlignmentOptions.Left;
        DJNetworkManager.OnStreamStarted += OnStreamStarted;
        DJNetworkManager.OnStreamStopped += OnStreamStopped;
        TrackButton.onClick.AddListener(RequestPlayTrack);
        AddTrackSaveButton.onClick.AddListener(StartDownload);
        DeleteButton.onClick.AddListener(OnDelete);
        AddTrackCancelButton.onClick.AddListener(() =>
        {
            OnAddTrackCancelled?.Invoke();
            Destroy(gameObject);
        });
    }

    private void OnDelete()
    {
        AudioManager.TrackList.Remove(Track);
        UIManager.Instance.RemoveTrackListButton(this);
        Destroy(gameObject);
    }

    private void OnStreamStopped(StreamStoppedEventArgs streamStoppedEventArgs)
    {
        if (_buttonState == ButtonState.Playing) SetButtonState(ButtonState.Default);
    }

    private void OnStreamStarted(StreamStartedEventArgs streamStartedEventArgs)
    {
        if (streamStartedEventArgs.StreamInformation.TrackMetadata.Id == Track.Id && _buttonState == ButtonState.Default)
        {
            SetButtonState(ButtonState.Playing);
        }
        else if (streamStartedEventArgs.StreamInformation.TrackMetadata.Id != Track.Id && _buttonState == ButtonState.Playing)
        {
            SetButtonState(ButtonState.Default);
        }
    }

    private void OnTracklistSorted()
    {
        if (_buttonState == ButtonState.Default) SetIndicatorAsIndex();
    }

    private void OnDestroy()
    {
        DJNetworkManager.OnStreamStarted -= OnStreamStarted;
        DJNetworkManager.OnStreamStopped -= OnStreamStopped;
        OnDestroyed?.Invoke();
        RemoveDownloadEventListeners();
    }

    private void RequestPlayTrack()
    {
        AudioManager.RequestPlayTrack(Track);
    }

    public event Action OnAddTrackCancelled;
    public event Action OnDownloadTaskFailed;
    public event Action OnDestroyed;

    public void SetTrack(Track track)
    {
        Track = track;
        SetIndicatorAsIndex();
        SetTrackText(_trackName);
    }

    public void SetTrackText(string trackText)
    {
        TrackNameText.text = trackText;
    }

    public void OnDownloadProgress(string url, float progress)
    {
        if (url != _activeDownloadUrl) return;
        DownloadProgressSlider.value = progress;
    }

    private void RemoveDownloadEventListeners()
    {
        AudioLoader.OnAudioDownloadTitleResolved -= OnDownloadTitleResolutionCompleted;
        AudioLoader.OnAudioDownloadProgress -= OnDownloadProgress;
        AudioLoader.OnAudioDownloadCompleted -= OnDownloadCompleted;
        AudioLoader.OnAudioDownloadFailed -= OnDownloadFailed;
    }

    private void OnDownloadTitleResolutionCompleted(string url, string title)
    {
        if (url != _activeDownloadUrl) return;
        SetTrackText(title);
    }

    private void OnDownloadFailed(string url, string error)
    {
        if (url != _activeDownloadUrl) return;
        OnDownloadTaskFailed?.Invoke();
        Destroy(gameObject);
    }

    private void OnDownloadCompleted(string url)
    {
        if (url != _activeDownloadUrl) return;
        _activeDownloadUrl = null;
        Destroy(gameObject);
    }

    private void SetupDownloadEventListeners()
    {
        AudioLoader.OnAudioDownloadTitleResolved += OnDownloadTitleResolutionCompleted;
        AudioLoader.OnAudioDownloadProgress += OnDownloadProgress;
        AudioLoader.OnAudioDownloadCompleted += OnDownloadCompleted;
        AudioLoader.OnAudioDownloadFailed += OnDownloadFailed;
    }

    private void StartDownload()
    {
        SetButtonState(ButtonState.Downloading);
        _activeDownloadUrl = URLInputField.text.EnsureUrlSchemeExists();
        AudioManager.DownloadContent(_activeDownloadUrl);
    }

    public static TrackListButton InstantiateAsPendingInput(Transform parent)
    {
        var trackListButton = Instantiate(AssetLoader.TrackListButtonPrefab, parent).GetComponent<TrackListButton>();
        trackListButton.SetButtonState(ButtonState.PendingInput);
        return trackListButton;
    }

    public static TrackListButton InstantiateAsNormal(Transform parent, Track track)
    {
        var trackListButton = Instantiate(AssetLoader.TrackListButtonPrefab, parent).GetComponent<TrackListButton>();
        AudioManager.TrackList.OnTracklistSorted += trackListButton.OnTracklistSorted;
        trackListButton.SetTrack(track);
        trackListButton.SetButtonState(ButtonState.Default);
        return trackListButton;
    }

    public void SetButtonState(ButtonState buttonState)
    {
        _buttonState = buttonState;
        switch (_buttonState)
        {
            case ButtonState.Default:
                SetButtonAsDefault();
                break;
            case ButtonState.PendingInput:
                SetButtonAsPendingInput();
                break;
            case ButtonState.Downloading:
                SetButtonAsDownloading();
                break;
            case ButtonState.Playing:
                SetButtonAsPlaying();
                break;
            default:
                SetButtonAsDefault();
                break;
        }
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
    
    // TODO: Repeated in DiscJockeyUIManager. DRY.
    private IEnumerator ScrollTrackName(float toPosition, CancellationToken ctx)
    {
        yield return new WaitForSeconds(0.5f);
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

    private void SetButtonAsPendingInput()
    {
        TrackNameContainerTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 475);
        DeleteButton.gameObject.SetActive(false);
        TrackButton.interactable = false;
        TrackContentContainer.SetActive(false);
        URLInputContainer.SetActive(true);
        AddTrackButtonContainer.SetActive(true);
        DownloadProgressSlider.gameObject.SetActive(false);
    }

    private void SetButtonAsDownloading()
    {
        SetupDownloadEventListeners();
        TrackNameContainerTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 475);
        DeleteButton.gameObject.SetActive(false);
        TrackButton.interactable = false;
        TrackContentContainer.SetActive(true);
        TrackNameText.text = "Loading...";
        URLInputContainer.SetActive(false);
        AddTrackButtonContainer.SetActive(false);
        DownloadProgressSlider.gameObject.SetActive(true);
    }

    private void SetButtonAsDefault()
    {
        RemoveDownloadEventListeners();
        TrackNameContainerTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 440);
        DeleteButton.gameObject.SetActive(true);
        TrackContentContainer.SetActive(true);
        URLInputContainer.SetActive(false);
        AddTrackButtonContainer.SetActive(false);
        DownloadProgressSlider.gameObject.SetActive(false);

        TrackNameText.text = _trackName;
        TrackIndicatorText.text = _trackIndexString;
        SetIndicatorAsIndex();
    }

    private void SetIndicatorAsIndex()
    {
        TrackIndicatorText.text = _trackIndexString;
    }

    private void SetButtonAsPlaying()
    {
        TrackNameContainerTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 475);
        DeleteButton.gameObject.SetActive(false);
        TrackContentContainer.SetActive(true);
        URLInputContainer.SetActive(false);
        AddTrackButtonContainer.SetActive(false);
        DownloadProgressSlider.gameObject.SetActive(false);

        TrackNameText.text = _trackName;
        TrackIndicatorText.text = _trackIndexString;
        SetIndicatorAsPlaying();
    }

    private void SetIndicatorAsPlaying()
    {
        TrackIndicatorText.text = PlayingIndicator;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_buttonState != ButtonState.Default) return;
        StopTrackScrollEffect();
        StartTrackScrollEffect();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopTrackScrollEffect();
    }
}