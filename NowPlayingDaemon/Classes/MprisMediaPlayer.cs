using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using hass_mpris.HassClasses;
using Microsoft.Extensions.Logging;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]

namespace NowPlayingDaemon
{
    public class MprisMediaPlayer : IMediaPlayer2, IPlayer, IMprisMediaPlayer
    {
        public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");

        private readonly ILogger<MprisMediaPlayer> _logger;
        private readonly DBusConnectionManager _connectionManager;

        readonly MprisMediaPlayerProperties mprisMediaPlayerProperties;

        readonly MprisPlayerProperties mprisPlayerProperties;

        private bool _serviceIsRegistered;
        private string _serviceName;

        public string ServiceName
        {
            get => _serviceName;
            private set => _serviceName = value;
        }

        public event Action<PropertyChanges> OnPropertiesChanged = delegate { };
        public event Action OnRaise = delegate { };
        public event Action OnQuit = delegate { };
        public event Action OnPlay = delegate { };
        public event Action OnPause = delegate { };
        public event Action OnStop = delegate { };
        public event Action OnPlayPause = delegate { };
        public event Action OnPrevious = delegate { };
        public event Action OnNext = delegate { };
        public event Action<long> OnSeek = delegate { };
        public event Action<string, long> OnSetPosition = delegate { };
        public event Action<string> OnOpenUri = delegate { };

        public MprisMediaPlayer(
            ILogger<MprisMediaPlayer> logger,
            DBusConnectionManager connectionManager
        )
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _serviceName = "";
            _serviceIsRegistered = false;

            mprisMediaPlayerProperties = new MprisMediaPlayerProperties();
            mprisPlayerProperties = new MprisPlayerProperties();
        }

        Task<MprisMediaPlayerProperties> IMediaPlayer2.GetAllAsync()
        {
            _logger.LogTrace("Getting all properties on interface IMediaPlayer2.");
            return Task.FromResult(mprisMediaPlayerProperties);
        }

        Task<MprisPlayerProperties> IPlayer.GetAllAsync()
        {
            _logger.LogTrace("Getting all properties on interface IPlayer.");
            return Task.FromResult(mprisPlayerProperties);
        }

        Task<object> IPlayer.GetAsync(string property)
        {
            var value = GetProperty(dbusInterface.IPlayer, property);
            return Task.FromResult<object>(value);
        }

        Task<object> IMediaPlayer2.GetAsync(string property)
        {
            var value = GetProperty(dbusInterface.IMediaPlayer2, property);
            return Task.FromResult<object>(value);
        }

        private object GetProperty(dbusInterface iface, string property)
        {
            _logger.LogTrace($"Getting property {property} on interface {iface}");

            object targetObject = iface switch
            {
                dbusInterface.IMediaPlayer2 => mprisMediaPlayerProperties,
                dbusInterface.IPlayer => mprisPlayerProperties,
                _ => throw new ArgumentException("Invalid interface type")
            };

            var propInfo = targetObject.GetType().GetField(property);

            if (propInfo == null)
            {
                _logger.LogError($"Property {property} not found on interface {iface}.");
                throw new ArgumentException($"Property {property} not found.");
            }

            var value = propInfo.GetValue(targetObject);
            if (value != null && value.GetType().IsEnum)
            {
                return value.ToString();
            }

            return value;
        }

        async Task IMprisMediaPlayer.SetAsync(dbusInterface iface, string property, object value)
        {
            await SetProperty(iface, property, value);
        }

        async Task IPlayer.SetAsync(string property, object value)
        {
            await SetProperty(dbusInterface.IPlayer, property, value);
        }

        async Task IMediaPlayer2.SetAsync(string property, object value)
        {
            await SetProperty(dbusInterface.IMediaPlayer2, property, value);
        }

        private Task SetProperty(dbusInterface iface, string property, object value)
        {
            _logger.LogInformation($"Setting property {property} on interface {iface}");

            object targetObject = iface switch
            {
                dbusInterface.IMediaPlayer2 => mprisMediaPlayerProperties,
                dbusInterface.IPlayer => mprisPlayerProperties,
                _ => throw new ArgumentException("Invalid interface type")
            };

            var propInfo = targetObject.GetType().GetField(property);

            if (propInfo == null || propInfo.IsInitOnly)
            {
                _logger.LogError($"Attempted to set non-existent or readonly property: {property}");
                throw new ArgumentException($"Property {property} not found or is readonly.");
            }

            if (value.GetType() != propInfo.FieldType)
            {
                try
                {
                    value = Convert.ChangeType(value, propInfo.FieldType);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to convert type for {property}: {ex.Message}");
                    throw;
                }
            }

            propInfo.SetValue(targetObject, value);
            OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty(property, value));

            return Task.CompletedTask;
        }

        public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
        {
            return SignalWatcher.AddAsync(this, nameof(OnPropertiesChanged), handler);
        }

        public async Task RegisterPlayer(string identity, string desktopEntry)
        {
            _logger.LogDebug("Registering player to dbus.");
            mprisMediaPlayerProperties.Identity = identity;
            mprisMediaPlayerProperties.DesktopEntry = desktopEntry;

            ServiceName = $"org.mpris.MediaPlayer2.{desktopEntry}";

            await _connectionManager.Connection.RegisterObjectAsync(this);
        }

        public void UnregisterPlayer()
        {
            _logger.LogDebug("Unregistering player from dbus.");
            _connectionManager.Connection.UnregisterObject(this);
        }

        private async Task<bool> IsServiceRegistered()
        {
            // currently not in use since I'm trying to see if a simple
            // bool property is enough to keep track of the registration status
            var allServices = await _connectionManager.Connection.ListServicesAsync();
            return allServices.Any(s => s == ServiceName);
        }

        public async Task RegisterService()
        {
            if (!_serviceIsRegistered)
            {
                _logger.LogDebug("Registering service to dbus.");
                await _connectionManager.Connection.RegisterServiceAsync(ServiceName);
                _serviceIsRegistered = true;
            }
        }

        public async Task UnregisterService()
        {
            if (_serviceIsRegistered)
            {
                _logger.LogDebug("UnRegistering service from dbus.");
                await _connectionManager.Connection.UnregisterServiceAsync(ServiceName);
                _serviceIsRegistered = false;
            }
        }

        public Task SetPlaybackStatus(PlaybackStatus status)
        {
            mprisPlayerProperties.PlaybackStatus = status;
            OnPropertiesChanged?.Invoke(
                PropertyChanges.ForProperty("PlaybackStatus", status.ToString())
            );
            return Task.CompletedTask;
        }

        public Task SetCanPlay(bool state)
        {
            if (mprisPlayerProperties.CanPlay == state)
            {
                return Task.CompletedTask;
            }

            mprisPlayerProperties.CanPlay = state;
            OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("CanPlay", state.ToString()));
            return Task.CompletedTask;
        }

        public Task SetCanPause(bool state)
        {
            if (mprisPlayerProperties.CanPause == state)
            {
                return Task.CompletedTask;
            }

            mprisPlayerProperties.CanPause = state;
            OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("CanPause", state.ToString()));
            return Task.CompletedTask;
        }

        public Task SetCanQuit(bool state)
        {
            if (mprisMediaPlayerProperties.CanQuit == state)
            {
                return Task.CompletedTask;
            }

            mprisMediaPlayerProperties.CanQuit = state;
            OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("CanQuit", state.ToString()));
            return Task.CompletedTask;
        }

        public Task SetCanGoPrevious(bool state)
        {
            if (mprisPlayerProperties.CanGoPrevious == state)
            {
                return Task.CompletedTask;
            }

            mprisPlayerProperties.CanGoPrevious = state;
            OnPropertiesChanged?.Invoke(
                PropertyChanges.ForProperty("CanGoPrevious", state.ToString())
            );
            return Task.CompletedTask;
        }

        public Task SetCanGoNext(bool state)
        {
            if (mprisPlayerProperties.CanGoNext == state)
            {
                return Task.CompletedTask;
            }

            mprisPlayerProperties.CanGoNext = state;
            OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("CanGoNext", state.ToString()));
            return Task.CompletedTask;
        }

        public Task UpdateMetadata(
            string trackId,
            long length,
            string[] artist,
            string title,
            string album
        )
        {
            var metadata = new Dictionary<string, object>
            {
                { "mpris:trackid", trackId },
                { "mpris:length", length },
                { "xesam:artist", artist },
                { "xesam:title", title },
                { "xesam:album", album }
            };

            return UpdateMetadata(metadata);
        }

        public Task UpdateMetadata(IDictionary<string, object> customMetadata)
        {
            // https://www.freedesktop.org/wiki/Specifications/mpris-spec/metadata/

            _logger.LogDebug("Updating Metadata");
            foreach (var item in customMetadata)
            {
                _logger.LogDebug($"Setting '{item.Key}' to '{item.Value}'.");

                mprisPlayerProperties.Metadata[item.Key] = item.Value;
            }
            OnPropertiesChanged?.Invoke(
                PropertyChanges.ForProperty("Metadata", mprisPlayerProperties.Metadata)
            );

            return Task.CompletedTask;
        }

        public Task AddMetadata(string key, object value)
        {
            _logger.LogInformation($"Adding Metadata item: {key}");
            if (!mprisPlayerProperties.Metadata.ContainsKey(key))
            {
                mprisPlayerProperties.Metadata.Add(key, value);
                OnPropertiesChanged?.Invoke(
                    PropertyChanges.ForProperty("Metadata", mprisPlayerProperties.Metadata)
                );
            }
            else
            {
                _logger.LogWarning($"Metadata key already exists: {key}");
            }
            return Task.CompletedTask;
        }

        public Task RemoveMetadata(string key)
        {
            _logger.LogInformation($"Removing Metadata item: {key}");
            if (mprisPlayerProperties.Metadata.ContainsKey(key))
            {
                mprisPlayerProperties.Metadata.Remove(key);
                OnPropertiesChanged?.Invoke(
                    PropertyChanges.ForProperty("Metadata", mprisPlayerProperties.Metadata)
                );
            }
            else
            {
                _logger.LogWarning($"Metadata key not found: {key}");
            }
            return Task.CompletedTask;
        }

        public Task RaiseAsync()
        {
            _logger.LogDebug("Received raise event.");
            if (!mprisMediaPlayerProperties.CanRaise)
            {
                _logger.LogError("Raise operation is not allowed.");
                throw new DBusException(
                    "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                    "Raise is not allowed."
                );
            }

            OnRaise.Invoke();
            return Task.CompletedTask;
        }

        public Task QuitAsync()
        {
            _logger.LogDebug("Received quit event.");
            if (!mprisMediaPlayerProperties.CanQuit)
            {
                _logger.LogError("Quit operation is not allowed.");
                throw new DBusException(
                    "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                    "Quit is not allowed."
                );
            }

            OnQuit.Invoke();
            return Task.CompletedTask;
        }

        public Task PlayAsync()
        {
            _logger.LogDebug("Received play event.");
            if (!mprisPlayerProperties.CanControl || !mprisPlayerProperties.CanPlay)
            {
                _logger.LogError("Play operation is not allowed.");
                throw new DBusException(
                    "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                    "Play is not allowed."
                );
            }

            OnPlay.Invoke();
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            _logger.LogDebug("Received pause event.");
            if (!mprisPlayerProperties.CanControl || !mprisPlayerProperties.CanPause)
            {
                _logger.LogError("Pause operation is not allowed.");
                throw new DBusException(
                    "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                    "Pause is not allowed."
                );
            }
            OnPause.Invoke();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _logger.LogDebug("Received stop event.");
            if (!mprisPlayerProperties.CanControl)
            {
                _logger.LogError("Stop operation is not allowed.");
                throw new DBusException(
                    "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                    "Stop is not allowed."
                );
            }
            OnStop.Invoke();
            return Task.CompletedTask;
        }

        public Task PlayPauseAsync()
        {
            _logger.LogDebug("Received playpause event.");
            if (!mprisPlayerProperties.CanControl || !mprisPlayerProperties.CanPause)
            {
                _logger.LogError("PlayPause operation is not allowed.");
                throw new DBusException(
                    "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                    "PlayPause is not allowed."
                );
            }
            OnPlayPause.Invoke();
            return Task.CompletedTask;
        }

        public Task PreviousAsync()
        {
            _logger.LogDebug("Reveived previous track event.");

            if (!mprisPlayerProperties.CanControl || !mprisPlayerProperties.CanGoPrevious)
            {
                _logger.LogError("Previous operation is not allowed.");
                throw new DBusException(
                    "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                    "Previous is not allowed."
                );
            }

            OnPrevious.Invoke();
            return Task.CompletedTask;
        }

        public Task NextAsync()
        {
            _logger.LogDebug("Received next track event.");
            if (!mprisPlayerProperties.CanControl || !mprisPlayerProperties.CanGoNext)
            {
                _logger.LogError("Next operation is not allowed.");
                throw new DBusException(
                    "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                    "Next is not allowed."
                );
            }

            OnNext.Invoke();
            return Task.CompletedTask;
        }

        public Task SeekAsync(long offset)
        {
            _logger.LogDebug("Received seek event.");
            if (!mprisPlayerProperties.CanControl || !mprisPlayerProperties.CanSeek)
            {
                _logger.LogError("Seek operation is not allowed.");
                throw new DBusException(
                    "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                    "Seek is not allowed."
                );
            }

            OnSeek.Invoke(offset);
            return Task.CompletedTask;
        }

        public Task SetPositionAsync(ObjectPath TrackId, long Position)
        {
            _logger.LogDebug("Received set position event.");
            if (!mprisPlayerProperties.CanControl || !mprisPlayerProperties.CanSeek)
            {
                _logger.LogError("set position operation is not allowed.");
                throw new DBusException(
                    "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                    "Set position is not allowed."
                );
            }

            OnSetPosition.Invoke(TrackId.ToString(), Position);
            return Task.CompletedTask;
        }

        public Task OpenUriAsync(string Uri)
        {
            _logger.LogDebug("Received open uri event.");
            _logger.LogError("Open uri operation is not allowed.");
            throw new DBusException(
                "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                "Open uri is not allowed."
            );

            // OnOpenUri.Invoke(Uri);
            // return Task.CompletedTask;
        }
    }
}
