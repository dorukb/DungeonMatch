using DorkyProductions.States;

namespace DorkyProductions.Core
{
    // Game State for "Pati"
    public class Context
    {
        public Deck DrawPile;
        public SpecialDeck SpecialDeck;
        public ShelterRow ShelterRow;
        public MiyavPlayer CurrentMiyavPlayer;
        public GameMaster gameMaster { get; private set; }

        public CatCard CurrentCardInPlay { get; private set; }
        
        public Context(GameMaster gameMaster)
        {
            this.gameMaster = gameMaster;
            this.ShelterRow = new ShelterRow();
        }
        
        // IMPORTANT: Should be called by the Players when they end their turn.
        public void SignalEndTurn()
        {
            // Debug.Log("Current player ended their turn.");
            CurrentMiyavPlayer.TurnEnded(this);
            TransitionToState(new PlayerTurnState());
        }

        public void SetCurrentCardInPlay(CatCard catCard)
        {
            this.CurrentCardInPlay = catCard;
        }
        public void TransitionToState(IGameState newState)
        {
            this.gameMaster.TransitionToState(newState);
        }
    }
}