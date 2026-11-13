using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace Daily_Goals_Editor;

// Info.plist names this class as the scene delegate. Newer macOS requires the scene lifecycle, and
// MAUI's MauiUISceneDelegate has no Objective-C name of its own to point Info.plist at.
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        // MAUI's own name for its scene configuration is internal; this must match Info.plist.
        const string MAUI_SCENE_CONFIGURATION = "__MAUI_DEFAULT_SCENE_CONFIGURATION__";

        // macOS restores sessions saved by earlier launches, and a session saved before Info.plist named
        // this configuration has none. MAUI skips those, which leaves the app running with no window.
        if (session.Configuration.Name == MAUI_SCENE_CONFIGURATION)
        {
            base.WillConnect(scene, session, connectionOptions);
        }
        else
        {
            var application = IPlatformApplication.Current?.Application
                              ?? throw new InvalidOperationException("A scene connected before the MAUI application was created.");
            this.CreatePlatformWindow(application, scene, session, connectionOptions);
        }
    }
}
