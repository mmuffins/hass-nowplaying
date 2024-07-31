// // global using System;
// // using System.Collections.Generic;
// // using NetDaemon.Client.HomeAssistant.Model;
// using NetDaemon.HassModel.Entities;
// using hass_mpris.HassClasses;
// using System.Linq;
// using System.Reactive.Linq;
// using Microsoft.Extensions.Logging;
// using NetDaemon.HassModel;
// using NetDaemon.AppModel;
// using Tmds.DBus;
// using MPRISInterface;

// namespace NowPlayingDaemon;

// public interface INowPlaying
// {
//     // Define your methods here that will be called elsewhere in your application
// }



// public class NowPlaying : INowPlaying

// {
//     private readonly ILogger<NowPlaying> logger;
//     // private readonly IMprisMediaPlayerService mprisMediaPlayerService;

//     public NowPlaying(IHaContext haContext, ILogger<NowPlaying> logger, DBusConnectionManager connectionManager)
//     {
//         this.logger = logger;
//         // IMprisMediaPlayerService mprisMediaPlayerService
//         // this.mprisMediaPlayerService = mprisMediaPlayerService;
//         logger.LogInformation("This is a log message from MyService.");

//         // ha.CallService("notify", "persistent_notification", data: new {message = "Notify meXX", title = "Hello world!"});

//         // logger.LogInformation("test");
//         var haPlayer = new Entity<MediaPlayerAttributes>(haContext, "media_player.sonos_arc");


//         var haPlayerX = haContext.GetAllEntities()
//             .Where(e => e.EntityId.StartsWith("media_player.sonos_arc"))
//             .Select(e => new MediaPlayerEntity(e))
//             .First();

//         // var atts = (Dictionary<string,object>)entity.Attributes;
//         // var pic = atts.Where(a => a.Key == "entity_picture").First();
//         // Console.WriteLine(entity.State);
//         // Console.WriteLine(entity.Attributes);
//         Console.WriteLine(haPlayer.Attributes?.MediaTitle);
//         Console.WriteLine(haPlayer.Attributes?.MediaArtist);
//         Console.WriteLine(haPlayer.Attributes?.EntityPicture);
//         Console.WriteLine(haPlayer.State);
        
//         // haPlayerX.MediaPlayPause();
//     }

//     // public static async Task<NowPlaying> CreateAsync(IHaContext haContext, ILogger<MprisMediaPlayer> logger)
//     // {
//     //     var connection = new Connection(Address.Session);
//     //     await connection.ConnectAsync();
//     //     return new NowPlaying(haContext, logger);
//     // }
// }