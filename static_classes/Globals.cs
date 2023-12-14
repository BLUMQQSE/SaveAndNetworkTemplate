using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class StringExtensions
{
    public static string RemovePath(this string path)
    {
        return path.Substring(path.RFind("/") + 1);
    }
}
public static class NodeExtensions
{
    public static bool IsValid<T>(this T node) where T : GodotObject
    {
        return GodotObject.IsInstanceValid(node);
    }


    public static T IfValid<T>(this T node) where T : GodotObject
        => node.IsValid() ? node : null;

    public static void SafeQueueFree(this Node node)
    {
        if (!node.IsValid()) return;

        node.QueueFree();
    }
    /// <summary>
    /// Function for searching for child node of Type T. Removes need for searching for a
    /// specific name of a node, reducing potential errors in name checking being inaccurate.
    /// Supports checking 5 layers of nodes. This method is ineffecient, and should never be used repetitively 
    /// in _process.
    /// </summary>
    /// <returns>First instance of Type T</returns>
    public static T GetChildOfType<T>(this Node node)
    {
        if (node == null)
            return default(T);

        foreach (Node child in node.GetChildren())
            if (child is T)
                return (T)(object)child;

        return default(T);
    }

    /// <summary>
    /// Function for searching for child node of Type T. Removes need for searching for a
    /// specific name of a node, reducing potential errors in name checking being inaccurate.
    /// Supports checking 5 layers of nodes. This method is ineffecient, and should never be used repetitively 
    /// in _process.
    /// </summary>
    /// <returns>First instance of Type T</returns>
    public static T2 GetChildOfType<T1, T2>(this Node node)
    {
        Node t1 = node.GetChildOfType<T1>() as Node;
        return t1.GetChildOfType<T2>();
    }
    /// <summary>
    /// Function for searching for child node of Type T. Removes need for searching for a
    /// specific name of a node, reducing potential errors in name checking being inaccurate.
    /// Supports checking 5 layers of nodes. This method is ineffecient, and should never be used repetitively 
    /// in _process.
    /// </summary>
    /// <returns>First instance of Type T</returns>
    public static T3 GetChildOfType<T1, T2, T3>(this Node node)
    {
        Node t2 = node.GetChildOfType<T1, T2>() as Node;
        return t2.GetChildOfType<T3>();
    }


    /// <summary>
    /// Function for searching for children nodes of Type T.
    /// </summary>
    /// <returns>List of all instances of Type T</returns>
    public static List<T> GetChildrenOfType<T>(this Node node)
    {
        List<T> list = new List<T>();
        if (node == null)
            return list;

        foreach (Node child in node.GetChildren())
            if (child is T)
                list.Add((T)(object)child);

        return list;
    }

    /// <summary>
    /// Function for searching for children nodes of Type T.
    /// </summary>
    /// <returns>List of all instances of Type T that are children or lower.</returns>
    public static List<T> GetAllChildrenOfType<T>(this Node node)
    {
        List<T> list = new List<T>();
        list.AddRange(GetChildrenOfType<T>(node));

        foreach (Node child in node.GetChildren())
        {
            list.AddRange(GetAllChildrenOfType<T>(child));
        }

        return list;
    }

    /// <summary>
    /// Function for searching for sibling node of Type T. Removes need for searching for a
    /// specific name of a node, reducing potential errors in name checking being inaccurate.
    /// </summary>
    /// <returns>First instance of Type T</returns>
    public static T GetSiblingOfType<T>(this Node node)
    {
        return node.GetParent().GetChildOfType<T>();
    }
    /// <summary>
    /// Function for searching for sibling nodes of Type T.
    /// </summary>
    /// <returns>List of all instances of Type T</returns>
    public static List<T> GetSiblingsOfType<T>(this Node node)
    {
        return node.GetParent().GetChildrenOfType<T>();
    }

}

public static class Globals
{
    public enum Groups
    {
        AutoLoad,
        NetworkForceVisible,
        SelfOnly,
        NotPersistent
    }

    public enum Meta
    {
        UniqueId,
        OwnerId,
        LevelPartitionName
    }
    public enum DataType
    {
        RpcCall,
        ClientUpdate,
        ClientInputUpdate,
        ClientUpdateReminder,
        ServerUpdate,
        FullServerData,
        ServerAdd,
        ServerRemove
    }

    public enum PhysicsLayer
    {
        Neutral = 1,
        Player,
        Interaction,
        Enemy,
        Ground,
        MouseInteraction,
        Layer7,
        Layer8,
        Light,
        BlockLight,
        Layer11,
        Layer12,
        Layer13,
        Layer14,
        Layer15,
        Layer16,
        Layer17,
        Layer18,
        Layer19,
        Invisible,
    }

    /// <summary>
    /// Returns true if every layer of mask2 is also on mask.
    /// </summary>
    public static bool LayersUnion(uint mask, uint mask2)
    {
        if (mask == mask2) return true;

        for (int i = 0; i < 32; i++)
        {
            if (((mask2 >> i) & 1) == 1)
            {
                if (((mask >> i) & 1) == 0)
                {
                    return false;
                }
            }
        }

        return true;
    }
    /// <summary>
    /// Returns true if any layer of mask2 is also on mask
    /// </summary>
    public static bool LayersIntersect(uint mask, uint mask2)
    {
        if (mask == mask2) return true;

        for (int i = 0; i < 32; i++)
        {
            if (((mask2 >> i) & 1) == 1)
            {
                if (((mask >> i) & 1) == 1)
                {
                    return true;
                }
            }
        }

        return false;
    }


    public static uint ConvertToBitVal(PhysicsLayer layer)
    {
        return (uint)Mathf.Pow(2, (uint)layer - 1);
    }

    public static bool PhysicsLayerActive(PhysicsLayer number, CollisionObject3D obj)
    {
        return obj.GetCollisionLayerValue((int)number);
    }

    public static string RemoveNamespace(string name)
    {
        int index = name.RFind(".");
        if (index < 0)
            return name;
        else
            return name.Substring(index + 1, name.Length - (index + 1));
    }
}