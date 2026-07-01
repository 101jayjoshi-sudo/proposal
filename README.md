Proposal 


I have extensive experience integrating C# .NET applications with Hikvision ISAPI and the HCNetSDK (specifically for DS-Kxxxx series Access Control and Face Recognition terminals). 

I know exactly why your C# implementation is failing even though the Web UI works perfectly. 

### Why Your Current C# Implementation Fails

There are three major issues when sending multipart uploads to Hikvision devices using standard .NET classes:

1. **The Boundary Quotes Issue (Critical)**
   By default, .NET's `MultipartFormDataContent` wraps the boundary parameter in the `Content-Type` header with double quotes (e.g., `boundary="----MyBoundary"`). Hikvision's lightweight embedded HTTP parser is extremely strict and fragile; it does not strip these quotes, causing it to fail to find the boundary markers in the request body, resulting in a formatting error or timeout.
2. **Strict Part Ordering**
   The Hikvision device expects the JSON metadata part (`FaceDataRecord`) **first** and the binary image part (`img` or `FaceImage`) **second**. If your code adds the image first, the parser gets overwhelmed and rejects the request before reading the metadata.
3. **Explicit Content-Types and Header Formatting**
   Each part in the multipart body must have its specific headers explicitly declared. The metadata part must have `Content-Type: application/json` and the image part must have `Content-Type: image/jpeg`.

---

### The C# Solution (ISAPI PUT Request Fix)

Here is the exact code to resolve the boundary quoting issue and structure the request correctly using `HttpClient`:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

public async Task<bool> UploadFaceToHikvisionAsync(
    string deviceIp, 
    string username, 
    string password, 
    string employeeNo, 
    byte[] jpegImageBytes)
{
    // 1. Setup Digest Authentication
    var handler = new HttpClientHandler
    {
        Credentials = new CredentialCache
        {
            { new Uri($"http://{deviceIp}"), "Digest", new NetworkCredential(username, password) }
        }
    };

    using (var client = new HttpClient(handler))
    {
        // Define a unique boundary
        string boundary = "----HikvisionBoundary" + DateTime.Now.Ticks.ToString("x");
        
        using (var multipartContent = new MultipartFormDataContent(boundary))
        {
            // WORKAROUND: Remove the default Content-Type header containing quotes around the boundary
            multipartContent.Headers.Remove("Content-Type");
            multipartContent.Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={boundary}");

            // 2. Add the JSON Metadata Part FIRST
            // Note: For DS-K1T671M, the library type is typically 'blackFD' or 'normalFD', FDID is usually '1'
            string jsonPayload = $"{{\"faceLibType\":\"blackFD\",\"FDID\":\"1\",\"FPID\":\"{employeeNo}\"}}";
            var jsonContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            
            // Set explicit Content-Disposition for the JSON part
            jsonContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"FaceDataRecord\""
            };
            multipartContent.Add(jsonContent);

            // 3. Add the JPEG Image Part SECOND
            var imageContent = new ByteArrayContent(jpegImageBytes);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            imageContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"img\"", // Some firmware versions use "FaceImage" instead of "img"
                FileName = "\"face.jpg\""
            };
            multipartContent.Add(imageContent);

            // 4. Send PUT request to the device
            string url = $"http://{deviceIp}/ISAPI/Intelligent/FDLib/FDSetUp?format=json";
            
            try
            {
                HttpResponseMessage response = await client.PutAsync(url, multipartContent);
                string responseText = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Response: {responseText}");
                
                return response.IsSuccessStatusCode && responseText.Contains("OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload failed: {ex.Message}");
                return false;
            }
        }
    }
}



Alternative: SDK approach (HCNetSDK.cs)
If you prefer to bypass HTTP entirely, I can quickly set up the HCNetSDK integration. We will use:

NET_DVR_UploadFile_V40 with structure NET_DVR_FACECFG_CARD_INFO_EX to upload face records.
I am ready to help you implement and test this immediately on your C# Agent to get it working. Looking forward to working with you!

