using Godot;
using System;

[GlobalClass]
public partial class BMLineEdit : LineEdit, INetworkData, ISaveData
{
    JsonValue INetworkData.SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        return new JsonValue();
    }
    void INetworkData.DeserializeNetworkData(JsonValue data)
    {
    }
    JsonValue ISaveData.SerializeSaveData()
    {
        return new JsonValue();
    }
    void ISaveData.DeserializeSaveData(JsonValue data) { }

   
}
