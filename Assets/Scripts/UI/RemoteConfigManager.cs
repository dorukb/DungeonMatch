using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DorkyProductions
{
    
public class RemoteConfigManager : MonoBehaviour
{
    public static RemoteConfigManager Instance;

    // Keys matching your Firebase Console
    private const string ATTACK_PREFIX = "attack_"; // attack_3, attack_4, attack_5
    private const string HEAL_PREFIX = "heal_";
    private const string SHIELD_PREFIX = "shield_";
    private const string CROSS_PREFIX = "cross_";
    
    private const string BET_AMOUNT_PREFIX = "bet_amount_";

    private const string STARTING_HEALTH_KEY = "starting_health";
    private const string STARTING_SHIELD_KEY = "starting_shield";
    private const string STARTING_COINS_KEY = "starting_coins";
    private const string AVOID_MATCH_CHANCE_KEY = "avoid_match_chance";
    private const string MATCH_STRATEGY_KEY = "match_strategy";
    private const string REWARD_SHIELD_KEY = "reward_shield";
    private const string STOLEN_HEALTH_KEY = "stolen_health";
    private const string DEFAULT_BOT_KEY = "default_bot";
    private const string BANNER_AD_UNIT_ID = "banner_ad_unit_id";
    
    // Fast local lookups
    private Dictionary<int, int> attackValues = new Dictionary<int, int>();
    private Dictionary<int, int> healValues = new Dictionary<int, int>();
    private Dictionary<int, int> shieldValues = new Dictionary<int, int>();
    private Dictionary<int, float> crossValues = new Dictionary<int, float>();
    
    private Dictionary<int, int>  betAmounts = new Dictionary<int, int>();
    private static readonly int PLAYER_STARTING_HEALTH = 20;
    private static readonly int PLAYER_STARTING_SHIELD = 10;
    private static readonly int PLAYER_STARTING_COINS = 50;
    
    private int startingHealth = PLAYER_STARTING_HEALTH;
    private int startingShield = PLAYER_STARTING_SHIELD;
    private int startingCoins = PLAYER_STARTING_COINS;
    private float avoidMatchChance = 0.85f;
    private int matchStrategyIdx = 0;
    private int rewardShield = 3;
    private int stolenHealth = 3;
    private int defaultBot = 1;
    private string bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        DontDestroyOnLoad(gameObject);
        InitializeFirebase();
    }
    // Call this after a successful Fetch and Activate
    private void UpdateLocalCaches()
    {
        Debug.Log("Entering UpdateLocalCaches."); // Add a log right at the beginning
        try
        {
            var config = FirebaseRemoteConfig.DefaultInstance;

            // Ensure these keys are either in your defaults or handled gracefully if they might be missing.
            startingHealth = (int)config.GetValue(STARTING_HEALTH_KEY).LongValue;
            startingShield = (int)config.GetValue(STARTING_SHIELD_KEY).LongValue;
            startingCoins = (int)config.GetValue(STARTING_COINS_KEY).LongValue;
            matchStrategyIdx = (int)config.GetValue(MATCH_STRATEGY_KEY).LongValue;
            avoidMatchChance = (float)config.GetValue(AVOID_MATCH_CHANCE_KEY).DoubleValue;
        
            // POTENTIAL PROBLEM AREA: These keys are not in your provided 'defaults' dictionary.
            // If they are not set in the Firebase Console either, GetValue() will return 0,
            // but if there's an unexpected type or another issue, it could cause an error.
            rewardShield = (int)config.GetValue(REWARD_SHIELD_KEY).LongValue;
            stolenHealth = (int)config.GetValue(STOLEN_HEALTH_KEY).LongValue;

            defaultBot = (int)config.GetValue(DEFAULT_BOT_KEY).LongValue;
            
            bannerAdUnitId = config.GetValue(BANNER_AD_UNIT_ID).StringValue;
            
            Debug.Log("Config updated. Match strategy: " + matchStrategyIdx);
            for (int i = 3; i <= 5; i++)
            {
                attackValues[i] = (int)config.GetValue($"{ATTACK_PREFIX}{i}").LongValue;
                healValues[i] = (int)config.GetValue($"{HEAL_PREFIX}{i}").LongValue;
                shieldValues[i] = (int)config.GetValue($"{SHIELD_PREFIX}{i}").LongValue;
                crossValues[i] = (float)config.GetValue($"{CROSS_PREFIX}{i}").DoubleValue;
            }
            
            for (int i = 0; i < 3; i++)
            {
                betAmounts[i] = (int)config.GetValue($"{BET_AMOUNT_PREFIX}{i}").LongValue;
            }
            Debug.Log($"Config updated. Avoid Match Chance: {avoidMatchChance}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in UpdateLocalCaches: {e.Message}\nStackTrace:\n{e.StackTrace}");
        }
        Debug.Log("Exiting UpdateLocalCaches."); // Add a log at the end
    }

    public float GetAvoidMatchChance() => avoidMatchChance;
    public int GetAttackVal(int count) => attackValues.ContainsKey(count) ? attackValues[count] : 1;
    public int GetHealVal(int count) => healValues.ContainsKey(count) ? healValues[count] : 1;
    public int GetShieldVal(int count) => shieldValues.ContainsKey(count) ? shieldValues[count] : 1;
    public float GetCrossVal(int count) => crossValues.ContainsKey(count) ? crossValues[count] : 2.0f;
    
    public int GetBetAmountVal(int lobbyType) => betAmounts.ContainsKey(lobbyType) ? betAmounts[lobbyType] : 100;
    
    public int GetStartingHealth() => startingHealth;
    public int GetStartingShield() => startingShield;
    public int GetStartingCoins() => startingCoins;
    
    public int GetMatchStrategyIdx()
    {
        return matchStrategyIdx;
    }

    public int GetRewardShield() => rewardShield;
    public int GetStolenHealth() => stolenHealth;
    public int GetDefaultBot() => defaultBot;
    public string GetBannerAdUnitId() => bannerAdUnitId;
    
    private void InitializeFirebase()
    {
        Debug.Log("Init Firebase call.");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // --- ADD THESE TWO LINES ---
                // 1. Explicitly enable collection
                Firebase.Crashlytics.Crashlytics.IsCrashlyticsCollectionEnabled = true;
            
                // 2. Set a custom log so you know Firebase initialized correctly
                Firebase.Crashlytics.Crashlytics.Log("App Started and Firebase Ready");
                // ---------------------------
                // Firebase is ready
                InitializeRemoteConfig();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    private void InitializeRemoteConfig()
    {
        // Debug.Log("Init remote config call.");
        // This dictionary acts as a fallback if fetch fails
        Dictionary<string, object> defaults = new Dictionary<string, object>
        {
            { "attack_3", 3 }, { "attack_4", 4 }, { "attack_5", 6 },
            { "heal_3", 3 },   { "heal_4", 4 },   { "heal_5", 6 },
            { "shield_3", 3 }, { "shield_4", 4 }, { "shield_5", 6 },
            { "cross_3", 2.0f }, { "cross_4", 2.25f }, { "cross_5", 2.5f },
            { "bet_amount_0", 50 }, { "bet_amount_1", 100 }, { "bet_amount_2", 200},
            { STARTING_SHIELD_KEY, 10 }, {STARTING_HEALTH_KEY, 20 },
            { STARTING_COINS_KEY, 50 },
            { AVOID_MATCH_CHANCE_KEY, 0.85f }, {MATCH_STRATEGY_KEY, 0},
            { REWARD_SHIELD_KEY, 5 }, {STOLEN_HEALTH_KEY, 3 },
            { DEFAULT_BOT_KEY, 1 },
            { BANNER_AD_UNIT_ID, "ca-app-pub-3940256099942544/6300978111"}
        };

        
        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"SetDefaultsAsync failed: {task.Exception}");
                }
                else if (task.IsCanceled)
                {
                    Debug.LogWarning("SetDefaultsAsync was canceled.");
                }
                else if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("SetDefaultsAsync completed successfully. Proceeding with updates.");
                    UpdateLocalCaches();
                    FetchData();
                }
                else
                {
                    // This state should ideally not be reached if the task is truly done,
                    // but good for comprehensive debugging.
                    Debug.LogWarning($"SetDefaultsAsync task status: {task.Status}");
                }
            });

        
        // FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults)
        //     .ContinueWithOnMainThread(task =>
        //     {
        //         UpdateLocalCaches();
        //         FetchData();
        //     });
    }
    private void FetchData()
    {
        // 1. Define the timeout (Zero for instant updates during dev)
        System.TimeSpan fetchTimeout = System.TimeSpan.Zero; 

        Debug.Log("Fetch Request...");
        FirebaseRemoteConfig.DefaultInstance.FetchAsync(fetchTimeout)
            .ContinueWithOnMainThread(fetchTask =>
            {
                if (fetchTask.IsCompletedSuccessfully)
                {
                    Debug.Log("Fetch successful! Now activating...");
                
                    // Once fetch is done, you must Activate the values
                    return FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
                }
                else
                {
                    Debug.LogError("Fetch failed.");
                    return Task.FromResult(false);
                }
            }).ContinueWithOnMainThread(activateTask => 
            {
                // This runs after ActivateAsync finishes
                UpdateLocalCaches();
                Debug.Log("Config is now active and ready to use.");
            });
    }
}
}