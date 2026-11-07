using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using LosTresMosqueteros.Pantry;
using LosTresMosqueteros.Productos;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Volo.Abp.Validation;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore;

public class PantryAppServiceTests : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    private readonly IPantryAppService _service;
    private readonly IRepository<Producto, Guid> _products;
    private readonly IRepository<ExpirationWarning, Guid> _warnings;
    private readonly ICurrentPrincipalAccessor _principal;

    public PantryAppServiceTests()
    {
        _service = GetRequiredService<IPantryAppService>();
        _products = GetRequiredService<IRepository<Producto, Guid>>();
        _warnings = GetRequiredService<IRepository<ExpirationWarning, Guid>>();
        _principal = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    private IDisposable AsUser(Guid id) => _principal.Change(new ClaimsPrincipal(
        new ClaimsIdentity(new[] { new Claim(AbpClaimTypes.UserId, id.ToString()) }, "Test")));

    [Fact]
    public async Task Own_items_can_be_created_listed_updated_consumed_and_deleted()
    {
        var productId = Guid.NewGuid();
        await WithUnitOfWorkAsync(() => _products.InsertAsync(new Producto(productId, "12345678", "Arroz")));
        using var user = AsUser(Guid.NewGuid());
        var created = await _service.CreateAsync(new CreatePantryItemDto { ProductId = productId, Quantity = 1.5m, Unit = " kg " });
        created.ProductName.ShouldBe("Arroz");
        created.Unit.ShouldBe("kg");
        (await _service.GetListAsync(new PagedResultRequestDto())).TotalCount.ShouldBe(1);
        var updated = await _service.UpdateAsync(created.Id, new UpdatePantryItemDto { Quantity = 2, Unit = "kg" });
        updated.Quantity.ShouldBe(2);
        await _service.MarkConsumedAsync(created.Id);
        (await _service.GetAsync(created.Id)).Status.ShouldBe(PantryItemStatus.Consumed);
        await WithUnitOfWorkAsync(() => _warnings.InsertAsync(new ExpirationWarning(Guid.NewGuid(), created.Id, new DateOnly(2026, 11, 1), 1)));
        await _service.DeleteAsync(created.Id);
        await Should.ThrowAsync<EntityNotFoundException>(() => _service.GetAsync(created.Id));
        (await WithUnitOfWorkAsync(() => _warnings.GetListAsync(x => x.PantryItemId == created.Id))).ShouldBeEmpty();
    }

    [Fact]
    public async Task Other_users_cannot_read_change_consume_or_delete_items()
    {
        var productId = Guid.NewGuid();
        await WithUnitOfWorkAsync(() => _products.InsertAsync(new Producto(productId, "87654321", "Leche")));
        Guid id;
        using (AsUser(Guid.NewGuid()))
            id = (await _service.CreateAsync(new CreatePantryItemDto { ProductId = productId })).Id;
        using (AsUser(Guid.NewGuid()))
        {
            (await _service.GetListAsync(new PagedResultRequestDto())).TotalCount.ShouldBe(0);
            await Should.ThrowAsync<EntityNotFoundException>(() => _service.GetAsync(id));
            await Should.ThrowAsync<EntityNotFoundException>(() => _service.UpdateAsync(id, new UpdatePantryItemDto()));
            await Should.ThrowAsync<EntityNotFoundException>(() => _service.MarkConsumedAsync(id));
            await Should.ThrowAsync<EntityNotFoundException>(() => _service.DeleteAsync(id));
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.234)]
    public void Invalid_quantities_are_rejected(double value)
    {
        var dto = new UpdatePantryItemDto { Quantity = (decimal)value };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, true).ShouldBeFalse();
    }

    [Fact]
    public async Task Invalid_update_is_rejected_and_does_not_change_the_item()
    {
        var productId = Guid.NewGuid();
        await WithUnitOfWorkAsync(() => _products.InsertAsync(new Producto(productId, "11112222", "Harina")));
        using var user = AsUser(Guid.NewGuid());
        var created = await _service.CreateAsync(new CreatePantryItemDto { ProductId = productId, Quantity = 2 });
        await Should.ThrowAsync<AbpValidationException>(() => _service.UpdateAsync(created.Id, new UpdatePantryItemDto { Quantity = -1 }));
        (await _service.GetAsync(created.Id)).Quantity.ShouldBe(2);
        await Should.ThrowAsync<EntityNotFoundException>(() => _service.CreateAsync(new CreatePantryItemDto { ProductId = Guid.NewGuid() }));
    }
}
