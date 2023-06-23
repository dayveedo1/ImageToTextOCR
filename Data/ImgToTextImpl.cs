using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Primitives;
using System.Linq;
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

                    var findShipToIndex = Array.FindIndex(results, x => x.Contains("SHIP"));

                    StringBuilder fromText = new();

                    for (int i = 0; i < findShipToIndex; i++)
                    {
                        if (i >= findShipToIndex)
                            break;

                        fromText.Append(results[i] + " ");
                    }

                    StringBuilder toText = new();

                    if (results[findShipToIndex + 1].Equals(""))
                    {
                        toText.Append(
                            results[findShipToIndex + 2]
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
                    }
                    else
                    {
                        toText.Append(
                            results[findShipToIndex + 1]
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
                    }

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

                    var findTrackingIdIndex = Array.FindIndex(results, x => x.Contains("TRACKING"));
                    var startingIndexforToTextData = findTrackingIdIndex - 8;

                    StringBuilder toTextExtraction = new();

                    for (int i = startingIndexforToTextData; i < findTrackingIdIndex; i++)
                    {

                        if (results[i].Equals(findTrackingIdIndex))
                            break;

                        toTextExtraction.Append(results[i].ToString() + " ");
                    }

                    var trackingId = string.Empty;

                    for (int i = findTrackingIdIndex; i < results.Length; i++)
                    {
                        if (results[i].Length == 27)
                        {
                            trackingId = results[i];
                            break;
                        }

                        continue;
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

                if (convertToText.Contains("FedEx") || convertToText.Contains("Express") || convertToText.Contains("EXPRESS"))
                {
                    results = convertToText.Split(new string[] { "\n" }, StringSplitOptions.None);

                    var findShRapaIndex = Array.FindIndex(results, x => x.Contains("SH RAPA") || x.Contains("RAPA"));  //|| x.Contains("SH ")
                    var trackingIdText = string.Empty;

                    for (int i = findShRapaIndex - 1; i < findShRapaIndex; i--)
                    {
                        if (results[i].Length >= 14 && !results[i].Equals(""))
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

                    var findBillSenderTextIndex = Array.FindIndex(results, x => x.Contains("BILL SENDER"));
                    var qrcodeIndex = Array.FindIndex(results, x => x.Contains("|||||||") || x.Contains("IIIIIII") || x.Contains("Illll"));
                    StringBuilder totext = new();

                    for (int i = findBillSenderTextIndex + 1; i < qrcodeIndex; i++)
                    {
                        //if (i > findBillSenderTextIndex)
                        //    break;

                        totext.Append(results[i] + " ");
                    }

                    //var trackToIndex = Array.FindIndex(results, x => x.Contains("|||||||") || x.Contains("IIIIIII") || x.Contains("Illll"));
                    //StringBuilder toText = new();

                    //for (int i = trackToIndex - 1; i < trackToIndex; i--)
                    //{
                    //    if (results[i].Contains("STATES US"))
                    //        break;

                    //    toText.Append(results[i] + " ");
                    //}

                    //string toTextStr = toText.ToString();
                    //var splitToTextStr = toTextStr.Split(" ");

                    //var finalToStr = splitToTextStr.Reverse();
                    //var strToText = String.Join(" ", finalToStr);

                    ;

                    //var trackFromTextIndex = Array.FindIndex(results, x => x.Contains("ORIGIN"));


                    StringBuilder fromText = new();

                    //for (int i = trackFromTextIndex + 1; i < results.Length; i++)
                    //{
                    //    if (results[i].Contains("STATES US"))
                    //        break;

                    //    fromText.Append(results[i] + " ");
                    //}

                    for (int i = 0; i <= findBillSenderTextIndex; i++)
                    {
                        if (results[i].Contains("STATES US") || i > findBillSenderTextIndex)
                            break;

                        fromText.Append(results[i] + " ");
                    }

                    var fromTextStr = fromText.ToString();
                    var splitFromText = fromTextStr.Split(" ");

                    for (int i = 0; i < splitFromText.Count(); i++)
                    {
                        if (splitFromText[i].Contains("ACTW") || splitFromText[i].Contains("AC"))
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
                        //To = strToText.ToString(),
                        To = totext.ToString(),
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

        public async Task<ViewApiResponse> GetAllRecords()
        {
            try
            {
                var result = await context.TextData.ToListAsync();
                return new ViewApiResponse
                {
                    ResponseCode = 200,
                    ResponseMessage = "Success",
                    ResponseData = result
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

        public async Task<ViewApiResponse> GetRecordsByTrackingId(string trackingId)
        {
            if (string.IsNullOrEmpty(trackingId))
            {
                return new ViewApiResponse
                {
                    ResponseCode = 400,
                    ResponseMessage = "Bad Request, Please input a valid tracking ID",
                    ResponseData = { }
                };

            }

            try
            {
                var response = await context.TextData.Where(x => x.TrackingId.Contains(trackingId)).ToListAsync();

                if (response.Count.Equals(0) || response is null)
                {
                    return new ViewApiResponse
                    {
                        ResponseCode = 404,
                        ResponseMessage = $"{trackingId} Not Found",
                        ResponseData = { }
                    };
                }

                return new ViewApiResponse
                {
                    ResponseCode = 200,
                    ResponseMessage = "Success",
                    ResponseData = response
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

        public async Task<ViewApiResponse> GetRecordById(int id)
        {
            if (id.Equals(null))
            {
                return new ViewApiResponse
                {
                    ResponseCode = 400,
                    ResponseMessage = "Bad Request",
                    ResponseData = { }
                };
            }

            try
            {
                var response = await context.TextData.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (response is null)
                {
                    return new ViewApiResponse
                    {
                        ResponseCode = 404,
                        ResponseMessage = "Not Found",
                        ResponseData = { }
                    };
                }

                return new ViewApiResponse
                {
                    ResponseCode = 200,
                    ResponseMessage = "Success",
                    ResponseData = response
                };
            } catch (Exception ex)
            {
                return new ViewApiResponse
                {
                    ResponseCode = 500,
                    ResponseMessage = $"Internal Server Error, {ex.Message}",
                    ResponseData = { }
                };
            }

            

        }

        public async Task<ViewApiResponse> UpdateRecordByTrackingId(TextDataDTO data)
        {
            if (string.IsNullOrEmpty(data.TrackingId))
            {
                return new ViewApiResponse
                {
                    ResponseCode = 400,
                    ResponseMessage = "Bad Request, Please input a valid tracking ID",
                    ResponseData = { }
                };
            }

            try
            {
                var response = await context.TextData.Where(x => x.Id.Equals(data.Id)).FirstOrDefaultAsync();
                if (response is null)
                {
                    return new ViewApiResponse
                    {
                        ResponseCode = 404,
                        ResponseMessage = $"{data.Id} Not Found",
                        ResponseData = response
                    };
                }

                var entityTobeModified = response;
                entityTobeModified.Courier = data.Courier;
                entityTobeModified.From = data.From;
                entityTobeModified.To = data.To;

                context.Entry(entityTobeModified).State = EntityState.Detached;
                context.Entry(entityTobeModified).State = EntityState.Modified;
                await context.SaveChangesAsync();

                var result = await GetRecordsByTrackingId(data.TrackingId);
                return result;


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
