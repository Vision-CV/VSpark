using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using Newtonsoft.Json;

using VSpark.Hubs;
using VSpark.Orchestrator.Services.Rpcs;
using VSpark.Protos;

namespace VSpark.API.Controllers;

[Authorize(AuthenticationSchemes = "Bearer,X-API")]
[ApiController]
[Route("api/v1/[controller]")]
public class MetricsController(IncidentsBridge bridge, IHubContext<MetricsHub> hubContext, CancellationToken ct) : ControllerBase
{
    // TODO: Incident timestamp required.
    [Authorize(Roles = "SA")]
    [HttpPost("send-incident")]
    [EndpointDescription("Отправка нового инцидента на сервер.")]
    public async Task<IActionResult> SendIncident([FromForm] string? incident, IFormFile? image)
    {
        if (incident == null)
            return BadRequest("Wron incident data sent.");

        IncidentDto? incidentDto = JsonConvert.DeserializeObject<IncidentDto>(incident);

        if (incidentDto == null)
            return BadRequest("Failed to parse an incident.");

        MemoryStream artifactStream = new MemoryStream();

        if (image != null)
            await image.CopyToAsync(artifactStream);

        GuidMessage? response = await bridge.CreateIncidentAsync(incidentDto, artifactStream, ct);

        if (response == null)
            return StatusCode(500, "Something went wrong.");

        if (response.Success == 1)
            return Ok($"Incident successfully created by id: {response.Guid}");

        return StatusCode(500, "Request failed.");
    }

    [Authorize(Roles = "SA")]
    [HttpPatch("patch-incident")]
    [EndpointDescription("Изменение существующего на сервере инцидента.")]
    public async Task<IActionResult> PatchIncident()
    {


        return Ok($"Incident successfully updated.");
    }

    [Authorize(Roles = "SA")]
    [HttpDelete("delete-incident")]
    [EndpointDescription("Удаление существующего на сервере инцидента.")]
    public async Task<IActionResult> DeleteIncident(string? guid)
    {

        return Ok($"Incident {guid} was successfully deleted.");
    }

    [Authorize(Roles = "SA")]
    [HttpPost("report-suspicious-activity")]
    [EndpointDescription("Создание уведомления о подозрительном поведении.")]
    public async Task<IActionResult> ReportSuspiciousActivity()
    {

        return Ok();
    }
}
