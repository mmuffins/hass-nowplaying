using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]

namespace NowPlayingDaemon
{
    public class MprisMediaPlayer : IMediaPlayer2, IPlayer, IMprisMediaPlayer
    {
        public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");

        private readonly ILogger<MprisMediaPlayer> _logger;
        readonly MprisMediaPlayerProperties mprisMediaPlayerProperties;

        readonly MprisPlayerProperties mprisPlayerProperties;

        public event Action<PropertyChanges> OnPropertiesChanged = delegate { };

        public event Action OnPlayPause;

        public MprisMediaPlayer(ILogger<MprisMediaPlayer> logger)
        {
            this._logger = logger;
            mprisMediaPlayerProperties = new MprisMediaPlayerProperties();
            mprisPlayerProperties = new MprisPlayerProperties();
        }

        Task<MprisMediaPlayerProperties> IMediaPlayer2.GetAllAsync()
        {
            _logger.LogDebug("Getting all properties on interface IMediaPlayer2.");
            return Task.FromResult(mprisMediaPlayerProperties);
        }

        Task<MprisPlayerProperties> IPlayer.GetAllAsync()
        {
            _logger.LogDebug("Getting all properties on interface IPlayer.");
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

        public async Task RegisterPlayer(
            Connection connection,
            string identity,
            string desktopEntry,
            bool canControl
        )
        {
            mprisMediaPlayerProperties.Identity = identity;
            mprisMediaPlayerProperties.DesktopEntry = desktopEntry;
            mprisPlayerProperties.CanControl = canControl;
            mprisPlayerProperties.CanPlay = true;

            await connection.RegisterObjectAsync(this);
            await connection.RegisterServiceAsync("org.mpris.MediaPlayer2.myplayer");
        }

        private object GetProperty(dbusInterface iface, string property)
        {
            _logger.LogInformation($"Getting property {property} on interface {iface}");

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

            _logger.LogInformation("Updating Metadata");
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
            _logger.LogDebug("RaiseAsync called.");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task QuitAsync()
        {
            _logger.LogDebug("QuitAsync called.");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task PlayAsync()
        {
            _logger.LogDebug("PlayAsync called.");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            _logger.LogDebug("PauseAsync called.");
            _logger.LogInformation("PauseAsync");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _logger.LogDebug("StopAsync called.");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task PlayPauseAsync()
        {
            _logger.LogDebug("PlayPauseAsync called.");
            if (!mprisPlayerProperties.CanPause)
            {
                _logger.LogError("PlayPause operation is not allowed.");
                throw new DBusException(
                    "org.mpris.MediaPlayer2.Player.Error.NotAllowed",
                    "PlayPause is not allowed."
                );
            }
            OnPlayPause?.Invoke();
            return Task.CompletedTask;
        }

        public Task PreviousAsync()
        {
            _logger.LogDebug("PreviousAsync called.");

            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task NextAsync()
        {
            _logger.LogDebug("NextAsync called.");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task SeekAsync(long Offset)
        {
            _logger.LogDebug("SeekAsync called.");
            throw new NotImplementedException();
        }

        public Task SetPositionAsync(ObjectPath TrackId, long Position)
        {
            _logger.LogDebug("SetPositionAsync called.");
            throw new NotImplementedException();
        }

        public Task OpenUriAsync(string Uri)
        {
            _logger.LogDebug("OpenUriAsync called.");
            throw new NotImplementedException();
        }
    }
}
