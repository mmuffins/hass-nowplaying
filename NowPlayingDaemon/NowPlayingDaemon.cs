// global using System;
// global using System.Reactive.Linq;
// global using Microsoft.Extensions.Logging;
// global using NetDaemon.AppModel;
// global using NetDaemon.HassModel;
// using System.Collections.Generic;
// using System.Linq;
// using NetDaemon.Client.HomeAssistant.Model;
// using NetDaemon.HassModel.Entities;
using HassClasses;

namespace NowPlayingDaemon;

/// <summary>
///     Hello world showcase using the new HassModel API
/// </summary>
[NetDaemonApp]
public class NowPlaying
{
    public NowPlaying(IHaContext ha)
    {
        // ha.CallService("notify", "persistent_notification", data: new {message = "Notify meXX", title = "Hello world!"});

        var _myEntities = new Entities(ha);
        var haPlayer = _myEntities.MediaPlayer.SonosArc;

        
        // var haPlayer2 = new Entity(ha, "media_player.sonos_arc");
        // var haPlayer3 = ha.Entity("media_player.sonos_arc");

        // var atts = (Dictionary<string,object>)entity.Attributes;
        // var pic = atts.Where(a => a.Key == "entity_picture").First();
        // Console.WriteLine(entity.State);
        // Console.WriteLine(entity.Attributes);
        Console.WriteLine(haPlayer.Attributes?.MediaTitle);
        Console.WriteLine(haPlayer.Attributes?.MediaArtist);
        Console.WriteLine(haPlayer.Attributes?.EntityPicture);
        Console.WriteLine(haPlayer.State);
    }
}