using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Drawing;

namespace recognize
{
    enum RecognitionMode { single, multi };

    /// <summary>
    /// Class to handle requests to recognize.im API.
    /// </summary>
    class recognizeProxy
    {
        private CookieContainer cookieJar;
        private string clientId;
        private string apiKey;
        private string clapiKey;

        //These are the limits for query images:
        //for SingleIR
        private double SINGLEIR_MAX_FILE_SIZE = 500.0;		//KBytes
        private int SINGLEIR_MIN_DIMENSION = 100;			//pix
        private double SINGLEIR_MIN_IMAGE_SURFACE = 0.05;	//Mpix
        private double SINGLEIR_MAX_IMAGE_SURFACE = 0.31;	//Mpix

        //for MultipleIR
        private double MULTIPLEIR_MAX_FILE_SIZE = 3500.0;	//KBytes
        private int MULTIPLEIR_MIN_DIMENSION = 100;		//pix
        private double MULTIPLEIR_MIN_IMAGE_SURFACE = 0.1;	//Mpix
        private double MULTIPLEIR_MAX_IMAGE_SURFACE = 5.1;	//Mpix

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="clientId">Your unique client ID. You can find it in the Account tab after logging in at recognize.im.</param>
        /// <param name="apiKey">Your unique API key. You can find it in the Account tab after logging in at recognize.im.</param>
        /// <param name="clapiKey">Your unique secret client key. You can find it in the Account tab after logging in at recognize.im.</param>
        public recognizeProxy(string clientId, string apiKey, string clapiKey)
        {
            this.clientId = clientId;
            this.apiKey = apiKey;
            this.clapiKey = clapiKey;

            string oRequest = "";
            oRequest = oRequest + "<client_id xsi:type=\"xsd:string\">" + clientId + "</client_id>";
            oRequest = oRequest + "<key_clapi xsi:type=\"xsd:string\">" + clapiKey + "</key_clapi>";
            oRequest = oRequest + "<ip xsi:type=\"xsd:string\"></ip>";

            this.cookieJar = new CookieContainer();

            Dictionary<string, string> result = sendSoapRequest(oRequest, "auth");
        }

        /// <summary>
        /// Converts response from xml to Dictionary<string, string>
        /// </summary>
        /// <param name="response">SOAP response</param>
        /// <returns>SOAP response converted to Dictionary<string, string></returns>
        private static Dictionary<string, string> convertResponse(Stream response)
        {
            XDocument doc = XDocument.Load(response);
            var results = doc.Descendants("item");
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var result in results)
            {
                if (!result.Element("value").HasElements) dict[result.Element("key").Value] = result.Element("value").Value;
            }
            return dict;
        }

        /// <summary>
        /// Generates MD5 from the glued: API key and the image. 
        /// </summary>
        /// <param name="api_key">Unique API key.</param>
        /// <param name="imageBytes">Image data in byte[] array</param>
        /// <returns>Generated MD5 hash.</returns>
        private static String getMD5(String api_key, byte[] imageBytes)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] api = System.Text.Encoding.ASCII.GetBytes(api_key);
            byte[] glued = new byte[api.Length + imageBytes.Length];

            System.Buffer.BlockCopy(api, 0, glued, 0, api.Length);
            System.Buffer.BlockCopy(imageBytes, 0, glued, api.Length, imageBytes.Length);

            byte[] retVal = md5.ComputeHash(glued);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }

            md5.Clear();

            return sb.ToString();
        }

        /// <summary>
        /// Send SOAP request to recognize.im
        /// </summary>
        /// <param name="body">Body of request</param>
        /// <param name="operation">SOAP opperation</param>
        /// <returns>Response converted to Dictionary<string, string></returns>
        private Dictionary<string, string> sendSoapRequest(string body, string operation)
        {
            string oRequest = "";
            oRequest = "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ns1=\"http://clapi.itraff.pl\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:ns2=\"http://xml.apache.org/xml-soap\" xmlns:SOAP-ENC=\"http://schemas.xmlsoap.org/soap/encoding/\" SOAP-ENV:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">";
            oRequest = oRequest + "<SOAP-ENV:Body>";
            oRequest = oRequest + "<ns1:" + operation + ">";
            oRequest = oRequest + body;
            oRequest = oRequest + "</ns1:" + operation + ">";
            oRequest = oRequest + "</SOAP-ENV:Body>";
            oRequest = oRequest + "</SOAP-ENV:Envelope>";

            //Builds the connection to the WebService.
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://clapi.itraff.pl/");
            req.Headers.Add("SOAPAction", "\"http://clapi.itraff.pl#" + operation + "\"");
            req.ContentType = "text/xml; charset=\"utf-8\"";
            req.Accept = "text/xml";
            req.Method = "POST";

            req.CookieContainer = this.cookieJar;

            //Passes the SoapRequest String to the WebService
            using (Stream stm = req.GetRequestStream())
            {
                using (StreamWriter stmw = new StreamWriter(stm))
                {
                    stmw.Write(oRequest);
                }
            }
            //Gets the response
            WebResponse response = req.GetResponse();
            //Writes the Response
            Stream responseStream = response.GetResponseStream();

            return convertResponse(responseStream);
        }

        /// <summary>
        /// Authentication
        /// </summary>
        /// <param name="clientId">Your unique client ID. You can find it in the Account tab after logging in at recognize.im.</param>
        /// <param name="clapiKey">Your unique secret client key. You can find it in the Account tab after logging in at recognize.im.</param>
        /// <returns>Server response</returns>
        public Dictionary<string, string> auth(string clientId, string clapiKey)
        {
            string oRequest = "";
            oRequest = oRequest + "<client_id xsi:type=\"xsd:string\">" + clientId + "</client_id>";
            oRequest = oRequest + "<key_clapi xsi:type=\"xsd:string\">" + clapiKey + "</key_clapi>";
            oRequest = oRequest + "<ip xsi:type=\"xsd:string\"></ip>";

            return sendSoapRequest(oRequest, "auth");
        }

        /// <summary>
        /// You need to call indexBuild method in order to apply all your recent (from the previous call of this method) changes, 
        /// including adding new images and deleting images. 
        /// </summary>
        /// <returns>Server response</returns>
        public Dictionary<string, string> indexBuild()
        {
            return sendSoapRequest("", "indexBuild");
        }

        /// <summary>
        /// Add new picture to your pictures list
        /// </summary>
        /// <param name="imageId">A unique identifier of the inserted image.</param>
        /// <param name="imageName">A label you want to assign to the inserted image.</param>
        /// <param name="imagePath">Path to the image file.</param>
        /// <returns>Server response</returns>
        public Dictionary<string, string> imageInsert(string imageId, string imageName, string imagePath)
        {
            FileStream image = File.OpenRead(imagePath);
            byte[] data = new byte[image.Length];
            image.Read(data, 0, data.Length);
            String imageData = System.Convert.ToBase64String(data);

            string oRequest = "";
            oRequest = oRequest + "<id xsi:type=\"xsd:string\">" + imageId + "</id>";
            oRequest = oRequest + "<name xsi:type=\"xsd:string\">" + imageName + "</name>";
            oRequest = oRequest + "<data xsi:type=\"xsd:string\">" + imageData + "</data>";

            return sendSoapRequest(oRequest, "imageInsert");
        }

        /// <summary>
        /// There are some situations when we might need to call one of your methods. 
        /// For example when we finish applying changes we may need to let you know that all your images are ready to be recognized.
        /// </summary>
        /// <param name="callbackUrl">The URL to the method you want us to call.</param>
        /// <returns>Server response</returns>
        public Dictionary<string, string> callback(string callbackUrl)
        {
            string oRequest = "";
            oRequest = oRequest + "<callbackURL xsi:type=\"xsd:anyURI\">" + callbackUrl + "</callbackURL>";

            return sendSoapRequest(oRequest, "callback");
        }

        /// <summary>
        /// If you don't need an image to be recognizable anymore you have to remove this image from the database. 
        /// You can do this by calling imageDelete method passing the ID of the image you want to remove. 
        /// You can also remove all of your images with one call of this method. In order to achieve this you need to pass null value as a parameter.
        /// </summary>
        /// <param name="imageId">ID of the image you would like to remove (this is the same ID you pass a an argument to the imageInsert method). 
        /// Pass null value if you want to remove all of your images.</param>
        /// <returns>Server response</returns>
        public Dictionary<string, string> imageDelete(string imageId)
        {
            string oRequest = "";
            oRequest = oRequest + "<ID xsi:type=\"xsd:string\">" + imageId + "</ID>";

            return sendSoapRequest(oRequest, "imageDelete");
        }

        /// <summary>
        /// There may be some situations when you would like to change the name or ID of an image stored in the database. 
        /// You can do this by calling the imageUpdate method.
        /// </summary>
        /// <param name="imageIdOld">ID of the image which data you would like to change (this is the same ID you pass a an argument to the imageInsert method).</param>
        /// <param name="imageIdNew">New ID of an image.</param>
        /// <param name="imageNameNew">New name of an image.</param>
        /// <returns>Server response</returns>
        public Dictionary<string, string> imageUpdate(string imageIdOld, string imageIdNew, string imageNameNew)
        {
            string oRequest = "";
            oRequest = oRequest + "<ID xsi:type=\"xsd:string\">" + imageIdOld + "</ID>";
            oRequest = oRequest + "<data xsi:type=\"ns2:Map\">";
            oRequest = oRequest + "<item>";
            oRequest = oRequest + "<key xsi:type=\"xsd:string\">id</key>";
            oRequest = oRequest + "<value xsi:type=\"xsd:string\">" + imageIdNew + "</value>";
            oRequest = oRequest + "</item>";
            oRequest = oRequest + "<item>";
            oRequest = oRequest + "<key xsi:type=\"xsd:string\">name</key>";
            oRequest = oRequest + "<value xsi:type=\"xsd:string\">" + imageNameNew + "</value>";
            oRequest = oRequest + "</item>";
            oRequest = oRequest + "</data>";

            return sendSoapRequest(oRequest, "imageUpdate");
        }

        /// <summary>
        /// You may be curious what is the progress of applying your changes. In order to do this you need to call indexStatus method.
        /// </summary>
        /// <returns>Server response</returns>
        public Dictionary<string, string> indexStatus()
        {
            return sendSoapRequest("", "indexStatus");
        }

        /// <summary>
        /// When using our API you are limited with regards the number of images and number of scans (recognition operations). 
        /// The limits depend on the type of account you have. 
        /// In order to check how many more images you can add and how many scans you have left use the userLimits method.
        /// </summary>
        /// <returns>Server response</returns>
        public Dictionary<string, string> userLimits()
        {
            return sendSoapRequest("", "userLimits");
        }

        /// <summary>
        /// Gets recognition mode
        /// </summary>
        /// <returns>Server response</returns>
        public Dictionary<string, string> modeGet()
        {
            return sendSoapRequest("", "modeGet");
        }

        /// <summary>
        /// Changes recognition mode
        /// </summary>
        /// <returns>Server response</returns>
        public Dictionary<string, string> modeChange(RecognitionMode mode)
        {
            string oRequest = "";
            if (mode == RecognitionMode.multi)
            {
                oRequest = "<mode xsi:type=\"xsd:string\">Multi</mode>";
            }
            else
            {
                oRequest = "<mode xsi:type=\"xsd:string\">Single</mode>";
            }


            return sendSoapRequest(oRequest, "modeChange");
        }

        /// <summary>
        /// Sends image recognition request.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <param name="mode">Recognize mode</param> 
        /// <returns>Server response</returns>
        public Dictionary<string, object> recognize(string imagePath, RecognitionMode mode, bool all)
        {
            //open image
            FileStream image = File.OpenRead(imagePath);

            byte[] data = new byte[image.Length];
            image.Read(data, 0, data.Length);

            if (!checkImageLimits(image, mode))
            {
                throw new Exception("Image Limits exception");
            }

            image.Close();

            //create request
            string url = "http://recognize.im/v2/recognize/";
            if (mode == RecognitionMode.multi)
            {
                url += "multi/";
            }
            else
            {
                url += "single/";
            }

            if (all)
            {
                url += "all/";
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url + this.clientId);
            request.Method = "POST";
            request.Headers["x-itraff-hash"] = getMD5(this.apiKey, data);
            request.ContentType = "image/jpeg";
            request.Accept = "application/json";
            request.ContentLength = data.Length;

            //send request
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(data, 0, data.Length);
            }

            //get response
            using (WebResponse response = request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    //read json response
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    String responseString = reader.ReadToEnd();

                    //deserialize json response into Dictionary<string, string>
                    var jss = new JavaScriptSerializer();
                    var dict = jss.Deserialize<Dictionary<string, object>>(responseString);
                    return dict;
                }
            }
        }

        /// <summary>
        /// Sends image recognition request.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <returns>Server response</returns>
        public Dictionary<string, string> recognize(string imagePath)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            Dictionary<string, object> tmp = recognize(imagePath, RecognitionMode.single, true);
            foreach (KeyValuePair<string, object> pair in tmp)
            {
                result[pair.Key] = pair.Value.ToString();
            }
            return result;
        }

        /// <summary>
        /// Checks the image limits for given recognize mode.
        /// </summary>
        /// <param name="imageStream">Image file stream.</param>
        /// <param name="mode">Recognize mode</param> 
        /// <returns>Server response</returns>
        public bool checkImageLimits(FileStream imageStream, RecognitionMode mode)
        {
            Image image = Image.FromStream(imageStream);
            double imageSurface = (double)(image.Height * image.Width) / 1000000.0;
            double fileSize = imageStream.Length / 1000.0;

            if (mode == RecognitionMode.single)
            {
                if (fileSize > SINGLEIR_MAX_FILE_SIZE ||
                    image.Height < SINGLEIR_MIN_DIMENSION ||
                    image.Width < SINGLEIR_MIN_DIMENSION ||
                    imageSurface < SINGLEIR_MIN_IMAGE_SURFACE ||
                    imageSurface > SINGLEIR_MAX_IMAGE_SURFACE)
                {
                    return false;
                }
            }
            else if (mode == RecognitionMode.multi)
            {
                if (fileSize > MULTIPLEIR_MAX_FILE_SIZE ||
                    image.Height < MULTIPLEIR_MIN_DIMENSION ||
                    image.Width < MULTIPLEIR_MIN_DIMENSION ||
                    imageSurface < MULTIPLEIR_MIN_IMAGE_SURFACE ||
                    imageSurface > MULTIPLEIR_MAX_IMAGE_SURFACE)
                {
                    return false;
                }
            }

            return true;
        }
    }
}