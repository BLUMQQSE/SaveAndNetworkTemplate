using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;

public partial class LevelManager : Node
{
    private static LevelManager instance;
    public static LevelManager Instance { get { return instance; } }

    public Dictionary<string, LevelPartition> LevelPartitions { get; private set; } = new Dictionary<string, LevelPartition>();
    public List<string> AllLevels { get; private set; } = new List<string>();

    public event Action<Node> PlayerLoaded;

    bool[] PositionsOccupied { get; set; }
    private int MaxConcurrentLevels { get; set; } = 4;
    private bool UseOffsets { get; set; } = true;
    private List<string> PartitionsQueuedClose = new List<string>();
    private List<(string, Node, bool)> PlayersQueuedToMove = new List<(string, Node, bool)>();

    public LevelManager() 
    {
        instance = this;
        PositionsOccupied = new bool[MaxConcurrentLevels];
        AddToGroup(Globals.Groups.AutoLoad.ToString());
    }

    public override void _Ready()
    {
        base._Ready();
        
        SaveManager.Instance.LoadingPlayerSaveComplete += LoadingPlayerSaveCompleteCallback;
        SaveManager.Instance.LoadingSaveComplete += LoadingSaveCompleteCallback;
        SaveManager.Instance.SavingSaveComplete += SavingSaveCompleteCallback;
        SaveManager.Instance.ChangedSave += ChangedSaveCallback;

        LoadLevelPartition("MainMenu");
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public bool HasLocalPlayers(string sceneName)
    {
        if (LevelPartitions.ContainsKey(sceneName))
            return LevelPartitions[sceneName].LocalPlayers.Count > 0;

        return false;
    }

    public bool LevelNameExists(string levelName)
    {
        return LevelPartitions.ContainsKey(levelName);
    }

    public void ConvertNodeToLevel(Node levelToBe)
    {
        if (LevelNameExists(levelToBe.Name))
            throw new Exception(levelToBe.Name + " cannot be converted to a level, a level with this name already exists");

        SaveManager.Instance.Save(levelToBe);
    }

    public void CreateLevelFromOriginalLevel(string levelOriginalName, string newLevelName)
    {
        if (!ResourceManager.Instance.LevelPaths.ContainsKey(levelOriginalName))
            throw new Exception(levelOriginalName + " is not an og level");
        Node root;

        root = GD.Load<PackedScene>(ResourceManager.Instance.GetLevelPath(levelOriginalName)).Instantiate<Node>();
        root.Name = newLevelName;
        SaveManager.Instance.Save(root);

        root.SafeQueueFree();
    }

    public void CreateLevelFromLevel(string levelName, string newLevelName)
    {
        if (!AllLevels.Contains(levelName))
            throw new Exception(levelName + " is not a level");
        if (AllLevels.Contains(newLevelName))
            throw new Exception(newLevelName + " is already a level");
        if (!LevelPartitions.ContainsKey(levelName))
            throw new Exception(levelName + " is not open to copy");

        Node root = LevelPartitions[levelName].Root.Duplicate(14);
        SaveManager.Instance.Save(root);

        root.SafeQueueFree();
    }

    public void CreateLevelFromScene(string sceneName, string newLevelName)
    {
        if (!ResourceManager.Instance.ScenePaths.ContainsKey(sceneName))
            throw new Exception(sceneName + " is not an og level");
        Node root;

        root = GD.Load<PackedScene>(ResourceManager.Instance.GetScenePath(sceneName)).Instantiate<Node>();
        root.Name = newLevelName;
        SaveManager.Instance.Save(root);

        root.SafeQueueFree();
    }

    private void TransferPlayer(Node player, string scene, bool firstLoad)
    {
        if(!LevelPartitions.ContainsKey(scene))
        {
            PlayersQueuedToMove.Add((scene, player, firstLoad));
            return;
        }

        string currentScene = player.GetMeta(Globals.Meta.LevelPartitionName.ToString()).ToString();
        if (firstLoad)
        {
            if(player is Node3D pp3)
            {
                LevelPartitions[scene].AddPlayer(pp3);
                pp3.Position += LevelPartitions[currentScene].Offset;
            }
            else if(player is Node2D pp2)
            {
                LevelPartitions[scene].AddPlayer(pp2);
                pp2.Position += new Vector2(LevelPartitions[currentScene].Offset.X, LevelPartitions[currentScene].Offset.Z);
            }

            NetworkDataManager.Instance.AddServerNode(GetTree().CurrentScene, player);

            return;
        }

        if (player is Node3D p3)
        {
            p3.Position -= LevelPartitions[currentScene].Offset;
            LevelPartitions[currentScene].RemovePlayer(p3);
            LevelPartitions[scene].AddPlayer(p3);
            p3.Position += LevelPartitions[scene].Offset;
        }
        else if(player is Node2D p2)
        {
            p2.Position -= new Vector2(LevelPartitions[currentScene].Offset.X, LevelPartitions[currentScene].Offset.Z);
            LevelPartitions[currentScene].RemovePlayer(p2);
            LevelPartitions[scene].AddPlayer(p2);
            p2.Position += new Vector2(LevelPartitions[scene].Offset.X, LevelPartitions[scene].Offset.Z);
        }
    }

    /// <summary>
    /// Converts a location in local units to scene's true position.
    /// Eg. Local Pos: (0, 20, 0), Scene Pos: (5000, 20, 0)
    /// </summary>
    /// <returns></returns>
    public Vector3 LocalPos2ScenePos(Vector3 position, string scene)
    {
        return position + LevelPartitions[scene].Offset;
    }

    public void InstantiatePlayer(ulong ownerId, string name)
    {
        if (FileManager.FileExists("saves/" + SaveManager.Instance.CurrentSave + "/players/" + ownerId.ToString()))
        {
            SaveManager.Instance.Load(ownerId.ToString(), true);
            return;
        }
        string levelPartition = SaveManager.Instance.StartUpScene;
        Node player = GD.Load<PackedScene>(ResourceManager.Instance.GetScenePath("Player")).Instantiate();
        player.SetMeta(Globals.Meta.OwnerId.ToString(), ownerId.ToString());
        player.SetMeta(Globals.Meta.LevelPartitionName.ToString(), levelPartition);
        player.Name = name;

        SaveManager.Instance.Save(player);
        

        if (!player.HasMeta(Globals.Meta.LevelPartitionName.ToString()))
            player.SetMeta(Globals.Meta.LevelPartitionName.ToString(), SaveManager.Instance.StartUpScene);

        LoadingPlayerSaveCompleteCallback(player);
        PlayerLoaded?.Invoke(player);
    }

    public void LoadLevelPartition(string levelName)
    {
        if (LevelPartitions.ContainsKey(levelName))
            return;

        if (LevelPartitions.Count >= MaxConcurrentLevels)
        {
            GD.Print("Attempting to load too many scenes, reach MaxConcurrentScenes limit of " + MaxConcurrentLevels);
            return;
        }
        SaveManager.Instance.Load(levelName);
    }

    public void SaveLevelPartition(string levelName)
    {
        if (!LevelPartitions.ContainsKey(levelName)) { return; }

        LevelPartition sp = LevelPartitions[levelName];

        // save data of the scene and all LocalPlayers within it
        if (sp.Root is Node2D r2)
            r2.Position -= new Vector2(sp.Offset.X, sp.Offset.Y);
        if (sp.Root is Node3D r3)
            r3.Position -= sp.Offset;

        SaveManager.Instance.Save(sp.Root);

        if (sp.Root is Node2D r22)
            r22.Position += new Vector2(sp.Offset.X, sp.Offset.Y);
        if (sp.Root is Node3D r33)
            r33.Position += sp.Offset;
       
        foreach (Node p in sp.LocalPlayers)
        {
            if (p is Node2D p2)
                p2.Position -= new Vector2(sp.Offset.X, sp.Offset.Y);
            if (p is Node3D p3)
                p3.Position -= sp.Offset;

            SaveManager.Instance.Save(p);

            if (p is Node2D p22)
                p22.Position += new Vector2(sp.Offset.X, sp.Offset.Y);
            if (p is Node3D p33)
                p33.Position += sp.Offset;
        }

    }
    
    public void SaveAndCloseLevelPartition(string levelName)
    {
        if (!LevelPartitions.ContainsKey(levelName)) { return; }
        PartitionsQueuedClose.Add(levelName);
        SaveLevelPartition(levelName);
    }

    public void CloseLevelPartitionWithoutSaving(string levelName)
    {
        if (!LevelPartitions.ContainsKey(levelName)) { return; }
        LevelPartitions.Remove(levelName);
        NetworkDataManager.Instance.RemoveServerNode(GetTree().CurrentScene.GetNode(levelName));
    }

    public void AddNode(Node owner, Node node)
    {
        string sceneName = node.Name;
        if (owner.HasMeta(Globals.Meta.LevelPartitionName.ToString()))
            sceneName = owner.GetMeta(Globals.Meta.LevelPartitionName.ToString()).ToString();

        if (node.HasMeta(Globals.Meta.OwnerId.ToString()))
        {
            if (!node.HasMeta(Globals.Meta.LevelPartitionName.ToString()))
                node.SetMeta(Globals.Meta.LevelPartitionName.ToString(), SaveManager.Instance.StartUpScene);

            sceneName = node.GetMeta(Globals.Meta.LevelPartitionName.ToString()).ToString();

            if (node is Node2D n2)
                n2.Position += new Vector2(LevelPartitions[sceneName].Offset.X, LevelPartitions[sceneName].Offset.Y);
            if (node is Node3D n3)
                n3.Position += LevelPartitions[sceneName].Offset;

            LevelPartitions[sceneName].AddPlayer(node as Player);
        }
        else
        {
            LevelPartitions[sceneName].AddNode(node);
        }

    }

    public void RemoveNode(Node node)
    {
        string scene = node.GetMeta(Globals.Meta.LevelPartitionName.ToString()).ToString();

        if (node.HasMeta(Globals.Meta.OwnerId.ToString()))
        {
            if (node is Node2D n2)
                n2.Position -= new Vector2(LevelPartitions[scene].Offset.X, LevelPartitions[scene].Offset.Y);
            if (node is Node3D n3)
                n3.Position -= LevelPartitions[scene].Offset;

            SaveManager.Instance.Save(node);

            LevelPartitions[scene].RemovePlayer(node as Player);
        }
        else
        {
            // safety check to verify partition has not been fully closed already
            if (LevelPartitions.ContainsKey(scene))
                LevelPartitions[scene].RemoveNode(node);
        }
    }

    private void CollectAllLevels()
    {
        AllLevels.Clear();
        List<string> list = FileManager.GetFiles("saves/"+SaveManager.Instance.CurrentSave+"/levels");
        for(int i = 0; i < list.Count; i++)
        {
            int lastIndex = list[i].Find(".");
            list[i] = list[i].Substring(0, lastIndex);
        }
        AllLevels = list;
        
    }

    private void SavingSaveCompleteCallback(string obj) { CallDeferred(nameof(SavingSaveCompleteCallbackDeferred), obj); }
    private void SavingSaveCompleteCallbackDeferred(string obj)
    {
        CollectAllLevels();

        if (PartitionsQueuedClose.Count == 0)
            return;

        if (PartitionsQueuedClose.Contains(obj))
        {
            PartitionsQueuedClose.Remove(obj);
            LevelPartitions.Remove(obj);

            NetworkDataManager.Instance.RemoveServerNode(GetTree().CurrentScene.GetNode(obj));
        }
    }

    private void LoadingSaveCompleteCallback(Node node) { CallDeferred(nameof(LoadingSaveCompleteCallbackDeferred), node); }
    private void LoadingSaveCompleteCallbackDeferred(Node node)
    {
        if (LevelPartitions.ContainsKey(node.Name))
        {
            node.SafeQueueFree();
            return;
        }
        NetworkDataManager.Instance.ApplyNextAvailableUniqueId(node);
        Vector3 offset = Vector3.Zero;
        // only apply a offset if this scene is not a Control and we want to use offsets
        int offsetIndex = -1;
        if (UseOffsets)
            if (node is not Control)
            {
                for (int i = 0; i < PositionsOccupied.Length; i++)
                {
                    if (PositionsOccupied[i] == false)
                    {
                        PositionsOccupied[i] = true;
                        offset = new Vector3(i * 5000, 0, 0);
                        offsetIndex = i;
                        break;
                    }
                }

            }

        LevelPartition lp = new LevelPartition(node.Name, node, offset);
        lp.PositionIndex = offsetIndex;

        LevelPartitions.Add(node.Name, lp);

        if (node is Node3D n3)
            n3.Position += offset;
        else if (node is Node2D n2)
            n2.Position += new Vector2(offset.X, offset.Z);

        NetworkDataManager.Instance.AddServerNode(GetTree().CurrentScene, node);

        CollectAllLevels();

        if (PlayersQueuedToMove.Count == 0)
            return;

        List<(string, Node, bool)> players = PlayersQueuedToMove.Where(cus => cus.Item1 == node.Name).ToList();
        foreach(var player in players)
        {
            PlayersQueuedToMove.Remove(player);
            TransferPlayer(player.Item2, player.Item1, player.Item3);
        }
    }

    private void LoadingPlayerSaveCompleteCallback(Node player) {  CallDeferred(nameof(LoadingPlayerSaveCompleteCallbackDeferred), player); }
    private void LoadingPlayerSaveCompleteCallbackDeferred(Node player)
    {
        NetworkDataManager.Instance.ApplyNextAvailableUniqueId(player);
        string scenePartition = player.GetMeta(Globals.Meta.LevelPartitionName.ToString()).ToString();
        TransferPlayer(player, scenePartition, true);

        // here I believe lies the issue
        //NetworkDataManager.Instance.AddServerNode(GetTree().CurrentScene, player);
        //GetTree().CurrentScene.AddChild(player);
    }


    private void ChangedSaveCallback(string priorSave) { CallDeferred(nameof(ChangedSaveCallbackDeferred), priorSave); }
    private void ChangedSaveCallbackDeferred(string priorSave) 
    {
        // TODO: clear list of levels
        // add list of new save
        CollectAllLevels();
    }

}

public class LevelPartition
{
    public LevelPartition(string levelName, Node root, Vector3 offset)
    {
        LevelName = levelName;
        Root = root;
        Offset = offset;
        AddNetworkNodes(root);
        ApplyLevelName(root);
    }
    public string LevelName { get; private set; }
    public Node Root { get; private set; }
    public Vector3 Offset { get; private set; } = new Vector3();
    public List<Node> LocalPlayers { get; private set; } = new List<Node>();
    private List<Node> NetworkNodes { get; set; } = new List<Node>();
    private List<Node> ForceUpdate { get; set; } = new List<Node>();
    public int PositionIndex { get; set; } = -1;

    public void AddPlayer(Node player)
    {
        if (LocalPlayers.Contains(player)) return;
        LocalPlayers.Add(player);
        ApplyLevelName(player);
        AddNetworkNodes(player);
    }
    public void RemovePlayer(Node player)
    {
        LocalPlayers.Remove(player);
        RemoveSceneName(player);
        RemoveNetworkNodes(player);
    }

    public void AddNode(Node node)
    {
        AddNetworkNodes(node);
        ApplyLevelName(node);
    }

    public void RemoveNode(Node node)
    {
        RemoveNetworkNodes(node);
        RemoveSceneName(node);
    }

    public void AddForceUpdate(Node node) { ForceUpdate.Add(node); }

    public JsonValue GetNetworkUpdate()
    {
        JsonValue data = new JsonValue();
        foreach (Node node in NetworkNodes)
        {
            bool forceUpdate = false;
            if (ForceUpdate.Contains(node))
                forceUpdate = true;

            data["NetworkNodes"][node.GetMeta(Globals.Meta.UniqueId.ToString()).ToString()]
                .Set((node as INetworkData).SerializeNetworkData(forceUpdate));
        }
        ForceUpdate.Clear();
        return data;
    }

    private void AddNetworkNodes(Node root)
    {
        if (root is INetworkData && !root.IsInGroup(Globals.Groups.SelfOnly.ToString()))
            if (!NetworkNodes.Contains(root))
                NetworkNodes.Add(root);

        foreach (Node node in root.GetChildren())
            AddNetworkNodes(node);
    }
    private void RemoveNetworkNodes(Node root)
    {
        if (NetworkNodes.Contains(root))
            NetworkNodes.Remove(root);

        foreach (Node node in root.GetChildren())
            RemoveNetworkNodes(node);
    }
    private void ApplyLevelName(Node root)
    {
        root.SetMeta(Globals.Meta.LevelPartitionName.ToString(), LevelName);

        foreach (Node node in root.GetChildren())
            ApplyLevelName(node);
    }
    private void RemoveSceneName(Node root)
    {
        if (root.GetMeta(Globals.Meta.LevelPartitionName.ToString()).ToString() == LevelName)
            root.RemoveMeta(Globals.Meta.LevelPartitionName.ToString());

        foreach (Node node in root.GetChildren())
            RemoveSceneName(node);
    }

}