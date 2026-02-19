using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Inventory.Application;
using Monolithic.Api.Modules.Inventory.Contracts;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Manages inventory item variants and images.
///
/// Route hierarchy:
///   /api/v1/inventory/items/{itemId}/variants           — variant CRUD
///   /api/v1/inventory/items/{itemId}/images             — main/all-variants images
///   /api/v1/inventory/items/{itemId}/variants/{variantId}/images  — variant-specific images
/// </summary>
[ApiController]
[Route("api/v1/inventory/items/{itemId:guid}")]
public sealed class InventoryItemMediaController(IInventoryImageService imageService) : ControllerBase
{
    // ── Variants ───────────────────────────────────────────────────────────

    [HttpGet("variants")]
    [RequirePermission("inventory:read")]
    [ProducesResponseType<IReadOnlyCollection<InventoryItemVariantDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVariants(Guid itemId, CancellationToken cancellationToken)
    {
        var variants = await imageService.GetVariantsAsync(itemId, cancellationToken);
        return Ok(variants);
    }

    [HttpGet("variants/{variantId:guid}")]
    [RequirePermission("inventory:read")]
    [ProducesResponseType<InventoryItemVariantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVariantById(Guid itemId, Guid variantId, CancellationToken cancellationToken)
    {
        var variant = await imageService.GetVariantByIdAsync(variantId, cancellationToken);
        return variant is null ? NotFound() : Ok(variant);
    }

    [HttpPost("variants")]
    [RequirePermission("inventory:update")]
    [ProducesResponseType<InventoryItemVariantDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateVariant(
        Guid itemId,
        [FromBody] CreateInventoryItemVariantRequest request,
        CancellationToken cancellationToken)
    {
        var created = await imageService.CreateVariantAsync(itemId, request, cancellationToken);
        return CreatedAtAction(nameof(GetVariantById), new { itemId, variantId = created.Id }, created);
    }

    [HttpPut("variants/{variantId:guid}")]
    [RequirePermission("inventory:update")]
    [ProducesResponseType<InventoryItemVariantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVariant(
        Guid itemId,
        Guid variantId,
        [FromBody] UpdateInventoryItemVariantRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await imageService.UpdateVariantAsync(variantId, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("variants/{variantId:guid}")]
    [RequirePermission("inventory:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVariant(Guid itemId, Guid variantId, CancellationToken cancellationToken)
    {
        var deleted = await imageService.DeleteVariantAsync(variantId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // ── Main / All-Variants Images ─────────────────────────────────────────
    // VariantId is not supplied → InventoryItemImage.VariantId == null

    [HttpGet("images")]
    [RequirePermission("inventory:read")]
    [ProducesResponseType<IReadOnlyCollection<InventoryItemImageDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImages(Guid itemId, CancellationToken cancellationToken)
    {
        var images = await imageService.GetImagesAsync(itemId, variantId: null, cancellationToken);
        return Ok(images);
    }

    /// <summary>
    /// Upload a main image for the item (applies to all variants).
    /// Use multipart/form-data. Fields: file (required), altText, displayOrder, setAsMain.
    /// </summary>
    [HttpPost("images")]
    [RequirePermission("inventory:update")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<InventoryItemImageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMainImage(
        Guid itemId,
        IFormFile file,
        [FromForm] string? altText,
        [FromForm] int displayOrder = 0,
        [FromForm] bool setAsMain = false,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("A file is required.");

        var userId = GetCurrentUserId();
        var image = await imageService.UploadImageAsync(itemId, file, variantId: null, altText ?? string.Empty, displayOrder, setAsMain, userId, cancellationToken);
        return Created(string.Empty, image);
    }

    // ── Variant-Specific Images ────────────────────────────────────────────

    [HttpGet("variants/{variantId:guid}/images")]
    [RequirePermission("inventory:read")]
    [ProducesResponseType<IReadOnlyCollection<InventoryItemImageDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVariantImages(Guid itemId, Guid variantId, CancellationToken cancellationToken)
    {
        var images = await imageService.GetImagesAsync(itemId, variantId, cancellationToken);
        return Ok(images);
    }

    /// <summary>
    /// Upload an image scoped to a specific variant.
    /// Use multipart/form-data. Fields: file (required), altText, displayOrder, setAsMain.
    /// </summary>
    [HttpPost("variants/{variantId:guid}/images")]
    [RequirePermission("inventory:update")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<InventoryItemImageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadVariantImage(
        Guid itemId,
        Guid variantId,
        IFormFile file,
        [FromForm] string? altText,
        [FromForm] int displayOrder = 0,
        [FromForm] bool setAsMain = false,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("A file is required.");

        var userId = GetCurrentUserId();
        var image = await imageService.UploadImageAsync(itemId, file, variantId, altText ?? string.Empty, displayOrder, setAsMain, userId, cancellationToken);
        return Created(string.Empty, image);
    }

    // ── Image Management (shared) ──────────────────────────────────────────

    [HttpPatch("images/{imageId:guid}")]
    [RequirePermission("inventory:update")]
    [ProducesResponseType<InventoryItemImageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateImageMeta(
        Guid itemId,
        Guid imageId,
        [FromBody] UpdateImageMetaRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await imageService.UpdateImageMetaAsync(imageId, request.AltText, request.DisplayOrder, request.IsMain, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("images/{imageId:guid}")]
    [RequirePermission("inventory:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(Guid itemId, Guid imageId, CancellationToken cancellationToken)
    {
        var deleted = await imageService.DeleteImageAsync(imageId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("images/reorder")]
    [RequirePermission("inventory:update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReorderImages(
        Guid itemId,
        [FromBody] ReorderImagesRequest request,
        CancellationToken cancellationToken)
    {
        await imageService.ReorderImagesAsync(request.OrderedImageIds, cancellationToken);
        return NoContent();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return value is not null ? Guid.Parse(value) : null;
    }
}

// ── Inline request models ──────────────────────────────────────────────────

public sealed record UpdateImageMetaRequest(
    string AltText,
    int DisplayOrder,
    bool IsMain
);

public sealed record ReorderImagesRequest(
    IReadOnlyList<Guid> OrderedImageIds
);
