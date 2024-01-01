using System;
using System.Collections.Generic;
using System.Linq;
using DiscJockey.API;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DiscJockey.Data;
using DiscJockey.Patches;
using DiscJockey.Utils;

namespace DiscJockey.Managers
{
    public class DiscJockeyUIManager : MonoBehaviour
    {
        public TextMeshProUGUI VersionText;
        public Button PreviousButton;
        public Button NextButton;
        public Button StopButton;
        public Button PlayButton;
        public Slider TrackProgressSlider;
        public TextMeshProUGUI TrackProgressText;
        public TextMeshProUGUI TrackName;
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

        private BoomboxItem activeBoombox;
        private string currentTrackLengthString;
        private bool trackListPopulated = false;
        private bool isPlayingTrack = false;
        private static bool _hasInitialized = false;
        private List<TrackListButton> trackListButtonInstances = new List<TrackListButton>();

        public static DiscJockeyUIManager Instance;

        private TrackListButton _pendingTrackButton;
        
        private TrackListButton _activeTrackListButton;
        private TrackListButton activeTrackListButton
        {
            get => _activeTrackListButton;
            set
            {
                if (_activeTrackListButton != null)
                {
                    _activeTrackListButton.SetButtonState(TrackListButton.ButtonState.Default);
                }
                _activeTrackListButton = value;
                if (_activeTrackListButton != null)
                {
                    _activeTrackListButton.SetButtonState(TrackListButton.ButtonState.Playing);
                }
            }
        }

        private TrackListButton FindMatchingTrackListButton(int trackIndex) =>
            trackListButtonInstances.FirstOrDefault((tlb) => tlb.Track.Metadata.Index == trackIndex);

        public void RegisterEventListeners()
        {
            DiscJockeyAudioManager.OnTrackAddedToTrackList += OnTrackAddedToTrackList;
            DiscJockeyNetworkManager.OnPlayTrackRequestReceived += OnPlayTrack;
            DiscJockeyNetworkManager.OnStopTrackRequestReceived += OnStopTrack;
            DiscJockeyNetworkManager.OnPlayerReceivedDownloadTask += AddDownloadingTrackButton;
            GameNetworkManagerPatches.OnDisconnect += OnDisconnect;
        }

        public void UnregisterEventListeners()
        {
            DiscJockeyAudioManager.OnTrackAddedToTrackList -= OnTrackAddedToTrackList;
            DiscJockeyNetworkManager.OnPlayTrackRequestReceived -= OnPlayTrack;
            DiscJockeyNetworkManager.OnStopTrackRequestReceived -= OnStopTrack;
            DiscJockeyNetworkManager.OnPlayerReceivedDownloadTask -= AddDownloadingTrackButton;
            GameNetworkManagerPatches.OnDisconnect -= OnDisconnect;
        }

        private void OnDisconnect()
        {
            if (_pendingTrackButton != null)
            {
                if (_pendingTrackButton.IsPending || _pendingTrackButton.IsDownloading || _pendingTrackButton.IsWaitingForPlayers)
                {
                    Destroy(_pendingTrackButton.gameObject);
                }
            }

            foreach (var button in trackListButtonInstances.ToList())
            {
                if (button.IsPending || button.IsDownloading || button.IsWaitingForPlayers)
                {
                    Destroy(button.gameObject);
                }
            }

            _activeTrackListButton = null;
        }

        private bool AlreadyHasDownloadingButtonForTask(string taskId) =>
            trackListButtonInstances.Any(button => button.IsAssignedDownloadTask && button.DownloadTaskId == taskId);

        private void AddDownloadingTrackButton(string taskId)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyUIManager<AddDownloadingTrackButton>: Received task {taskId}");
            if (AlreadyHasDownloadingButtonForTask(taskId)) return;
            DiscJockeyPlugin.LogInfo($"DiscJockeyUIManager<AddDownloadingTrackButton>: We don't have a button for task {taskId}, so adding one");
            var trackListButton = TrackListButton.InstantiateAsDownloading(TracklistContainer.transform, taskId);
            trackListButton.transform.SetAsFirstSibling();
            trackListButtonInstances.Add(trackListButton);
            trackListButton.OnAssignedDownloadTaskFailed += () => { trackListButtonInstances.Remove(trackListButton); };
            trackListButton.OnAddTrackCancelled += () => { trackListButtonInstances.Remove(trackListButton); };
            trackListButton.OnButtonAssignedTrack += SortTrackList;
        }
        
        private void OnTrackAddedToTrackList(Track track, AudioLoaderAPI.FileSource source, string taskId)
        {
            if (source == AudioLoaderAPI.FileSource.LocalFile)
            {
                AddButtonToTrackList(track);
            }
        }

        public void ScrubSong(float time)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyUIManager<ScrubSong>: Scrub song to {time}");
            var currentMetadata = DiscJockeyBoomboxManager.ActiveBoomboxMetadata;
            currentMetadata.CurrentTrackMetadata.Progress = time;
            DiscJockeyNetworkManager.Instance.ScrubTrackOnBoomboxServerRpc(DiscJockeyBoomboxManager.ActiveBoombox.NetworkObjectId, currentMetadata.CurrentTrackMetadata);
        }

        private void OnPlayTrack(BoomboxItem boomboxItem, BoomboxMetadata boomboxMetadata)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyUIManager<OnPlayTrack>: Playing Track: {boomboxMetadata}");
            DiscJockeyPlugin.LogInfo($"DiscJockeyUIManager<OnPlayTrack>: Setting button {boomboxMetadata.CurrentTrackMetadata.Index} as Playing");
            activeTrackListButton = FindMatchingTrackListButton(boomboxMetadata.CurrentTrackMetadata.Index);
            var lengthSpan = TimeSpan.FromSeconds(boomboxMetadata.CurrentTrackMetadata.Length);
            currentTrackLengthString = $"{lengthSpan.Minutes:D2}:{lengthSpan.Seconds:D2}";
            TrackProgressSlider.maxValue = boomboxMetadata.CurrentTrackMetadata.Length;
            TrackName.text = boomboxMetadata.CurrentTrackMetadata.Name;
            StopButton.gameObject.SetActive(true);
            PlayButton.gameObject.SetActive(false);
            isPlayingTrack = true;
        }
        
        private void OnStopTrack(BoomboxItem boomboxItem, BoomboxMetadata boomboxMetadata)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyUIManager<OnStopTrack>: Stopping Track");
            activeTrackListButton = null;
            TrackProgressSlider.value = 0;
            TrackProgressSlider.minValue = 0;
            TrackProgressSlider.maxValue = 0;

            TrackProgressText.text = string.Empty;
            currentTrackLengthString = string.Empty;
            
            TrackName.text = string.Empty;
            StopButton.gameObject.SetActive(false);
            PlayButton.gameObject.SetActive(true);
            isPlayingTrack = false;
        }

        private void AddButtonToTrackList(Track track)
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyUIManager<AddButtonToTrackList>: Adding button for track {track}");
                
            var trackListButton = TrackListButton.InstantiateAsNormal(TracklistContainer.transform, track);
            trackListButton.transform.SetSiblingIndex(track.Metadata.Index);
            trackListButtonInstances.Add(trackListButton);
            SortTrackList();
        }

        public void DisableAddTrackButton()
        {
            AddTrackButton.gameObject.SetActive(false);
        }

        public void Awake()
        {
            if (_hasInitialized) return;
            if (Instance == null) Instance = this;
            DiscJockeyPlugin.LogInfo($"DiscJockeyUIManager<Awake>: Called");
            InitUIEvents();
            DiscJockeyUIPanel.SetActive(false);
            RegisterEventListeners();

            if (DiscJockeyConfig.DisableCreditsText.Value)
            {
                CreditsText.gameObject.SetActive(false);
            }
            
            DiscJockeyPlugin.LogInfo($"DiscJockeyUIManager<Awake>: Initialized");
            _hasInitialized = true;
        }

        public void ToggleUIPanel()
        {
            if (PanelIsActive())
            {
                HideDJPanel();
            }
            else
            {
                ShowDJPanel();
            }
        }

        public void ShowDJPanel()
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyUIManager<ShowDJPanel>: Called");
            DiscJockeyInputManager.DisablePlayerInteractions();
            DiscJockeyUIPanel.SetActive(true);
        }

        public void HideDJPanel()
        {
            DiscJockeyPlugin.LogInfo($"DiscJockeyUIManager<HideDJPanel>: Called");
            DiscJockeyInputManager.EnablePlayerInteractions();
            DiscJockeyUIPanel.SetActive(false);
        }

        private void SortTrackList()
        {
            var buttonsNotReadyForSorting =
                trackListButtonInstances.Where(button => !button.IsReadyForSorting).ToList();
            var buttonsReadyForSorting =
                trackListButtonInstances.Where(button => button.IsReadyForSorting).ToList();
            
            buttonsReadyForSorting.Sort((a, b) => string.CompareOrdinal(a.Track.Metadata.Name, b.Track.Metadata.Name));

            for (var i = 0; i < buttonsReadyForSorting.Count; i++)
            {
                var button = buttonsReadyForSorting[i];
                button.transform.SetSiblingIndex(i);
                button.SetSortIndex(i);
            }

            foreach (var button in buttonsNotReadyForSorting)
            {
                button.transform.SetAsFirstSibling();
            }
        }

        private bool PanelIsActive() => DiscJockeyUIPanel.activeSelf;

        private void InitUIEvents()
        {
            TrackProgressSlider.interactable = true;
            TrackProgressSlider.onValueChanged.AddListener(ScrubSong);
            
            NextButton.onClick.AddListener(() =>
            {
                DiscJockeyAudioManager.RequestPlayTrack(
                    DiscJockeyAudioManager.TrackList.GetNextTrack(
                        DiscJockeyBoomboxManager.ActiveBoomboxMetadata.CurrentTrackMetadata.Index,
                        DiscJockeyBoomboxManager.ActiveBoomboxMetadata.TrackMode,
                        true));
            });

            PreviousButton.onClick.AddListener(() =>
            {
                DiscJockeyAudioManager.RequestPlayTrack(
                    DiscJockeyAudioManager.TrackList.GetPreviousTrack(
                        DiscJockeyBoomboxManager.ActiveBoomboxMetadata.CurrentTrackMetadata.Index
                    ));
            });

            StopButton.onClick.AddListener(DiscJockeyAudioManager.RequestStopTrack);

            PlayButton.onClick.AddListener(() =>
            {
                DiscJockeyAudioManager.RequestPlayTrack(DiscJockeyAudioManager.TrackList.GetRandomTrack());
            });

            CloseButton.onClick.AddListener(HideDJPanel);

            SequentialButton.onClick.AddListener(() =>
            {
                if (!DiscJockeyBoomboxManager.InteractionsActive)
                {
                    return;
                }

                var currentMetadata = DiscJockeyBoomboxManager.ActiveBoomboxMetadata;
                currentMetadata.TrackMode = TrackMode.Shuffle;
                DiscJockeyNetworkManager.Instance.UpdateMetadataForBoomboxServerRpc(DiscJockeyBoomboxManager.ActiveBoombox.NetworkObjectId, currentMetadata);
                ShuffleButton.gameObject.SetActive(true);
                SequentialButton.gameObject.SetActive(false);
            });

            ShuffleButton.onClick.AddListener(() =>
            {
                if (!DiscJockeyBoomboxManager.InteractionsActive)
                {
                    return;
                }

                var currentMetadata = DiscJockeyBoomboxManager.ActiveBoomboxMetadata;
                currentMetadata.TrackMode = TrackMode.Repeat;
                DiscJockeyNetworkManager.Instance.UpdateMetadataForBoomboxServerRpc(DiscJockeyBoomboxManager.ActiveBoombox.NetworkObjectId, currentMetadata);
                RepeatButton.gameObject.SetActive(true);
                ShuffleButton.gameObject.SetActive(false);
            });

            RepeatButton.onClick.AddListener(() =>
            {
                if (!DiscJockeyBoomboxManager.InteractionsActive)
                {
                    return;
                }

                var currentMetadata = DiscJockeyBoomboxManager.ActiveBoomboxMetadata;
                currentMetadata.TrackMode = TrackMode.Sequential;
                DiscJockeyNetworkManager.Instance.UpdateMetadataForBoomboxServerRpc(DiscJockeyBoomboxManager.ActiveBoombox.NetworkObjectId, currentMetadata);
                SequentialButton.gameObject.SetActive(true);
                RepeatButton.gameObject.SetActive(false);
            });

            VolumeSlider.onValueChanged.AddListener((value) =>
            {
                if (!DiscJockeyBoomboxManager.InteractionsActive)
                {
                    return;
                }

                DiscJockeyBoomboxManager.ActiveBoombox.boomboxAudio.volume = value;
            });
            
            AddTrackButton.onClick.AddListener(AddPendingTrackButton);
        }

        public void AddPendingTrackButton()
        {
            if (_pendingTrackButton != null) return;
            
            _pendingTrackButton = TrackListButton.InstantiateAsPendingInput(TracklistContainer.transform);
            _pendingTrackButton.transform.SetAsFirstSibling();
            trackListButtonInstances.Add(_pendingTrackButton);
            TrackListScrollRect.normalizedPosition = new Vector2(0, 1);

            _pendingTrackButton.OnAddTrackCancelled += () =>
            {
                trackListButtonInstances.Remove(_pendingTrackButton);
                _pendingTrackButton = null;
            };
            
            _pendingTrackButton.OnAssignedDownloadTaskFailed += () =>
            {
                trackListButtonInstances.Remove(_pendingTrackButton);
                _pendingTrackButton = null;
            };

            _pendingTrackButton.OnAssignedDownloadTaskComplete += () =>
            {
                _pendingTrackButton = null;
            };
            
            _pendingTrackButton.OnButtonAssignedTrack += SortTrackList;

            _pendingTrackButton.OnDestroyed += () =>
            {
                trackListButtonInstances.Remove(_pendingTrackButton);
                _pendingTrackButton = null;
            };
        }

        public void ClearTracklist()
        {
            foreach (var instance in trackListButtonInstances)
            {
                Destroy(instance.gameObject);
            }

            trackListButtonInstances.Clear();
        }

        public void SetTrackName(string trackName) => TrackName.text = trackName;
        
        public void Update()
        {
            if (!DiscJockeyBoomboxManager.InteractionsActive)
            {
                return;
            }

            var activeBoomboxMetadata = DiscJockeyBoomboxManager.ActiveBoomboxMetadata;
            if(activeBoomboxMetadata.CurrentTrackMetadata.TrackSelected)
            {
                TrackProgressSlider.SetValueWithoutNotify(activeBoomboxMetadata.CurrentTrackMetadata.Progress);
                TrackProgressSlider.maxValue = activeBoomboxMetadata.CurrentTrackMetadata.Length;

                var currentTimespan = TimeSpan.FromSeconds(TrackProgressSlider.value);
                var currentTime = $"{currentTimespan.Minutes:D2}:{currentTimespan.Seconds:D2}";
                TrackProgressText.text = $"{currentTime}/{currentTrackLengthString}";
                TrackName.text = activeBoomboxMetadata.CurrentTrackMetadata.Name;
            }
            else
            {
                TrackProgressSlider.value = 0f;
                TrackProgressSlider.maxValue = 1f;
                TrackProgressText.text = string.Empty;
                TrackName.text = "No Track Playing";
            }
        }
    }
}
