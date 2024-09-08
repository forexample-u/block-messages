using Server.Nodes;

namespace Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseStaticFiles();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        Directory.CreateDirectory(Node.DataPath);
        if (!File.Exists(Node.ServerFilePath))
        {
            await File.WriteAllTextAsync(Node.ServerFilePath, string.Empty);
        }

        Parallel.Invoke(async () =>
        {
            string[] nodes = await Node.GetNodesFromLocalAsync();
            HashSet<string> topics = await Node.GetTopicsFromNodesAsync(nodes);
            foreach (string topic in topics)
            {
                HashSet<string> messages = await Node.ReadFromNodesAsync(nodes, topic);
                HashSet<string> localMessages = await Node.ReadFromLocalAsync(topic);
                foreach (var message in localMessages)
                {
                    messages.Add(message);
                }
                await Node.WriteToLocalAsync(topic, messages);
            }
            Console.WriteLine("Sync end!");
        },
        () => {
            app.Run();
        });
    }
}