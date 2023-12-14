using Godot;
using System;
[GlobalClass]
public partial class BMAudioStreamPlayer3D : AudioStreamPlayer3D, INetworkData
{
    string LastDataSent;
    public AudioData AudioData { get; private set; }
    [Export]int playCount = 1;

    public void PlaySound()
    {
        playCount++;
        Play();
    }

    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false)
    {
        JsonValue data = new JsonValue();
        if(AudioData != null) 
            data["AD"].Set(AudioData.ResourcePath.RemovePath());

        data["PC"].Set(playCount);
        return Helper.Instance.DetermineSerializeReturn(ref LastDataSent, data, forceReturn, ignoreThisUpdateOccurred);
    }

    public void DeserializeNetworkData(JsonValue data)
    {
        if (data["AD"].IsValue)
        {
            if (AudioData != null)
            {
                if(AudioData.ResourcePath.RemovePath() != data["AD"].AsString())
                    ApplyAudioData(GD.Load<AudioData>(ResourceManager.Instance.GetResourcePath(data["AD"].AsString())));
            }
            else
                ApplyAudioData(GD.Load<AudioData>(ResourceManager.Instance.GetResourcePath(data["AD"].AsString())));
        }
        if(playCount < data["PC"].AsInt())
        {
            int dif = data["PC"].AsInt() - playCount;
            for(int i = 0; i < dif; i++)
            {
                PlaySound();
            }
        }
    }

    public void ApplyAudioData(AudioData data)
    {
        AudioData = data;
        Stream = data.AudioStream;
        MaxDb = data.MaxDB;
        MaxPolyphony = data.MaxSimultaneousInstances;
        VolumeDb = data.VolumeDB;
        AttenuationModel = data.AttenuationModel;
        Autoplay = true;
        MaxDistance = data.MaxDistance;
        PitchScale = data.PitchScale;
        UnitSize = data.UnitSize;
    }

}
