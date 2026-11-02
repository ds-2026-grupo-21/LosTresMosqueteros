using System;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;
using LosTresMosqueteros.Productos;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Users;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.Application.Dtos;

namespace LosTresMosqueteros.Pantry;

[Authorize]
public class PantryAppService : LosTresMosqueterosAppService, IPantryAppService
{
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IRepository<Producto, Guid> _productRepository;
    private readonly IRepository<ExpirationWarning, Guid> _warningRepository;
    public PantryAppService(
    IRepository<PantryItem, Guid> pantryItemRepository,
    IRepository<Producto, Guid> productRepository,
    IRepository<ExpirationWarning, Guid> warningRepository)
    {
        _pantryItemRepository = pantryItemRepository;
        _productRepository = productRepository;
        _warningRepository = warningRepository;
    }
    public async Task<PagedResultDto<PantryItemDto>> GetListAsync(
    PagedResultRequestDto input)
    {
        var ownerId = CurrentUser.GetId();

        var query = await _pantryItemRepository.GetQueryableAsync();

        query = query
            .Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreationTime)
            .ThenBy(x => x.Id);

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query.Skip(input.SkipCount).Take(input.MaxResultCount));

        var result = new List<PantryItemDto>();

        foreach (var item in items)
        {
            result.Add(await MapToDtoAsync(item));
        }

        return new PagedResultDto<PantryItemDto>(totalCount, result);
    }

    public async Task<PantryItemDto> GetAsync(Guid id)
    {
        var item = await GetOwnedItemAsync(id);
        return await MapToDtoAsync(item);
    }
    public async Task<PantryItemDto> CreateAsync(CreatePantryItemDto input)
    {
        var ownerId = CurrentUser.GetId();

        var product = await _productRepository.FindAsync(input.ProductId);

        if (product == null)
        {
            throw new EntityNotFoundException(typeof(Producto), input.ProductId);
        }

        var item = new PantryItem(
            GuidGenerator.Create(),
            ownerId,
            product.Id,
            input.ExpirationDate);

        item.UpdateDetails(
            input.Quantity,
            input.Unit,
            input.ExpirationDate);

        await _pantryItemRepository.InsertAsync(item);

        return await MapToDtoAsync(item);
    }


    public async Task<PantryItemDto> UpdateAsync(Guid id, UpdatePantryItemDto input)
    {
        var item = await GetOwnedItemAsync(id);

        item.UpdateDetails(input.Quantity, input.Unit, input.ExpirationDate);
        await _pantryItemRepository.UpdateAsync(item);

        return await MapToDtoAsync(item);
    }

    public async Task MarkConsumedAsync(Guid id)
    {
        var item = await GetOwnedItemAsync(id);

        item.MarkConsumed();
        await _pantryItemRepository.UpdateAsync(item);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await GetOwnedItemAsync(id);
        var warnings = await _warningRepository.GetListAsync(x => x.PantryItemId == item.Id);
        foreach (var warning in warnings)
        {
            await _warningRepository.DeleteAsync(warning);
        }
        await _pantryItemRepository.DeleteAsync(item);
    }

    private async Task<PantryItem> GetOwnedItemAsync(Guid id)
    {
        var ownerId = CurrentUser.GetId();

        var item = await _pantryItemRepository.FindAsync(
            x => x.Id == id && x.OwnerId == ownerId);

        if (item == null)
        {
            throw new EntityNotFoundException(typeof(PantryItem), id);
        }

        return item;
    }

    private async Task<PantryItemDto> MapToDtoAsync(PantryItem item)
    {
        var product = await _productRepository.FindAsync(item.ProductId);

        return new PantryItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = product?.Nombre,
            Status = item.Status,
            Quantity = item.Quantity,
            Unit = item.Unit,
            ExpirationDate = item.ExpirationDate
        };
    }
}
