using System.Drawing;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using PointsServer.Common.GraphQL;
using PointsServer.Points.Dtos;
using PointsServer.Points.Provider;
using Xunit;
using Xunit.Abstractions;

namespace PointsServer;

public class DomainInfoTests : PointsServerDomainTestBase
{
    //private IPointsProvider _pointsProvider;
    private IGraphQLClient _client;
    public DomainInfoTests(ITestOutputHelper output) : base(output)
    {
       // _pointsProvider = GetRequiredService<IPointsProvider>();
        _client = GetRequiredService<IGraphQLClient>();
    }

    [Fact]
    public async Task Domain_test()
    {
        var a =await Graphics();
        
    }

    public async Task<OperatorDomainDto> Graphics()
    {
        var _graphQlHelper = new GraphQlHelper(_client,null);
        var indexerResult = await _graphQlHelper.QueryAsync<OperatorDomainIndexerQueryDto>(new GraphQLRequest
        {
            Query =
                @"query($domain:String!){
                    operatorDomainInfo(input: {domain:$domain}){
                        id,
                        domain,
                        depositAddress,
                        inviterAddress,
    					dappId               
                }
            }",
            Variables = new
            {
                domain ="linnnnnnnnnnnnnnnnk8.schrodingernft.ai"
            }
        });
        return indexerResult.OperatorDomainInfo;
    }


}