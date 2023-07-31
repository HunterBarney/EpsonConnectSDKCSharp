using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace EpsonConnectSDK
{
    //TODO: Create get device info funcion
    //TODO: Create cancel authentication function
    //TODO: Add CreatePrintJob overload that takes 3 strings. 1 for file, 1 for name, 1 for type. This overload will use just default print settings.

    public class PrinterMediaSize
    {
        public string media_size { get; set; }
        public List<PrinterMediaType> media_types { get; set; }
    }

    public class PrinterMediaType
    {
        public string media_type { get; set; }
        public bool borderless { get; set; }
        public List<string> sources { get; set; }
        public List<string> print_qualities { get; set; }

        [JsonProperty("2_sided")]
        public bool _2_sided { get; set; }
    }

    public class MediaStorage
    {
        public List<string> color_modes { get; set; }
        public List<PrinterMediaSize> media_sizes { get; set; }
    }

    public class ECSDK
    {
        private string _host;
        private string _clientId;
        private string _clientSecret;
        private string _device;
        private bool _authenticated = false;
        private string? _subject_id;
        private string? _access_token;
        private DateTime _expiration;
        private string? _refresh_token;

        MediaStorage _doc_print_capabilities;
        MediaStorage _pic_print_capabilities;

        private static readonly HttpClient client = new HttpClient();

        public string DeviceID { get { return _subject_id ?? ""; } }
        public DateTime Expereration { get { return _expiration; } }
        public string AccessToken { get { return _access_token.ToString(); } }
        public string Refresh { get { return _refresh_token.ToString(); } }
        public MediaStorage SupportedPictureCapabilities { get { return _pic_print_capabilities; } }
        public MediaStorage SupportedDocumentCapabilities { get { return _doc_print_capabilities; } }

        /// <summary>
        /// Creates a client using your host,client ID, client secret, and device email.
        /// The client will then let you create and execute print jobs on your device.
        /// Your host and client ID & secret is provided to you when you apply for a developer licence.
        /// </summary>
        /// <param name="host"> Example:api.epsonconnect.com</param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="device">Your printer's Email Adress</param>
        public ECSDK(string host, string clientId, string clientSecret, string device)
        {
            _host = host;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _device = device;
        }

        /// <summary>
        /// Authenticates your client, and populates the device print capabilities collection
        /// </summary>
        /// <returns>A bool based on if the request was succesfull</returns>
        /// <exception cref="Exception"></exception>
        public async Task<bool> Authenticate()
        {
            //converts ID and secret to Base64
            string idSecret = _clientId + ":" + _clientSecret;
            var ptb = Encoding.UTF8.GetBytes(idSecret);
            string b64 = Convert.ToBase64String(ptb);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + b64);

            string requestBody = $"grant_type=password&username={_device}&password=";
            HttpContent body = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await client.PostAsync($"https://{_host}/api/1/printing/oauth2/auth/token?subject=printer", body);
            string responseBody = await response.Content.ReadAsStringAsync();

            if ((int)response.StatusCode != 200)
            {
                throw new Exception("Authentication Failed", new Exception(responseBody));
            }

            _expiration = DateTime.Now.AddHours(1);
            Dictionary<string, string> responseJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody) ?? new Dictionary<string, string>();

            _access_token = responseJson["access_token"];
            _refresh_token = responseJson["refresh_token"];
            _subject_id = responseJson["subject_id"];
            _authenticated = true;
            
            //uncomment once json error is fixed
            //await GetPrintCapabilities();

            return true;
        }

        /// <summary>
        /// Creates a print job and uploads the given file to redy it for the print.
        /// To execute the job use the ExecutePrintJob function
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>A string contatining the JobID</returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> CreatePrintJob(string filePath, PrintSettings settings)
        {
            if (!IsAuthenticated())
            {
                throw new Exception("Client not authenticated");
            }
            await CheckAndRefresh();

            string[] jobInfo = await CreatePrintJob(settings);
            await UploadPrintFile(filePath, jobInfo[1]);
            return jobInfo[0];
        }
        /// <summary>
        /// Executes the given print job
        /// </summary>
        /// <param name="JobID">Job to execute</param>
        /// <returns>A bool indicating if the process was successful</returns>
        public async Task<bool> ExecutePrintJob(string jobid)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_access_token}");

            HttpResponseMessage response = await client.PostAsync($"https://{_host}/api/1/printing/printers/{_subject_id}/jobs/{jobid}/print", null);
            if ((int)response.StatusCode != 200)
            {
                throw new Exception("Print Execution Failed", new Exception(await response.Content.ReadAsStringAsync()));
            }
            return true;
        }

        public bool IsAuthenticated()
        {
            return _authenticated;
        }

        /// <summary>
        /// Cancels the given print job. A job can not be canceled if it is already printing.
        /// To check if a jobs status use the GetJobInfo function
        /// </summary>
        /// <param name="JobID">The job to print</param>
        /// <param name="Operator">If set to true, it will show that an operator cancled the job, otherwise it will show a user cancled it.</param>
        /// <returns></returns>
        public async Task<bool> CancelJobOperator(string JobID, bool Operator = false)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _access_token);


            string requestBody;
            if (Operator)
            {
                requestBody = @$"""operated_by"": ""operator""";
            }
            else
            {
                requestBody = @$"""operated_by"": ""user""";
            }

            HttpContent body = new StringContent(requestBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"https://{_host}/api/1/printing/printers/{_subject_id}/jobs/{JobID}/cancel", body);
            return false;
        }

        /// <summary>
        /// Gets and returns the specified jobs status.
        /// </summary>
        /// <param name="JobID"></param>
        /// <returns>The HTTP response body as a JSON string</returns>
        public async Task<string> GetJobInfo(string JobID)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _access_token);

            HttpResponseMessage response = await client.GetAsync($"https://{_host}/api/1/printing/printers/{_subject_id}/jobs/{JobID}");
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        /// <summary>
        /// Checks the print capabilities and saves them as objects, can be accessed in code.
        /// </summary>
        /// <returns>A bool indicationg success.</returns>
        private async Task<bool> GetPrintCapabilities()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _access_token);


            HttpResponseMessage responseDoc = await client.GetAsync($"https://{_host}/api/1/printing/printers/{_subject_id}/capability/document");
            HttpResponseMessage responsePic = await client.GetAsync($"https://{_host}/api/1/printing/printers/{_subject_id}/capability/photo");
            string responseBody = await responseDoc.Content.ReadAsStringAsync();
            string responseBody2 = await responsePic.Content.ReadAsStringAsync();

            _pic_print_capabilities = JsonConvert.DeserializeObject<MediaStorage>(responseBody) ?? new MediaStorage();
            _doc_print_capabilities = JsonConvert.DeserializeObject<MediaStorage>(responseBody2) ?? new MediaStorage();
            return true;
        }


        /// <summary>
        /// Checks to see if the access token will expire in the next 2 minutes, if so refresh.
        /// </summary>
        /// <returns></returns>
        async private Task<bool> CheckAndRefresh()
        {
            //If experation is within 2 minutes from now, refresh to prevent any errors.
            if (DateTime.Now > _expiration.AddMinutes(-2))
            {
                await RefreshToken();
            }
            return true;
        }
        /// <summary>
        /// Refreshes the current access token
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        async private Task<bool> RefreshToken()
        {
            string idSecret = _clientId + ":" + _clientSecret;
            var ptb = Encoding.UTF8.GetBytes(idSecret);
            string b64 = Convert.ToBase64String(ptb);

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + b64);

            string requestBody = $"grant_type=refresh_token&refresh_token={_refresh_token}";
            HttpContent body = new StringContent(requestBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"https://{_host}/api/1/printing/oauth2/auth/token?subject=printer", body);
            string responseBody = await response.Content.ReadAsStringAsync();

            if ((int)response.StatusCode != 200)
            {
                throw new Exception("Token Refresh Failed", new Exception(responseBody));
            }

            Dictionary<string, string> ResponseJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody) ?? new Dictionary<string, string>();
            _access_token = ResponseJson["access_token"];
            _subject_id = ResponseJson["subject_id"];
            _expiration = DateTime.Now.AddHours(1);
            return true;
        }

        /// <summary>
        /// Creates a print job on Epson Connect and redys it for printing
        /// </summary>
        /// <param name="settings"></param>
        /// <returns>A string array containing the JobID([0]), which is used to reference the current job, and the upload URI([1]) which is used to upload the file</returns>
        /// <exception cref="Exception"></exception>
        private async Task<string[]> CreatePrintJob(PrintSettings settings)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_access_token}");

            string requestbody = JsonConvert.SerializeObject(settings);
            HttpContent body = new StringContent(requestbody, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"https://{_host}/api/1/printing/printers/{_subject_id}/jobs", body);
            string responsebody = await response.Content.ReadAsStringAsync();
            if ((int)response.StatusCode != 201)
            {
                throw new Exception("Print Job Creation Failed", new Exception(responsebody));
            }

            Dictionary<string, string> responsejson = JsonConvert.DeserializeObject<Dictionary<string, string>>(responsebody) ?? new Dictionary<string, string>();

            string JobId = responsejson["id"];
            string uploadUri = responsejson["upload_uri"];

            string[] vars = { JobId, uploadUri };
            return vars;
        }

        /// <summary>
        /// Uses a filestream to upload the given file using a baseuri given as a response from creating a printjob.
        /// </summary>
        /// <param name="filePath">Relitive or full path of the file</param>
        /// <param name="baseUri">Provided as a response from creating a print job</param>
        /// <returns>A bool indicating the success of the function</returns>
        /// <exception cref="Exception"></exception>
        private async Task<bool> UploadPrintFile(string filePath, string baseUri)
        {
            client.DefaultRequestHeaders.Clear();
            filePath = Path.GetFullPath(filePath);
            FileInfo fileInfo = new FileInfo(filePath);


            using (FileStream fileStream = File.OpenRead(filePath))
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{baseUri}&File=1{fileInfo.Extension}");
                request.Content = new StreamContent(fileStream);
                request.Content.Headers.ContentLength = fileStream.Length;
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                HttpResponseMessage response = await client.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();
                if ((int)response.StatusCode != 200)
                {
                    throw new Exception("File Upload Failed", new Exception(responseBody));
                }

            }

            return true;
        }
    }
}