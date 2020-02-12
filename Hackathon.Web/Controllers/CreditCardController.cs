using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Net;

namespace Hackathon.Web.Controllers
{
    public class CreditCardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


        public IActionResult CompareCreditCard()
        {
            return View();
        }
        public IActionResult ApplyCreditCard()
        {
            return View();
        }

        public async Task<IActionResult> UploadAndAuthenticate()
        {

            byte[] bytes = new byte[Request.Form.Files[0].Length];
            using (var reader = Request.Form.Files[0].OpenReadStream())
            {
                await reader.ReadAsync(bytes, 0, (int)Request.Form.Files[0].Length);
            }

            if (await AuthenticateUserByFace(bytes))
            {
                AmazonSQSClient sqsClient = new AmazonSQSClient("AKIAX2ZTBCX4OX6XH77Q", "X/FcCoEFyuIl5+hmwE+IVMk4t1089mgf0jIQI7Xo", RegionEndpoint.USEast2);
                Amazon.SQS.Model.SendMessageRequest request = new Amazon.SQS.Model.SendMessageRequest();
                request.QueueUrl = "https://sqs.us-east-2.amazonaws.com/538588550648/ProcessCreditCardRequest";
                request.MessageBody = "User 12345 Authenticated successfully";

                SendMessageResponse response = await sqsClient.SendMessageAsync(request);
            }

            return RedirectToAction("Success");
        }

        public async Task<bool> AuthenticateUserByFace(byte[] targetImage) //FileStream targetImage
        {
            float similarityThreshold = 90F;
            String sourceImage = "https://hackathonimagedump.s3.us-east-2.amazonaws.com/123456.jpeg";
            // String targetImage = "https://hackathonimagedump.s3.us-east-2.amazonaws.com/HappyFace.jpeg";


            AmazonRekognitionClient rekognitionClient = new AmazonRekognitionClient("AKIAX2ZTBCX4OX6XH77Q", "X/FcCoEFyuIl5+hmwE+IVMk4t1089mgf0jIQI7Xo", RegionEndpoint.USWest2);

            Amazon.Rekognition.Model.Image imageSource = new Amazon.Rekognition.Model.Image();
            try
            {

                var webClient = new WebClient();
                byte[] imageBytes = webClient.DownloadData(sourceImage);
                imageSource.Bytes = new MemoryStream(imageBytes);
            }
            catch 
            {
                
            }

            Amazon.Rekognition.Model.Image imageTarget = new Amazon.Rekognition.Model.Image();
            try
            {
                imageTarget.Bytes = new MemoryStream(targetImage);
            }
            catch
            {
                
            }

            CompareFacesRequest compareFacesRequest = new CompareFacesRequest()
            {
                SourceImage = imageSource,
                TargetImage = imageTarget,
                SimilarityThreshold = similarityThreshold
            };

            // Call operation
            CompareFacesResponse compareFacesResponse = await rekognitionClient.CompareFacesAsync(compareFacesRequest);

            if (compareFacesResponse.HttpStatusCode == HttpStatusCode.OK)
            {
                if (compareFacesResponse.SourceImageFace.Confidence > 90F)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public IActionResult Success()
        {
            return View();
        }
        




    }
}