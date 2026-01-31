using Mirror;

namespace DorkyProductions
{
    public class NetworkMessages
    {
        
        public struct CreatePlayerMessage : NetworkMessage
        {
            public uint guestID;
        }
    }
}