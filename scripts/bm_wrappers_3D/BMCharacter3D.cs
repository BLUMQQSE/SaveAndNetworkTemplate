using Godot;
using System;

[GlobalClass]
public partial class BMCharacter3D : CharacterBody3D, INetworkData, ISaveData, IListener
{
	string LastDataSent;

    [Export] public float Speed { get; set; } = 7.0f;
	[Export]public float JumpVelocity { get; set; } = 8f;
	private bool ShouldJump { get; set; } = false;

	private float gravity = 9.8f;
	protected NetworkTransform NetworkTransform { get; set; }
    Vector3 Direction {  get; set; }
    public override void _Ready()
    {
        base._Ready();
        NetworkTransform = this.GetChildOfType<NetworkTransform>();

        if (!NetworkManager.Instance.IsServer)
            EventSystem.Instance.Subscribe(EventID.OnNetworkUpdate_Client, this);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        EventSystem.Instance.UnsubscribeAll(this);
    }

    public override void _PhysicsProcess(double delta)
	{
		if (NetworkManager.Instance.IsServer)
		{
			if (NetworkTransform.IsOwnedByThisInstance)
			{
                // we are the server and we are the server player or an npc
                HandleInstant((float)delta);
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
				HandleInstant((float)delta);
			}
			// we are the client handling anything else
			else
			{
				HandleInterpolated((float)(delta));
			}
		}
		
		Direction = Vector3.Zero;
	}

	public void HandleInstant(float delta)
	{
        Vector3 vel = new Vector3();
		if (Direction != Vector3.Zero)
		{
			vel.X = Direction.X * Speed;
			vel.Y = Velocity.Y;
			vel.Z = Direction.Z * Speed;
		}
		else
		{
			vel.X = 0;
			vel.Y = Velocity.Y;
			vel.Z = 0;

		}

        Velocity = vel;

		if (ShouldJump)
			Velocity = new Vector3(Velocity.X, JumpVelocity, Velocity.Z);
		else if (!IsOnFloor())
		{
			Velocity = new Vector3(Velocity.X, Velocity.Y - (gravity * delta), Velocity.Z);
		}

		ShouldJump = false;
        MoveAndSlide();
	}

	public void HandleInterpolated(float delta)
	{
		Position = Position.Lerp(NetworkTransform.SyncPos, 5 * delta);
		Rotation = Rotation.Lerp(NetworkTransform.SyncRot, 5 * delta);
	}

	public void Move(Vector2 direction)
	{
		Direction = (Transform.Basis * new Vector3(direction.X, 0, direction.Y)).Normalized();
    }

	public void MoveTowards(Vector3 destination)
    {
		Direction = new Vector3(destination.X - 
			GlobalPosition.X, GlobalPosition.Y, destination.Z - GlobalPosition.Z).Normalized();
    }
	public bool Jump()
	{
		if(!IsOnFloor()) return false;
		ShouldJump = true;

		return ShouldJump;
	}


    public JsonValue SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        JsonValue data = new JsonValue();
		
		data["CL"].Set(CollisionLayer);
		data["CM"].Set(CollisionMask);

		return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
		CollisionLayer = data["CL"].AsUInt();
		CollisionMask = data["CM"].AsUInt();
    }


    JsonValue ISaveData.SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data["CL"].Set(CollisionLayer);
        data["CM"].Set(CollisionMask);

		return data;
    }

    void ISaveData.DeserializeSaveData(JsonValue data)
    {
		CollisionLayer = data["CL"].AsUInt();
		CollisionMask = data["CM"].AsUInt();
    }

    public void HandleEvent(Event e)
    {
        if (e.IDAsEvent == EventID.OnNetworkUpdate_Client)
        {

            if (NetworkTransform.SyncPos.DistanceSquaredTo(Position) > Mathf.Pow(NetworkTransform.MaxOffsetPermitted, 2) || !NetworkTransform.TrustClientPos)
            {
                // teleport, because we're far off and is likely intended to not be walked
                Position = NetworkTransform.SyncPos;
            }


            if (NetworkTransform.IsOwnedByThisInstance)
            {
            // only update the server of our position if it has changed
                NetworkDataManager.Instance.ClientUpdate(this);
            }
        }
    }
}
