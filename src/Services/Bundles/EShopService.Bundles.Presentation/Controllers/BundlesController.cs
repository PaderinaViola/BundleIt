using EShopService.Bundles.Application.DTOs;
using EShopService.Bundles.Application.Services;
using EShopService.Bundles.Domain.Exceptions;
using EShopService.Bundles.Presentation.Contracts;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EShopService.Bundles.Presentation.Controllers;

[ApiController]
[Route("api/bundles")]
public sealed class BundlesController : ControllerBase
{
    private readonly IBundleService _bundleService;

    public BundlesController(IBundleService bundleService)
    {
        _bundleService = bundleService;
    }

    [HttpPost]
    public async Task<ActionResult<BundleDto>> PublishBundle(
        [FromBody] PublishBundleRequest request,
        CancellationToken cancellationToken)
    {
        var bundle = await _bundleService.PublishBundleAsync(
            request.PrimaryArtistId,
            request.Title,
            request.PackLicenseType,
            cancellationToken);

        return CreatedAtAction(nameof(GetBundle), new { bundleId = bundle.BundleId }, bundle);
    }

    [HttpGet("{bundleId:guid}")]
    public async Task<ActionResult<BundleDto>> GetBundle(Guid bundleId, CancellationToken cancellationToken)
    {
        var bundle = await _bundleService.GetBundleAsync(bundleId, cancellationToken);
        if (bundle is null)
            return NotFound(new { code = "bundle.not_found", message = $"Bundle '{bundleId}' not found." });

        return Ok(bundle);
    }

    [HttpPost("{bundleId:guid}/items")]
    public async Task<ActionResult<BundleDto>> AddItem(
        Guid bundleId,
        [FromBody] AddBundleItemRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updatedBundle = await _bundleService.AddItemAsync(
                bundleId,
                request.ItemId,
                request.Name,
                request.LicenseType,
                cancellationToken);

            return Ok(updatedBundle);
        }
        catch (DomainException ex)
        {
            return MapDomainException(ex);
        }
    }

    [HttpPost("{bundleId:guid}/archive")]
    public async Task<ActionResult<BundleDto>> ArchiveBundle(Guid bundleId, CancellationToken cancellationToken)
    {
        try
        {
            var archivedBundle = await _bundleService.ArchiveBundleAsync(bundleId, cancellationToken);
            return Ok(archivedBundle);
        }
        catch (DomainException ex)
        {
            return MapDomainException(ex);
        }
    }

    private ActionResult MapDomainException(DomainException ex) => ex switch
    {
        BundleArchivedException archiveEx => UnprocessableEntity(new { code = archiveEx.Code, message = archiveEx.Message }),
        InvalidLicenseException licEx => BadRequest(new { code = licEx.Code, message = licEx.Message }),
        _ => UnprocessableEntity(new { code = ex.Code, message = ex.Message })
    };
}