using Godot;
using System;

public partial class BMArea2D : Area2D, INetworkData, ISaveData
{
    string LastDataSent;
    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        JsonValue data = new JsonValue();

        data["CM"].Set(CollisionMask);
        data["CL"].Set(CollisionLayer);
        data["MB"].Set(Monitorable);
        data["MG"].Set(Monitoring);

        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }
    public void DeserializeNetworkData(JsonValue data)
    {
        CollisionLayer = data["CL"].AsUInt();
        CollisionMask = data["CM"].AsUInt();
        Monitorable = data["MB"].AsBool();
        Monitoring = data["MG"].AsBool();
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data["CollisionMask"].Set(CollisionMask);
        data["CollisionLayer"].Set(CollisionLayer);
        data["Monitoring"].Set(Monitoring);
        data["Monitorable"].Set(Monitorable);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        CollisionLayer = data["CollisionLayer"].AsUInt();
        CollisionMask = data["CollisionMask"].AsUInt();
        Monitorable = data["Monitorable"].AsBool();
        Monitoring = data["Monitoring"].AsBool();
    }
}
