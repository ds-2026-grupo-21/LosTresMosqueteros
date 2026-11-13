using System.Threading.Tasks;

namespace LosTresMosqueteros.Productos;

public interface IExternalProductSearchClient
{
    Task<ExternalProductSearchResultDto> SearchAsync(string text, int page, int pageSize);
}
