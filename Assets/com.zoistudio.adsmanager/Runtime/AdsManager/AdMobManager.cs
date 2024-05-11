using GoogleMobileAds.Api;
using System;
using UnityEngine;

public class AdMobManager : MonoBehaviour
{
    private BannerView bannerView;
    private InterstitialAd _interstitialAd;
    private RewardedInterstitialAd _rewardedInterstitialAd;

    public Action<Reward> OnUserEarnedRewardEvent;
    public Action OnAdClosedEvent;
    private AdsLoader _adsManager;

    public void Init(AdsLoader adsManager, ShowAdTypes showAdTypes)
    {
        /*
            About the same app key:
            Version 8.3.0 of the GMA SDK introduces the same app key, an encrypted key that identifies a unique user within a single app.
            The same app key helps you deliver more relevant and personalized ads by using data collected from the app the user is using.
            The same app key cannot be used to link user activity across multiple apps.
            The same app key is enabled by default, but you can always choose to disable it in your SDK.
            Your app users are able to opt-out of ads personalization based on the same app key through in-ad controls.
            Ads personalization using the same app key respects existing privacy settings, including NPA, RDP, and TFCD/TFUA.
        */

        // The same app key is enabled by default, but we can disable it with the following API:
        //RequestConfiguration requestConfiguration =
        //   new RequestConfiguration.Builder()
        //   .SetSameAppKeyEnabled(true).build();
        //MobileAds.SetRequestConfiguration(requestConfiguration);

        _adsManager = adsManager;

        MobileAds.Initialize(initStatus => { Debug.Log("Admob initialized " + initStatus); });

        if (showAdTypes.showRewardedInterstitials)
            LoadRewardedInterstitialAd();

        if (showAdTypes.showInterstatial)
            LoadInterstitialAd();
    }

    // Returns an ad request with custom ad targeting.
    private AdRequest CreateAdRequest()
    {
        return new AdRequest();
    }

    public bool ShowAd(AdType type, AdPosition position = AdPosition.Top)
    {
        switch (type)
        {
            case AdType.Banner:
                return false;
            case AdType.RewardedInterstitial:
            case AdType.Rewarded:
                return ShowRewardedInterstitialAd();
            case AdType.Interstetial:
            default:
                return ShowInterstitial();
        }
    }

    public void HideAd(AdType type)
    {
        if (type == AdType.Banner)
            bannerView.Hide();
    }

    private bool ShowInterstitial()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            _interstitialAd.Show();
            return true;
        }
        LoadInterstitialAd();
        return false;
    }
    
    private bool ShowRewardedInterstitialAd()
    {
        if (_rewardedInterstitialAd != null && _rewardedInterstitialAd.CanShowAd())
        {
            _rewardedInterstitialAd.Show(HandleAdReward);
            return true;
        }
        
        LoadRewardedInterstitialAd();
        return false;
    }

    private void LoadInterstitialAd()
    {
        // Clean up the old ad before loading a new one.
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        Debug.Log("Loading the interstitial ad.");

        InterstitialAd.Load(_adsManager.GetAdId(AdType.Interstetial), CreateAdRequest(), OnInterstitialAdLoaded);
    }

    private void OnInterstitialAdLoaded(InterstitialAd ad, LoadAdError error)
    {
        // if error is not null, the load request failed.
        if (error != null || ad == null)
        {
            Debug.LogError("interstitial ad failed to load an ad " +
                           "with error : " + error);
            return;
        }

        Debug.Log("Interstitial ad loaded with response : "
                  + ad.GetResponseInfo());

        _interstitialAd = ad;
        RegisterInterstitialEventHandlers(_interstitialAd);
    }

    private void RegisterInterstitialEventHandlers(InterstitialAd interstitialAd)
    {
        // Raised when the ad is estimated to have earned money.
        interstitialAd.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        interstitialAd.OnAdImpressionRecorded += () => { Debug.Log("Interstitial ad recorded an impression."); };
        // Raised when a click is recorded for an ad.
        interstitialAd.OnAdClicked += () => { Debug.Log("Interstitial ad was clicked."); };
        // Raised when an ad opened full screen content.
        interstitialAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened.");
        };
        
        
        // Raised when the ad closed full screen content.
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial Ad full screen content closed.");

            // Reload the ad so that we can show another as soon as possible.
            LoadInterstitialAd();
        };
        // Raised when the ad failed to open full screen content.
        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content " +
                           "with error : " + error);

            // Reload the ad so that we can show another as soon as possible.
            LoadInterstitialAd();
        };
    }
    
    private void LoadRewardedInterstitialAd()
    {
        // Clean up the old ad before loading a new one.
        if (_rewardedInterstitialAd != null)
        {
            _rewardedInterstitialAd.Destroy();
            _rewardedInterstitialAd = null;
        }
        Debug.Log("Loading the rewarded interstitial ad.");
        RewardedInterstitialAd.Load(_adsManager.GetAdId(AdType.RewardedInterstitial), CreateAdRequest(), OnRewardedInterstitialAdLoaded);
    }

    private void OnRewardedInterstitialAdLoaded(RewardedInterstitialAd ad, LoadAdError error)
    {
        // if error is not null, the load request failed.
        if (error != null || ad == null)
        {
            Debug.LogError("rewarded interstitial ad failed to load an ad " +
                           "with error : " + error);
            return;
        }

        Debug.Log("Rewarded interstitial ad loaded with response : "
                  + ad.GetResponseInfo());

        _rewardedInterstitialAd = ad;
        RegisterRewardedEventHandlers(_rewardedInterstitialAd);
    }

    private void RegisterRewardedEventHandlers(RewardedInterstitialAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded interstitial ad full screen content opened.");
        };
        
        // RELOAD LOGIC
        
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded interstitial ad full screen content closed.");

            // Reload the ad so that we can show another as soon as possible.
            LoadRewardedInterstitialAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded interstitial ad failed to open " +
                           "full screen content with error : " + error);

            // Reload the ad so that we can show another as soon as possible.
            LoadRewardedInterstitialAd();
        };
    }

    private void HandleAdReward(Reward reward)
    {
        const string rewardMsg =
            "Rewarded interstitial ad rewarded the user. Type: {0}, amount: {1}.";
        Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
        OnUserEarnedRewardEvent?.Invoke(reward);
        OnAdClosedEvent?.Invoke();
    }

    public void HandleinterstitialAdClosed(object sender, EventArgs e)
    {
        // Requesting a new add on close
        Debug.Log("Requesting Interstitial Closed");
        LoadInterstitialAd();
        OnAdClosedEvent?.Invoke();
    }
}