using Godot;
using System;
using System.Collections.Generic;
[GlobalClass]
public partial class BMCollisionShape3D : CollisionShape3D, INetworkData, ISaveData
{
    string LastDataSent;
    enum ShapeType
    {
        Null,
        Capsule,
        Sphere,
        Box
    }
    ShapeType CurrentShapeType {  get; set; }

    public override void _Ready()
    {
        SetShapeType();
    }     

    private void SetShapeType()
    {
        if(Shape is null)
            CurrentShapeType = ShapeType.Null;
        if (Shape is CapsuleShape3D)
            CurrentShapeType = ShapeType.Capsule;
        if (Shape is SphereShape3D)
            CurrentShapeType = ShapeType.Sphere;
        if (Shape is BoxShape3D)
            CurrentShapeType = ShapeType.Box;

    }

    JsonValue INetworkData.SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        JsonValue data = new JsonValue();

        if (CurrentShapeType == ShapeType.Null && Shape != null)
            SetShapeType();

        data["Shape"].Set(CurrentShapeType.ToString());

        if (Shape is CapsuleShape3D cs3)
        {
            data["Radius"].Set(cs3.Radius);
            data["Height"].Set(cs3.Height);
        }
        if (Shape is SphereShape3D ss3)
        {
            data["Radius"].Set(ss3.Radius);
        }
        if(Shape is BoxShape3D bs3)
        {
            data["Size"].Set(bs3.Size);
        }
        
        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);    
    }

    void INetworkData.DeserializeNetworkData(JsonValue data)
    {
        if (data["Shape"].AsString() == "Null") { return; }
        if (data["Shape"].AsString() == CurrentShapeType.ToString())
        {
            // just updata
            UpdateShape(data);
        }
        else
        {
            if (data["Shape"].AsString() == ShapeType.Capsule.ToString())
                Shape = new CapsuleShape3D();
            if (data["Shape"].AsString() == ShapeType.Sphere.ToString())
                Shape = new SphereShape3D();
            if (data["Shape"].AsString() == ShapeType.Box.ToString())
                Shape = new BoxShape3D();


            UpdateShape(data);
        }
        
    }

    JsonValue ISaveData.SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        if (CurrentShapeType == ShapeType.Null && Shape != null)
            SetShapeType();
        data["Shape"].Set(CurrentShapeType.ToString());

        if (Shape is CapsuleShape3D cs3)
        {
            data["Radius"].Set(cs3.Radius);
            data["Height"].Set(cs3.Height);
        }
        if (Shape is SphereShape3D ss3)
        {
            data["Radius"].Set(ss3.Radius);
        }
        if (Shape is BoxShape3D bs3)
        {
            data["Size"].Set(bs3.Size);
        }
        return data;
    }

    void ISaveData.DeserializeSaveData(JsonValue data)
    {
        if (data["Shape"].AsString() == "Null") { return; }
        if (data["Shape"].AsString() == CurrentShapeType.ToString())
        {
            // just update
            UpdateShape(data);
        }
        else
        {
            if (data["Shape"].AsString() == ShapeType.Capsule.ToString())
                Shape = new CapsuleShape3D();
            if (data["Shape"].AsString() == ShapeType.Sphere.ToString())
                Shape = new SphereShape3D();
            if (data["Shape"].AsString() == ShapeType.Box.ToString())
                Shape = new BoxShape3D();


            UpdateShape(data);
        }
    }
    private void UpdateShape(JsonValue data)
    {
        if (Shape is CapsuleShape3D cs3)
        {
            cs3.Radius = data["Radius"].AsFloat();
            cs3.Height = data["Height"].AsFloat();
        }
        if (Shape is SphereShape3D ss3)
        {
            ss3.Radius = data["Radius"].AsFloat();
        }
        if (Shape is BoxShape3D bs3)
            bs3.Size = data["Size"].AsVector3();
    }


}
