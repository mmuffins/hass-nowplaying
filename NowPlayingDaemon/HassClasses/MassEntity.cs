﻿//------------------------------------------------------------------------------
// <auto-generated>
// Generated using NetDaemon CodeGenerator nd-codegen v24.28.1.0
//   At: 2024-07-29T19:20:19.1911768+00:00
//
// *** Make sure the version of the codegen tool and your nugets Joysoftware.NetDaemon.* have the same version.***
// You can use following command to keep it up to date with the latest version:
//   dotnet tool update NetDaemon.HassModel.CodeGen
//
// To update this file with latest entities run this command in your project directory:
//   dotnet tool run nd-codegen
//
// In the template projects we provided a convenience powershell script that will update
// the codegen and nugets to latest versions update_all_dependencies.ps1.
//
// For more information: https://netdaemon.xyz/docs/user/hass_model/hass_model_codegen
// For more information about NetDaemon: https://netdaemon.xyz/
// </auto-generated>
//------------------------------------------------------------------------------

#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace hass_mpris.HassClasses;

public partial class MassServices
{
    private readonly IHaContext _haContext;
    public MassServices(IHaContext haContext)
    {
        _haContext = haContext;
    }

    ///<summary>Play announcement on a Music Assistant player with more fine grained control options.</summary>
    ///<param name="target">The target for this service call</param>
    public void PlayAnnouncement(ServiceTarget target, MassPlayAnnouncementParameters data)
    {
        _haContext.CallService("mass", "play_announcement", target, data);
    }

    ///<summary>Play announcement on a Music Assistant player with more fine grained control options.</summary>
    ///<param name="url">URL to the notification sound. eg: http://someremotesite.com/doorbell.mp3</param>
    ///<param name="usePreAnnounce">Use pre-announcement sound for the announcement. Omit to use the player default. eg: true</param>
    ///<param name="announceVolume">Use a forced volume level for the announcement. Omit to use player default. eg: 75</param>
    public void PlayAnnouncement(ServiceTarget target, string url, bool? usePreAnnounce = null, long? announceVolume = null)
    {
        _haContext.CallService("mass", "play_announcement", target, new MassPlayAnnouncementParameters { Url = url, UsePreAnnounce = usePreAnnounce, AnnounceVolume = announceVolume });
    }

    ///<summary>Play media on a Music Assistant player with more fine grained control options.</summary>
    ///<param name="target">The target for this service call</param>
    public void PlayMedia(ServiceTarget target, MassPlayMediaParameters data)
    {
        _haContext.CallService("mass", "play_media", target, data);
    }

    ///<summary>Play media on a Music Assistant player with more fine grained control options.</summary>
    ///<param name="mediaId">URI or name of the item you want to play. Specify a list if you want to play/enqueue multiple items. eg: spotify://playlist/aabbccddeeff</param>
    ///<param name="mediaType">The type of the content to play. Such as artist, album, track or playlist. Will be auto determined if omitted. eg: playlist</param>
    ///<param name="artist">When specifying a track or album by name in the Media ID field, you can optionally restrict results by this artist name. eg: Queen</param>
    ///<param name="album">When specifying a track by name in the Media ID field, you can optionally restrict results by this album name. eg: News of the world</param>
    ///<param name="enqueue">If the content should be played now or be added to the queue. Options are: play, replace, next. replace_next, add</param>
    ///<param name="radioMode">Enable radio mode to auto generate a playlist based on the selection.</param>
    public void PlayMedia(ServiceTarget target, object mediaId, object? mediaType = null, string? artist = null, string? album = null, object? enqueue = null, bool? radioMode = null)
    {
        _haContext.CallService("mass", "play_media", target, new MassPlayMediaParameters { MediaId = mediaId, MediaType = mediaType, Artist = artist, Album = album, Enqueue = enqueue, RadioMode = radioMode });
    }

    ///<summary>Perform a global search on the Music Assistant library and all providers.</summary>
    public void Search(MassSearchParameters data)
    {
        _haContext.CallService("mass", "search", null, data);
    }

    ///<summary>Perform a global search on the Music Assistant library and all providers.</summary>
    ///<param name="name">The name/title to search for. eg: We Are The Champions</param>
    ///<param name="mediaType">The type of the content to search. Such as artist, album, track, radio or playlist. All types if omitted. eg: playlist</param>
    ///<param name="artist">When specifying a track or album name in the name field, you can optionally restrict results by this artist name. eg: Queen</param>
    ///<param name="album">When specifying a track name in the name field, you can optionally restrict results by this album name. eg: News of the world</param>
    ///<param name="limit">Maximum number of items to return (per media type). eg: 25</param>
    public void Search(string name, object? mediaType = null, string? artist = null, string? album = null, long? limit = null)
    {
        _haContext.CallService("mass", "search", null, new MassSearchParameters { Name = name, MediaType = mediaType, Artist = artist, Album = album, Limit = limit });
    }

    ///<summary>Perform a global search on the Music Assistant library and all providers.</summary>
    public Task<JsonElement?> SearchAsync(MassSearchParameters data)
    {
        return _haContext.CallServiceWithResponseAsync("mass", "search", null, data);
    }

    ///<summary>Perform a global search on the Music Assistant library and all providers.</summary>
    ///<param name="name">The name/title to search for. eg: We Are The Champions</param>
    ///<param name="mediaType">The type of the content to search. Such as artist, album, track, radio or playlist. All types if omitted. eg: playlist</param>
    ///<param name="artist">When specifying a track or album name in the name field, you can optionally restrict results by this artist name. eg: Queen</param>
    ///<param name="album">When specifying a track name in the name field, you can optionally restrict results by this album name. eg: News of the world</param>
    ///<param name="limit">Maximum number of items to return (per media type). eg: 25</param>
    public Task<JsonElement?> SearchAsync(string name, object? mediaType = null, string? artist = null, string? album = null, long? limit = null)
    {
        return _haContext.CallServiceWithResponseAsync("mass", "search", null, new MassSearchParameters { Name = name, MediaType = mediaType, Artist = artist, Album = album, Limit = limit });
    }
}

public partial record MassPlayAnnouncementParameters
{
    ///<summary>URL to the notification sound. eg: http://someremotesite.com/doorbell.mp3</summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    ///<summary>Use pre-announcement sound for the announcement. Omit to use the player default. eg: true</summary>
    [JsonPropertyName("use_pre_announce")]
    public bool? UsePreAnnounce { get; init; }

    ///<summary>Use a forced volume level for the announcement. Omit to use player default. eg: 75</summary>
    [JsonPropertyName("announce_volume")]
    public long? AnnounceVolume { get; init; }
}

public partial record MassPlayMediaParameters
{
    ///<summary>URI or name of the item you want to play. Specify a list if you want to play/enqueue multiple items. eg: spotify://playlist/aabbccddeeff</summary>
    [JsonPropertyName("media_id")]
    public object? MediaId { get; init; }

    ///<summary>The type of the content to play. Such as artist, album, track or playlist. Will be auto determined if omitted. eg: playlist</summary>
    [JsonPropertyName("media_type")]
    public object? MediaType { get; init; }

    ///<summary>When specifying a track or album by name in the Media ID field, you can optionally restrict results by this artist name. eg: Queen</summary>
    [JsonPropertyName("artist")]
    public string? Artist { get; init; }

    ///<summary>When specifying a track by name in the Media ID field, you can optionally restrict results by this album name. eg: News of the world</summary>
    [JsonPropertyName("album")]
    public string? Album { get; init; }

    ///<summary>If the content should be played now or be added to the queue. Options are: play, replace, next. replace_next, add</summary>
    [JsonPropertyName("enqueue")]
    public object? Enqueue { get; init; }

    ///<summary>Enable radio mode to auto generate a playlist based on the selection.</summary>
    [JsonPropertyName("radio_mode")]
    public bool? RadioMode { get; init; }
}

public partial record MassSearchParameters
{
    ///<summary>The name/title to search for. eg: We Are The Champions</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    ///<summary>The type of the content to search. Such as artist, album, track, radio or playlist. All types if omitted. eg: playlist</summary>
    [JsonPropertyName("media_type")]
    public object? MediaType { get; init; }

    ///<summary>When specifying a track or album name in the name field, you can optionally restrict results by this artist name. eg: Queen</summary>
    [JsonPropertyName("artist")]
    public string? Artist { get; init; }

    ///<summary>When specifying a track name in the name field, you can optionally restrict results by this album name. eg: News of the world</summary>
    [JsonPropertyName("album")]
    public string? Album { get; init; }

    ///<summary>Maximum number of items to return (per media type). eg: 25</summary>
    [JsonPropertyName("limit")]
    public long? Limit { get; init; }
}
