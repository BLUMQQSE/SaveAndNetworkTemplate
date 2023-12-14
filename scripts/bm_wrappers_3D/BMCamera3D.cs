using Godot;
using System;
using System.Collections.Generic;
[GlobalClass]
public partial class BMCamera3D : Camera3D, ISaveData, INetworkData
{
    string LastDataSent;

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        JsonValue data = new JsonValue();

        data["Orth"].Set(Projection == ProjectionType.Orthogonal);
        data["Size"].Set(Size);
        data["FOV"].Set(Fov);
        data["F"].Set(Far);

        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        if (data["Orth"].AsBool())
            Projection = ProjectionType.Orthogonal;

        Size = data["Size"].AsFloat();
        Fov = data["FOV"].AsFloat();
        Far = data["F"].AsFloat();
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data["Orth"].Set(Projection == ProjectionType.Orthogonal);
        data["Size"].Set(Size);
        data["FOV"].Set(Fov);
        data["Far"].Set(Far);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        if(data["Orth"].AsBool())
            Projection = ProjectionType.Orthogonal;

        Size = data["Size"].AsFloat();
        Fov = data["FOV"].AsFloat();
        Far = data["Far"].AsFloat();

    }

}
