namespace DorkyProductions
{
    public class Context
    {
        public uint ActivePlayerNetId;
        public int ExtraTurnsLeft = 0;
        public int ChestsLeft = 0;

        public bool IsCurrentTurnExtra = false;
        // maybe even the rewards themselves.
        public bool isTutorial = false;
        
        public void Reset()
        {
            ActivePlayerNetId = uint.MaxValue;
            ExtraTurnsLeft = 0;
            ChestsLeft = 0;
            IsCurrentTurnExtra = false;
        }

        public void Setup(uint activePlayerNetId, int extraTurnsLeft, int chestsLeft, bool isCurrentTurnExtra, bool isTutorial)
        {
            this.ActivePlayerNetId = activePlayerNetId;
            this.ExtraTurnsLeft = extraTurnsLeft;
            this.ChestsLeft = chestsLeft;
            this.IsCurrentTurnExtra = isCurrentTurnExtra;
            this.isTutorial = isTutorial;
        }
    }
}