using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Net;
using System.Text;
using Tesseract;

namespace ImgToText.Data
{
    public class TextToImgImpl : ITextToImg
    {

        private readonly ImgToTextDbContext context;

        //private readonly string SubscriptionKey = "1beeaf17759a4e50b644d8d4e7736f02";
        //private readonly string endpoint = "https://riddleimgtotext.cognitiveservices.azure.com/";

        //string path = "C:\\Users\\Davyeedo\\Downloads\\Whatsapp\\360_F_182011806_mxcDzt9ckBYbGpxAne8o73DbyDHpXOe9.jpg";

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

                    //string toTextExtraction = results[11].Substring(5); 
                    //string toTextExtraction = results[11][5..];
                    //string trackingIDTextExtraction = results[25][11..];

                    var findCourierIndex = Array.FindIndex(results, x => x.Contains("UPS GROUND"));

                    //var findShipToIndex = Array.FindIndex(results, x => x.Contains("TO"));
                    var findShipToIndex = Array.FindIndex(results, x => x.Contains("SHIP"));
                    //var index = findShipToIndex is -1 ? Array.FindIndex(results, x => x.Contains("TO")) : findShipToIndex;

                    var findTrackingIdIndex = Array.FindIndex(results, x => x.Contains("TRACKING"));

                    TextData textData = new()
                    {
                        Text = "",
                        Courier = results[findCourierIndex].ToString(),
                        From = "",
                        //To = toTextExtraction,
                        //To = results[findShipToIndex].ToString() is " " ? results[index + 1].ToString() : results[findShipToIndex + 1],
                        To = results[findShipToIndex].Equals(" ") ? results[findShipToIndex + 1] : results[findShipToIndex + 2],  //|| results[index + 1].ToString().Equals(" ") 
                        //TrackingId = trackingIDTextExtraction
                        TrackingId = results[findTrackingIdIndex][31..].ToString()
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

                    StringBuilder fromTextExtraction = new();
                    fromTextExtraction.Append(results[18]);
                    fromTextExtraction.Append(results[21]);
                    fromTextExtraction.Append(results[22]);

                    StringBuilder toTextExtraction = new();
                    //string toFirstSubstring = results[26][6..];
                    toTextExtraction.Append(results[26][6..]);
                    toTextExtraction.Append("," + " ");
                    toTextExtraction.Append(results[28] + "," + " ");
                    toTextExtraction.Append(results[29]);

                    TextData textData = new()
                    {
                        Text = "",
                        Courier = "USPS",
                        From = fromTextExtraction.ToString(),
                        To = toTextExtraction.ToString(),
                        TrackingId = results[34]
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
