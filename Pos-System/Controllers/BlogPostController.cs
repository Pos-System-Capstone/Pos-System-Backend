using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pos_System.API.Constants;
using Pos_System.API.Enums;
using Pos_System.API.Validators;

namespace Pos_System.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogPostController : BaseController<BlogPostController>
    {
        public BlogPostController(ILogger<BlogPostController> logger) : base(logger)
        {
        }

        //[CustomAuthorize(RoleEnum.SysAdmin)]
        //[HttpGet(ApiEndPointConstant.BlogPost.BlogPostEndpoint)]
    }
}
