using Android.App;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;
using AISandbox.Game;

namespace AISandbox
{
    [Activity(
        Label = "AI Sandbox",
        MainLauncher = true,
        Icon = "@android:drawable/sym_def_app_icon",
        Exported = true,
        Theme = "@android:style/Theme.Material.Light.NoActionBar.Fullscreen")]
    public class MainActivity : AndroidGameActivity
    {
        private SandboxGame game;

        protected override void OnCreate(Bundle? bundle)
        {
            base.OnCreate(bundle);

            game = new SandboxGame();
            SetContentView((View)game.Services.GetService(typeof(View))!);
            game.Run();
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (game != null)
                game.Exit();
        }
    }
}
