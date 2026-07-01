I have analyzed and updated the UploadFaceToDevice method in your 
HikvisionUserService.cs file.

What was wrong with the original code:
Double-quoted Boundary (boundary="----"): C#'s MultipartFormDataContent by default quotes the boundary string in the header. Hikvision's internal web server is extremely sensitive and fails to parse the multipart segments because of this.
Missing application/json Content-Type: The client cleared the Content-Type header on the JSON part (faceDataContent.Headers.ContentType = null;). The camera expects application/json for the configuration part of the request payload.
What I updated in 

HikvisionUserService.cs
:
Generated a custom boundary and removed the default double quotes using TryAddWithoutValidation.
Set the JSON metadata block's Content-Type explicitly to application/json.

"Here is the updated HikvisionUserService.cs file.
See the repo 
Why it failed previously:

In .NET, MultipartFormDataContent automatically wraps the boundary parameter in the Content-Type header with double quotes (e.g. boundary="----1234"). The Hikvision terminal's embedded web server has a strict parser that does not handle boundary quotes correctly, causing it to fail parsing the payload.
The JSON metadata part's Content-Type header was set to null instead of explicitly being application/json.
The Fix: I modified UploadFaceToDevice to generate a custom boundary without double quotes and added the metadata section as a valid application/json media type. Please build and run the C# agent now to verify that face uploads work successfully!"*
