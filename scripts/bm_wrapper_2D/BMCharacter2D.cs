using Godot;
using System;
[GlobalClass]
public partial class BMCharacter2D : CharacterBody2D, INetworkData
{
    string LastDataSent;

    [Export] public float Speed { get; set; } = 300.0f;
    Vector2 Direction { get; set; }
    protected NetworkTransform NetworkTransform { get; set; }

    public override void _Ready()
    {
        base._Ready();
        NetworkTransform = this.GetChildOfType<NetworkTransform>();
    }

    public override void _PhysicsProcess(double delta)
	{
        if (NetworkManager.Instance.IsServer)
        {
            if (NetworkTransform.IsOwnedByThisInstance)
            {
                // we are the server and we are the server player or an npc
                HandleInstant();
            }
            else
            {
                // we are the server handling a client player
                if (HasMeta(Globals.Meta.OwnerId.ToString()))
                {
                    HandleInterpolated((float)delta);
                }
            }
        }
        else
        {
            // we are the client handling our own player
            if (NetworkTransform.IsOwnedByThisInstance)
            {
                HandleInstant();
            }
            // we are the client handling anything else
            else
            {
                HandleInterpolated((float)(delta));
            }
        }

        Direction = Vector2.Zero;
    }

    public void HandleInstant()
    {
        if (Direction != Vector2.Zero)
            Velocity = Direction * Speed;
        else
            Velocity = Vector2.Zero;
        
        MoveAndSlide();
    }

    public void HandleInterpolated(float delta)
    {
        Position = Position.Lerp(new Vector2(NetworkTransform.SyncPos.X, NetworkTransform.SyncPos.Y), 5 * delta);
        Rotation = Mathf.Lerp(Rotation, NetworkTransform.SyncRot.X, 5 * delta);
    }


    public void Move(Vector2 direction)
	{
		Direction = direction.Normalized();
	}

    JsonValue INetworkData.SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        JsonValue data = new JsonValue();

        data["CL"].Set(CollisionLayer);
        data["CM"].Set(CollisionMask);

        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    void INetworkData.DeserializeNetworkData(JsonValue data)
    {
        CollisionLayer = data["CL"].AsUInt();
        CollisionMask = data["CM"].AsUInt();
    }

}
