using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using LosTresMosqueteros.Preferences;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Validation;
using Volo.Abp.Guids;
using Volo.Abp.ObjectMapping;

namespace LosTresMosqueteros.Productos;

[Authorize]
public class GuardarProductoExternoAppService : LosTresMosqueterosAppService, IGuardarProductoExternoAppService
{
    private readonly IProductoRepository _repository;
    private readonly IExternalProductCatalogClient _catalog;
    private readonly IGuidGenerator _guids;
    private readonly IObjectMapper _mapper;
    private readonly IFoodWarningEvaluator _warnings;

    public GuardarProductoExternoAppService(IProductoRepository repository, IExternalProductCatalogClient catalog,
        IGuidGenerator guids, IObjectMapper mapper, IFoodWarningEvaluator warnings)
    {
        _repository = repository;
        _catalog = catalog;
        _guids = guids;
        _mapper = mapper;
        _warnings = warnings;
    }

    public async Task<GuardarProductoExternoResultadoDto> SaveAsync(ConsultarProductoExternoDto input)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errores, true))
            throw new AbpValidationException("El código no es válido.", errores);

        var existente = await _repository.FindAsync(x => x.CodigoBarras == input.CodigoBarras);
        if (existente != null) return await ResultadoAsync(existente);

        var externo = await _catalog.GetByBarcodeAsync(input.CodigoBarras);
        if (externo == null) return new() { Resultado = ResultadoConsultaExterna.NoEncontrado };
        if (externo.Resultado != ResultadoConsultaExterna.Encontrado)
            return new() { Resultado = externo.Resultado };
        if (externo.CodigoBarras != input.CodigoBarras)
            return new() { Resultado = ResultadoConsultaExterna.ServicioNoDisponible };

        var producto = Producto.DesdeCatalogo(_guids.Create(), input.CodigoBarras,
            externo.Nombre, externo.Marca, externo.ImagenUrl, externo.Categorias, externo.Ingredientes,
            externo.Alergenos is { Count: > 0 } ? string.Join(", ", externo.Alergenos) : null,
            externo.NutriScore, externo.GrupoNova);
        return await ResultadoAsync(await _repository.InsertarUnicoAsync(producto, devolverExistente: true));
    }

    // La evaluación usa los datos del producto ya guardado (nuevo o existente), sin consultas externas extra,
    // y las preferencias actuales del usuario de la sesión. No se persiste.
    private async Task<GuardarProductoExternoResultadoDto> ResultadoAsync(Producto producto) => new()
    {
        Resultado = ResultadoConsultaExterna.Encontrado,
        Producto = _mapper.Map<Producto, ProductoDto>(producto),
        Advertencias = await _warnings.EvaluateAsync(producto.Ingredientes, producto.Alergenos)
    };
}