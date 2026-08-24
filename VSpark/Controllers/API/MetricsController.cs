using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using Newtonsoft.Json;

using VSpark.Hubs;

namespace VSpark.API.Controllers;

[Authorize(AuthenticationSchemes = "Bearer,X-API")]
[ApiController]
[Route("api/[controller]")]
public class MetricsController(IHubContext<MetricsHub> hubContext) : ControllerBase
{
    [Authorize(Roles = "SA")]
    [HttpPost("send-incident")]
    [EndpointDescription("Отправка нового инцидента на сервер.")]
    public async Task<IActionResult> SendIncident([FromForm] string? incident, IFormFile? image)
    {

        
        return Ok($"Incident successfully saved");
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
