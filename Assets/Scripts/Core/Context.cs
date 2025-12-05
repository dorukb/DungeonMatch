namespace DorkyProductions
{
    public class Context
    {
        public uint ActivePlayerNetId;
        public int ExtraTurnsLeft = 0;
        public int ChestsLeft = 0;
        // maybe even the rewards themselves.

        public void Reset()
        {
            ActivePlayerNetId = uint.MaxValue;
            ExtraTurnsLeft = 0;
            ChestsLeft = 0;
        }

        public void Setup(uint activePlayerNetId, int extraTurnsLeft, int chestsLeft)
        {
            this.ActivePlayerNetId = activePlayerNetId;
            this.ExtraTurnsLeft = extraTurnsLeft;
            this.ChestsLeft = chestsLeft;
        }
    }
}