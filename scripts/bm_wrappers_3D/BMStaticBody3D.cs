using Godot;
using System;
[GlobalClass]
public partial class BMStaticBody3D : StaticBody3D, ISaveData, INetworkData
{
    string LastDataSent;
    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        JsonValue data = new JsonValue();

        data["CM"].Set(CollisionMask);
        data["CL"].Set(CollisionLayer);

        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        CollisionMask = data["CM"].AsUInt();
        CollisionLayer = data["CL"].AsUInt();
    }
    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data["CM"].Set(CollisionMask);
        data["CL"].Set(CollisionLayer);

        return data;
    }


    public void DeserializeSaveData(JsonValue data)
    {
        CollisionMask = data["CM"].AsUInt();
        CollisionLayer = data["CL"].AsUInt();
    }

}
