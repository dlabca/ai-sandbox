using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Xna.Framework;

namespace AISandbox
{
    [Activity(
        Label = "AI Sandbox",
        MainLauncher = true,
        Exported = true,
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class MainActivity : AndroidGameActivity
    {
        private SandboxGame game;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            game = new SandboxGame();
            Android.Views.View view = (Android.Views.View)game.Services.GetService(typeof(Android.Views.View));
            SetContentView(view);
            game.Run();
        }
    }
}
