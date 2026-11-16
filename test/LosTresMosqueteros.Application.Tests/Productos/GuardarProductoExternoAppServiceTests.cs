using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LosTresMosqueteros.Preferences;
using NSubstitute;
using Shouldly;
using Volo.Abp.Guids;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Validation;
using Xunit;

namespace LosTresMosqueteros.Productos;

public class GuardarProductoExternoAppServiceTests
{
    private const string Codigo = "0009800800049";
    private readonly IProductoRepository _repo = Substitute.For<IProductoRepository>();
    private readonly IExternalProductCatalogClient _catalog = Substitute.For<IExternalProductCatalogClient>();
    private readonly IFoodWarningEvaluator _warnings = Substitute.For<IFoodWarningEvaluator>();
    private readonly GuardarProductoExternoAppService _service;
    public GuardarProductoExternoAppServiceTests()
    {
        var mapper = Substitute.For<IObjectMapper>();
        mapper.Map<Producto, ProductoDto>(Arg.Any<Producto>()).Returns(x => new ProductoDto { Id = x.Arg<Producto>().Id });
        _warnings.EvaluateAsync(Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(Task.FromResult(new FoodWarningDto { HasPreferences = false }));
        _service = new(_repo, _catalog, SimpleGuidGenerator.Instance, mapper, _warnings);
    }

    [Fact]
    public async Task Guarda_los_datos_parciales_sin_inventar_nombre()
    {
        _catalog.GetByBarcodeAsync(Codigo).Returns(new ExternalProductDto
        {
            CodigoBarras = Codigo,
            Resultado = ResultadoConsultaExterna.Encontrado,
            Ingredientes = "Maní",
            Alergenos = new() { "en:peanuts" }
        });
        _repo.InsertarUnicoAsync(Arg.Any<Producto>(), true).Returns(x => x.Arg<Producto>());
        var result = await _service.SaveAsync(new() { CodigoBarras = Codigo });
        result.Producto.ShouldNotBeNull();
        await _repo.Received(1).InsertarUnicoAsync(Arg.Is<Producto>(p => p.Nombre == null &&
            p.CodigoBarras == Codigo && p.Ingredientes == "Maní" && p.Alergenos == "en:peanuts"), true);
    }

    [Fact]
    public async Task Al_guardar_uno_nuevo_adjunta_las_advertencias_con_los_datos_guardados()
    {
        var advertencias = new FoodWarningDto { HasPreferences = true };
        _warnings.EvaluateAsync("Maní", "en:peanuts").Returns(Task.FromResult(advertencias));
        _catalog.GetByBarcodeAsync(Codigo).Returns(new ExternalProductDto
        {
            CodigoBarras = Codigo,
            Resultado = ResultadoConsultaExterna.Encontrado,
            Ingredientes = "Maní",
            Alergenos = new() { "en:peanuts" }
        });
        _repo.InsertarUnicoAsync(Arg.Any<Producto>(), true).Returns(x => x.Arg<Producto>());

        var result = await _service.SaveAsync(new() { CodigoBarras = Codigo });

        result.Advertencias.ShouldBeSameAs(advertencias);
        await _warnings.Received(1).EvaluateAsync("Maní", "en:peanuts");
        await _catalog.Received(1).GetByBarcodeAsync(Codigo);
    }

    [Fact]
    public async Task Al_guardar_uno_existente_tambien_recalcula_las_advertencias_sin_consultar_al_proveedor()
    {
        var advertencias = new FoodWarningDto { HasPreferences = true };
        var existing = new Producto(Guid.NewGuid(), Codigo, "Galletas", "Maní", "en:peanuts");
        _warnings.EvaluateAsync("Maní", "en:peanuts").Returns(Task.FromResult(advertencias));
        _repo.FindAsync(Arg.Any<Expression<Func<Producto, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _service.SaveAsync(new() { CodigoBarras = Codigo });

        result.Advertencias.ShouldBeSameAs(advertencias);
        await _warnings.Received(1).EvaluateAsync("Maní", "en:peanuts");
        await _catalog.DidNotReceive().GetByBarcodeAsync(Arg.Any<string>());
    }

    [Theory]
    [InlineData(ResultadoConsultaExterna.LimiteDeSolicitudesExcedido)]
    [InlineData(ResultadoConsultaExterna.ServicioNoDisponible)]
    [InlineData(ResultadoConsultaExterna.NoEncontrado)]
    public async Task Una_falla_no_guarda_filas(ResultadoConsultaExterna estado)
    {
        _catalog.GetByBarcodeAsync(Codigo).Returns(new ExternalProductDto { Resultado = estado });
        var result = await _service.SaveAsync(new() { CodigoBarras = Codigo });
        result.Resultado.ShouldBe(estado);
        result.Advertencias.ShouldBeNull();
        await _repo.DidNotReceive().InsertarUnicoAsync(Arg.Any<Producto>(), Arg.Any<bool>());
        await _warnings.DidNotReceiveWithAnyArgs().EvaluateAsync(default, default);
    }

    [Fact]
    public async Task No_encontrado_o_codigo_distinto_no_se_guardan()
    {
        (await _service.SaveAsync(new() { CodigoBarras = Codigo })).Resultado.ShouldBe(ResultadoConsultaExterna.NoEncontrado);
        _catalog.GetByBarcodeAsync(Codigo).Returns(new ExternalProductDto
        {
            CodigoBarras = "3017620422003",
            Resultado = ResultadoConsultaExterna.Encontrado
        });
        (await _service.SaveAsync(new() { CodigoBarras = Codigo })).Resultado.ShouldBe(ResultadoConsultaExterna.ServicioNoDisponible);
        await _repo.DidNotReceive().InsertarUnicoAsync(Arg.Any<Producto>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task Existente_no_vuelve_a_consultar_al_proveedor()
    {
        var existing = new Producto(Guid.NewGuid(), Codigo, "Maní");
        _repo.FindAsync(Arg.Any<Expression<Func<Producto, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(existing);
        (await _service.SaveAsync(new() { CodigoBarras = Codigo })).Producto!.Id.ShouldBe(existing.Id);
        await _catalog.DidNotReceive().GetByBarcodeAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Codigo_invalido_no_consulta_ni_guarda()
    {
        await Should.ThrowAsync<AbpValidationException>(() => _service.SaveAsync(new() { CodigoBarras = "abc" }));
        await _catalog.DidNotReceive().GetByBarcodeAsync(Arg.Any<string>());
    }
}