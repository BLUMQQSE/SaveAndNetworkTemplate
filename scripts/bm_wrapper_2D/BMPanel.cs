using Godot;
using System;

public partial class BMPanel : Panel, INetworkData, ISaveData
{
    string LastDataSent;
    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        JsonValue data = new JsonValue();

        data["SZ"].Set(Size);

        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        Size = data["SZ"].AsVector2();
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data["Size"].Set(Size);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        Size = data["Size"].AsVector2();
    }
}
