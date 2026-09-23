using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace LosTresMosqueteros.Productos;

public class Producto : FullAuditedAggregateRoot<Guid>
{
    public string CodigoBarras { get; private set; }
    public string Nombre { get; private set; }
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

    public void SetNombre(string nombre)
    {
        Check.NotNullOrWhiteSpace(nombre, nameof(nombre), ProductoConsts.MaxNombreLength);
        Nombre = nombre.Trim();
    }
}