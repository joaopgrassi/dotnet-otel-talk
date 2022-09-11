using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers
{
    public record UserClaimsResponse(string Type, string Value);
    
    [Authorize]
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        /// <summary>
        /// Call this endpoint to see all the logged-in user claims!
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(List<UserClaimsResponse>), Status200OK)]
        public IActionResult GetUserClaims()
        {
            return Ok(User.Claims.Select(c => new UserClaimsResponse(c.Type, c.Value)));
        }
    }
}   
