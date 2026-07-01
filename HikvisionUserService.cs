using System.Net;
using System.Text;
using System.Text.Json;
using HikvisionAgent.Models;

namespace HikvisionAgent.Services;

public class HikvisionUserService
{
    public async Task<string> GetUsersFromDevice(Device device)
    {
        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential(
                device.Username,
                device.Password
            )
        };

        using var client = new HttpClient(handler);

        var data = new
        {
            UserInfoSearchCond = new
            {
                searchID = "1",
                searchResultPosition = 0,
                maxResults = 100
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(data),
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync(
            $"http://{device.Ip}/ISAPI/AccessControl/UserInfo/Search?format=json",
            content
        );

        return await response.Content.ReadAsStringAsync();
    }


    public async Task<string> AddUserToDevice(
    Device device,
    string employeeNo,
    string name)
{
    var handler = new HttpClientHandler
    {
        Credentials = new NetworkCredential(
            device.Username,
            device.Password
        )
    };

    using var client = new HttpClient(handler);

    var data = new
    {
        UserInfo = new
        {
            employeeNo = employeeNo,
            name = name,
            userType = "normal",
            closeDelayEnabled = false,
            Valid = new
            {
                enable = true,
                beginTime = "2024-01-01T00:00:00",
                endTime = "2034-12-31T23:59:59",
                timeType = "local"
            },
            doorRight = "1"
        }
    };

    var content = new StringContent(
        JsonSerializer.Serialize(data),
        Encoding.UTF8,
        "application/json"
    );

    var response = await client.PostAsync(
        $"http://{device.Ip}/ISAPI/AccessControl/UserInfo/Record?format=json",
        content
    );

    return await response.Content.ReadAsStringAsync();
}
//rasm yuklash
public async Task<string> UploadFaceToDevice(
    Device device,
    string employeeNo,
    string imageUrl)
{
    // ===== IMAGE DOWNLOAD TEST =====

    using var imageClient = new HttpClient();

    Console.WriteLine("IMAGE URL:");
    Console.WriteLine(imageUrl);

    var imageResponse = await imageClient.GetAsync(imageUrl);

    Console.WriteLine("IMAGE STATUS:");
    Console.WriteLine(imageResponse.StatusCode);

    Console.WriteLine("IMAGE CONTENT TYPE:");
    Console.WriteLine(imageResponse.Content.Headers.ContentType);

    var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();

    Console.WriteLine("IMAGE SIZE:");
    Console.WriteLine(imageBytes.Length);

    // Agar HTML kelayotgan bo'lsa ko'rsatadi
    if (imageResponse.Content.Headers.ContentType?.MediaType == "text/html")
    {
        var html = System.Text.Encoding.UTF8.GetString(imageBytes);

        Console.WriteLine("HTML RESPONSE:");
        Console.WriteLine(html);
    }

    // ===== HIKVISION =====

    var handler = new HttpClientHandler
    {
        Credentials = new NetworkCredential(
            device.Username,
            device.Password
        )
    };

    using var client = new HttpClient(handler);

    client.DefaultRequestHeaders.ExpectContinue = false;

    // Generate custom boundary without double quotes in the Content-Type header
    string boundary = "----HikvisionBoundary" + DateTime.Now.Ticks.ToString("x");
    using var form = new MultipartFormDataContent(boundary);

    // Remove the default Content-Type containing boundary quotes
    form.Headers.Remove("Content-Type");
    form.Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={boundary}");

    var json =
        $@"{{""faceLibType"":""blackFD"",""FDID"":""1"",""FPID"":""{employeeNo}""}}";

    var faceDataContent = new StringContent(
        json.Replace("\r", "").Replace("\n", ""),
        Encoding.UTF8,
        "application/json"
    );

    form.Add(faceDataContent, "FaceDataRecord");

    var imageContent = new ByteArrayContent(imageBytes);
    imageContent.Headers.ContentType =
        new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

    form.Add(imageContent, "img", "kkkk.jpg");

    Console.WriteLine("JSON:");
    Console.WriteLine(json);

    Console.WriteLine("FACE URL:");
    Console.WriteLine(
        $"http://{device.Ip}/ISAPI/Intelligent/FDLib/FDSetUp?format=json"
    );

    Console.WriteLine("FPID:");
    Console.WriteLine(employeeNo);

    var response = await client.PutAsync(
        $"http://{device.Ip}/ISAPI/Intelligent/FDLib/FDSetUp?format=json",
        form
    );

    Console.WriteLine("FACE STATUS:");
    Console.WriteLine(response.StatusCode);

    var result = await response.Content.ReadAsStringAsync();

    Console.WriteLine("FACE RESULT:");
    Console.WriteLine(result);

    return result;
}

}