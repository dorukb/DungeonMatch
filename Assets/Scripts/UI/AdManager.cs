using System.Collections.Generic;
using DorkyProductions;
using GoogleMobileAds.Api;
using UnityEngine;

namespace UI
{
    public class AdManager : MonoBehaviour
    {
        private BannerView _bannerView;
        public static AdManager Instance;

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); return; }
        }

        void Start()
        {
            // 1. Tell AdMob to automatically move all events to the Unity Main Thread
            // This replaces the "RaiseOnMainThread" logic entirely.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            RequestConfiguration requestConfiguration = new RequestConfiguration
            {
                TestDeviceIds = new List<string> { AdRequest.TestDeviceSimulator }
            };
            MobileAds.SetRequestConfiguration(requestConfiguration);

            // 2. Initialize
            MobileAds.Initialize((InitializationStatus status) =>
            {
                // This callback is now safe to call Unity functions from
                // because we set RaiseAdEventsOnUnityMainThread to true above.
                RequestBanner();
            });
        }

        public void RequestBanner()
        {
            string adUnitId = RemoteConfigManager.Instance.GetBannerAdUnitId();

            if (_bannerView != null) _bannerView.Destroy();

            _bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);
            
            // This is now safe and will happen instantly when ready
            AdRequest request = new AdRequest();
            _bannerView.LoadAd(request);
        }
    }
}