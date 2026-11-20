using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using LosTresMosqueteros.Pantry;
using LosTresMosqueteros.Productos;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
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

    [Fact]
    public async Task Private_notes_are_isolated_for_two_owners_of_the_same_product()
    {
        var productId = Guid.NewGuid();
        await WithUnitOfWorkAsync(() => _products.InsertAsync(new Producto(productId, "11223344", "Arroz")));
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();
        Guid itemA;
        Guid itemB;

        using (AsUser(ownerA))
        {
            itemA = (await _service.CreateAsync(new CreatePantryItemDto { ProductId = productId })).Id;
            (await _service.GetAsync(itemA)).PrivateNote.ShouldBeNull();
            await _service.UpdateAsync(itemA, new UpdatePantryItemDto { PrivateNote = "Nota de A\nDos líneas" });
        }

        using (AsUser(ownerB))
        {
            itemB = (await _service.CreateAsync(new CreatePantryItemDto
            {
                ProductId = productId,
                PrivateNote = "Nota de B"
            })).Id;
            var list = await _service.GetListAsync(new PagedResultRequestDto());
            list.TotalCount.ShouldBe(1);
            list.Items[0].Id.ShouldBe(itemB);
            list.Items[0].PrivateNote.ShouldBe("Nota de B");
            await Should.ThrowAsync<EntityNotFoundException>(() => _service.GetAsync(itemA));
        }

        using (AsUser(ownerA))
        {
            var list = await _service.GetListAsync(new PagedResultRequestDto());
            list.TotalCount.ShouldBe(1);
            list.Items[0].Id.ShouldBe(itemA);
            list.Items[0].PrivateNote.ShouldBe("Nota de A\nDos líneas");
            await Should.ThrowAsync<EntityNotFoundException>(() => _service.GetAsync(itemB));
            await Should.ThrowAsync<EntityNotFoundException>(() =>
                _service.UpdateAsync(itemB, new UpdatePantryItemDto { PrivateNote = "Cambio ajeno" }));
        }

        using (AsUser(ownerB))
        {
            (await _service.GetAsync(itemB)).PrivateNote.ShouldBe("Nota de B");
        }
    }

    [Fact]
    public async Task Private_note_can_be_edited_preserved_and_cleared_via_json_contract()
    {
        var productId = Guid.NewGuid();
        await WithUnitOfWorkAsync(() => _products.InsertAsync(new Producto(productId, "22334455", "Fideos")));
        using var user = AsUser(Guid.NewGuid());
        var item = await _service.CreateAsync(new CreatePantryItemDto { ProductId = productId });
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var multiline = "Primera línea\nSegunda línea";
        (await _service.UpdateAsync(item.Id, new UpdatePantryItemDto { PrivateNote = multiline }))
            .PrivateNote.ShouldBe(multiline);
        var quantityEdit = JsonSerializer.Deserialize<UpdatePantryItemDto>("""{"quantity":3}""", json)!;
        var quantityResult = await _service.UpdateAsync(item.Id, quantityEdit);
        quantityResult.Quantity.ShouldBe(3);
        quantityResult.PrivateNote.ShouldBe(multiline);

        var maxNote = new string('x', PantryItemConsts.MaxPrivateNoteLength);
        await _service.UpdateAsync(item.Id, new UpdatePantryItemDto { PrivateNote = maxNote });
        await Should.ThrowAsync<AbpValidationException>(() =>
            _service.UpdateAsync(item.Id, new UpdatePantryItemDto { PrivateNote = maxNote + "x" }));
        (await _service.GetAsync(item.Id)).PrivateNote.ShouldBe(maxNote);

        var clearNote = JsonSerializer.Deserialize<UpdatePantryItemDto>("""{"privateNote":null}""", json)!;
        await _service.UpdateAsync(item.Id, clearNote);
        (await _service.GetAsync(item.Id)).PrivateNote.ShouldBeNull();

        await _service.UpdateAsync(item.Id, new UpdatePantryItemDto { PrivateNote = "Otra nota" });
        await _service.UpdateAsync(item.Id, new UpdatePantryItemDto { PrivateNote = " \n\t " });
        (await _service.GetAsync(item.Id)).PrivateNote.ShouldBeNull();
    }

    [Fact]
    public void Audit_serialization_omits_note_text_in_create_and_update_requests()
    {
        var serializer = GetRequiredService<IAuditSerializer>();
        const string note = "texto privado";
        serializer.Serialize(new UpdatePantryItemDto { PrivateNote = note }).ShouldNotContain(note);
        serializer.Serialize(new CreatePantryItemDto { PrivateNote = note }).ShouldNotContain(note);
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
