using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using System.Collections.Generic;
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

    private const string STARTING_HEALTH_KEY = "starting_health";
    private const string STARTING_SHIELD_KEY = "starting_shield";
    private const string AVOID_MATCH_CHANCE_KEY = "avoid_match_chance";
    // Fast local lookups
    private Dictionary<int, int> attackValues = new Dictionary<int, int>();
    private Dictionary<int, int> healValues = new Dictionary<int, int>();
    private Dictionary<int, int> shieldValues = new Dictionary<int, int>();
    private Dictionary<int, float> crossValues = new Dictionary<int, float>();

    private static readonly int PLAYER_STARTING_HEALTH = 20;
    private static readonly int PLAYER_STARTING_SHIELD = 10;
    
    private int startingHealth = PLAYER_STARTING_HEALTH;
    private int startingShield = PLAYER_STARTING_SHIELD;
    private float avoidMatchChance = 0.85f;
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
        var config = FirebaseRemoteConfig.DefaultInstance;

        startingHealth = (int)config.GetValue(STARTING_HEALTH_KEY).LongValue;
        startingShield = (int)config.GetValue(STARTING_SHIELD_KEY).LongValue;
            
        // --- CACHE NEW VALUE ---
        avoidMatchChance = (float)config.GetValue(AVOID_MATCH_CHANCE_KEY).DoubleValue;
            
        for (int i = 3; i <= 5; i++)
        {
            attackValues[i] = (int)config.GetValue($"{ATTACK_PREFIX}{i}").LongValue;
            healValues[i] = (int)config.GetValue($"{HEAL_PREFIX}{i}").LongValue;
            shieldValues[i] = (int)config.GetValue($"{SHIELD_PREFIX}{i}").LongValue;
            crossValues[i] = (float)config.GetValue($"{CROSS_PREFIX}{i}").DoubleValue;
        }
        Debug.Log($"Config updated. Avoid Match Chance: {avoidMatchChance}");
    }
    public float GetAvoidMatchChance() => avoidMatchChance;
    public int GetAttackVal(int count) => attackValues.ContainsKey(count) ? attackValues[count] : 1;
    public int GetHealVal(int count) => healValues.ContainsKey(count) ? healValues[count] : 1;
    public int GetShieldVal(int count) => shieldValues.ContainsKey(count) ? shieldValues[count] : 1;
    public float GetCrossVal(int count) => crossValues.ContainsKey(count) ? crossValues[count] : 2.0f;

    public int GetStartingHealth() => startingHealth;
    public int GetStartingShield() => startingShield;
    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
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
        // This dictionary acts as a fallback if fetch fails
        Dictionary<string, object> defaults = new Dictionary<string, object>
        {
            { "attack_3", 3 }, { "attack_4", 4 }, { "attack_5", 6 },
            { "heal_3", 3 },   { "heal_4", 4 },   { "heal_5", 6 },
            { "shield_3", 3 }, { "shield_4", 4 }, { "shield_5", 6 },
            { "cross_3", 2.0f }, { "cross_4", 2.25f }, { "cross_5", 2.5f },
            { STARTING_SHIELD_KEY, 10 }, {STARTING_HEALTH_KEY, 20 },
            { AVOID_MATCH_CHANCE_KEY, 0.85f }
        };

        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults)
            .ContinueWithOnMainThread(task =>
            {
                UpdateLocalCaches();
                FetchData();
            });
    }
    private void FetchData()
    {
        // 1. Define the timeout (Zero for instant updates during dev)
        System.TimeSpan fetchTimeout = System.TimeSpan.Zero; 

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