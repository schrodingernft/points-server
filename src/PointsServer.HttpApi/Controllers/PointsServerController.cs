using PointsServer.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace PointsServer.Controllers;

public class PointsServerController : AbpControllerBase
{
    protected PointsServerController()
    {
        LocalizationResource = typeof(PointsServerResource);
    }
}