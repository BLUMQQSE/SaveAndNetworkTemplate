using Godot;
using System;
[GlobalClass]
public partial class NetworkTransform : Node, INetworkData
{
    string LastDataSent;
    [Export]
	public Vector3 SyncPos { get; set; }
    [Export]
    public Vector3 SyncRot { get; set; }

    public float MaxOffsetPermitted { get; set; } = 8;

    public bool TrustClientPos { get; set; } = true;
    [Export]
    public bool IsOwnedByThisInstance { get; private set; } = false;
    [Export]
    public bool ParentIs3D { get; private set; } = false;
    Node2D Parent2D { get; set; }
    Node3D Parent3D { get; set; }

    public override void _Ready()
    {
        base._Ready();
        if (GetParent() is Node3D n3)
        {
            ParentIs3D = true;
            Parent3D = n3;
        }
        else
            Parent2D = GetParent<Node2D>();

        bool foundPlayer = SetOwnership(GetParent());
        if (!foundPlayer && NetworkManager.Instance.IsServer)
            IsOwnedByThisInstance = true;

        if (!GetParent().HasMeta(Globals.Meta.OwnerId.ToString()) && NetworkManager.Instance.IsServer)
            IsOwnedByThisInstance = true;
        else if(GetParent().HasMeta(Globals.Meta.OwnerId.ToString()))
        {
            string idStr = GetParent().GetMeta(Globals.Meta.OwnerId.ToString()).ToString();
            ulong id = Convert.ToUInt64(idStr);
            if (id == NetworkManager.Instance.PlayerId)
                IsOwnedByThisInstance = true;
        }
    }

    private bool SetOwnership(Node node)
    {
        if (node == GetTree().Root)
            return false;
        
        if (node.HasMeta(Globals.Meta.OwnerId.ToString()))
        {
            ulong id = Convert.ToUInt64(node.GetMeta(Globals.Meta.OwnerId.ToString()).ToString());
            if (id == NetworkManager.Instance.PlayerId)
            {
                IsOwnedByThisInstance = true;
                return true;
            }
            else
                return true;
        }
        else
        {
            return SetOwnership(node.GetParent());
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (!IsOwnedByThisInstance)
            return;

        if(ParentIs3D)
        {
            SyncPos = GetParent<Node3D>().Position;
            SyncRot = GetParent<Node3D>().Rotation;
        }
        else
        {
            SyncPos = new Vector3(Parent2D.Position.X, Parent2D.Position.Y, 0);
            SyncRot = new Vector3(Parent2D.Rotation, 0, 0);
        }
        
        
    }

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        JsonValue data = new JsonValue();

        if (GetParent() is Node3D n3)
        {
            data["SP"].Set(n3.Position);
            data["SR"].Set(n3.Rotation);
        }
        else if(GetParent() is Node2D n2)
        {
            data["SP"].Set(new Vector3(n2.Position.X, n2.Position.Y, 0));
            data["SR"].Set(new Vector3(n2.Rotation, 0, 0));
        }
        data["TCP"].Set(TrustClientPos);

        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        SyncPos = data["SP"].AsVector3();
        SyncRot = data["SR"].AsVector3();
        TrustClientPos = data["TCP"].AsBool();
    }

}
