using System.Threading.Tasks;

namespace LosTresMosqueteros.Productos;

public interface IExternalProductCatalogClient
{
    Task<ExternalProductDto?> GetByBarcodeAsync(string barcode);
}