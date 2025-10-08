namespace DorkyProductions.Algorithms
{
    // TODO: Unify this and MatchGroup
    public class MatchResult
    {
        public int val;
        public int idx;

        public MatchResult (int val, int idx)
        {
            this.val = val;
            this.idx = idx;
        }

        public MatchResult(MatchResult other)
        {
            val = other.val;
            idx = other.idx;
        }
    }
}