using ImgToText.Data;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ImgToText.Controllers
{
    [ApiController]
    [EnableCors("AllowAll")]
    [Route("/api/[controller]")]
    public class ImgToTextController : Controller
    {

        private readonly ITextToImg textToImg;

        public ImgToTextController(ITextToImg textToImg)
        {
            this.textToImg = textToImg;
        }



        [HttpPost]
        public async Task<ActionResult<ViewApiResponse>> PostImage(IFormFile file)
        {
            var response = await textToImg.PostImage(file);

            if (response.ResponseCode == 500)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            else if (response.ResponseCode == 400)
                return StatusCode(StatusCodes.Status400BadRequest, response);

            return StatusCode(StatusCodes.Status200OK, response);
        }
    }
}
