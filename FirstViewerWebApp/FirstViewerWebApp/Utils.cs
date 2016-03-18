using RestSharp;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace FirstViewerWebApp
{
  public class Util
  {
    /// <summary>
    /// Authorization token stored in Session
    /// </summary>
    public static string Token
    {
      get
      {
        if (string.IsNullOrWhiteSpace(HttpContext.Current.Session["Token"] as string)) Authenticate();
        return HttpContext.Current.Session["Token"] as string;
      }
      private set
      {
        HttpContext.Current.Session["Token"] = value;
        HttpContext.Current.Session.Timeout = 30; // same as token
      }
    }

    /// <summary>
    /// Contact Autodesk server to authorize using the Consumer Key and Consumer Secret. Result is stored in Token property.
    /// </summary>
    public static void Authenticate()
    {
      //if (!string.IsNullOrWhiteSpace(Token)) return; // not need for a new token

      RestClient restClient = new RestClient(Credentials.BASE_URL);

      RestRequest req = new RestRequest();
      req.Resource = "authentication/v1/authenticate";
      req.Method = Method.POST;
      req.AddHeader("Content-Type", "application/x-www-form-urlencoded");
      req.AddParameter("client_id", Credentials.CONSUMER_KEY);
      req.AddParameter("client_secret", Credentials.CONSUMER_SECRET);
      req.AddParameter("grant_type", "client_credentials");

      IRestResponse<Models.AccessToken> resp = restClient.Execute<Models.AccessToken>(req);

      if (resp.StatusCode == System.Net.HttpStatusCode.OK)
        Token = resp.Data.access_token;
      else
        Token = string.Empty;
    }

    public static string Upload(HttpPostedFile postedFile)
    {
      // The View & Data requires 3 steps: create bucket, upload, translate

      // 1. Create bucket
      string bucketName = CreateBucket(postedFile.FileName);

      // 2. Upload the file to the bucket
      string urn = UploadToAutodesk(bucketName, postedFile);

      // 3. Start the transalation process
      // For this sample, we'll not wait for the translation process
      StartTranslation(urn);

      return urn;
    }

    /// <summary>
    /// Create a bucket (add a timestamp suffix)
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="policy">transient by default, or temporary or persistent</param>
    /// <returns>Bucket name created (with timestamp suffix)</returns>
    private static string CreateBucket(string bucketName, string policy = "transient")
    {
      bucketName = Regex.Replace(bucketName, "[^0-9a-zA-Z]+", "").ToLower(); // only letters and numbers
      bucketName += DateTime.Now.ToString("yyyyMMddHHmmss"); // avoid duplicate bucket name

      RestClient restClient = new RestClient(Credentials.BASE_URL);

      RestRequest req = new RestRequest();
      req.Resource = "/oss/v2/buckets";
      req.Method = Method.POST;
      req.AddHeader("Content-Type", "application/json");
      req.AddHeader("Authorization", string.Format("Bearer {0}", Token));

      string body = string.Format("{2}\"bucketKey\":\"{0}\",\"policyKey\":\"{1}\"{3}", bucketName, policy, "{", "}");
      req.AddParameter("application/json", body, ParameterType.RequestBody);

      IRestResponse resp = restClient.Execute(req);

      if (resp.StatusCode != System.Net.HttpStatusCode.OK)
        throw new Exception("Cloud not create bucket");

      return bucketName;
    }

    /// <summary>
    /// Uploads a file to Autodesk at the specifiec bucket
    /// </summary>
    /// <param name="bucketName">bucket name where the file will be uploaded</param>
    /// <param name="postedFile">posted file to be uploaded</param>
    /// <returns>The ID (URN) enconded on Base64</returns>
    private static string UploadToAutodesk(string bucketName, HttpPostedFile postedFile)
    {
      string objectKey = Uri.EscapeDataString(postedFile.FileName);

      //read the file content
      byte[] fileData = null;
      using (var binaryReader = new BinaryReader(postedFile.InputStream))
      {
        fileData = binaryReader.ReadBytes(postedFile.ContentLength);
      }

      RestRequest req = new RestRequest();

      req.Resource = "oss/v1/buckets/" + bucketName + "/objects/" + objectKey;
      req.Method = Method.PUT;
      req.AddHeader("Authorization", string.Format("Bearer {0}", Token));

      req.AddParameter("Content-Type", postedFile.ContentType);
      req.AddParameter("Content-Length", postedFile.ContentLength);
      req.AddParameter("requestBody", fileData, ParameterType.RequestBody);

      RestClient restClient = new RestClient(Credentials.BASE_URL);
      IRestResponse<Models.UploadBucket> resp = restClient.Execute<Models.UploadBucket>(req);
      if (resp.StatusCode == System.Net.HttpStatusCode.OK)
      {
        string content = resp.Content;
        return Base64Encode(resp.Data.objects[0].id);
      }
      throw new Exception("Cloud not upload file");
    }

    private static string Base64Encode(string plainText)
    {
      byte[] bytes = Encoding.UTF8.GetBytes(plainText);
      return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Invoke the start translation for the specified URN
    /// </summary>
    /// <param name="urn">URN of the file to translate</param>
    private static void StartTranslation(string urn)
    {
      RestClient restClient = new RestClient(Credentials.BASE_URL);

      RestRequest req = new RestRequest();
      req.Resource = "viewingservice/v1/register";
      req.Method = Method.POST;
      req.AddHeader("Authorization", string.Format("Bearer {0}", Token));
      req.AddHeader("Content-Type", "application/json");

      string body = "{\"urn\":\"" + urn + "\"}";
      req.AddParameter("application/json", body, ParameterType.RequestBody);

      IRestResponse resp = restClient.Execute(req);

      if (resp.StatusCode != System.Net.HttpStatusCode.OK)
        throw new Exception("Cloud not start translation");

      // Note this sample is not checking the /status/ of the request
    }
  }
}