using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore;

[CollectionDefinition(LosTresMosqueterosTestConsts.CollectionDefinitionName)]
public class LosTresMosqueterosEntityFrameworkCoreCollection : ICollectionFixture<LosTresMosqueterosEntityFrameworkCoreFixture>
{

}
