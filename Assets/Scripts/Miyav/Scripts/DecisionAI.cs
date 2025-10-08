using DorkyProductions.Core;

namespace DorkyProductions.AI
{
    public interface DecisionAI
    {
        public void SelectMove(Context context, PlayerAI controller);
    }
}