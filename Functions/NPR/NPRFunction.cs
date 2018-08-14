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

            try {

                log.Info(Environment.GetEnvironmentVariable("NAME"));
                log.Info("C# HTTP trigger function processed a request.");

                //ResponseInformation object will compose a JSON response to return to the client.
                ResponseInformation ri = new ResponseInformation
                {
                    /*
                     * Immediately assign a guid to this request for tracking purposes 
                     * (In case you'd like to store each entry attempt)
                    */
                    RequestGuid = Guid.NewGuid()
                };

                //key-value parameter pairs in req
                IEnumerable<KeyValuePair<string, string>> parameters = req.GetQueryNameValuePairs();

                //WARNING: Expecting lane num as first value passed in from client (camera app)
                //TODO: it would be nice to just find the value by key "lane" in the request
                try
                {
                    var lane = parameters.First().Value;
                    ri.Lane = Convert.ToInt32(lane); //pass it along
                }
                catch (Exception ex)
                {
                    //end here
                    return req.CreateResponse(HttpStatusCode.BadRequest, ex.Message + " lane not provided or not as first param. Please provide the lane number.");
                }

                byte[] imagebytes = new byte[] { };

                try
                {
                    //grab raw bytes of the image provided
                    using (var streamReader = new MemoryStream())
                    {
                        await req.Content.CopyToAsync(streamReader);
                        imagebytes = streamReader.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    //end here
                    return req.CreateResponse(HttpStatusCode.BadRequest, ex.Message + " Image is not present or is not suitable. Please provide jpg image binary as body.");

                }

                //Useful debug option: Write to result file somewhere to check cropping
                //File.WriteAllBytes("C:/Users/me/myfiles/result.jpg", imagebytes);

                //Detect the plate using Custom Vision

                var responseString = ""; //for raw API responses
                dynamic parsed;

                try
                {
                    responseString = await DetectPlate(imagebytes);
                    parsed = JObject.Parse(responseString);
                }
                catch(Exception ex)
                {
                   //end here
                   return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message + " Error detecting the number plate. Please check your input image and Custom Vision model.");
                }

                //use the prediction with the highest probability as scored by the model
                var highestprob = 0.0;

                //TODO: refactor to more efficiently find max value
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

                /*
                 * Interestingly, The response from the Custom Vision detection endpoint is a 
                 * set of values you multiply your original image dimensions by, for a relative bounding. 
                */

                //multiply out to get final cropping coordinates
                int intleft = Convert.ToInt32(left * origWidth);
                int inttop = Convert.ToInt32(top * origHeight);
                int intwidth = Convert.ToInt32(width * origWidth);
                int intheight = Convert.ToInt32(height * origHeight);

                dynamic croppedimagebytes;

                try
                {
                    //image deduced from request req in cropping function
                    croppedimagebytes = await Crop(req, intleft, inttop, intwidth, intheight);

                }
                catch (Exception ex)
                {
                    //end here
                    return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message + " Error when cropping. Please check your input image and test your Custom Vision detection model.");
                }

                //OCR
                //TODO: breaks when given image is too small (set min)/scale accordingly

                try
                {
                    responseString = await ReadPlate(croppedimagebytes);
                } catch (Exception ex)
                {
                    //end here
                    return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message + " Error reading plate (OCR). Please ensure cropped image is between (40 x 40) and (3200 x 3200) pixels.");
                }

                //TODO: convert all JSON handling to take advantage of Response classes as below (where helpful) https://json2csharp.com
                var parsedOCRresponse = JsonConvert.DeserializeObject<NumPlateVerifierFinal.OCRResponse>(responseString);

                string ocrtext = ""; //concatenate all recognized text to this variable for return to ri

                //TODO: remove nested for loops
                foreach (var region in parsedOCRresponse.regions)
                {
                    foreach (var line in region.lines)
                    {

                        foreach (var word in line.words)
                        {

                            ocrtext += word.text;

                        }


                    }
                }

                //TODO: post processing - handle case that e.g. ocr returns something in plate's region box (excess characters returned)
                ri.PlateContent = ocrtext;

                //if we get here, everything is good!
                return req.CreateResponse(HttpStatusCode.OK, ri);


            }
            catch (Exception ex)
            {
                //in event of other unhandled error, provide details.
                return req.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

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

    //constructing the JSON response of the Function
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
