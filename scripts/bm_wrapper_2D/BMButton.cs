using Godot;
using System;

[GlobalClass]
public partial class BMButton : Button, INetworkData, ISaveData
{
    string LastDataSent;
    public JsonValue SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        JsonValue data = new JsonValue();

        data["SZ"].Set(Size);
        data["TX"].Set(Text);

        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        Size = data["SZ"].AsVector2();
        Text = data["TX"].AsString();
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data["Text"].Set(Text);
        data["Size"].Set(Size);
        
        return data;
    }
    public void DeserializeSaveData(JsonValue data)
    {
        Text = data["Text"].AsString();
        Size = data["Size"].AsVector2();
    }
     
}
