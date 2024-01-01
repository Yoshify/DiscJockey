using System;
using System.Collections.Generic;
using DiscJockey.API;
using DiscJockey.Managers;
using DiscJockey.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YoutubeDLSharp.Metadata;

namespace DiscJockey.Data
{
    public class TrackListButton : MonoBehaviour
    {
        public Button TrackButton;
        public GameObject URLInputContainer;
        public TMP_InputField URLInputField;
        public GameObject AddTrackButtonContainer;
        public Button AddTrackSaveButton;
        public Button AddTrackCancelButton;
        public TextMeshProUGUI TrackNameText;
        public Slider DownloadProgressSlider;
        
        private string _originalTrackText;
        private List<ulong> _waitingForPlayers;
        public string DownloadTaskId;
        private bool _playersHaveContent;

        public event Action OnAddTrackCancelled;
        public event Action OnAssignedDownloadTaskFailed;
        public event Action OnButtonAssignedTrack;
        public event Action OnAssignedDownloadTaskComplete;
        public event Action OnDestroyed;

        public bool IsReadyForSorting => Track != null;

        public bool IsPending =>
            _buttonState == ButtonState.PendingInput;

        public bool IsDownloading => _buttonState == ButtonState.Downloading;
        public bool IsWaitingForPlayers => _buttonState == ButtonState.WaitingForPlayers;

        public bool IsPlaying => _buttonState == ButtonState.Playing;

        public bool IsAssignedDownloadTask =>
            !string.IsNullOrEmpty(DownloadTaskId);
        
        public Track Track { get; private set; }

        private ButtonState _buttonState = ButtonState.Default;

        public enum ButtonState
        {
            Default,
            PendingInput,
            Downloading,
            WaitingForPlayers,
            Playing
        }

        private void Awake()
        {
            TrackButton.onClick.AddListener(() =>
            {
                DiscJockeyAudioManager.RequestPlayTrack(Track);
            });
            
            AddTrackSaveButton.onClick.AddListener(StartDownload);
            AddTrackCancelButton.onClick.AddListener(() =>
            {
                OnAddTrackCancelled?.Invoke();
                Destroy(gameObject);
            });
        }

        public void SetTrack(Track track)
        {
            var trackWasNotSet = Track == null;
            Track = track;
            _originalTrackText = $"{track.Metadata.Index + 1}. {track.Metadata.Name}";
            SetTrackText(_originalTrackText);
            if(trackWasNotSet) OnButtonAssignedTrack?.Invoke();
        }

        public void SetDownloadTask(string taskId) => DownloadTaskId = taskId;

        public void SetSortIndex(int sortIndex)
        {
            _originalTrackText = $"{sortIndex + 1}. {Track.Metadata.Name}";
            SetTrackText(_originalTrackText);
        }

        public void SetTrackText(string trackText) => TrackNameText.text = trackText;

        public void OnDownloadProgress(string taskId, float progress)
        {
            if (taskId == DownloadTaskId)
            {
                DownloadProgressSlider.value = progress;
            }
        }

        private void OnDestroy()
        {
            OnDestroyed?.Invoke();
            RemoveDownloadEventListeners();
        }

        private void RemoveDownloadEventListeners()
        {
            AudioLoaderAPI.OnAudioDownloadInitialResolutionCompleted -= OnDownloadInitialResolutionCompleted;
            AudioLoaderAPI.OnAudioDownloadProgress -= OnDownloadProgress;
            AudioLoaderAPI.OnAudioDownloadCompleted -= OnDownloadCompleted;
            AudioLoaderAPI.OnAudioDownloadFailed -= OnDownloadFailed;
            DiscJockeyNetworkManager.OnAllPlayersCompletedDownloadTask -= OnAllPlayersFinishedDownloadingTrack;
            DiscJockeyNetworkManager.OnPlayerFailedDownloadTask -= OnPlayerFailedDownloadTask;
            DiscJockeyAudioManager.OnTrackAddedToTrackList -= SetDownloadedTrackMetadata;
        }

        private void OnDownloadFailed(string taskId, string error)
        {
            if (taskId == DownloadTaskId)
            {
                OnAssignedDownloadTaskFailed?.Invoke();
                DiscJockeyNetworkManager.Instance.NotifyPlayerFailedDownloadTaskServerRpc(
                    LocalPlayerHelper.Player.playerClientId, taskId);
                Destroy(gameObject);
            }
        }

        private void SetDownloadedTrackMetadata(Track track, AudioLoaderAPI.FileSource source, string taskId)
        {
            if (source != AudioLoaderAPI.FileSource.DownloadedFile) return;
            if (taskId != DownloadTaskId) return;
            DiscJockeyPlugin.LogInfo($"TrackListButton<OnDownloadCompleted>: Setting our Track to {track}");
            SetTrack(track);
        }

        private void OnDownloadCompleted(string taskId)
        {
            if (taskId == DownloadTaskId)
            {
                DiscJockeyPlugin.LogInfo($"TrackListButton<OnDownloadCompleted>: We've completed {taskId}");
                SetButtonState(ButtonState.WaitingForPlayers);
                OnAssignedDownloadTaskComplete?.Invoke();
                DiscJockeyNetworkManager.Instance.NotifyDownloadTaskCompleteServerRpc(LocalPlayerHelper.Player.playerClientId, taskId);
            }
        }

        private void OnAllPlayersFinishedDownloadingTrack(string taskId)
        {
            if (taskId != DownloadTaskId) return;
            DiscJockeyPlugin.LogInfo($"TrackListButton<OnAllPlayersFinishedDownloadingTrack>: All players have completed {taskId}");
            
            if (_buttonState == ButtonState.WaitingForPlayers)
            {
                SetButtonState(ButtonState.Default);
            }
        }
        
        private void OnDownloadInitialResolutionCompleted(string taskId, VideoData videoData)
        {
            if (taskId == DownloadTaskId)
            {
                SetTrackText(videoData.Title);
                GameUtils.LogDiscJockeyMessageToServer($"Now downloading: {videoData.Title}");
            }
        }

        private void SetupDownloadEventListeners()
        {
            AudioLoaderAPI.OnAudioDownloadInitialResolutionCompleted += OnDownloadInitialResolutionCompleted;
            AudioLoaderAPI.OnAudioDownloadProgress += OnDownloadProgress;
            AudioLoaderAPI.OnAudioDownloadCompleted += OnDownloadCompleted;
            AudioLoaderAPI.OnAudioDownloadFailed += OnDownloadFailed;
            DiscJockeyNetworkManager.OnAllPlayersCompletedDownloadTask += OnAllPlayersFinishedDownloadingTrack;
            DiscJockeyNetworkManager.OnPlayerFailedDownloadTask += OnPlayerFailedDownloadTask;
            DiscJockeyAudioManager.OnTrackAddedToTrackList += SetDownloadedTrackMetadata;
        }

        private void OnPlayerFailedDownloadTask(ulong playerId, string taskId)
        {
            if (taskId == DownloadTaskId)
            {
                OnAssignedDownloadTaskFailed?.Invoke();
                Destroy(gameObject);
            }
        }

        private void StartDownload()
        {
            SetButtonState(ButtonState.Downloading);
            SetDownloadTask(AudioLoaderAPI.GetUniqueTaskId());
            DiscJockeyAudioManager.DownloadYouTubeAudio(URLInputField.text, DownloadTaskId);
        }

        public static TrackListButton InstantiateAsPendingInput(Transform parent)
        {
            var trackListButton = Instantiate(AssetUtils.TrackListButtonPrefab, parent).GetComponent<TrackListButton>();
            trackListButton.SetButtonState(ButtonState.PendingInput);
            return trackListButton;
        }

        public static TrackListButton InstantiateAsNormal(Transform parent, Track track)
        {
            var trackListButton = Instantiate(AssetUtils.TrackListButtonPrefab, parent).GetComponent<TrackListButton>();
            trackListButton.SetTrack(track);
            trackListButton.SetButtonState(ButtonState.Default);
            return trackListButton;
        }
        
        public static TrackListButton InstantiateAsDownloading(Transform parent, string taskId)
        {
            var trackListButton = Instantiate(AssetUtils.TrackListButtonPrefab, parent).GetComponent<TrackListButton>();
            trackListButton.SetButtonState(ButtonState.Downloading);
            trackListButton.SetDownloadTask(taskId);
            return trackListButton;
        }

        public void SetButtonState(ButtonState buttonState)
        {
            _buttonState = buttonState;
            DiscJockeyPlugin.LogInfo($"TrackListButton<SetButtonState>: Updating Button State to {buttonState}");
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
                case ButtonState.WaitingForPlayers:
                    SetButtonAsWaiting();
                    break;
                case ButtonState.Playing:
                    SetButtonAsPlaying();
                    break;
                default:
                    SetButtonAsDefault();
                    break;
            }
        }

        private void SetButtonAsPendingInput()
        {
            TrackButton.interactable = false;
            TrackNameText.gameObject.SetActive(false);
            URLInputContainer.SetActive(true);
            AddTrackButtonContainer.SetActive(true);
            DownloadProgressSlider.gameObject.SetActive(false);
        }

        private void SetButtonAsDownloading()
        {
            SetupDownloadEventListeners();
            TrackButton.interactable = false;
            TrackNameText.gameObject.SetActive(true);
            TrackNameText.text = "Loading...";
            URLInputContainer.SetActive(false);
            AddTrackButtonContainer.SetActive(false);
            DownloadProgressSlider.gameObject.SetActive(true);
        }

        private void SetButtonAsDefault()
        {
            DownloadTaskId = null;
            RemoveDownloadEventListeners();
            TrackButton.interactable = true;
            TrackNameText.gameObject.SetActive(true);
            URLInputContainer.SetActive(false);
            AddTrackButtonContainer.SetActive(false);
            DownloadProgressSlider.gameObject.SetActive(false);

            TrackNameText.text = _originalTrackText;
        }
        
        private void SetButtonAsWaiting()
        {
            TrackButton.interactable = false;
            TrackNameText.gameObject.SetActive(true);
            URLInputContainer.SetActive(false);
            AddTrackButtonContainer.SetActive(false);
            DownloadProgressSlider.gameObject.SetActive(false);

            var currentText = TrackNameText.text;
            SetTrackText($"[WAITING FOR PLAYERS] {currentText}");
        }

        private void SetButtonAsPlaying()
        {
            TrackNameText.text = $"> {_originalTrackText}";
        }
    }
}