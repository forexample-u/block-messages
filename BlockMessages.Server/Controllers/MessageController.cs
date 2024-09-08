using Microsoft.AspNetCore.Mvc;
using Server.Nodes;

namespace Server.Controllers;

[ApiController]
[Route("api/message")]
public class MessageController : ControllerBase
{
    [HttpGet("save")]
    public async Task<bool> Save(string text, string? topic = null)
    {
        topic = string.IsNullOrWhiteSpace(topic) ? "empty" : topic;
        await Node.AppendToLocalAsync(topic, text);
        return await Node.AppendToNodesAsync(await Node.GetNodesFromLocalAsync(), text, topic);
    }

    [HttpGet("list")]
    public async Task<IEnumerable<string>> List(string? topic = null)
    {
        return await Node.ReadFromLocalAsync(string.IsNullOrWhiteSpace(topic) ? "empty" : topic);
    }

    [HttpGet("topic")]
    public IEnumerable<string> Topic()
    {
        return Node.GetTopic();
    }

    [HttpGet("nodes")]
    public async Task<string[]> Nodes()
    {
        return await Node.GetNodesFromLocalAsync();
    }

    [HttpGet("addNode")]
    public async Task<bool> AddNode(string url)
    {
        return await Node.AddNodeAsync(await Node.GetNodesFromLocalAsync(), url);
    }
}
