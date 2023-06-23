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

        private readonly IImgToText textToImg;

        public ImgToTextController(IImgToText textToImg)
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

        [HttpGet]
        public async Task<ActionResult<ViewApiResponse>> GetAllRecords()
        {
            var response = await textToImg.GetAllRecords();

            if (response.ResponseCode == 500)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return StatusCode(StatusCodes.Status200OK, response);
        }


        [HttpGet("{trackingId}")]
        public async Task<ActionResult<ViewApiResponse>> GetRecordsByTrackingId(string trackingId)
        {
            var response = await textToImg.GetRecordsByTrackingId(trackingId);

            if (response.ResponseCode == 500)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            else if (response.ResponseCode == 404)
                return StatusCode(StatusCodes.Status404NotFound, response);

            else if (response.ResponseCode == 400)
                return StatusCode(StatusCodes.Status400BadRequest, response);

            return StatusCode(StatusCodes.Status200OK, response);

        }

        [HttpGet("id/{id}")]
        public async Task<ActionResult<ViewApiResponse>> GetRecordById(int id)
        {
            var response = await textToImg.GetRecordById(id);

            if (response.ResponseCode == 500)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            else if (response.ResponseCode == 404)
                return StatusCode(StatusCodes.Status404NotFound, response);

            else if (response.ResponseCode == 400)
                return StatusCode(StatusCodes.Status400BadRequest, response);

            return StatusCode(StatusCodes.Status200OK, response);

        }



        [HttpPut]
        public async Task<ActionResult<ViewApiResponse>> UpdateRecordByTrackingId(TextDataDTO data)
        {
            var response = await textToImg.UpdateRecordByTrackingId(data);

            if (response.ResponseCode == 500)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            else if (response.ResponseCode == 404)
                return StatusCode(StatusCodes.Status404NotFound, response);

            else if (response.ResponseCode == 400)
                return StatusCode(StatusCodes.Status400BadRequest, response);

            return StatusCode(StatusCodes.Status200OK, response);

        }
    }
}
