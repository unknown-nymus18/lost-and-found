using CampusLostAndFound.DTOs;
using CampusLostAndFound.Models;
using CampusLostAndFound.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusLostAndFound.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ReportsController : ApiControllerBase
{
    private readonly ReportService _reports;
    private readonly FileStorage _files;
    private readonly ClaimService _claims;

    public ReportsController(ReportService reports, FileStorage files, ClaimService claims)
    {
        _reports = reports;
        _files = files;
        _claims = claims;
    }

    /// <summary>Browse Open and Matched reports by default; specify status to view another status.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ReportDto>>> Browse(
        [FromQuery] string? kind,
        [FromQuery] ItemCategory? category,
        [FromQuery] ReportStatus? status,
        [FromQuery] string? location,
        [FromQuery] string? query,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        if (status is { } value && !Enum.IsDefined(value))
        {
            ModelState.AddModelError(nameof(status), "Status must be Open, Matched, Claimed, Resolved, or Closed.");
            return ValidationProblem(ModelState);
        }

        var filter = new ReportFilter
        {
            Kind = kind,
            Category = category,
            Status = status,
            Location = location,
            Query = query,
            From = from,
            To = to
        };
        return Ok(await _reports.BrowseAsync(filter));
    }

    /// <summary>Get a lost-item report by ID.</summary>
    [HttpGet("lost/{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReportDto>> GetLost(int id)
    {
        var report = await _reports.GetLostAsync(id);
        return report is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Lost report {id} was not found.")
            : Ok(report);
    }

    /// <summary>Get a found-item report by ID; handover details only for the finder, admins, and the approved claimant.</summary>
    [HttpGet("found/{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReportDto>> GetFound(int id)
    {
        var report = await _reports.GetFoundAsync(id, CurrentUserIdOrNull, User.IsInRole(Roles.Admin));
        return report is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Found report {id} was not found.")
            : Ok(report);
    }

    /// <summary>Replace the fields of your lost report; its photo is retained.</summary>
    [HttpPut("lost/{id:int}")]
    public async Task<ActionResult<ReportDto>> UpdateLost(int id, CreateReportRequest req)
    {
        var result = await _reports.UpdateLostAsync(id, CurrentUserId, req);
        if (result.Status != ReportChangeStatus.Success)
            return ReportError(result.Status, "Lost", id);
        return Ok(result.Report);
    }

    /// <summary>Replace the fields of your found report; its photo is retained.</summary>
    [HttpPut("found/{id:int}")]
    public async Task<ActionResult<ReportDto>> UpdateFound(int id, CreateReportRequest req)
    {
        if (HandoverProblem(req) is { } problem) return problem;
        var result = await _reports.UpdateFoundAsync(id, CurrentUserId, req);
        if (result.Status != ReportChangeStatus.Success)
            return ReportError(result.Status, "Found", id);
        return Ok(result.Report);
    }

    /// <summary>Delete your lost report and its associated photo.</summary>
    [HttpDelete("lost/{id:int}")]
    public async Task<IActionResult> DeleteLost(int id)
    {
        var result = await _reports.DeleteLostAsync(id, CurrentUserId);
        if (result.Status != ReportChangeStatus.Success)
            return ReportError(result.Status, "Lost", id);
        await _files.DeleteAsync(result.Report!.PhotoUrl);
        return NoContent();
    }

    /// <summary>Delete your found report, its claims, and its associated photo.</summary>
    [HttpDelete("found/{id:int}")]
    public async Task<IActionResult> DeleteFound(int id)
    {
        var result = await _reports.DeleteFoundAsync(id, CurrentUserId);
        if (result.Status != ReportChangeStatus.Success)
            return ReportError(result.Status, "Found", id);
        await _files.DeleteAsync(result.Report!.PhotoUrl);
        return NoContent();
    }

    private ActionResult ReportError(ReportChangeStatus status, string kind, int id) =>
        status == ReportChangeStatus.NotFound
            ? Problem(statusCode: StatusCodes.Status404NotFound, detail: $"{kind} report {id} was not found.")
            : Problem(statusCode: StatusCodes.Status403Forbidden, detail: "You can only change your own reports.");

    private ActionResult? HandoverProblem(CreateReportRequest req)
    {
        if (ReportService.ValidateHandover(req) is not { } error) return null;
        ModelState.AddModelError(error.Field, error.Error);
        return ValidationProblem(ModelState);
    }

    /// <summary>Confirm a claimed found item was handed over (finder, approved claimant, or admin).</summary>
    [HttpPost("found/{id:int}/resolve")]
    public async Task<ActionResult<ReportDto>> ResolveFound(int id)
    {
        var result = await _claims.ResolveAsync(id, CurrentUserId, User.IsInRole(Roles.Admin));
        if (!result.Succeeded) return Problem(statusCode: result.StatusCode, detail: result.Error);
        return Ok(ReportService.MapWithHandover(result.Claim!.FoundReport!));
    }

    /// <summary>Reports filed by the authenticated user.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<ReportDto>>> Mine() =>
        Ok(await _reports.ForUserAsync(CurrentUserId));

    /// <summary>File a lost-item report using a JSON body.</summary>
    [HttpPost("lost")]
    public async Task<ActionResult<ReportDto>> ReportLost(CreateReportRequest req)
    {
        var (report, matches) = await _reports.CreateLostAsync(CurrentUserId, req, photoUrl: null);
        Response.Headers["X-Matches-Found"] = matches.Count.ToString();
        return Ok(ReportService.Map(report));
    }

    /// <summary>File a found-item report using a JSON body.</summary>
    [HttpPost("found")]
    public async Task<ActionResult<ReportDto>> ReportFound(CreateReportRequest req)
    {
        if (HandoverProblem(req) is { } problem) return problem;
        var (report, matches) = await _reports.CreateFoundAsync(CurrentUserId, req, photoUrl: null);
        Response.Headers["X-Matches-Found"] = matches.Count.ToString();
        return Ok(ReportService.MapWithHandover(report));
    }

    /// <summary>File a lost-item report with an optional photo using multipart form data.</summary>
    [HttpPost("lost/with-photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ReportDto>> ReportLostWithPhoto(
        [FromForm] CreateReportWithPhotoRequest req)
    {
        var photoError = _files.Validate(req.Photo);
        if (photoError is not null)
        {
            ModelState.AddModelError(nameof(req.Photo), photoError);
            return ValidationProblem(ModelState);
        }

        var photoUrl = await _files.SaveAsync(req.Photo);
        var (report, matches) = await _reports.CreateLostAsync(
            CurrentUserId, req.ToReportRequest(), photoUrl);
        Response.Headers["X-Matches-Found"] = matches.Count.ToString();
        return Ok(ReportService.Map(report));
    }

    /// <summary>File a found-item report with an optional photo using multipart form data.</summary>
    [HttpPost("found/with-photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ReportDto>> ReportFoundWithPhoto(
        [FromForm] CreateReportWithPhotoRequest req)
    {
        var reportRequest = req.ToReportRequest();
        if (HandoverProblem(reportRequest) is { } problem) return problem;

        var photoError = _files.Validate(req.Photo);
        if (photoError is not null)
        {
            ModelState.AddModelError(nameof(req.Photo), photoError);
            return ValidationProblem(ModelState);
        }

        var photoUrl = await _files.SaveAsync(req.Photo);
        var (report, matches) = await _reports.CreateFoundAsync(
            CurrentUserId, reportRequest, photoUrl);
        Response.Headers["X-Matches-Found"] = matches.Count.ToString();
        return Ok(ReportService.MapWithHandover(report));
    }
}
