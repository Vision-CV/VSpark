using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using Newtonsoft.Json;

using VSpark.Hubs;
using VSpark.Models.Data;
using VSpark.Services.Metrics;

namespace VSpark.API.Controllers;

[Authorize(AuthenticationSchemes = "Bearer,X-API")]
[ApiController]
[Route("api/[controller]")]
public class MetricsController(IIncidentsRepository incidentsRepository, IHubContext<MetricsHub> hubContext) : ControllerBase
{
    [Authorize(Roles = "SA")]
    [HttpPost("send-incident")]
    [EndpointDescription("Отправка нового инцидента на сервер.")]
    public async Task<IActionResult> SendIncident([FromForm] string? incident, IFormFile? image)
    {
        if (incident == null)
            return BadRequest("No incident data provided.");

        IncidentData? incidentData = JsonConvert.DeserializeObject<IncidentData>(incident);

        if (incidentData == null)
            return BadRequest("Incident data is incorrect.");

        if (image == null)
            return BadRequest("Image was not received.");

        using MemoryStream imageStream = new MemoryStream();

        if (imageStream == null)
            return BadRequest("Failed to save image.");

        await image.CopyToAsync(imageStream);

        byte[] imageBuffer = imageStream.ToArray();

        if (!await incidentsRepository.TrySaveIncidentAsync(incidentData, imageBuffer))
            return BadRequest("Failed to add an incident.");

        await hubContext.Clients.All.SendAsync("OnIncidentCreated", incidentData);
        
        return Ok($"Incident successfully saved by {incidentData.Guid}");
    }

    [Authorize(Roles = "SA")]
    [HttpPatch("patch-incident")]
    [EndpointDescription("Изменение существующего на сервере инцидента.")]
    public async Task<IActionResult> PatchIncident([FromBody] IncidentData? data)
    {
        if (data == null)
            return BadRequest("No incident data provided.");

        string guid = data.Guid.ToString();

        if (!await incidentsRepository.TryUpdateIncidentAsync(guid, data))
            return BadRequest("Failed to update incident. Possible that provided guid doesn't associated with any incident.");

        await hubContext.Clients.All.SendAsync("OnIncidentPatched", data);

        return Ok($"Incident {guid} successfully updated.");
    }

    [Authorize(Roles = "SA")]
    [HttpDelete("delete-incident")]
    [EndpointDescription("Удаление существующего на сервере инцидента.")]
    public async Task<IActionResult> DeleteIncident(string? guid)
    {
        if (guid == null)
            return BadRequest("No correct guid provided.");

        // Duplication of TryGetIncidentAsync call. The second inside TryDeleteIncidentAsync.
        IncidentData? targetIncident = await incidentsRepository.TryGetIncidentAsync(guid);

        if (targetIncident == null)
            return BadRequest("There's no incident with the provided guid.");

        if (!await incidentsRepository.TryDeleteIncidentAsync(guid))
            return BadRequest("Failed to delete incident. Possible that provided guid doesn't associated with any incident.");

        await hubContext.Clients.All.SendAsync("OnIncidentDeleted", targetIncident);

        return Ok($"Incident {guid} was successfully deleted.");
    }

    [Authorize(Roles = "SA")]
    [HttpPost("report-suspicious-activity")]
    [EndpointDescription("Создание уведомления о подозрительном поведении.")]
    public async Task<IActionResult> ReportSuspiciousActivity([FromBody] SuspiciousActivityData? suspiciousActivityData)
    {
        if (suspiciousActivityData == null)
            return BadRequest("Suspicious activity data was not received.");

        await hubContext.Clients.Group("").SendAsync("CreateSuspiciousActivity", suspiciousActivityData);

        return Ok();
    }
}
