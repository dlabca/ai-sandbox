using Android.App;
using Android.Content.PM;
using Android.OS;
using AISandbox.Game;

namespace AISandbox
{
    [Activity(
        Label = "AI Sandbox",
        MainLauncher = true,
        Icon = "@android:drawable/sym_def_app_icon",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.Landscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class MainActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var game = new SandboxGame();
            SetContentView(game.Services.GetService(typeof(Android.Views.View)) as Android.Views.View);
            game.Run();
        }
    }
}