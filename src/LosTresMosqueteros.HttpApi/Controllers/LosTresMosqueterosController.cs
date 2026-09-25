using LosTresMosqueteros.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace LosTresMosqueteros.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class LosTresMosqueterosController : AbpControllerBase
{
    protected LosTresMosqueterosController()
    {
        LocalizationResource = typeof(LosTresMosqueterosResource);
    }
}
