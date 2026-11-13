using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace LosTresMosqueteros.Productos;

public class Producto : FullAuditedAggregateRoot<Guid>
{
    public string CodigoBarras { get; private set; }
    public string? Nombre { get; private set; }
    public string? Marca { get; private set; }
    public string? ImagenUrl { get; private set; }
    public string? Categorias { get; private set; }
    public string? NutriScore { get; private set; }
    public int? GrupoNova { get; private set; }
    public string? Ingredientes { get; private set; }
    public string? Alergenos { get; private set; }

    protected Producto()
    {
        /* Este constructor vacío es requerido por Entity Framework Core */
    }

    public Producto(
        Guid id,
        string codigoBarras,
        string nombre,
        string? ingredientes = null,
        string? alergenos = null) : base(id)
    {
        SetCodigoBarras(codigoBarras);
        SetNombre(nombre);
        Ingredientes = ingredientes;
        Alergenos = alergenos;
    }

    public void SetCodigoBarras(string codigoBarras)
    {
        Check.NotNullOrWhiteSpace(codigoBarras, nameof(codigoBarras), ProductoConsts.MaxCodigoBarrasLength);
        CodigoBarras = codigoBarras.Trim();
    }

    public static Producto DesdeCatalogo(Guid id, string codigo, string? nombre, string? marca,
        string? imagen, string? categorias, string? ingredientes, string? alergenos,
        string? nutriScore, int? grupoNova)
    {
        var producto = new Producto { Id = id };
        producto.SetCodigoBarras(codigo);
        producto.Nombre = Disponible(nombre);
        producto.Marca = Disponible(marca);
        producto.ImagenUrl = Disponible(imagen);
        producto.Categorias = Disponible(categorias);
        producto.Ingredientes = Disponible(ingredientes);
        producto.Alergenos = Disponible(alergenos);
        producto.NutriScore = nutriScore is "a" or "b" or "c" or "d" or "e" ? nutriScore : null;
        producto.GrupoNova = grupoNova is >= 1 and <= 4 ? grupoNova : null;
        return producto;
    }

    private static string? Disponible(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void SetNombre(string nombre)
    {
        Check.NotNullOrWhiteSpace(nombre, nameof(nombre), ProductoConsts.MaxNombreLength);
        Nombre = nombre.Trim();
    }
    public void SetIngredientes(string? ingredientes)
    {
        Ingredientes = ingredientes;
    }

    public void SetAlergenos(string? alergenos)
    {
        Alergenos = alergenos;
    }
}
