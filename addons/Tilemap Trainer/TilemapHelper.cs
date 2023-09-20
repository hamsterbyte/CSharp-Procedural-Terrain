using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public static class TilemapHelper{
    /// <summary>
    /// Serialize texture coord dictionary to JSON and write to specified path
    /// </summary>
    /// <param name="path"></param>
    /// <param name="dictionary"></param>
    public static void SaveTextureCoords(string path, Dictionary<int, Vector2I> dictionary){
        Dictionary<int, string> safeDictionary = dictionary.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
        string jsonString = JsonSerializer.Serialize(safeDictionary, new JsonSerializerOptions(){ WriteIndented = true });
        using FileStream fileStream = new(path, FileMode.Create);
        fileStream.Write(jsonString.ToAsciiBuffer());
        fileStream.Close();
    }


    /// <summary>
    /// Load texture coords from file on the disc and deserialize from JSON
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Dictionary<int, Vector2I> LoadTextureCoords(string path){
        string jsonString = File.ReadAllText(path);
        Dictionary<int, string> temp = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonString);
        return temp?.ToDictionary(pair => pair.Key, pair => pair.Value.ToVector2I());
    }

    /// <summary>
    /// Extension method to convert from string to Vector2I
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private static Vector2I ToVector2I(this string s){
        Vector2I temp = Vector2I.Zero;
        string[] sTemp = s.Split(", ");
        temp.X = int.Parse(sTemp[0].TrimStart('('));
        temp.Y = int.Parse(sTemp[1].TrimEnd(')'));
        return temp;
    }
}