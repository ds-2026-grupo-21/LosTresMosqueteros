using LosTresMosqueteros.Samples;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore.Domains;

[Collection(LosTresMosqueterosTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<LosTresMosqueterosEntityFrameworkCoreTestModule>
{

}
