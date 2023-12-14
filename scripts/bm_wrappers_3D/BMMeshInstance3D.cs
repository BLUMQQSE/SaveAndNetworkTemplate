using Godot;
using System;
[GlobalClass]
public partial class BMMeshInstance3D : MeshInstance3D, INetworkData, ISaveData
{
    string LastDataSent;
    public override void _Ready()
    {
        base._Ready();
    }

    JsonValue INetworkData.SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        JsonValue data = new JsonValue();

        //Mesh info
        if (Mesh != null)
        {
            if (Mesh is CapsuleMesh c)
            {
                data["Mesh"].Set("Capsule");
                data["Rad"].Set(c.Radius);
                data["Height"].Set(c.Height);
            }
            else if (Mesh is BoxMesh b)
            {
                data["Mesh"].Set("Box");
                data["Size"].Set(b.Size);
            }
            else if (Mesh is TextMesh)
                data["Mesh"].Set("Text");
            else if (Mesh is PlaneMesh)
                data["Mesh"].Set("Plane");
            else if (Mesh is SphereMesh s)
            {
                data["Mesh"].Set("Sphere");
                data["Rad"].Set(s.Radius);
            }
            else
                data["Mesh"].Set(Mesh.ResourcePath);
        }
        if(MaterialOverride  != null)
        data["Mat"].Set(MaterialOverride.ResourcePath);


        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    void INetworkData.DeserializeNetworkData(JsonValue data)
    {
        if (data["Mesh"].IsValue)
        {
            ApplyMesh(data);
        }
        else
            Mesh = null;
        if (data["Mat"].IsValue)
            MaterialOverride = GD.Load<Material>(data["Mat"].AsString());
        else
            MaterialOverride = null;
    }


    private void ApplyMesh(JsonValue data)
    {
        string meshName = data["Mesh"].AsString();
        if (meshName == "Capsule")
        {
            Mesh = new CapsuleMesh();
            (Mesh as CapsuleMesh).Radius = data["Rad"].AsFloat();
            (Mesh as CapsuleMesh).Height = data["Height"].AsFloat();
        }
        else if (meshName == "Box")
        {
            Mesh = new BoxMesh();
            (Mesh as BoxMesh).Size = data["Size"].AsVector3();
        }
        else if (meshName == "Text")
            Mesh = new TextMesh();
        else if (meshName == "Sphere")
        {
            Mesh = new SphereMesh();
            (Mesh as SphereMesh).Radius = data["Rad"].AsFloat();
        }
        else if (meshName == "Plane")
            Mesh = new PlaneMesh();
        else
            Mesh = GD.Load<Mesh>(meshName);
    }

    JsonValue ISaveData.SerializeSaveData()
    {
        JsonValue data = new JsonValue();
        if (Mesh != null)
        {
            if (Mesh is CapsuleMesh c)
            {
                data["Mesh"].Set("Capsule");
                data["Rad"].Set(c.Radius);
                data["Height"].Set(c.Height);
            }
            else if (Mesh is BoxMesh b)
            {
                data["Mesh"].Set("Box");
                data["Size"].Set(b.Size);
            }
            else if (Mesh is TextMesh)
                data["Mesh"].Set("Text");
            else if (Mesh is PlaneMesh)
                data["Mesh"].Set("Plane");
            else if (Mesh is SphereMesh s)
            {
                data["Mesh"].Set("Sphere");
                data["Rad"].Set(s.Radius);
            }
            else
                data["Mesh"].Set(Mesh.ResourcePath);
        }
        if (MaterialOverride != null)
            data["Mat"].Set(MaterialOverride.ResourcePath);

        return data;
    }

    void ISaveData.DeserializeSaveData(JsonValue data)
    {
        if (data["Mesh"].IsValue)
        {
            ApplyMesh(data);
        }
        else
            Mesh = null;
        if (data["Mat"].IsValue)
            MaterialOverride = GD.Load<Material>(data["Mat"].AsString());
        else
            MaterialOverride = null;
    }
}
