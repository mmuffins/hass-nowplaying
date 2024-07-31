using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]
namespace MPRISInterface
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

    public interface IMprisMediaPlayer
    {
        Task UpdateMetadata(string trackId, long length, string[] artist, string title, string album);
        Task RegisterPlayer(Connection connection);
    }



    [Dictionary]
    public class MprisMediaPlayerProperties
    {
        public bool CanQuit;
        public bool CanRaise;
        public bool HasTrackList;
        public bool Fullscreen;
        public bool CanSetFullscreen;
        public string Identity;
        public string DesktopEntry;
        public string[] SupportedUriSchemes;
        public string[] SupportedMimeTypes;

        public MprisMediaPlayerProperties(string identity, string desktopEntry)
        {
            CanQuit = false;
            CanRaise = false;
            HasTrackList = false;
            Fullscreen = false;
            CanSetFullscreen = false;
            SupportedUriSchemes = Array.Empty<string>();
            SupportedMimeTypes = Array.Empty<string>();

            Identity = identity;
            DesktopEntry = desktopEntry;
        }
    }


    [DBusInterface("org.mpris.MediaPlayer2",
        GetPropertyMethod = "GetAsync",
        PropertyType = typeof(MprisMediaPlayerProperties),
        SetPropertyMethod = "SetAsync"
    )]
    interface IMediaPlayer2 : IDBusObject
    {
        public Task<MprisMediaPlayerProperties> GetAllAsync();
        public Task<object> GetAsync(string property);
        public Task SetAsync(string property, object value);

        Task RaiseAsync();
        Task QuitAsync();
    }

    [Dictionary]
    public class MprisPlayerProperties
    {
        public bool CanPlay;
        public bool CanPause;
        public bool CanGoPrevious;
        public bool CanGoNext;
        public bool CanSeek;
        public bool CanControl;
        public PlaybackStatus PlaybackStatus;
        public LoopStatus LoopStatus;
        public double Rate;
        public bool Shuffle;
        public double Volume;
        public long Position;
        public double MinimumRate;
        public double MaximumRate;
        public IDictionary<string, object> Metadata;

        public MprisPlayerProperties(bool canControl)
        {
            CanPlay = true;
            CanPause = false;
            CanGoPrevious = false;
            CanGoNext = false;
            CanSeek = false;
            PlaybackStatus = PlaybackStatus.Stopped;
            LoopStatus = LoopStatus.None;
            Rate = 0;
            Shuffle = false;
            Volume = 0;
            Position = 0;
            MinimumRate = 0;
            MaximumRate = 0;
            Metadata = new Dictionary<string, object>
            {
                {"mpris:trackid", "0"},
                {"mpris:length", 0L}, // Track length in microseconds
                {"xesam:artist", new string[] {""}},
                {"xesam:title", ""},
                {"xesam:album", ""}
            };

            CanControl = canControl;
        }
    }

    [DBusInterface("org.mpris.MediaPlayer2.Player", 
        GetPropertyMethod = "GetAsync",
        PropertyType = typeof(MprisPlayerProperties),
        SetPropertyMethod = "SetAsync"
    )]
    public interface IPlayer : IDBusObject
    {
        public Task<MprisPlayerProperties> GetAllAsync();
        public Task<object> GetAsync(string property);
        public Task SetAsync(string property, object value);
        public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);

        Task PlayAsync();
        Task PauseAsync();
        Task PlayPauseAsync();
        Task StopAsync();
        Task NextAsync();
        Task PreviousAsync();
        Task SeekAsync(long Offset);
        Task SetPositionAsync(ObjectPath TrackId, long Position);
        Task OpenUriAsync(string Uri);
    }

    public class MprisMediaPlayer : IMediaPlayer2, IPlayer, IMprisMediaPlayer
    {
        public ObjectPath ObjectPath => new ObjectPath("/org/mpris/MediaPlayer2");

        private readonly ILogger<MprisMediaPlayer> logger;
        readonly MprisMediaPlayerProperties mprisMediaPlayerProperties;

        readonly MprisPlayerProperties mprisPlayerProperties;

        public event Action<PropertyChanges> OnPropertiesChanged = delegate {};


        public MprisMediaPlayer(ILogger<MprisMediaPlayer> logger, string identity = "", string desktopEntry = "", bool canControl = false)
        {
            this.logger = logger;
            mprisMediaPlayerProperties = new MprisMediaPlayerProperties(identity, desktopEntry);
            mprisPlayerProperties = new MprisPlayerProperties(canControl);
        }

        Task<MprisMediaPlayerProperties> IMediaPlayer2.GetAllAsync(){
            logger.LogInformation("Getting all properties on interface IMediaPlayer2.");
            return Task.FromResult(mprisMediaPlayerProperties);
        }

        Task<MprisPlayerProperties> IPlayer.GetAllAsync(){
            logger.LogInformation("Getting all properties on interface IPlayer.");
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

        public async Task RegisterPlayer(Connection connection){

            mprisMediaPlayerProperties.Identity = "delmeplayer";
            mprisMediaPlayerProperties.DesktopEntry = "DELETE ME PLAYER";
            mprisPlayerProperties.CanControl = true;
            
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

        public Task UpdateMetadata(string trackId, long length, string[] artist, string title, string album)
        {
            var metadata = new Dictionary<string, object>
            {
                {"mpris:trackid", trackId},
                {"mpris:length", length},
                {"xesam:artist", artist},
                {"xesam:title", title},
                {"xesam:album", album}
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
            OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("Metadata", mprisPlayerProperties.Metadata));
            return Task.CompletedTask;
        }

        public Task AddMetadata(string key, object value)
        {
            logger.LogInformation($"Adding Metadata item: {key}");
            if (!mprisPlayerProperties.Metadata.ContainsKey(key))
            {
                mprisPlayerProperties.Metadata.Add(key, value);
                OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("Metadata", mprisPlayerProperties.Metadata));
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
                OnPropertiesChanged?.Invoke(PropertyChanges.ForProperty("Metadata", mprisPlayerProperties.Metadata));
            }
            else
            {
                logger.LogWarning($"Metadata key not found: {key}");
            }
            return Task.CompletedTask;
        }

        public Task RaiseAsync()
        {
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task QuitAsync()
        {
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task PlayAsync()
        {
            logger.LogInformation("PlayAsync");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            logger.LogInformation("PauseAsync");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task PlayPauseAsync()
        {
            logger.LogInformation("PlayPause");
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task PreviousAsync()
        {
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task NextAsync()
        {
            throw new NotImplementedException();
            return Task.CompletedTask;
        }

        public Task SeekAsync(long Offset)
        {
            throw new NotImplementedException();
        }

        public Task SetPositionAsync(ObjectPath TrackId, long Position)
        {
            throw new NotImplementedException();
        }

        public Task OpenUriAsync(string Uri)
        {
            throw new NotImplementedException();
        }
    }
}