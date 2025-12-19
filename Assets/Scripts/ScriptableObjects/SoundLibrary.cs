using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "DungeonMatch/SoundLibrary")]
public class SoundLibrary : ScriptableObject
{
    public List<SoundEffectGroup> sfxGroups;
    public List<MusicGroup> musicGroups;

    public AudioClip GetRandomClip(SFXType type) {
        var mapping = sfxGroups.Find(m => m.type == type);
        if (mapping != null && mapping.clips.Length > 0) {
            return mapping.clips[Random.Range(0, mapping.clips.Length)];
        }
        Debug.LogWarning($"No clips found for SoundType: {type}");
        return null;
    }
    public AudioClip GetMusic(MusicType type) {
        var mapping = musicGroups.Find(m => m.type == type);
        return mapping?.clip;
    }
}

[System.Serializable]
public class MusicGroup
{
    public MusicType type;
    public AudioClip clip;
}

[System.Serializable]
public class SoundEffectGroup {
    public SFXType type;
    public AudioClip[] clips;
}
public enum SFXType {
    ChestOpen,
    MatchChest,
    MatchAttack,
    MatchCritAttack,
    MatchCross,
    MatchPotion,
    MatchShield,
    AttackHitOnShield,
    TileLand,
    TileSwap,
    LoseScreen,
    WinScreen,
    GameFound,
    TapMenuButton,
    TapPlayButton,
    YourTurn,
    PhantomMatchTap1,
    PhantomMatchTap2,
    PhantomMatchTap3,
    Lightning,
    CoinCollected,
    DiceRoll,
    FinalHit,
}
public enum MusicType
{
    Gameplay,
    Intro
}