using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Android.Widget;
using Microsoft.Maui.Controls;
using System.Linq;
using System.Text;

[Activity(Label = "RegistrationMobile", Theme = "@style/Maui.SplashTheme", MainLauncher = true)]
[IntentFilter(new[] { NfcAdapter.ActionTechDiscovered, NfcAdapter.ActionTagDiscovered, NfcAdapter.ActionNdefDiscovered })]
public class MainActivity : MauiAppCompatActivity
{
    private NfcAdapter _nfcAdapter;
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
        if (_nfcAdapter == null)
        {
            Toast.MakeText(this, "NFC not supported on this device", ToastLength.Long).Show();
            return;
        }
    }
    protected override void OnResume()
    {
        base.OnResume();
        var nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
        if (nfcAdapter != null)
        {
            var pendingIntent = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(MainActivity)).AddFlags(ActivityFlags.SingleTop), PendingIntentFlags.Mutable);
            var intentFilters = new IntentFilter[] { new IntentFilter(NfcAdapter.ActionTagDiscovered) };
            var techLists = new string[][] { new string[] { Java.Lang.Class.FromType(typeof(Android.Nfc.Tech.NfcA)).Name } };

            nfcAdapter.EnableForegroundDispatch(this, pendingIntent, intentFilters, techLists);
        }
    }

    protected override void OnNewIntent(Intent intent)
    {
        base.OnNewIntent(intent);
        if (intent.Action == NfcAdapter.ActionTagDiscovered ||
            intent.Action == NfcAdapter.ActionTechDiscovered ||
            intent.Action == NfcAdapter.ActionNdefDiscovered)
        {
            var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            if (tag != null)
            {
                byte[] uidBytes = tag.GetId();
                string uid = BitConverter.ToString(uidBytes).Replace("-", "").ToUpper();

                // 🔹 Send UID to MainPage.xaml.cs
                MessagingCenter.Send<object, string>(this, "NFC_UID_SCANNED", uid);

                Toast.MakeText(this, $"Scanned UID: {uid}", ToastLength.Short).Show();
            }
        }
    }
}
