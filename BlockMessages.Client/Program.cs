using BlockMessages.Utils;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace BlockMessages;

public class Program
{
    public static string DataPath = "data";
    public static string ServerFilePath = Path.Combine(DataPath, "server.txt");

    public static async Task AppendToNodesAsync(string[] nodes, string topic, string encryptText)
    {
        foreach (string node in nodes)
        {
            await HttpQuery.GetAsync($"{node}/api/message/save?text={encryptText}&topic={topic}");
        }
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

    public static T DeserializeObject<T>(string value)
    {
        try { return JsonSerializer.Deserialize<T>(value); }
        catch { return default; }
    }

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        string topic = "empty", alg = "aes", msg = null, key = null, privateKey = null, publicKey = null, channel = "", person = "";
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-t") { topic = args[i + 1]; }
            if (args[i] == "-m") { msg = args[i + 1]; }
            if (args[i] == "-c") { channel = args[i + 1]; alg = "rsa"; }
            if (args[i] == "-p") { person = args[i + 1]; alg = "aes"; }
        }

        string channelFolderPath = Path.Combine(DataPath, "channel", channel);
        string personFolderPath = Path.Combine(DataPath, "personal", person);
        string topicFolderPath = Path.Combine(DataPath, "temp", topic);
        string privateKeyPath = Path.Combine(channelFolderPath, "private.txt");
        string publicKeyPath = Path.Combine(channelFolderPath, "public.txt");
        string blockFilePath = Path.Combine(topicFolderPath, "block1.json");

        if (!Directory.Exists(topicFolderPath)) Directory.CreateDirectory(topicFolderPath);
        if (!File.Exists(blockFilePath)) { await File.WriteAllTextAsync(blockFilePath, string.Empty); }
        if (!File.Exists(ServerFilePath)) { await File.WriteAllTextAsync(ServerFilePath, string.Empty); }
        string[] nodes = await File.ReadAllLinesAsync(ServerFilePath);

        if (!string.IsNullOrWhiteSpace(person) && alg == "aes")
        {
            if (!Directory.Exists(personFolderPath))
            {
                Directory.CreateDirectory(personFolderPath);
                XCipherString.GenerateAesKey(out key, 2000);
                await File.WriteAllTextAsync(Path.Combine(personFolderPath, $"{person}_{Guid.NewGuid().ToString("N")}.txt"), key);
            }
            key = await File.ReadAllTextAsync(Directory.GetFiles(personFolderPath).FirstOrDefault());
        }
        if (!string.IsNullOrWhiteSpace(channel) && alg == "rsa")
        {
            if (!File.Exists(publicKeyPath) || !File.Exists(privateKeyPath))
            {
                Directory.CreateDirectory(channelFolderPath);
                XCipherString.GenerateRsaKeys(out publicKey, out privateKey);
                await File.WriteAllTextAsync(publicKeyPath, publicKey);
                await File.WriteAllTextAsync(privateKeyPath, privateKey);
            }
            publicKey = await File.ReadAllTextAsync(publicKeyPath);
            privateKey = await File.ReadAllTextAsync(privateKeyPath);
        }
        if (msg != null && alg != null)
        {
            string json = JsonSerializer.Serialize(new Message(msg));
            string encryptText = alg == "rsa" ? XCipherString.EncryptRsa(json, privateKey) : XCipherString.EncryptAes(json, key);
            await File.AppendAllTextAsync(blockFilePath, encryptText + "\n");
            await AppendToNodesAsync(nodes, topic, encryptText);
        }
        if (msg == null && alg != null)
        {
            HashSet<string> messages = await ReadFromNodesAsync(nodes, topic);
            messages.UnionWith(await File.ReadAllLinesAsync(blockFilePath));
            await File.WriteAllLinesAsync(blockFilePath, messages);
            List<Message> resultMessages = messages
                .Select(text => DeserializeObject<Message>(
                    (alg == "rsa" ? XCipherString.DecryptRsa(text, publicKey) : XCipherString.DecryptAes(text, key)) ?? ""))
                .Where(x => x?.Date != null)
                .OrderBy(x => x.Date)
                .ToList();

            resultMessages.ForEach(message =>
                Console.WriteLine($"{DateTime.ParseExact(message.Date, "yyyyMMddHHmmss", CultureInfo.InvariantCulture)}\n{message?.Data}"));
        }
    }
}