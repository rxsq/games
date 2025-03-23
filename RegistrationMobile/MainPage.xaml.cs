using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Dispatching;

#if ANDROID
using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Android.Widget;
using Android.Util;
using Android.Runtime;
using Java.Lang;
using AndroidX.Core.App;
#endif

namespace RegistrationMobile
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

#if ANDROID
        MessagingCenter.Subscribe<object, string>(this, "NFC_UID_SCANNED", (sender, uid) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NfcUidLabel.Text = $"Scanned UID: {uid}";
            });
        });
#endif
        }
        public void UpdateUid(string uid)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NfcUidLabel.Text = $"UID: {uid}";
            });
        }
    }

}