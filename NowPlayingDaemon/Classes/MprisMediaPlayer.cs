using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]

namespace NowPlayingDaemon
{
    public enum dbusInterface
    {
        IMediaPlayer2,
        IPlayer
    }

    public enum PlaybackStatus
    {
        // https://specifications.freedesktop.org/mpris-spec/latest/Player_Interface.html#Enum:Playback_Status
        Playing,
        Paused,
        Stopped
    }

    public enum LoopStatus
    {
        // https://specifications.freedesktop.org/mpris-spec/latest/Player_Interface.html#Enum:Loop_Status
        None,
        Track,
        Playlist
    }

    public class MprisMediaPlayer : IMediaPlayer2, IPlayer, IMprisMediaPlayer
    {
        public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");

        private readonly ILogger<MprisMediaPlayer> logger;
        readonly MprisMediaPlayerProperties mprisMediaPlayerProperties;

        readonly MprisPlayerProperties mprisPlayerProperties;

        public event Action<PropertyChanges> OnPropertiesChanged = delegate { };

        public event Action OnPlayPause;

        public MprisMediaPlayer(ILogger<MprisMediaPlayer> logger)
        {
            this.logger = logger;
            mprisMediaPlayerProperties = new MprisMediaPlayerProperties();
            mprisPlayerProperties = new MprisPlayerProperties();
        }

        Task<MprisMediaPlayerProperties> IMediaPlayer2.GetAllAsync()
        {
            logger.LogDebug("Getting all properties on interface IMediaPlayer2.");
            return Task.FromResult(mprisMediaPlayerProperties);
        }

        Task<MprisPlayerProperties> IPlayer.GetAllAsync()
        {
            logger.LogDebug("Getting all properties on interface IPlayer.");
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
            logger.LogInformation($"Getting property {property} on interface {iface}");

            object targetObject = iface switch
            {
                dbusInterface.IMediaPlayer2 => mprisMediaPlayerProperties,
                dbusInterface.IPlayer => mprisPlayerProperties,
                _ => throw new ArgumentException("Invalid interface type")
            };

            var propInfo = targetObject.GetType().GetField(property);

            if (propInfo == null)
            {
                logger.LogError($"Property {property} not found on interface {iface}.");
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
            logger.LogInformation($"Setting property {property} on interface {iface}");

            object targetObject = iface switch
            {
                dbusInterface.IMediaPlayer2 => mprisMediaPlayerProperties,
                dbusInterface.IPlayer => mprisPlayerProperties,
                _ => throw new ArgumentException("Invalid interface type")
            };

            var propInfo = targetObject.GetType().GetField(property);

            if (propInfo == null || propInfo.IsInitOnly)
            {
                logger.LogError($"Attempted to set non-existent or readonly property: {property}");
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
                    logger.LogError($"Failed to convert type for {property}: {ex.Message}");
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

            logger.LogInformation("Updating Metadata");
            foreach (var item in customMetadata)
            {
                logger.LogDebug($"Setting '{item.Key}' to '{item.Value}'.");

                mprisPlayerProperties.Metadata[item.Key] = item.Value;
            }
            OnPropertiesChanged?.Invoke(
                PropertyChanges.ForProperty("Metadata", mprisPlayerProperties.Metadata)
            );
            return Task.CompletedTask;
        }

        public Task AddMetadata(string key, object value)
        {
            logger.LogInformation($"Adding Metadata item: {key}");
            if (!mprisPlayerProperties.Metadata.ContainsKey(key))
            {
                mprisPlayerProperties.Metadata.Add(key, value);
                OnPropertiesChanged?.Invoke(
                    PropertyChanges.ForProperty("Metadata", mprisPlayerProperties.Metadata)
                );
            }
            else
            {
                logger.LogWarning($"Metadata key already exists: {key}");
            }
            return Task.CompletedTask;
        }

        public Task RemoveMetadata(string key)
        {
            logger.LogInformation($"Removing Metadata item: {key}");
            if (mprisPlayerProperties.Metadata.ContainsKey(key))
            {
                mprisPlayerProperties.Metadata.Remove(key);
                OnPropertiesChanged?.Invoke(
                    PropertyChanges.ForProperty("Metadata", mprisPlayerProperties.Metadata)
                );
            }
            else
            {
                logger.LogWarning($"Metadata key not found: {key}");
            }
            return Task.CompletedTask;
        }

        public Task RaiseAsync()
        {
            logger.LogDebug("RaiseAsync called.");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task QuitAsync()
        {
            logger.LogDebug("QuitAsync called.");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task PlayAsync()
        {
            logger.LogDebug("PlayAsync called.");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            logger.LogDebug("PauseAsync called.");
            logger.LogInformation("PauseAsync");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            logger.LogDebug("StopAsync called.");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task PlayPauseAsync()
        {
            logger.LogDebug("PlayPauseAsync called.");
            if (!mprisPlayerProperties.CanPause)
            {
                logger.LogError("PlayPause operation is not allowed.");
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
            logger.LogDebug("PreviousAsync called.");

            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task NextAsync()
        {
            logger.LogDebug("NextAsync called.");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task SeekAsync(long Offset)
        {
            logger.LogDebug("SeekAsync called.");
            throw new NotImplementedException();
        }

        public Task SetPositionAsync(ObjectPath TrackId, long Position)
        {
            logger.LogDebug("SetPositionAsync called.");
            throw new NotImplementedException();
        }

        public Task OpenUriAsync(string Uri)
        {
            logger.LogDebug("OpenUriAsync called.");
            throw new NotImplementedException();
        }
    }
}
