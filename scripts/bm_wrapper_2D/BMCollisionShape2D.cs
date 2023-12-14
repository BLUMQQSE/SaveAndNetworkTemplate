using Godot;
using System;
[GlobalClass]
public partial class BMCollisionShape2D : CollisionShape2D, INetworkData, ISaveData
{
    string LastDataSent;
    enum ShapeType
    {
        Null,
        Capsule,
        Rectangle,
        Circle
    }
    ShapeType CurrentShapeType { get; set; }

    public override void _Ready()
	{

        SetShapeType();
    }

    private void SetShapeType()
    {
        if (Shape is null)
            CurrentShapeType = ShapeType.Null;
        if (Shape is CapsuleShape2D)
            CurrentShapeType = ShapeType.Capsule;
        if (Shape is RectangleShape2D)
            CurrentShapeType = ShapeType.Rectangle;
        if (Shape is CircleShape2D)
            CurrentShapeType = ShapeType.Circle;

    }

    private void UpdateShape(JsonValue data)
    {
        if (Shape is CapsuleShape2D cs2)
        {
            cs2.Radius = data["Radius"].AsFloat();
            cs2.Height = data["Height"].AsFloat();
        }
        if (Shape is CircleShape2D cc2)
        {
            cc2.Radius = data["Radius"].AsFloat();
        }
        if (Shape is RectangleShape2D rs2)
            rs2.Size = data["Size"].AsVector2();
    }


    JsonValue INetworkData.SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        JsonValue data = new JsonValue();

        if (CurrentShapeType == ShapeType.Null && Shape != null)
            SetShapeType();

        data["Shape"].Set(CurrentShapeType.ToString());

        if (Shape is CapsuleShape2D cs3)
        {
            data["Radius"].Set(cs3.Radius);
            data["Height"].Set(cs3.Height);
        }
        if (Shape is CircleShape2D ss3)
        {
            data["Radius"].Set(ss3.Radius);
        }
        if (Shape is RectangleShape2D bs3)
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
                Shape = new CapsuleShape2D();
            if (data["Shape"].AsString() == ShapeType.Circle.ToString())
                Shape = new CircleShape2D();
            if (data["Shape"].AsString() == ShapeType.Rectangle.ToString())
                Shape = new RectangleShape2D();


            UpdateShape(data);
        }

    }

    JsonValue ISaveData.SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        if (CurrentShapeType == ShapeType.Null && Shape != null)
            SetShapeType();
        data["Shape"].Set(CurrentShapeType.ToString());

        if (Shape is CapsuleShape2D cs3)
        {
            data["Radius"].Set(cs3.Radius);
            data["Height"].Set(cs3.Height);
        }
        if (Shape is CircleShape2D ss3)
        {
            data["Radius"].Set(ss3.Radius);
        }
        if (Shape is RectangleShape2D bs3)
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
                Shape = new RectangleShape2D();
            if (data["Shape"].AsString() == ShapeType.Circle.ToString())
                Shape = new CircleShape2D();
            if (data["Shape"].AsString() == ShapeType.Rectangle.ToString())
                Shape = new RectangleShape2D();


            UpdateShape(data);
        }
    }

}
