using Android.App;
using Android.Content;

namespace AuthenticatorPro.WearOS.Util
{
    // TEMP: workaround for ANR on Broadcast of Intent { act=android.intent.action.SCREEN_OFF }
    // https://issuetracker.google.com/issues/220190983
    [BroadcastReceiver(Enabled = true, Exported = false)]
    [IntentFilter(new[] { Intent.ActionScreenOff, Intent.ActionScreenOn })]
    public class ScreenBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Logger.Info("Screen broadcast received: " + intent.Action);
            GoAsync().Finish();
        }
    }
}