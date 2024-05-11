using System;
using UnityEngine;

public enum AdType
{
    Banner,
    Interstetial,
    RewardedInterstitial,
    Rewarded,
    Native
}

[CreateAssetMenu(fileName = "AdIds", menuName = "AdsManager/Data/AdIDs")]
public class SOAdIds : ScriptableObject
{
    public string AppOpenId;
    public string BannerId;
    public string InterstitialId;
    public string RewardedId;
    public string RewardedInterstitialId;
}