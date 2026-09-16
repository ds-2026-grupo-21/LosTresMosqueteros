using Microsoft.Extensions.Localization;
using LosTresMosqueteros.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace LosTresMosqueteros;

[Dependency(ReplaceServices = true)]
public class LosTresMosqueterosBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<LosTresMosqueterosResource> _localizer;

    public LosTresMosqueterosBrandingProvider(IStringLocalizer<LosTresMosqueterosResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
