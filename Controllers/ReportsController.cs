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

    public ReportsController(ReportService reports, FileStorage files)
    {
        _reports = reports;
        _files = files;
    }

    /// <summary>Browse and filter active reports. All parameters are optional.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ReportDto>>> Browse(
        [FromQuery] string? kind,
        [FromQuery] ItemCategory? category,
        [FromQuery] string? location,
        [FromQuery] string? query,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var filter = new ReportFilter
        {
            Kind = kind,
            Category = category,
            Location = location,
            Query = query,
            From = from,
            To = to
        };
        return Ok(await _reports.BrowseAsync(filter));
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
        var (report, matches) = await _reports.CreateFoundAsync(CurrentUserId, req, photoUrl: null);
        Response.Headers["X-Matches-Found"] = matches.Count.ToString();
        return Ok(ReportService.Map(report));
    }

    /// <summary>File a lost-item report with an optional photo using multipart form data.</summary>
    [HttpPost("lost/with-photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ReportDto>> ReportLostWithPhoto(
        [FromForm] CreateReportWithPhotoRequest req)
    {
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
        var photoUrl = await _files.SaveAsync(req.Photo);
        var (report, matches) = await _reports.CreateFoundAsync(
            CurrentUserId, req.ToReportRequest(), photoUrl);
        Response.Headers["X-Matches-Found"] = matches.Count.ToString();
        return Ok(ReportService.Map(report));
    }
}
