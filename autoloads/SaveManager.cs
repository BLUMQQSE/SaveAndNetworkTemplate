using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

public partial class SaveManager : Node
{
	
	private static SaveManager instance;
	public static SaveManager Instance { get { return instance; } }

	public string CurrentSave { get; private set; } = "Static";
	public string StartUpScene { get; private set; } = "World";

    public event Action<Node> LoadingSaveComplete;
    public event Action<Node> LoadingPlayerSaveComplete;
    public event Action<string> SavingSaveComplete;
    public event Action<string> ChangedSave;

    private List<(string, bool)> LoadQueue = new List<(string, bool)>();
    private bool LoadQueueHalted = false;
    private List<Node> SaveQueue = new List<Node>();
    private bool SaveQueueHalted = false;

    public SaveManager() 
	{ 
		instance = this;
        AddToGroup(Globals.Groups.AutoLoad.ToString());
    }
    public enum SaveSuccessResult
    {
        Success,
        SaveAlreadyExists
    }

    public override void _Ready()
    {
        base._Ready();

        LoadingSaveComplete += OnSaveLoaded;
        LoadingPlayerSaveComplete += OnPlayerSaveLoaded;
        FileManager.SavingFileCompleteCallback += OnFileSaved;

        CreateSave("Static");
    }
    /// <summary>
    /// Creates a save if it does not already exist. This involved creating Json files of all levels, and for the player
    /// </summary>
    /// <param name="saveName"></param>
    /// <returns></returns>
    public SaveSuccessResult CreateSave(string saveName)
	{
        string priorSave = CurrentSave;
        if (FileManager.DirExists("saves/" + saveName))
        {
            // temporary, in future would send message back asking if we should override 
            FileManager.RemoveDir("/saves/" + saveName);
            //    return SaveSuccessResult.SaveAlreadyExists;
        }

        CurrentSave = saveName;
        InstantiateScenesDir();
        InstantiateNewPlayerFile(NetworkManager.Instance.PlayerId.ToString(), NetworkManager.Instance.PlayerName);

        ChangedSave?.Invoke(priorSave);
        return SaveSuccessResult.Success;
    }
    
    public bool LoadSave(string saveName)
    {
        if (!FileManager.DirExists("saves/" + saveName))
            return false;
        string priorSave = CurrentSave;
        CurrentSave = saveName;
        ChangedSave?.Invoke(priorSave);
        return true;
    }

    // fileName should include path beyond SaveName/ (Ex. scenes/Main || players/1234)
    // SHOULD BE MULTITHREADED
    public void Save(Node rootNode)
    {
        if (SaveQueue.Contains(rootNode))
            return;
        if (SaveQueue.Count == 0 && LoadQueue.Count == 0)
        {
            SaveQueue.Add(rootNode);
            SaveFromQueue();
            return;
        }
        else if (LoadQueue.Count > 0)
        {
            if (LoadQueue[0].Item1 == rootNode.Name)
            {
                SaveQueueHalted = true;
                return;
            }
            else if (SaveQueue.Count == 1)
            {
                SaveFromQueue();
                return;
            }   
        }
        Node rootDup = rootNode.Duplicate(14);
        SaveQueue.Add(rootDup);
        
    }
    
	public void Load(string fileName, bool player = false)
	{
        if (LoadQueue.Any(m => m.Item1 == fileName))
        {
            return;
        }
        LoadQueue.Add((fileName, player));

        if (LoadQueue.Count == 1 && SaveQueue.Count == 0)
        {
            LoadFromQueue();
        }
        else if (SaveQueue.Count > 0)
        {
            if (SaveQueue[0].Name == fileName)
            {
                LoadQueueHalted = true;
            }
            else if (LoadQueue.Count == 1)
                LoadFromQueue();
        }

    }

	private void InstantiateNewPlayerFile(string fileName, string playerName)
	{
        Node player = GD.Load<PackedScene>(ResourceManager.Instance.GetScenePath("Player")).Instantiate();
        player.Name = playerName;
        player.SetMeta(Globals.Meta.OwnerId.ToString(), fileName);
        ApplySceneName(player, StartUpScene);
        FileManager.SaveToFileFormatted(SaveManager.ConvertNodeToJson(player), "saves/" + CurrentSave + "/players/" + fileName);
    }

    private void InstantiateScenesDir()
	{
        foreach (KeyValuePair<string, string> filePath in ResourceManager.Instance.LevelPaths)
        {
            if (filePath.Key == "Main")
                continue;
            // useful for when making removals of levels while developing
            if (!ResourceLoader.Exists(filePath.Value)) 
                continue;
            Node root = GD.Load<PackedScene>(filePath.Value).Instantiate() as Node;
            ApplySceneName(root, root.Name);
            JsonValue sceneData = SaveManager.ConvertNodeToJson(root);
            AddHash(ref sceneData);
            FileManager.SaveToFileFormatted(sceneData, "saves/" + CurrentSave + "/levels/" + root.Name);

            root.QueueFree();
        }
    }
    private void ApplySceneName(Node root, string sceneName)
    {
        root.SetMeta(Globals.Meta.LevelPartitionName.ToString(), sceneName);
        foreach (Node child in root.GetChildren())
            ApplySceneName(child, sceneName);
    }

    #region HASH
    static string GetHash(string inputString)
    {
        byte[] hashBytes;
        using (HashAlgorithm algorithm = SHA256.Create())
            hashBytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));

        return BitConverter
                .ToString(hashBytes)
                .Replace("-", String.Empty);
    }

    static private void AddHash(ref JsonValue obj)
    {
        obj.Remove("hash");
        string hash = GetHash(obj.ToString());
        obj["hash"].Set(hash);
    }
    private bool HashMatches(JsonValue obj)
    {
        string hashStored = obj["hash"].AsString();
        obj.Remove("hash");

        return hashStored == GetHash(obj.ToString());
    }

    #endregion

    public static JsonValue ConvertNodeToJson(Node node)
    {
        JsonValue val = CollectNodeData(node);
        return val;
    }

    private static JsonValue CollectNodeData(Node node)
    {
        JsonValue jsonNode = new JsonValue();

        if (node.IsInGroup(Globals.Groups.NotPersistent.ToString()) || node.IsInGroup(Globals.Groups.SelfOnly.ToString())) 
            return new JsonValue();

        jsonNode["Name"].Set(node.Name);
        jsonNode["Type"].Set(Globals.RemoveNamespace(node.GetType().ToString()));
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
            if (meta == Globals.Meta.UniqueId.ToString())
                continue;
            jsonNode["Meta"][meta].Set((string)node.GetMeta(meta));
        }
        foreach (string group in node.GetGroups()) // not accessible outside main
            jsonNode["Group"].Append(group);

        for (int i = 0; i < node.GetChildCount(); i++) // not accessible outside main
            jsonNode["Children"].Append(CollectNodeData(node.GetChild(i)));

        if (node is ISaveData)
            jsonNode["ISaveData"].Set((node as ISaveData).SerializeSaveData());

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
        else if(node is Control c)
        {
            c.Position = data["Position"].AsVector2();
            c.Scale = data["Scale"].AsVector2();
            c.Rotation =  data["Rotation"].AsFloat();
            c.Size =  data["Size"].AsVector2();
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
        }

        node = GodotObject.InstanceFromId(nodeID) as Node;

        foreach (KeyValuePair<string, JsonValue> meta in data["Meta"].Object)
            node.SetMeta(meta.Key, meta.Value.AsString());
        foreach (JsonValue group in data["Group"].Array)
            node.AddToGroup(group.AsString());

        foreach (JsonValue child in data["Children"].Array)
            node.CallDeferred("add_child", ConvertJsonToNode(child));

        if (node is ISaveData)
            (node as ISaveData).DeserializeSaveData(data["ISaveData"]);

        return node;
    }

    private async Task LoadFromQueue()
    {
        if (LoadQueue.Count == 0)
            return;
        string fileName = LoadQueue[0].Item1;
        string fileHolder = "levels";
        if (LoadQueue[0].Item2)
            fileHolder = "players";
        
        Task.Run(() =>
        {
            JsonValue data = FileManager.LoadFromFile("saves/" + CurrentSave + "/" + fileHolder + "/" + fileName);

            CallDeferred(nameof(ConvertToNode), fileHolder, data);
            
        });
    }
    private void ConvertToNode(string fileHolder, JsonValue data)
    {
        Node root = ConvertJsonToNode(data);

        if (fileHolder == "levels")
            LoadingSaveComplete.Invoke(root);
        else
            LoadingPlayerSaveComplete.Invoke(root);
    }

    private void SaveFromQueue()
    {
        // while loop a safety check to varify any nodes intended to be saved still exists
        while (SaveQueue.Count > 0)
        {
            if (SaveQueue[0] != null)
                break;
            else
                SaveQueue.RemoveAt(0);
        }

        if (SaveQueue.Count == 0)
            return;

        Node rootNode = SaveQueue[0];
        JsonValue data = SaveManager.ConvertNodeToJson(rootNode);

        if (!rootNode.HasMeta(Globals.Meta.OwnerId.ToString()))
            FileManager.SaveToFileFormattedAsync(data, "saves/" + CurrentSave + "/levels/" + rootNode.Name);
        else
        {
            string fileName = rootNode.GetMeta(Globals.Meta.OwnerId.ToString()).ToString();
            FileManager.SaveToFileFormattedAsync(data, "saves/" + CurrentSave + "/players/" + fileName);
        }
    }

    private void OnSaveLoaded(Node node)
    {
        CallDeferred(nameof(OnSaveLoadedDeferred), node);
    }

    private void OnSaveLoadedDeferred(Node node)
    {
        LoadQueue.RemoveAt(0);
        if (SaveQueueHalted)
        {
            SaveQueueHalted = false;
            SaveFromQueue();
        }
        if(LoadQueue.Count > 0)
        {
            LoadFromQueue();
        }
        
    }

    private void OnPlayerSaveLoaded(Node node)
    {
        CallDeferred(nameof(OnPlayerSaveLoadedDeferred), node);
    }

    private void OnPlayerSaveLoadedDeferred(Node node)
    {
        LoadQueue.RemoveAt(0);

        if (SaveQueueHalted)
        {
            SaveQueueHalted = false;
            SaveFromQueue();
        }
        // BIG ISSUE HERE, PLAYER NEVER ADDED TO LOAD QUEUE?
        
        if (LoadQueue.Count > 0)
        {
            LoadFromQueue();
        }
    }
    private void OnFileSaved(string obj)
    {
        string fileName = obj.Substring(obj.RFind("/")+1);
        SavingSaveComplete?.Invoke(fileName);
        CallDeferred(nameof(OnFileSavedDeferred), obj);
    }

    private void OnFileSavedDeferred(string obj)
    {

        if (LoadQueueHalted)
        {
            LoadQueueHalted = false;
            LoadFromQueue();
        }
        
        SaveQueue.RemoveAt(0);
        if(SaveQueue.Count > 0)
        {
            SaveFromQueue();
        }
    }

}

public interface ISaveData
{
    public JsonValue SerializeSaveData();
    public void DeserializeSaveData(JsonValue data);
}

