using LosTresMosqueteros.Localization;
using Volo.Abp.Application.Services;

namespace LosTresMosqueteros;

/* Inherit your application services from this class.
 */
public abstract class LosTresMosqueterosAppService : ApplicationService
{
    protected LosTresMosqueterosAppService()
    {
        LocalizationResource = typeof(LosTresMosqueterosResource);
    }
}