using System.Linq;
using System.Threading.Tasks;
using DataLayer.Context;
using Microsoft.AspNetCore.Mvc;

namespace Asancode.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductApiController : ControllerBase
    {
        private AsanwebContext _context;

        public ProductApiController(AsanwebContext context)
        {
            _context = context;
        }

        [Produces("application/json")]
        [HttpGet("search")]
        public async Task<IActionResult> SearchProduct()
        {
            try
            {
                var term = HttpContext.Request.Query["term"].ToString();
                var courseTitle = _context.Course
                    .Where(c => c.CourseTitle.Contains(term))
                    .Select(c => c.CourseTitle)
                    .ToList();
                return Ok(courseTitle);
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}