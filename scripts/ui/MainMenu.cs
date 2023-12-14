using Godot;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class MainMenu : Control
{
    
    BMButton HostLobby;
    BMButton JoinLobby;
    BMButton RefreshButton;
    Lobby lobbyFound;

    CheckBox box;

    Stopwatch sw = new Stopwatch();
    bool lobbyWasFound = false;

    public override void _Ready()
    {
        base._Ready();

        SteamManager.Instance.GetMultiplayerLobbies();
        SteamManager.Instance.OnLobbyRefreshCompleted += OnLobbyRefresh;

        HostLobby = GetNode<BMButton>("HostButton");
        JoinLobby = GetNode<BMButton>("JoinButton");
        box = GetNode<CheckBox>("CheckBox");

        HostLobby.Pressed += CreateNewLobby;
        JoinLobby.Pressed += JoinLobbyM;
        sw.Start();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if(sw.Elapsed.TotalSeconds > 1)
        {
            SteamManager.Instance.GetMultiplayerLobbies();
            sw.Restart();
        }
    }

    private void JoinLobbyM()
    {
        if (lobbyWasFound)
        {
            LevelManager.Instance.CloseLevelPartitionWithoutSaving("MainMenu");
            lobbyFound.Join();
        }
    }

    private void OnLobbyRefresh(List<Lobby> list)
    {
        if(list.Count > 0)
        {
            lobbyFound = list[0];
            lobbyWasFound=true;
        }
        else
        {
            box.ButtonPressed = false;
        }
    }


    private void CreateNewLobby()
    {
        SaveManager.Instance.CreateSave("DefaultSave");
        LevelManager.Instance.CloseLevelPartitionWithoutSaving("MainMenu");

        LevelManager.Instance.LoadLevelPartition("World");
        LevelManager.Instance.InstantiatePlayer(SteamManager.Instance.PlayerId, SteamManager.Instance.PlayerName);

        SteamManager.Instance.CreateLobby("Default");
    }
    
}
