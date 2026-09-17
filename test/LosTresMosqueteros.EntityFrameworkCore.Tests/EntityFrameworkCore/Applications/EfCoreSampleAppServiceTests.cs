using LosTresMosqueteros.Samples;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore.Applications;

[Collection(LosTresMosqueterosTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<LosTresMosqueterosEntityFrameworkCoreTestModule>
{

}
