using Server.Utils;
using System.Text.Json;

namespace Server.Nodes;

public class Node
{
    public static string DataPath = Path.Combine("wwwroot", "data", "temp");
    public static string ServerFilePath = Path.Combine("wwwroot", "data", "server.txt");
    public static HashSet<string> Messages { get; set; } = new();

    public static async Task<string[]> GetNodesFromLocalAsync()
    {
        return await File.ReadAllLinesAsync(ServerFilePath);
    }

    public static async Task<HashSet<string>> ReadFromNodesAsync(string[] nodes, string topic)
    {
        HashSet<string> messages = new HashSet<string>();
        foreach (string node in nodes)
        {
            string json = await HttpQuery.GetAsync($"{node.TrimEnd('/')}/api/message/list?topic={topic}");
            string[] nodeMessages = JsonSerializer.Deserialize<string[]>(json ?? string.Empty) ?? Array.Empty<string>();
            messages.UnionWith(nodeMessages);
        }
        return messages;
    }

    public static async Task<HashSet<string>> ReadFromLocalAsync(string topic)
    {
        string filePath = Path.Combine(DataPath, topic, "block1.json");
        Directory.CreateDirectory(Path.Combine(DataPath, topic));
        HashSet<string> messages = File.Exists(filePath) ? (await File.ReadAllLinesAsync(filePath)).ToHashSet() : new();
        return messages;
    }

    public static async Task WriteToLocalAsync(string topic, HashSet<string> messages)
    {
        Directory.CreateDirectory(Path.Combine(DataPath, topic));
        await File.WriteAllLinesAsync(Path.Combine(DataPath, topic, "block1.json"), messages);
    }

    public static async Task<bool> AppendToLocalAsync(string topic, string encryptText)
    {
        if (Messages.Contains(topic + encryptText)) { return false; }
        Directory.CreateDirectory(Path.Combine(DataPath, topic));
        await File.AppendAllTextAsync(Path.Combine(DataPath, topic, "block1.json"), encryptText + "\n");
        return true;
    }

    public static async Task<bool> AppendToNodesAsync(string[] nodes, string encryptText, string topic)
    {
        if (Messages.Contains(topic + encryptText)) { return false; }
        if (Messages.Count > 10000) { Messages.Clear(); }
        Messages.Add(topic + encryptText);
        foreach (string node in nodes)
        {
            await HttpQuery.GetAsync($"{node}/api/message/save?text={encryptText}&topic={topic}");
        }
        return true;
    }

    public static async Task<HashSet<string>> GetTopicsFromNodesAsync(string[] nodes)
    {
        HashSet<string> topics = new();
        foreach (string node in nodes)
        {
            string json = await HttpQuery.GetAsync($"{node}/api/message/topic");
            string[] nodeTopics = JsonSerializer.Deserialize<string[]>(json ?? string.Empty) ?? Array.Empty<string>();
            topics.UnionWith(nodeTopics);
        }
        return topics;
    }

    public static async Task<bool> AddNodeAsync(string[] nodes, string node)
    {
        string formatNode = node.TrimEnd('/');
        if (!Uri.IsWellFormedUriString(node, UriKind.RelativeOrAbsolute) || nodes.Contains(formatNode))
        {
            return false;
        }
        await File.WriteAllLinesAsync(ServerFilePath, nodes.Append(formatNode));
        return true;
    }

    public static IEnumerable<string> GetTopic()
    {
        return new DirectoryInfo(DataPath).GetDirectories().Select(x => x.Name);
    }
}