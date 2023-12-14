using Godot;
using System;
[GlobalClass]
public partial class BMSprite2D : Sprite2D, INetworkData, ISaveData
{
	string LastDataSent;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    JsonValue INetworkData.SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
		JsonValue data = new JsonValue();

		if (Texture != null)
			data["Tex"].Set(Texture.ResourcePath);
		else
			data["Tex"].Set("null");


		return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    void INetworkData.DeserializeNetworkData(JsonValue data)
    {
		if (data["Tex"].IsValue)
			Texture = GD.Load<Texture2D>(data["Tex"].AsString());
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        if (Texture != null)
            data["Texture"].Set(Texture.ResourcePath);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        if (data["Texture"].IsValue)
            Texture = GD.Load<Texture2D>(data["Texture"].AsString());
    }
}
