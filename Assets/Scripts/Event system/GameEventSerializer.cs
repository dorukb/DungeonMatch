using Mirror;

namespace DorkyProductions
{
    // Custom serializer for manually writing/reading GameEventBase
    // to and from a byte stream. Integrates with the EventPool.
    public static class GameEventSerializer
    {
        public static void WriteGameEvent(this NetworkWriter writer, GameEventBase ev)
        {
            // The type needs to be written first for the Deserialization on Client side.
            writer.Write((byte)ev.EventType);
            ev.Serialize(writer);
        }

        public static GameEventBase ReadGameEvent(this NetworkReader reader)
        {
            // Read the "EventType" byte first to get
            // a correctly-typed, recycled object from the pool
            EventType type = (EventType)reader.Read<byte>();
            GameEventBase ev = EventPool.Get(type);
            
            // let the polymorphic deserialization work its magic.
            ev.Deserialize(reader);
            return ev;
        }
    }
}