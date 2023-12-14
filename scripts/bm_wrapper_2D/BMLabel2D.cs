using Godot;
using System;

[GlobalClass]
public partial class BMLabel2D : Label, INetworkData, ISaveData
{
    string LastDataSent;


    public override void _Process(double delta)
    {
        base._Process(delta);
        if(!Visible)
            return;
    }

    JsonValue INetworkData.SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        JsonValue data = new JsonValue();

        data["T"].Set(Text);
        data["S"].Set(Size);
        data["VA"].Set((int)VerticalAlignment);
        data["HA"].Set((int)HorizontalAlignment);


        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);

    }
    void INetworkData.DeserializeNetworkData(JsonValue data)
    {
        Text = data["T"].AsString();
        Size = data["S"].AsVector2();
        HorizontalAlignment = (HorizontalAlignment)data["HA"].AsInt();
        VerticalAlignment = (VerticalAlignment)data["VA"].AsInt();

    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data["T"].Set(Text);
        data["VA"].Set((int)VerticalAlignment);
        data["HA"].Set((int)HorizontalAlignment);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        Text = data["T"].AsString();
        HorizontalAlignment = (HorizontalAlignment)data["HA"].AsInt();
        VerticalAlignment = (VerticalAlignment)data["VA"].AsInt();
    }
}
