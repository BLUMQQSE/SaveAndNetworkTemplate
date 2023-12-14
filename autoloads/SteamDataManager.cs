using Godot;
using Steamworks;
using System;

public partial class SteamDataManager : NetworkDataManager, IListener
{
    public override void _Ready()
    {
        base._Ready();
        EventSystem.Instance.Subscribe(EventID.OnPlayerConnectedToServer_Server, this);
        SaveManager.Instance.LoadingPlayerSaveComplete += OnPlayerJoined;
        LevelManager.Instance.PlayerLoaded += OnPlayerJoined;
    }

    private void OnPlayerJoined(Node node)
    {
        CallDeferred(nameof(OnPlayerJoinedDeferred), node);
    }
    private void OnPlayerJoinedDeferred(Node node)
    {
        ulong ownerId = Convert.ToUInt64(node.GetMeta(Globals.Meta.OwnerId.ToString()).ToString());
        OwnerIdToPlayer.Add(ownerId, node);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        EventSystem.Instance.UnsubscribeAll(this);
    }

    public override void SendToClientId(ulong client, JsonValue data, bool reliable = true)
    {
        if (!SteamManager.Instance.IsServer) { return; }

        SteamManager.Instance.SocketManager.SendMessage((SteamId)client, data.ToString(), reliable);
    }

    public override void SendToClients(JsonValue data, bool reliable = true)
    {
        if (!SteamManager.Instance.IsServer) { return; }
        // if only server is connected, return
        if (SteamManager.Instance.SocketConnections < 2) { return; }

        SteamManager.Instance.SocketManager.SendBroadcast(data.ToString(), reliable);
    }

    public override void SendToServer(JsonValue data, bool reliable = true)
    {
        if (SteamManager.Instance.IsServer) { return; }

        SteamManager.Instance.ConnectionManager.SendMessage(data.ToString(), reliable);
    }

    private void OnPlayerConnectedToServer(Friend friend)
    {
        if (SteamManager.Instance.IsServer)
        {
            LevelManager.Instance.InstantiatePlayer((ulong)friend.Id, friend.Name);
            JsonValue fullData = FullServerData();
            SendToClientId(friend.Id, fullData);
        }
    }

    public void HandleEvent(Event e)
    {
        base.HandleEvent(e);
        switch (e.IDAsEvent)
        {
            case EventID.OnPlayerConnectedToServer_Server:
                OnPlayerConnectedToServer((Friend)e.Parameter);
                break;
        }
    }

}
