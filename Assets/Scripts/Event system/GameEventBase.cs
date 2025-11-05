using DG.Tweening;

namespace DorkyProductions
{
    // Using a byte is more network-efficient
    public enum EventType : byte
    {
        // Blocking
        SwappedTiles,
        SwapDenied,
        MatchedTiles,
        Attack,
        Heal,
        Shield,
        NegateAttackByShield,

        // Parallel
        TileMoved,
        TileSpawned,

        // Immediate
        TurnStarted,
        TurnEnded,
        GameStarted,
        GameEnded,
    }

    public enum SyncType
    {
        Blocking,
        Immediate,
        Parallel
    }

    /// <summary>
    /// The abstract base class for all game events.
    /// Used for object pooling.
    /// </summary>
    public abstract class GameEventBase
    {
        public abstract SyncType SyncType { get; }
        public abstract EventType EventType { get; }

        /// <summary>
        /// The "Accept" method for the Visitor pattern.
        /// </summary>
        public abstract Tween Accept(IGameEventHandler handler);
        
        /// <summary>
        /// Resets the event's data to its default state when
        /// it is returned to the object pool.
        /// </summary>
        public abstract void Reset();
    }

    /// <summary>
    /// The "Visitor" interface. Your client-side event processor
    /// must implement this. All Handle methods now return a Tween.
    /// </summary>
    public interface IGameEventHandler
    {
        // Blocking
        Tween Handle(GameStartedEvent e);
        Tween Handle(GameEndedEvent e);
        Tween Handle(MatchedTilesEvent e);
        Tween Handle(SwappedTilesEvent e);
        Tween Handle(SwapDeniedEvent e);
        Tween Handle(AttackEvent e);
        Tween Handle(HealEvent e);
        Tween Handle(ShieldEvent e);
        Tween Handle(NegateAttackByShieldEvent e);

        // Immediate
        Tween Handle(TurnStartedEvent e);
        Tween Handle(TurnEndedEvent e);

        // Parallel
        Tween Handle(TileMovedEvent e);
        Tween Handle(TileSpawnedEvent e);
    }
}