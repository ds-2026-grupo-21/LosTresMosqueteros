using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LosTresMosqueteros.Pantry;

public interface IPantryAppService : IApplicationService
{
    Task<PagedResultDto<PantryItemDto>> GetListAsync(
        PagedResultRequestDto input);

    Task<PantryItemDto> GetAsync(Guid id);
    Task<PantryItemDto> CreateAsync(CreatePantryItemDto input);

    Task<PantryItemDto> UpdateAsync(Guid id, UpdatePantryItemDto input);

    Task MarkConsumedAsync(Guid id);

    Task DeleteAsync(Guid id);
}
