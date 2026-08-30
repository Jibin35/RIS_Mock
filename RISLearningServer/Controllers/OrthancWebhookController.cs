using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace RisLearning.Server.Controllers;

[ApiController]
[Route("webhooks/orthanc")]
public sealed class OrthancWebhookController : ControllerBase
{
    private const string ExpectedSecret = "dev-secret";

    [HttpPost]
    public IActionResult Receive(
        [FromBody] OrthancWebhookRequest request)
    {
        if (!Request.Headers.TryGetValue(
                "X-Webhook-Secret",
                out var secret))
        {
            return Unauthorized();
        }

        if (secret != ExpectedSecret)
        {
            return Unauthorized();
        }

        Console.WriteLine();
        Console.WriteLine(
            "=== ORTHANC WEBHOOK RECEIVED ===");

        Console.WriteLine(
            $"Study Instance UID: {request.StudyInstanceUid}");

        return Ok();
    }

    public sealed class OrthancWebhookRequest
    {
        [JsonPropertyName("studyInstanceUid")]
        public string StudyInstanceUid { get; set; } = "";
    }
}   