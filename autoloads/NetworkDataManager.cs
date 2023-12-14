using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class ServerRpc : Attribute { }
public abstract partial class NetworkDataManager : Node, IListener
{
    protected static NetworkDataManager instance;
    public static NetworkDataManager Instance { get { return instance; } }
    public NetworkDataManager() 
    {
        instance = this;
        AddToGroup(Globals.Groups.AutoLoad.ToString());
    }

    public Dictionary<ulong, Node> OwnerIdToPlayer { get; private set; } = new Dictionary<ulong, Node>();
    private Stopwatch UpdateTimer { get; set; } = new Stopwatch();
    public float UpdateIntervalInMil { get; private set; } = 50f;

    #region Flags
    private bool RecievedFullServerData = false;
    #endregion

    #region UniqueId

    private uint nextAvailableSelfUniqueId = uint.MaxValue - 10000000;
    private uint nextAvailableUniqueId = 0;

    private Dictionary<uint, Node> uniqueIdToNode = new Dictionary<uint, Node>();


    public Node UniqueIdToNode(uint id)
    {
        if (uniqueIdToNode.ContainsKey(id))
            return uniqueIdToNode[id];
        return null;
    }
    public T UniqueIdToNode<T>(uint id)
    {
        Node n = UniqueIdToNode(id);
        if (n == null)
            return default(T);
        if (n is T)
        {
            return (T)Convert.ChangeType(n, typeof(T));
        }
        return default;
    }

    public void ApplyNextAvailableUniqueId(Node node)
    {
        if (!node.HasMeta(Globals.Meta.UniqueId.ToString()))
        {
            uint result = nextAvailableUniqueId;
            nextAvailableUniqueId++;
            node.SetMeta(Globals.Meta.UniqueId.ToString(), result.ToString());

            uniqueIdToNode[result] = node;
        }
        foreach (Node child in node.GetChildren())
        {
            ApplyNextAvailableUniqueId(child);
        }
    }

    public void ApplyNextAvailableSelfUniqueId(Node node)
    {
        node.AddToGroup(Globals.Groups.SelfOnly.ToString());
        if (!node.HasMeta(Globals.Meta.UniqueId.ToString()))
        {
            uint result = nextAvailableSelfUniqueId;
            nextAvailableSelfUniqueId++;
            node.SetMeta(Globals.Meta.UniqueId.ToString(), result.ToString());

            uniqueIdToNode[result] = node;
        }
        foreach (Node child in node.GetChildren())
        {
            ApplyNextAvailableSelfUniqueId(child);
        }
    }

    #endregion

    #region LevellessNetworkData

    private List<INetworkData> LevellessNetworkNode = new List<INetworkData>();
    public void AddLevellessNetworkNode(INetworkData node)
    {
        if (!LevellessNetworkNode.Contains(node))
            LevellessNetworkNode.Add(node);
    }
    public void RemoveLevellessNetworkNode(INetworkData node)
    {
        LevellessNetworkNode.Remove(node);
    }

    #endregion

    public override void _Ready()
    {
        base._Ready();

        UpdateTimer.Start();
        EventSystem.Instance.Subscribe(EventID.OnConnectionRecievedData, this);
        EventSystem.Instance.Subscribe(EventID.OnSocketRecievedData, this);
        EventSystem.Instance.Subscribe(EventID.OnServerGameLoaded, this);
        ApplyNextAvailableUniqueId(GetTree().Root);
    }
    public override void _ExitTree()
    {
        base._ExitTree();
        EventSystem.Instance.UnsubscribeAll(this);
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!NetworkManager.Instance.IsServer) return;
        if (NetworkManager.Instance.SocketConnections < 2) return;

        if (UpdateTimer.Elapsed.TotalMilliseconds >= UpdateIntervalInMil)
        {
            /*
             * 1) Request all clients ` their player data
             * 2) Server updates itself with info
             * 3) Server sends the full scene back out to all players
             * 
             */

            JsonValue scenelessData = new JsonValue();
            scenelessData["DataType"].Set((int)Globals.DataType.ServerUpdate);
            foreach (var n in LevellessNetworkNode)
            {
                scenelessData["NetworkNodes"][(n as Node).GetMeta(Globals.Meta.UniqueId.ToString()).ToString()]
                .Set((n).SerializeNetworkData(false));
            }
            if (scenelessData["NetworkNodes"].ToString() != "{}" && scenelessData["NetworkNodes"].ToString() != "null")
                SendToClients(scenelessData);

            foreach (var sp in LevelManager.Instance.LevelPartitions.Values)
            {
                if (sp.LocalPlayers.Count == 0)
                {
                    GD.Print("Save and close scene");
                    LevelManager.Instance.SaveAndCloseLevelPartition(sp.LevelName);
                    continue;
                }

                JsonValue spData = sp.GetNetworkUpdate();
                spData["DataType"].Set((int)Globals.DataType.ServerUpdate);

                if (spData["NetworkNodes"].ToString() != "{}" && spData["NetworkNodes"].ToString() != "null")
                    SendToClients(spData);

            }

            JsonValue data = new JsonValue();
            data["DataType"].Set((int)Globals.DataType.ClientUpdateReminder);
            SendToClients(data);
            UpdateTimer.Restart();
        }
    }

    public abstract void SendToServer(JsonValue data, bool reliable = true);
    public abstract void SendToClientId(ulong client, JsonValue data, bool reliable = true);
    public abstract void SendToClients(JsonValue data, bool reliable = true);

    #region AddRemoveNode

    public void AddSelfNode(Node owner, Node newNode)
    {
        ApplyNextAvailableSelfUniqueId(newNode);
        owner.AddChild(newNode, true);
    }

    public void RemoveSelfNode(Node node)
    {
        node.SafeQueueFree();
    }

    public void AddServerNode(Node owner, Node newNode, Vector3 positionOverride = new Vector3(), bool persistent = true)
    {
        if (!NetworkManager.Instance.IsServer)
        {
            throw new Exception("ERROR: CLIENT IS TRYING TO CALL AddServerNode");
        }

        bool levelless = false;
        if (!owner.HasMeta(Globals.Meta.LevelPartitionName.ToString()))
        {
            if (!LevelManager.Instance.LevelPartitions.ContainsKey(newNode.Name))
                levelless = true;
        }


        if (!persistent || levelless)
            newNode.AddToGroup(Globals.Groups.NotPersistent.ToString());

        if (positionOverride != Vector3.Zero)
        {
            if (newNode is Node2D n2d)
                n2d.Position = new Vector2(positionOverride.X, positionOverride.Y);

            else if (newNode is Control c)
                c.Position = new Vector2(positionOverride.X, positionOverride.Y);
            else if (newNode is Node3D n3d)
                n3d.Position = positionOverride;
        }

        ApplyNextAvailableUniqueId(newNode);
        owner.AddChild(newNode, true);

        if (!newNode.IsInGroup(Globals.Groups.NotPersistent.ToString()) && !levelless)
            LevelManager.Instance.AddNode(owner, newNode);

        JsonValue data = new JsonValue();

        data["DataType"].Set((int)Globals.DataType.ServerAdd);

        // need to collect all data about the node and send to clients
        data["Owner"].Set(owner.GetMeta(Globals.Meta.UniqueId.ToString()).ToString());

        data["Node"].Set(ConvertNodeToJson(newNode));


        SendToClients(data);
    }

    public void RemoveServerNode(Node removeNode)
    {
        if (!NetworkManager.Instance.IsServer)
        {
            throw new Exception("ERROR: CLIENT IS TRYING TO CALL RemoveNode");
        }
        if (removeNode is INetworkData ind)
        {
            LevellessNetworkNode.Remove(ind);
        }
        JsonValue data = new JsonValue();
        data["UniqueId"].Set(removeNode.GetMeta(Globals.Meta.UniqueId.ToString()).ToString());

        removeNode.SafeQueueFree();
        if (!removeNode.IsInGroup(Globals.Groups.NotPersistent.ToString()))
            LevelManager.Instance.RemoveNode(removeNode);
        // tell all clients to queue free this node
        data["DataType"].Set((int)Globals.DataType.ServerRemove);

        SendToClients(data);

    }

    #endregion

    #region Server
    private void OnSocketDataRecieved(JsonValue value)
    {
        Globals.DataType dataType = (Globals.DataType)value["DataType"].AsInt();

        switch (dataType)
        {
            case Globals.DataType.RpcCall:
                HandleRpc(value);
                break;
            case Globals.DataType.ClientUpdate:
                HandleClientUpdate(value);
                break;
            case Globals.DataType.ClientInputUpdate:
                {
                    //uint id = Convert.ToUInt32(value[Globals.Meta.UniqueId.ToString()].AsString());
                    //UniqueIdToNode<NetworkInput>(id).HandleClientInputUpdate(value);
                }
                break;
        }
    }

    protected JsonValue FullServerData()
    {
        JsonValue data = new JsonValue();
        data["DataType"].Set((int)Globals.DataType.FullServerData);

        foreach (Node child in GetTree().CurrentScene.GetChildren())
        {
            JsonValue nodeData = ConvertNodeToJson(child);
            data["Nodes"].Append(nodeData);
            AddNodeToUniqueIdDict(child);
        }

        return data;
    }

    private void HandleClientUpdate(JsonValue data)
    {
        Node clientPlayer = UniqueIdToNode(Convert.ToUInt32(data["UniqueId"].AsString()));

        NetworkTransform pa = clientPlayer.GetChildOfType<NetworkTransform>();
        if (data["D"].AsInt() == 3)
        {
            Vector3 syncPos = data["Pos"].AsVector3();
            Node3D player = clientPlayer as Node3D;

            if (!pa.TrustClientPos)
            {
                if (data["TCP"].AsBool() == false)
                {
                    pa.TrustClientPos = true;
                    pa.SyncPos = player.Position;
                    pa.SyncRot = player.Rotation;
                    return;
                }
            }

            if (pa.TrustClientPos)
            {
                if (syncPos.DistanceSquaredTo(player.Position) > Mathf.Pow(pa.MaxOffsetPermitted, 2))
                {
                    // ignore clients position and fix i
                    pa.SyncPos = player.Position;
                }
                else
                {
                    // acknowledge player position and set sync to it
                    pa.SyncPos = syncPos;
                }
            }
            else
            {
                pa.SyncPos = player.Position;
                pa.SyncRot = player.Rotation;
            }
            pa.SyncRot = data["Rot"].AsVector3();
        }
        else
        {
            Vector3 syncPos = data["Pos"].AsVector3();
            Node2D player = clientPlayer as Node2D;
            Vector3 playerPos = new Vector3(player.Position.X, player.Position.Y, 0);

            if (!pa.TrustClientPos)
            {
                if (data["TCP"].AsBool() == false)
                {
                    pa.TrustClientPos = true;
                    pa.SyncPos = playerPos;
                    pa.SyncRot = new Vector3(player.Rotation, 0, 0);
                    return;
                }
            }

            if (pa.TrustClientPos)
            { 
                if (syncPos.DistanceSquaredTo(playerPos) > Mathf.Pow(pa.MaxOffsetPermitted, 2))
                {
                    // ignore clients position and fix i
                    pa.SyncPos = playerPos;
                }
                else
                {
                    // acknowledge player position and set sync to it
                    pa.SyncPos = syncPos;
                }
            }
            else
            {
                pa.SyncPos = playerPos;
                pa.SyncRot = new Vector3(player.Rotation, 0, 0);
            }
            pa.SyncRot = data["Rot"].AsVector3();
        }
    }

    #endregion

    #region Client
    private void OnConnectionDataRecieved(JsonValue value)
    {
        Globals.DataType dataType = (Globals.DataType)value["DataType"].AsInt();

        switch (dataType)
        {
            case Globals.DataType.ServerUpdate:
                HandleServerUpdate(value);
                break;
            case Globals.DataType.ServerAdd:
                HandleServerAdd(value);
                break;
            case Globals.DataType.ServerRemove:
                // TODO: Add logic to find node of unique

                string uniqueIdStr = value["UniqueId"].AsString();
                uint uniqueId = uint.Parse(uniqueIdStr);
                Node removeNode = uniqueIdToNode[uniqueId];
                removeNode.SafeQueueFree();
                uniqueIdToNode.Remove(uniqueId);
                break;
            case Globals.DataType.FullServerData:
                HandleFullServerData(value);
                break;
            case Globals.DataType.ClientUpdateReminder:
                EventSystem.Instance.PushEvent(EventID.OnNetworkUpdate_Client);
                break;
        }
    }


    private void HandleServerAdd(JsonValue data)
    {
        if (!RecievedFullServerData)
            return;

        // Client does not know who owner is, so we'll ignore this add for now
        // currently an issue when player first joins
        if (!uniqueIdToNode.ContainsKey(Convert.ToUInt32(data["Owner"].AsString())))
        {
            GD.Print("HandleServerAdd: We dont know ID: " + data["Owner"].AsString());
            return;
        }

        string uniqueIdStr = data["Node"]["Meta"][Globals.Meta.UniqueId.ToString()].AsString();
        uint uId = Convert.ToUInt32(uniqueIdStr);
        if (uniqueIdToNode.ContainsKey(uId))
        {
            GD.Print("I already know about this node?");
            // client already knows about this object, dont add again
            return;
        }

        Node node = ConvertJsonToNode(data["Node"]);
        Node owner = uniqueIdToNode[Convert.ToUInt32(data["Owner"].AsString())];

        AddNodeToUniqueIdDict(node);

        owner.CallDeferred("add_child", node, true);
    }

    public void ClientUpdate(Node client)
    {
        JsonValue data = new JsonValue();
        data["DataType"].Set((int)Globals.DataType.ClientUpdate);

        data["UniqueId"].Set(client.GetMeta(Globals.Meta.UniqueId.ToString()).ToString());
        if (client is Node3D p3)
        {
            data["D"].Set(3);
            data["Pos"].Set(p3.Position);
            data["Rot"].Set(p3.Rotation);
        }
        else if(client is Node2D p2)
        {
            data["D"].Set(2);
            data["Pos"].Set(new Vector3(p2.Position.X, p2.Position.Y, 0));
            data["Rot"].Set(new Vector3(p2.Rotation, 0, 0));
        }
        data["TCP"].Set(client.GetChildOfType<NetworkTransform>().TrustClientPos);

        SendToServer(data, false);
    }

    public void ClientInputUpdate(JsonValue playerInputData)
    {
        playerInputData["DataType"].Set((int)Globals.DataType.ClientInputUpdate);
        SendToServer(playerInputData);
    }

    private void HandleServerUpdate(JsonValue data)
    {
        if (!RecievedFullServerData) return;
        // first we verify we have all these nodes in our instance
        foreach (var item in data["NetworkNodes"].Object)
        {
            if (!uniqueIdToNode.ContainsKey(Convert.ToUInt32(item.Key)))
            {
                //GD.Print("Client does not recognize: "+item.Key);
                Node n = null;
                bool found = SearchForNode(item.Key, ref n, GetTree().Root);

                if (!found)
                    return;
                else
                    uniqueIdToNode[Convert.ToUInt32(item.Key)] = n;
            }
        }

        foreach (var item in data["NetworkNodes"].Object)
        {
            INetworkData n = uniqueIdToNode[Convert.ToUInt32(item.Key)] as INetworkData;

            if (n == null)
            {
                return;
            }
            n.DeserializeNetworkData(item.Value);
        }
    }

    private void HandleFullServerData(JsonValue data)
    {
        RecievedFullServerData = true;
        foreach (var item in data["Nodes"].Array)
        {
            uint id = Convert.ToUInt32(item["Meta"][Globals.Meta.UniqueId.ToString()].AsString());
            if (!uniqueIdToNode.ContainsKey(id))
                GetTree().CurrentScene.AddChild(ConvertJsonToNode(item));
        }
    }

    #endregion

    #region RPC
    public JsonValue RpcServer(Node caller, string methodName, params Variant[] param)
    {
        JsonValue message = new JsonValue();
        message["DataType"].Set((int)Globals.DataType.RpcCall);
        message["Caller"].Set((string)caller.GetMeta(Globals.Meta.UniqueId.ToString()));
        message["MethodName"].Set(methodName);

        foreach (Variant variant in param)
        {
            message["Params"].Append(Helper.Instance.VariantToJson(variant));
        }

        SendToServer(message);
        return message;
    }

    /// <summary>
    /// Server Handles an Rpc call
    /// </summary>
    private void HandleRpc(JsonValue value)
    {
        Node node = uniqueIdToNode[uint.Parse(value["Caller"].AsString())];
        string methodName = value["MethodName"].AsString();

        List<Variant> args = new List<Variant>();
        foreach (JsonValue variant in value["Params"].Array)
        {
            Variant variantToAdd = new Variant();
            variantToAdd = Helper.Instance.JsonToVariant(variant);
            args.Add(variantToAdd);
        }

        switch (args.Count)
        {
            case 0:
                node.Call(methodName);
                break;
            case 1:
                node.Call(methodName, args[0]);
                break;
            case 2:
                node.Call(methodName, args[0], args[1]);
                break;
            case 3:
                node.Call(methodName, args[0], args[1], args[2]);
                break;
            case 4:
                node.Call(methodName, args[0], args[1], args[2], args[3]);
                break;
            case 5:
                node.Call(methodName, args[0], args[1], args[2], args[3], args[4]);
                break;
            case 6:
                node.Call(methodName, args[0], args[1], args[2], args[3], args[4], args[5]);
                break;
            case 7:
                node.Call(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
                break;
            case 8:
                node.Call(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6],
                    args[7]);
                break;
            case 9:
                node.Call(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6],
                    args[7], args[8]);
                break;
            case 10:
                node.Call(methodName, args[0], args[1], args[2], args[3], args[4], args[5], args[6],
                    args[7], args[8], args[9]);
                break;
        }

    }
    #endregion


    #region HelperFunctions

    void AddNodeToUniqueIdDict(Node node)
    {
        string uniqueStr = node.GetMeta(Globals.Meta.UniqueId.ToString()).ToString();
        uint id = uint.Parse(uniqueStr);
        uniqueIdToNode[id] = node;
        foreach (Node n in node.GetChildren())
            AddNodeToUniqueIdDict(n);
    }
    private bool SearchForNode(string id, ref Node reference, Node searchPoint)
    {
        if (searchPoint.GetMeta(Globals.Meta.UniqueId.ToString()).ToString() == id)
        {
            reference = searchPoint;
            return true;
        }
        foreach (Node child in searchPoint.GetChildren())
        {
            if (SearchForNode(id, ref reference, child))
                return true;
        }

        return false;
    }

    #endregion

    #region NodeJsonConversion

    public static JsonValue ConvertNodeToJson(Node node)
    {
        JsonValue val = CollectNodeData(node);
        return val;
    }

    private static JsonValue CollectNodeData(Node node)
    {
        JsonValue jsonNode = new JsonValue();

        if (node.IsInGroup(Globals.Groups.SelfOnly.ToString()))
            return new JsonValue();

        jsonNode["Name"].Set(node.Name);
        jsonNode["Type"].Set(RemoveNamespace(node.GetType().ToString()));
        jsonNode["DerivedType"].Set(node.GetClass());
        if (node is Node2D)
        {
            Node2D node2d = (Node2D)node;
            jsonNode["ZIsRelative"].Set(node2d.ZAsRelative);
            jsonNode["YSortEnabled"].Set(node2d.YSortEnabled);
            jsonNode["ZIndex"].Set(node2d.ZIndex);

            jsonNode["Position"].Set(node2d.Position);
            jsonNode["Rotation"].Set(node2d.Rotation);
            jsonNode["Scale"].Set(node2d.Scale);
        }
        else if (node is Control c)
        {
            jsonNode["Position"].Set(c.Position);
            jsonNode["Rotation"].Set(c.Rotation);
            jsonNode["Scale"].Set(c.Scale);
            jsonNode["Size"].Set(c.Size);
        }
        else if (node is Node3D)
        {
            Node3D node3d = (Node3D)node;

            jsonNode["Position"].Set(node3d.Position);
            jsonNode["Rotation"].Set(node3d.Rotation);
            jsonNode["Scale"].Set(node3d.Scale);

        }

        foreach (string meta in node.GetMetaList())
        {
            jsonNode["Meta"][meta].Set((string)node.GetMeta(meta));
        }
        foreach (string group in node.GetGroups())
            jsonNode["Group"].Append(group);

        for (int i = 0; i < node.GetChildCount(); i++)
            jsonNode["Children"].Append(CollectNodeData(node.GetChild(i)));

        if (node is INetworkData)
            jsonNode["INetworkData"].Set((node as INetworkData).SerializeNetworkData(true, true));

        return jsonNode;
    }

    public static Node ConvertJsonToNode(JsonValue data)
    {

        Node node = (Node)ClassDB.Instantiate(data["DerivedType"].AsString());

        // Set Basic Node Data
        node.Name = data["Name"].AsString();
        if (node is Node2D)
        {
            Node2D node2d = (Node2D)node;

            node2d.Position = data["Position"].AsVector2();
            node2d.Rotation = data["Rotation"].AsFloat();
            node2d.Scale = data["Scale"].AsVector2();

            node2d.ZIndex = data["ZIndex"].AsInt();
            node2d.ZAsRelative = data["ZIsRelative"].AsBool();
            node2d.YSortEnabled = data["YSortEnabled"].AsBool();
        }
        else if (node is Control c)
        {
            c.Position = data["Position"].AsVector2();
            c.Rotation = data["Rotation"].AsFloat();
            c.Scale = data["Scale"].AsVector2();
            c.Size = data["Size"].AsVector2();
        }
        else if (node is Node3D)
        {
            Node3D node3d = (Node3D)node;

            node3d.Position = data["Position"].AsVector3();
            node3d.Rotation = data["Rotation"].AsVector3();
            node3d.Scale = data["Scale"].AsVector3();

        }


        // Save node instance id to re-reference after setting script
        ulong nodeID = node.GetInstanceId();
        // if type != derived-type, a script is attached
        if (!data["Type"].AsString().Equals(data["DerivedType"].AsString()))
        {
            node.SetScript(GD.Load<Script>(ResourceManager.Instance.GetScriptPath(data["Type"].AsString())));
            // Retrive node after losing it from SetScript

        }

        node = GodotObject.InstanceFromId(nodeID) as Node;

        foreach (KeyValuePair<string, JsonValue> meta in data["Meta"].Object)
            node.SetMeta(meta.Key, meta.Value.AsString());
        foreach (JsonValue group in data["Group"].Array)
            node.AddToGroup(group.AsString());

        foreach (JsonValue child in data["Children"].Array)
            node.AddChild(ConvertJsonToNode(child));

        if (node is INetworkData ind)
            ind.DeserializeNetworkData(data["INetworkData"]);


        return node;
    }


    private static string RemoveNamespace(string name)
    {
        int index = name.RFind(".");
        if (index < 0)
            return name;
        else
            return name.Substring(index + 1, name.Length - (index + 1));
    }

    #endregion

    public void HandleEvent(Event e)
    {
        switch (e.IDAsEvent)
        {
            case EventID.OnConnectionRecievedData:
                //OnConnectionDataRecieved((JsonValue)e.Parameter);
                CallDeferred(nameof(OnConnectionDataRecieved), (JsonValue)e.Parameter);
                break;
            case EventID.OnSocketRecievedData:
                //OnSocketDataRecieved((JsonValue)e.Parameter);
                CallDeferred(nameof(OnSocketDataRecieved), (JsonValue)e.Parameter);
                break;
        }
    }

}
public interface INetworkData
{
    public JsonValue SerializeNetworkData(bool forceReturn = false, bool ignoreThisUpdateOccurred = false);
    public void DeserializeNetworkData(JsonValue data);
}