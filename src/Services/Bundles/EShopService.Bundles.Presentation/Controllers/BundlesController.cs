using EShopService.Bundles.Application.Services;
using EShopService.Bundles.Domain.Exceptions;
using EShopService.Bundles.Presentation.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace EShopService.Bundles.Presentation.Controllers;

[ApiController]
[Route("api/bundles")]
public sealed class BundlesController : ControllerBase
{
    private readonly IBundleService _bundleService;

    public BundlesController(IBundleService bundleService) => _bundleService = bundleService;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var bundles = await _bundleService.GetAllBundlesAsync(cancellationToken);
        return Ok(bundles);
    }

    [HttpGet("{bundleId:guid}")]
    public async Task<IActionResult> GetById(Guid bundleId, CancellationToken cancellationToken)
    {
        var bundle = await _bundleService.GetBundleAsync(bundleId, cancellationToken);
        return bundle is null ? NotFound() : Ok(bundle);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBundleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _bundleService.CreateBundleAsync(
                request.PrimaryArtistId,
                request.Title,
                request.PackLicenseType,
                request.PersonalPrice,
                request.Currency,
                request.CommercialPrice,
                request.Description,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { bundleId = created.BundleId }, created);
        }
        catch (DomainException ex)
        {
            return UnprocessableEntity(new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("{bundleId:guid}/items")]
    public async Task<IActionResult> AddItem(Guid bundleId, [FromBody] AddBundleItemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _bundleService.AddItemAsync(
                bundleId,
                request.ItemId,
                request.Name,
                request.LicenseType,
                request.Description,
                request.FileFormat,
                request.FileSizeBytes,
                cancellationToken);

            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return UnprocessableEntity(new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("{bundleId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid bundleId, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _bundleService.PublishBundleAsync(bundleId, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainException ex)
        {
            return UnprocessableEntity(new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("{bundleId:guid}/archive")]
    public async Task<IActionResult> Archive(Guid bundleId, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _bundleService.ArchiveBundleAsync(bundleId, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{bundleId:guid}")]
    public async Task<IActionResult> Delete(Guid bundleId, CancellationToken cancellationToken)
    {
        try
        {
            await _bundleService.DeleteBundleAsync(bundleId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}