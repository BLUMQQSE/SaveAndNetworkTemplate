using Godot;
using System;
[GlobalClass]
public partial class BMOmniLight3D : OmniLight3D, ISaveData, INetworkData
{
    string LastDataSent;
    JsonValue INetworkData.SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        JsonValue data = new JsonValue();

        data["LC"].Set(new Vector3(LightColor.R, LightColor.G, LightColor.B));
        data["LA"].Set(LightColor.A);
        data["LE"].Set(LightEnergy);
        data["SE"].Set(ShadowEnabled);
        data["RA"].Set(OmniRange);
        

        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    void INetworkData.DeserializeNetworkData(JsonValue data)
    {
        LightColor = new Color(data["LC"]["X"].AsFloat(), data["LC"]["Y"].AsFloat(),
            data["LC"]["Z"].AsFloat(), data["LA"].AsFloat());
        LightEnergy = data["LE"].AsFloat();
        ShadowEnabled = data["SE"].AsBool();
        OmniRange = data["RA"].AsFloat();
    }
    JsonValue ISaveData.SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data["LightColor"].Set(new Vector3(LightColor.R, LightColor.G, LightColor.B));
        data["LightAlpha"].Set(LightColor.A);
        data["LightEnergy"].Set(LightEnergy);
        data["ShadowEnabled"].Set(ShadowEnabled);
        data["Range"].Set(OmniRange);

        return data;
    }

    void ISaveData.DeserializeSaveData(JsonValue data)
    {
        LightColor = new Color(data["LightColor"]["X"].AsFloat(), data["LightColor"]["Y"].AsFloat(),
            data["LightColor"]["Z"].AsFloat(), data["LightAlpha"].AsFloat());
        LightEnergy = data["LightEnergy"].AsFloat();
        ShadowEnabled = data["ShadowEnabled"].AsBool();
        OmniRange = data["Range"].AsFloat();
    }


}
