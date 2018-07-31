using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NumPlateFunction
{
    public static class NPRFunction
    {
        [FunctionName("NPRFunction")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {

            log.Info(Environment.GetEnvironmentVariable("NAME"));
            log.Info("C# HTTP trigger function processed a request.");

            ResponseInformation ri = new ResponseInformation();

            //assign a guid to this request for tracking purposes
            ri.RequestGuid = Guid.NewGuid();

            //key-value parameter pairs in req
            IEnumerable<KeyValuePair<string, string>> parameters = req.GetQueryNameValuePairs();

            //WARNING: Expecting lane num as first value passed in
            var lane = parameters.First().Value;

            ri.Lane = Convert.ToInt32(lane); //from input string

            byte[] imagebytes = new byte[] { };

            using (var streamReader = new MemoryStream())
            {
                await req.Content.CopyToAsync(streamReader);
                imagebytes = streamReader.ToArray();
            }
            
            //Write to result file somewhere to check cropping
            //File.WriteAllBytes("C:/Users/me/myfiles/result.jpg", imagebytes);

            var responseString = await DetectPlate(imagebytes);

            dynamic parsed = JObject.Parse(responseString);

            var highestprob = 0.0;

            JArray predictions = parsed.predictions;

            foreach (var prediction in predictions)
            {
                var prob = prediction.Value<double>("probability");
                if (prob > highestprob)
                {
                    highestprob = prob;
                }
            }

            //left, top, width, height
            double left = 0.0;
            double top = 0.0;
            double width = 0.0;
            double height = 0.0;

            foreach (var prediction in predictions)
            {
                var prob = prediction.Value<double>("probability");
                if (prob == highestprob)
                {
                    var boundingbox = prediction.Value<JObject>("boundingBox");
                    left = (double)boundingbox["left"];
                    top = (double)boundingbox["top"];
                    width = (double)boundingbox["width"];
                    height = (double)boundingbox["height"];
                }
            }

            //convert to <Image> object for original dims
            MemoryStream ms = new MemoryStream(imagebytes);
            Image imageobj = Image.FromStream(ms);

            //original dims (w, h)
            var origWidth = imageobj.Width;
            var origHeight = imageobj.Height;

            //convert bounding values to int for cropper and scale
            int intleft = Convert.ToInt32(left * origWidth);
            int inttop = Convert.ToInt32(top * origHeight);
            int intwidth = Convert.ToInt32(width * origWidth);
            int intheight = Convert.ToInt32(height * origHeight);

            //image deduced from request req in cropping function
            var croppedimagebytes = await Crop(req, intleft, inttop, intwidth, intheight);

            //OCR
            //TODO: breaks when given image is too small (set min)/scale accordingly
            responseString = await ReadPlate(croppedimagebytes);

            //TODO: convert all JSON handling to take advantage of Response classes as below https://json2csharp.com
            //beware incorrect variable types
            var parsedOCRresponse = JsonConvert.DeserializeObject<NumPlateVerifierFinal.OCRResponse>(responseString);

            string ocrtext = ""; //concatenate all recognized text to this variable for return to ri

            foreach (var region in parsedOCRresponse.regions)
            {
                foreach (var line in region.lines) {

                    foreach (var word in line.words)
                    {

                        ocrtext += word.text;

                    }


                }
            }

            //TODO: handle case that ocr returns something in plate's region box (excess characters returned)
            ri.PlateContent = ocrtext;

            return imagebytes == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass an array of bytes in your request body")
                : req.CreateResponse(HttpStatusCode.OK, ri);
        }

        private static async Task<string> DetectPlate(byte[] bytes)
        {
            //just a basic http call
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://southcentralus.api.cognitive.microsoft.com/customvision/v2.0/Prediction/e7684194-933b-41c7-9e1a-d770373c00b8/image")
            };

            //load in custom vision detection prediction key
            var cvPredictionKey = Environment.GetEnvironmentVariable("CVPredictionKey");

            httpClient.DefaultRequestHeaders.Add("Prediction-Key", cvPredictionKey);
            ByteArrayContent content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

            var response = await httpClient.PostAsync(httpClient.BaseAddress, content);
            return await response.Content.ReadAsStringAsync();
        }


        private static async Task<byte[]> Crop(HttpRequestMessage req, int x, int y, int width, int height)
        {
            Image sourceImage = null;
            Image destinationImage = null;

            // resize image
            using (var streamReader = new MemoryStream())
            {
                await req.Content.CopyToAsync(streamReader);

                using (Bitmap temp = new Bitmap(streamReader))
                {
                    sourceImage = new Bitmap(temp);

                    destinationImage = new Bitmap(width, height);
                    Graphics g = Graphics.FromImage(destinationImage);

                    g.DrawImage(
                      sourceImage,
                      new Rectangle(0, 0, width, height),
                      new Rectangle(x, y, width, height),
                      GraphicsUnit.Pixel
                    );
                }
            }

            // write to output byte array
            byte[] outputBytes = null;
            using (var ms = new MemoryStream())
            {
                //always Jpeg (for now)
                destinationImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                outputBytes = ms.ToArray();
            }

            return outputBytes;

        }


        //Plate OCR using vision API
        private static async Task<string> ReadPlate(byte[] platebytes)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://uksouth.api.cognitive.microsoft.com/vision/v1.0/ocr?language=unk&detectOrientation=trueHTTP/1.1&detectOrientation%20=true%20HTTP/1.1")
            };

            //computer vision ocr subscription key
            var ocrSubscriptionKey = Environment.GetEnvironmentVariable("OCRSubscriptionKey");

            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocrSubscriptionKey);

            var jsonContent = platebytes;
            var content = new ByteArrayContent(jsonContent);
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

            var response = await httpClient.PostAsync(httpClient.BaseAddress, content);
            return await response.Content.ReadAsStringAsync();
        }

    }

    //constructing the response of the Azure Function
    public class ResponseInformation
    {
        //which lane is the car in question at?
        public int Lane { get; set; }

        //raw ocr'd content of plate (string)
        public string PlateContent { get; set; }

        //Unique identifier for this particular sighting of the plate (in case we want to store examples for human verification/posterity)
        public Guid RequestGuid { get; set; }

    }
}
