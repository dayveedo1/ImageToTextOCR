using Microsoft.AspNetCore.Mvc;

namespace ImgToText.Data
{
    public interface IImgToText
    {
        public string ConvertImageToText(IFormFile file);

        Task<ViewApiResponse> PostImage(IFormFile file);
    }
}
