using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace PointsServer.Points;

[Collection(PointsServerTestConsts.CollectionDefinitionName)]
public class PointsServiceTest : PointsServerApplicationTestBase
{
    public PointsServiceTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task GetMyPointsAsyncTest()
    {
    }
}