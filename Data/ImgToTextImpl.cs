using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Text;
using Tesseract;

namespace ImgToText.Data
{
    public class TextToImgImpl : ITextToImg
    {

        private readonly ImgToTextDbContext context;

        public TextToImgImpl(ImgToTextDbContext context)
        {
            this.context = context;
        }
        public string ConvertImageToText(IFormFile file)
        {
            string text = string.Empty;

            var tessdataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");


            using (var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default))
            {
                using (var imgStream = new MemoryStream())
                {
                    file.CopyTo(imgStream);
                    using (var img = Pix.LoadFromMemory(imgStream.ToArray()))
                    {
                        using (var page = engine.Process(img))
                        {
                            text = page.GetText();
                        }

                    }
                }
            }

            return text;

        }

        public async Task<ViewApiResponse> PostImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new ViewApiResponse
                {
                    ResponseCode = 400,
                    ResponseMessage = "Bad Request, Select a valid Image file",
                    ResponseData = { }
                };


            try
            {
                var convertToText = ConvertImageToText(file);

                string[] results = Array.Empty<string>();

                if (convertToText.Contains("UPS"))
                {
                    results = convertToText.Split(new string[] { "\n" }, StringSplitOptions.None);

                    var findCourierIndex = Array.FindIndex(results, x => x.Contains("UPS GROUND"));

                    StringBuilder fromText = new();
                    fromText.Append(results[0]
                        + string.Empty
                        + results[1]
                        + string.Empty
                        + results[2]
                        + string.Empty
                        + results[3]
                        + string.Empty
                        + results[4]
                        + string.Empty
                        + results[5]);

                    var findShipToIndex = Array.FindIndex(results, x => x.Contains("SHIP"));

                    StringBuilder toText = new();
                    toText.Append(results[findShipToIndex].Equals(" ") ? results[findShipToIndex + 1] : results[findShipToIndex + 2]
                        + string.Empty
                        + results[findShipToIndex + 3]
                        + string.Empty
                        + results[findShipToIndex + 4]
                        + string.Empty
                        + results[findShipToIndex + 5]
                        + string.Empty
                        + results[findShipToIndex + 6]
                        + string.Empty
                        + results[findShipToIndex + 7]);

                    var findTrackingIdIndex = Array.FindIndex(results, x => x.Contains("TRACKING"));

                    TextData textData = new()
                    {
                        Text = "",
                        Courier = results[findCourierIndex].ToString(),
                        From = fromText.ToString(),

                        //To = results[findShipToIndex].ToString() is " " ? results[index + 1].ToString() : results[findShipToIndex + 1],
                        //To = results[findShipToIndex].Equals(" ") ? results[findShipToIndex + 1] : results[findShipToIndex + 2],  //|| results[index + 1].ToString().Equals(" ") 
                        //To = indexText,
                        To = toText.ToString(),


                        TrackingId = results[findTrackingIdIndex][^4..].ToString()
                    };

                    await context.TextData.AddAsync(textData);
                    await context.SaveChangesAsync();

                    return new ViewApiResponse
                    {
                        ResponseCode = 200,
                        ResponseMessage = "Success",
                        ResponseData = textData
                    };
                }

                if (convertToText.Contains("USPS"))
                {
                    results = convertToText.Split(new string[] { "\n" }, StringSplitOptions.None);

                    var findCourierIndex = Array.FindIndex(results, x => x.Contains("USPS"));
                    var courierText = results[findCourierIndex].Split(" ");

                    StringBuilder fromTextExtraction = new();
                    fromTextExtraction.Append(results[findCourierIndex + 2]
                        + string.Empty
                        + results[findCourierIndex + 3]
                        + string.Empty 
                        + string.Empty
                        + results[findCourierIndex + 4]);

                    //fromTextExtraction.Append(results[findCourierIndex + 3]);
                    //fromTextExtraction.Append(results[findCourierIndex + 4]);

                    var findShipToIndex = Array.FindIndex(results, x => x.Contains("SHIP"));

                    StringBuilder toTextExtraction = new();
                    toTextExtraction.Append(results[findShipToIndex + 1]
                        + string.Empty
                        + results[findShipToIndex + 2]
                        + string.Empty
                        + results[findShipToIndex + 3]
                        + string.Empty
                        + results[findShipToIndex + 4]
                        + string.Empty
                        + results[findShipToIndex + 5]
                        + string.Empty
                        + results[findShipToIndex + 6]);

                    //toTextExtraction.Append(results[26][6..]);
                    //toTextExtraction.Append("," + " ");
                    //toTextExtraction.Append(results[28] + "," + " ");
                    //toTextExtraction.Append(results[29]);

                    var findTrackingIdIndex = Array.FindIndex(results, x => x.Contains("TRACKING"));
                    var trackingId = string.Empty;

                    for (int i = findTrackingIdIndex; i < results.Length; i++)
                    {
                        if (results[i].Length == 27)
                        {
                            trackingId = results[i];
                            break;
                        }
                    }

                    TextData textData = new()
                    {
                        Text = "",
                        Courier = courierText[0].ToString(),
                        From = fromTextExtraction.ToString(),
                        To = toTextExtraction.ToString(),
                        TrackingId = trackingId.ToString()  //results[34]
                    };

                    await context.TextData.AddAsync(textData);
                    await context.SaveChangesAsync();

                    return new ViewApiResponse
                    {
                        ResponseCode = 200,
                        ResponseMessage = "Success",
                        ResponseData = textData
                    };
                }

                if (convertToText.Contains("FedEx"))
                {
                    results = convertToText.Split(new string[] { "\n" }, StringSplitOptions.None);

                }


                //string[] results = convertToText.Split(new string[] { "\n" }, StringSplitOptions.None);
                //convertToText.Split("\n\n");
                //strings.Add()

                TextData data = new()
                {
                    Text = convertToText
                };

                await context.TextData.AddAsync(data);
                await context.SaveChangesAsync();

                return new ViewApiResponse
                {
                    ResponseCode = 200,
                    ResponseMessage = "Success",
                    ResponseData = data
                };
            }
            catch (Exception ex)
            {
                return new ViewApiResponse
                {
                    ResponseCode = 500,
                    ResponseMessage = $"Internal Server Error, {ex.Message}",
                    ResponseData = { }
                };
            }

        }




    }
}
