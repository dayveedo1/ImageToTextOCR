using Microsoft.AspNetCore.Mvc;

namespace ImgToText.Data
{
    public interface IImgToText
    {
        public string ConvertImageToText(IFormFile file);

        Task<ViewApiResponse> PostImage(IFormFile file);

        Task<ViewApiResponse> GetAllRecords();

        Task<ViewApiResponse> GetRecordsByTrackingId(string trackingId);

        Task<ViewApiResponse> GetRecordById(int id);

        Task<ViewApiResponse> UpdateRecordByTrackingId(TextDataDTO data);


    }
}
