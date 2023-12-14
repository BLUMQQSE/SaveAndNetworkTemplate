using Godot;
using System;
[GlobalClass]
public partial class BMLabel3D : Label3D, INetworkData, ISaveData
{
    string LastDataSent;
    
    JsonValue INetworkData.SerializeNetworkData(bool forceReturn, bool ignoreThisUpdateOccurred)
    {
        JsonValue data = new JsonValue();

        data["BB"].Set((int)Billboard);
        data["TX"].Set(Text);
        data["PS"].Set(PixelSize);
        data["FS"].Set(FontSize);
        data["OS"].Set(OutlineSize);

        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }
    void INetworkData.DeserializeNetworkData(JsonValue data)
    {
        Billboard = (BaseMaterial3D.BillboardModeEnum)data["BB"].AsInt();
        Text = data["TX"].AsString();
        PixelSize = data["PS"].AsFloat();
        FontSize = data["FS"].AsInt();
        OutlineSize = data["OS"].AsInt();
    }

    public JsonValue SerializeSaveData()
    {
        JsonValue data = new JsonValue();

        data["Billboard"].Set((int)Billboard);
        data["Text"].Set(Text);
        data["PS"].Set(PixelSize);
        data["FS"].Set(FontSize);
        data["OS"].Set(OutlineSize);

        return data;
    }

    public void DeserializeSaveData(JsonValue data)
    {
        Billboard = (BaseMaterial3D.BillboardModeEnum)data["Billboard"].AsInt();
        Text = data["Text"].AsString();
        PixelSize = data["PS"].AsFloat();
        FontSize = data["FS"].AsInt();
        OutlineSize = data["OS"].AsInt();
    }
}
