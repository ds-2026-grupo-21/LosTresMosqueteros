using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly GuardarProductoExternoAppService _service;
    public GuardarProductoExternoAppServiceTests()
    {
        var mapper = Substitute.For<IObjectMapper>();
        mapper.Map<Producto, ProductoDto>(Arg.Any<Producto>()).Returns(x => new ProductoDto { Id = x.Arg<Producto>().Id });
        _service = new(_repo, _catalog, SimpleGuidGenerator.Instance, mapper);
    }

    [Fact]
    public async Task Guarda_los_datos_parciales_sin_inventar_nombre()
    {
        _catalog.GetByBarcodeAsync(Codigo).Returns(new ExternalProductDto { CodigoBarras = Codigo,
            Resultado = ResultadoConsultaExterna.Encontrado, Ingredientes = "Maní", Alergenos = new() { "en:peanuts" } });
        _repo.InsertarUnicoAsync(Arg.Any<Producto>(), true).Returns(x => x.Arg<Producto>());
        var result = await _service.SaveAsync(new() { CodigoBarras = Codigo });
        result.Producto.ShouldNotBeNull();
        await _repo.Received(1).InsertarUnicoAsync(Arg.Is<Producto>(p => p.Nombre == null &&
            p.CodigoBarras == Codigo && p.Ingredientes == "Maní" && p.Alergenos == "en:peanuts"), true);
    }

    [Theory]
    [InlineData(ResultadoConsultaExterna.LimiteDeSolicitudesExcedido)]
    [InlineData(ResultadoConsultaExterna.ServicioNoDisponible)]
    [InlineData(ResultadoConsultaExterna.NoEncontrado)]
    public async Task Una_falla_no_guarda_filas(ResultadoConsultaExterna estado)
    {
        _catalog.GetByBarcodeAsync(Codigo).Returns(new ExternalProductDto { Resultado = estado });
        (await _service.SaveAsync(new() { CodigoBarras = Codigo })).Resultado.ShouldBe(estado);
        await _repo.DidNotReceive().InsertarUnicoAsync(Arg.Any<Producto>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task No_encontrado_o_codigo_distinto_no_se_guardan()
    {
        (await _service.SaveAsync(new() { CodigoBarras = Codigo })).Resultado.ShouldBe(ResultadoConsultaExterna.NoEncontrado);
        _catalog.GetByBarcodeAsync(Codigo).Returns(new ExternalProductDto {
            CodigoBarras = "3017620422003", Resultado = ResultadoConsultaExterna.Encontrado });
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
