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
    public class ImgToTextImpl : IImgToText
    {

        private readonly ImgToTextDbContext context;

        public ImgToTextImpl(ImgToTextDbContext context)
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
                        + " "
                        + results[1]
                        + " "
                        + results[2]
                        + " "
                        + results[3]
                        + " "
                        + results[4]
                        + " "
                        + results[5]);

                    var findShipToIndex = Array.FindIndex(results, x => x.Contains("SHIP"));

                    StringBuilder toText = new();
                    toText.Append(results[findShipToIndex].Equals(" ") ? results[findShipToIndex + 1] : results[findShipToIndex + 2]
                        + " "
                        + results[findShipToIndex + 3]
                        + " "
                        + results[findShipToIndex + 4]
                        + " "
                        + results[findShipToIndex + 5]
                        + " "
                        + results[findShipToIndex + 6]
                        + " "
                        + results[findShipToIndex + 7]);

                    var findTrackingIdIndex = Array.FindIndex(results, x => x.Contains("TRACKING"));

                    TextData textData = new()
                    {
                        Text = "",
                        Courier = results[findCourierIndex].ToString(),
                        From = fromText.ToString(),
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
                        + " "
                        + results[findCourierIndex + 3]
                        + " "
                        + " "
                        + results[findCourierIndex + 4]);

                    //fromTextExtraction.Append(results[findCourierIndex + 3]);
                    //fromTextExtraction.Append(results[findCourierIndex + 4]);

                    var findShipToIndex = Array.FindIndex(results, x => x.Contains("SHIP"));

                    StringBuilder toTextExtraction = new();
                    toTextExtraction.Append(results[findShipToIndex + 1]
                        + " "
                        + results[findShipToIndex + 2]
                        + " "
                        + results[findShipToIndex + 3]
                        + " "
                        + results[findShipToIndex + 4]
                        + " "
                        + results[findShipToIndex + 5]
                        + " "
                        + results[findShipToIndex + 6]);

                    var findTrackingIdIndex = Array.FindIndex(results, x => x.Contains("TRACKING"));
                    var trackingId = string.Empty;

                    for (int i = findTrackingIdIndex; i < results.Length; i++)
                    {
                        if (results[i].Length == 27)
                            trackingId = results[i];
                        break;
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

                if (convertToText.Contains("FedEx") || convertToText.Contains("Express"))
                {
                    results = convertToText.Split(new string[] { "\n" }, StringSplitOptions.None);

                    var findShRapaIndex = Array.FindIndex(results, x => x.Contains("SH RAPA") || x.Contains("SH "));
                    var trackingIdText = string.Empty;

                    for (int i = findShRapaIndex - 1; i < findShRapaIndex; i--)
                    {
                        if (results[i].Length > 14 && !results[i].Equals(""))
                        {
                            trackingIdText = results[i];
                            break;
                        }
                    }

                    var splitTrackingId = trackingIdText.Split(" ");
                    StringBuilder trackingId = new();
                    var trackingIdTextBuilder = splitTrackingId[0].Length < 4 ? trackingId.Append(splitTrackingId[1]
                        + " "
                        + splitTrackingId[2]
                        + " "
                        + splitTrackingId[3]) :

                        trackingId.Append(splitTrackingId[0]
                        + " "
                        + splitTrackingId[1]
                        + " "
                        + splitTrackingId[2]);

                    var trackToIndex = Array.FindIndex(results, x => x.Contains("|||||||") || x.Contains("IIIIIII"));
                    StringBuilder toText = new();

                    for (int i = trackToIndex - 1; i < trackToIndex; i--)
                    {
                        if (results[i].Contains("STATES US"))
                            break;

                        toText.Append(results[i] + " ");
                    }

                    string toTextStr = toText.ToString();
                    var splitToTextStr = toTextStr.Split(" ");

                    var finalToStr = splitToTextStr.Reverse();
                    var strToText = String.Join(" ", finalToStr);

                    var trackFromTextIndex = Array.FindIndex(results, x => x.Contains("ORIGIN"));

                    StringBuilder fromText = new();

                    for (int i = trackFromTextIndex + 1; i < results.Length; i++)
                    {
                        if (results[i].Contains("STATES US"))
                            break;

                        fromText.Append(results[i] + " ");
                    }

                    var fromTextStr = fromText.ToString();
                    var splitFromText = fromTextStr.Split(" ");

                    for (int i = 0; i < splitFromText.Count(); i++)
                    {
                        if (splitFromText[i].Contains("ACTW"))
                        {
                            splitFromText[i] = " ";
                            splitFromText[i + 1] = " ";
                            splitFromText[i + 2] = " ";
                            splitFromText[i + 3] = " ";
                            splitFromText[i + 4] = " ";
                            break;
                        }
                        continue;
                    }

                    var finalFromStr = String.Join(" ", splitFromText);

                    TextData textData = new()
                    {
                        Text = "",
                        Courier = "FedEx",
                        From = finalFromStr.ToString(),
                        To = strToText.ToString(),
                        //TrackingId = trackingId.ToString()
                        TrackingId = trackingIdTextBuilder.ToString()
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

                //TextData data = new()
                //{
                //    Text = convertToText
                //};

                //await context.TextData.AddAsync(data);
                //await context.SaveChangesAsync();

                return new ViewApiResponse
                {
                    ResponseCode = 400,
                    ResponseMessage = "Bad Request",
                    ResponseData = { }
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
