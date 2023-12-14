using Godot;
using Microsoft.VisualBasic;
using System;

public partial class Player : BMCharacter3D
{
    public BMCamera3D Camera { get; private set; }

    public override void _Ready()
    {
        base._Ready();

        

        if (NetworkTransform.IsOwnedByThisInstance)
        {
            Camera = GD.Load<PackedScene>(ResourceManager.Instance.GetScenePath("MainCamera")).Instantiate<BMCamera3D>();
            NetworkDataManager.Instance.AddSelfNode(GetNode("CameraHolder"), Camera);
            Camera.MakeCurrent();
            Helper.Instance.LocalPlayer = this;
            Helper.Instance.LocalCamera = Camera;
            
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Vector2 direction = Input.GetVector("a", "d", "w", "s");
        if (NetworkTransform.IsOwnedByThisInstance)
        {
            if (direction != Vector2.Zero)
            {
                Move(direction);
            }

        }


    }

}
