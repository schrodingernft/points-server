using Nest;

namespace PointsServer.Users;

public class UserAddress
{
    [Keyword]public string ChainId { get; set; }
    [Keyword] public string Address { get; set; }
}