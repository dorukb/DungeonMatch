using Mirror;
using System.Collections.Generic;

namespace DorkyProductions
{
    /// This is a dummy struct with no purpose other than
    /// to force Mirror's weaver to "see" certain types
    /// and generate serializers for them automatically.
    ///
    /// By defining a NetworkMessage struct, the weaver is
    /// forced to scan its fields and generate serializers for them.
    /// 
    /// This message is never actually sent.
    public struct WeaverMagicMessage : NetworkMessage
    {
        // By including these types as fields, the weaver
        // is forced to generate serializers for them.
        public List<ushort> dummyList1;
        public List<TileState> dummyList2;
        // We can also add any other types we need
    }
}