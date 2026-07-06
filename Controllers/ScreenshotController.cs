using Microsoft.AspNetCore.Mvc;

namespace Playwright.Controllers {
    [ApiController]
    [Route( "[controller]" )]
    public class ScreenshotController : ControllerBase {
        private readonly ScreenshotService _service;

        public ScreenshotController( ScreenshotService service ) {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get( [FromQuery] string url ) {
            try {
                var image = await _service.CaptureAsync( url );
                return File( image, "image/png" );
            } catch ( Exception ex ) {
                return BadRequest( ex.Message );
            }
        }
    }
}