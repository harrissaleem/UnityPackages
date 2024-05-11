using GoogleMobileAds.Api;
using System;
using UnityEngine;

[Serializable]
public class ShowAdTypes
{
	[Header("UNDER DEVELOPMENT - NOT BEING USED ATM")]
	public bool showbanner;
	public bool showRewarded;
	
	[Header("IMPLEMENTED")]
	public bool showInterstatial;
	public bool showRewardedInterstitials;
}

public class AdsLoader : MonoBehaviour
{
	// Show Admob only - For Main Menu/ Vehicle Selection Play Button and Pause/Game Over Home Button

	public ShowAdTypes adTypes;
	public bool inTestMode;
	
	[Header("Admob Ad Ids")]
	[SerializeField]
	private SOAdIds admobAndroidAdIds;
	[SerializeField]
	private SOAdIds admobIOSAdIds;
	
	[Header("Test Ad Ids")]
	[SerializeField]
	private SOAdIds testAndroidAdIds;
	[SerializeField]
	private SOAdIds testIOSAdIds;

	public static AdsLoader Instance;    
	public static Action<Reward> OnUserEarnedRewardEvent;
	public static Action OnAdClosedEvent;

	private AdMobManager adMobManager;
	
	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(this.gameObject);
		}
		DontDestroyOnLoad(this.gameObject);
	}

	private void Start()
	{
		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		if (CheckAdsDisabled())
			return;

		// Initialize AdMob Manager
		adMobManager = gameObject.AddComponent<AdMobManager>();
		adMobManager.Init(this, adTypes);
		adMobManager.OnUserEarnedRewardEvent += EarnedRewarededEvnt;
		adMobManager.OnAdClosedEvent += AdClosed;
	}

	#region Ads

	public bool CheckAdsDisabled()
	{
		string settingsName = "";
#if UNITY_ANDROID
		settingsName = "DisableAds_Android";
#elif UNITY_IOS
		settingsName = "DisableAds_iOS";
#else
		settingsName = "DisableAds";
#endif
		return RemoteSettings.GetBool(settingsName);
	}

	private void EarnedRewarededEvnt(Reward reward)
	{
		Debug.Log("Rewarded");
		OnUserEarnedRewardEvent?.Invoke(reward);
	}

	private void AdClosed()
	{
		OnAdClosedEvent?.Invoke();
	}
	public string GetAdId(AdType adType)
	{
		SOAdIds soAdIds = null;
#if UNITY_ANDROID
		adIds = inTestMode ? Test_Android_AdIds : Admob_Android_AdIds;
#elif UNITY_IOS
		soAdIds = inTestMode ? testIOSAdIds : admobIOSAdIds;
#else
		return string.Empty;
#endif

		if (soAdIds == null)
			return string.Empty;

		switch (adType)
		{
			case AdType.Banner:
				return soAdIds.BannerId;
			case AdType.Interstetial:
				return soAdIds.InterstitialId;
			case AdType.RewardedInterstitial:
			case AdType.Rewarded:
				return soAdIds.RewardedId;
			default:
				return string.Empty;
		}
	}

	public bool ShowAd(AdType type)
	{
		if (CheckAdsDisabled())
			return false;

		//if (Application.platform != RuntimePlatform.Android || Application.platform != RuntimePlatform.IPhonePlayer)
		//	return;
		if (!adMobManager.ShowAd(type))
		{
			Debug.LogError("Show Ad Failed");
			return false;
		}

		return true;
	}

	public void HideAd(AdType type)
	{
		adMobManager.HideAd(type);
	}
#endregion
}
