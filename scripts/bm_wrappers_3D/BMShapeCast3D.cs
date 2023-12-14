using Godot;
using System;

public partial class BMShapeCast3D : ShapeCast3D, ISaveData
{

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();
        data["Rad"].Set((Shape as SphereShape3D).Radius);
        data["TP"].Set(TargetPosition);
        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        Shape = new SphereShape3D();
        TargetPosition = data["TP"].AsVector3();
        (Shape as SphereShape3D).Radius=data["Rad"].AsFloat();
    }
}
