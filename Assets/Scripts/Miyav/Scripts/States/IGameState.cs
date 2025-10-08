using DorkyProductions.Core;

namespace DorkyProductions.States
{
    public interface IGameState
    {
        void Enter(Context context);
        void Update(Context context);
        void Exit(Context context);
    }
}