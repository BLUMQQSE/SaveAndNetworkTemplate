using Godot;
using System;
using System.Text.Json.Nodes;

[GlobalClass]
public partial class BMCSGBox3D : CsgBox3D, INetworkData, ISaveData
{
    string LastDataSent;

    JsonValue INetworkData.SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        JsonValue data = new JsonValue();

        if (MaterialOverride != null)
            data["Material-Override-Path"].Set(MaterialOverride.ResourcePath);

        data["Size"].Set(Size);
        data["CM"].Set(CollisionMask);
        data["CL"].Set(CollisionLayer);
        data["UC"].Set(UseCollision);

        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    void INetworkData.DeserializeNetworkData(JsonValue data)
    {
        if (MaterialOverride == null)
            MaterialOverride = GD.Load<Material>(data["Material-Override-Path"].AsString());
        else if (data["Material-Override-Path"].AsString() != MaterialOverride.ResourcePath)
            MaterialOverride = GD.Load<Material>(data["Material-Override-Path"].AsString());

        Size = data["Size"].AsVector3(); 
        CollisionLayer = data["CL"].AsUInt();
        CollisionMask = data["CM"].AsUInt();
        UseCollision = data["UC"].AsBool();
    }
    JsonValue ISaveData.SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        if (MaterialOverride != null)
            data["MaterialOverridePath"].Set(MaterialOverride.ResourcePath);

        data["CM"].Set(CollisionMask);
        data["CL"].Set(CollisionLayer);
        data["Size"].Set(Size);
        data["UseCollision"].Set(UseCollision);

        return data;
    }

    void ISaveData.DeserializeSaveData(JsonValue data)
    {
        if (data["MaterialOverridePath"].IsValue)
        {
            MaterialOverride = GD.Load<Material>(data["MaterialOverridePath"].AsString());
        }
        Size = data["Size"].AsVector3();
        CollisionLayer = data["CL"].AsUInt();
        CollisionMask = data["CM"].AsUInt();
        UseCollision = data["UseCollision"].AsBool();
    }
}
