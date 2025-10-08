using DorkyProductions.Core;

namespace DorkyProductions
{
    // a.k.a Command Pattern
    public interface IEffect
    {
        void Execute(Context context);
    }
}