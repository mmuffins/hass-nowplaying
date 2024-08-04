using Tmds.DBus;

namespace NowPlayingDaemon;

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

    public MprisMediaPlayerProperties()
    {
        CanQuit = false;
        CanRaise = false;
        HasTrackList = false;
        Fullscreen = false;
        CanSetFullscreen = false;
        Identity = "";
        DesktopEntry = "";
        SupportedUriSchemes = Array.Empty<string>();
        SupportedMimeTypes = Array.Empty<string>();
    }
}
