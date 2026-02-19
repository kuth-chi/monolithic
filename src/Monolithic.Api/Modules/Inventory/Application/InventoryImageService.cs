using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Common.Storage;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Inventory.Contracts;
using Monolithic.Api.Modules.Inventory.Domain;

namespace Monolithic.Api.Modules.Inventory.Application;

public sealed class InventoryImageService(
    ApplicationDbContext db,
    IImageStorageService storage) : IInventoryImageService
{
    // ── Variants ───────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<InventoryItemVariantDto>> GetVariantsAsync(
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        var variants = await db.InventoryItemVariants
            .Where(v => v.InventoryItemId == inventoryItemId)
            .Include(v => v.Attributes)
            .Include(v => v.Images)
            .OrderBy(v => v.DisplayOrder).ThenBy(v => v.Name)
            .ToListAsync(cancellationToken);

        return variants.Select(v => MapVariantToDto(v)).ToList();
    }

    public async Task<InventoryItemVariantDto?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        var variant = await db.InventoryItemVariants
            .Include(v => v.Attributes)
            .Include(v => v.Images)
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

        return variant is null ? null : MapVariantToDto(variant);
    }

    public async Task<InventoryItemVariantDto> CreateVariantAsync(
        Guid inventoryItemId,
        CreateInventoryItemVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        var variant = new InventoryItemVariant
        {
            Id = Guid.NewGuid(),
            InventoryItemId = inventoryItemId,
            Name = request.Name,
            SkuSuffix = request.SkuSuffix ?? string.Empty,
            PriceAdjustment = request.PriceAdjustment,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Attributes = request.Attributes?.Select(a => new InventoryItemVariantAttribute
            {
                Id = Guid.NewGuid(),
                AttributeName = a.AttributeName,
                AttributeValue = a.AttributeValue
            }).ToList() ?? []
        };

        db.InventoryItemVariants.Add(variant);
        await db.SaveChangesAsync(cancellationToken);
        return MapVariantToDto(variant);
    }

    public async Task<InventoryItemVariantDto?> UpdateVariantAsync(
        Guid variantId,
        UpdateInventoryItemVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        var variant = await db.InventoryItemVariants
            .Include(v => v.Attributes)
            .Include(v => v.Images)
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

        if (variant is null) return null;

        variant.Name = request.Name;
        variant.SkuSuffix = request.SkuSuffix ?? string.Empty;
        variant.PriceAdjustment = request.PriceAdjustment;
        variant.DisplayOrder = request.DisplayOrder;
        variant.IsActive = request.IsActive;
        variant.ModifiedAtUtc = DateTimeOffset.UtcNow;

        // Replace attributes — remove old, add new
        db.InventoryItemVariantAttributes.RemoveRange(variant.Attributes);
        variant.Attributes = request.Attributes?.Select(a => new InventoryItemVariantAttribute
        {
            Id = Guid.NewGuid(),
            VariantId = variantId,
            AttributeName = a.AttributeName,
            AttributeValue = a.AttributeValue
        }).ToList() ?? [];

        await db.SaveChangesAsync(cancellationToken);
        return MapVariantToDto(variant);
    }

    public async Task<bool> DeleteVariantAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        var variant = await db.InventoryItemVariants
            .Include(v => v.Images)
            .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

        if (variant is null) return false;

        // Delete backing files for all variant images
        foreach (var image in variant.Images)
            await storage.DeleteAsync(image.StoragePath, cancellationToken);

        db.InventoryItemVariants.Remove(variant);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ── Images ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<InventoryItemImageDto>> GetImagesAsync(
        Guid inventoryItemId,
        Guid? variantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.InventoryItemImages
            .Where(i => i.InventoryItemId == inventoryItemId)
            .AsQueryable();

        if (variantId.HasValue)
            query = query.Where(i => i.VariantId == variantId.Value);

        var images = await query
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        return images.Select(MapImageToDto).ToList();
    }

    public async Task<InventoryItemImageDto> UploadImageAsync(
        Guid inventoryItemId,
        IFormFile file,
        Guid? variantId,
        string altText,
        int displayOrder,
        bool setAsMain,
        Guid? uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        var folder = $"inventory/{inventoryItemId}";
        var storagePath = await storage.SaveAsync(file, folder, cancellationToken);

        // If setAsMain, demote any existing main image for the same scope
        if (setAsMain)
        {
            var existingMain = await db.InventoryItemImages
                .Where(i => i.InventoryItemId == inventoryItemId
                            && i.VariantId == variantId
                            && i.IsMain)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingMain)
                existing.IsMain = false;
        }

        var image = new InventoryItemImage
        {
            Id = Guid.NewGuid(),
            InventoryItemId = inventoryItemId,
            VariantId = variantId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoragePath = storagePath,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            AltText = altText ?? string.Empty,
            DisplayOrder = displayOrder,
            IsMain = setAsMain,
            UploadedByUserId = uploadedByUserId,
            UploadedAtUtc = DateTimeOffset.UtcNow
        };

        db.InventoryItemImages.Add(image);
        await db.SaveChangesAsync(cancellationToken);
        return MapImageToDto(image);
    }

    public async Task<InventoryItemImageDto?> UpdateImageMetaAsync(
        Guid imageId,
        string altText,
        int displayOrder,
        bool isMain,
        CancellationToken cancellationToken = default)
    {
        var image = await db.InventoryItemImages
            .FirstOrDefaultAsync(i => i.Id == imageId, cancellationToken);

        if (image is null) return null;

        // Demote existing main in same scope if promoting this one
        if (isMain && !image.IsMain)
        {
            var existingMain = await db.InventoryItemImages
                .Where(i => i.InventoryItemId == image.InventoryItemId
                            && i.VariantId == image.VariantId
                            && i.IsMain
                            && i.Id != imageId)
                .ToListAsync(cancellationToken);

            foreach (var e in existingMain)
                e.IsMain = false;
        }

        image.AltText = altText ?? string.Empty;
        image.DisplayOrder = displayOrder;
        image.IsMain = isMain;

        await db.SaveChangesAsync(cancellationToken);
        return MapImageToDto(image);
    }

    public async Task<bool> DeleteImageAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await db.InventoryItemImages
            .FirstOrDefaultAsync(i => i.Id == imageId, cancellationToken);

        if (image is null) return false;

        await storage.DeleteAsync(image.StoragePath, cancellationToken);
        db.InventoryItemImages.Remove(image);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task ReorderImagesAsync(
        IReadOnlyList<Guid> orderedImageIds,
        CancellationToken cancellationToken = default)
    {
        var images = await db.InventoryItemImages
            .Where(i => orderedImageIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        for (var i = 0; i < orderedImageIds.Count; i++)
        {
            var image = images.FirstOrDefault(img => img.Id == orderedImageIds[i]);
            if (image is not null)
                image.DisplayOrder = i;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // ── Mapping Helpers ────────────────────────────────────────────────────

    private InventoryItemImageDto MapImageToDto(InventoryItemImage i) => new(
        i.Id,
        i.InventoryItemId,
        i.VariantId,
        i.OriginalFileName,
        storage.GetPublicUrl(i.StoragePath),
        i.ContentType,
        i.FileSizeBytes,
        i.AltText,
        i.DisplayOrder,
        i.IsMain,
        i.UploadedByUserId,
        i.UploadedAtUtc
    );

    private InventoryItemVariantDto MapVariantToDto(InventoryItemVariant v) => new(
        v.Id,
        v.InventoryItemId,
        v.Name,
        v.SkuSuffix,
        v.PriceAdjustment,
        v.DisplayOrder,
        v.IsActive,
        v.Attributes.Select(a => new InventoryItemVariantAttributeDto(a.Id, a.AttributeName, a.AttributeValue)).ToList(),
        v.Images.OrderBy(i => i.DisplayOrder).Select(MapImageToDto).ToList(),
        v.CreatedAtUtc,
        v.ModifiedAtUtc
    );
}
