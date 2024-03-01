using System.Threading.Tasks;
using PointsServer.Common.Dtos;
using PointsServer.Samples.Users.Dto;

namespace PointsServer.Samples.Users;

public interface IUserAppService
{
    /// <summary>
    ///     add or update
    /// </summary>
    /// <param name="userSourceInput"></param>
    /// <returns></returns>
    Task<UserDto> AddUserAsync(UserSourceInput userSourceInput);

    /// <summary>
    ///     query single user by id
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<UserDto> GetById(string userId);
    
    /// <summary>
    ///     query pager
    /// </summary>
    /// <param name="requestDto"></param>
    /// <returns></returns>
    Task<PageResultDto<UserDto>> QueryUserPagerAsync(UserQueryRequestDto requestDto);
}
