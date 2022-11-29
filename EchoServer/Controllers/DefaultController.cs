using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace EchoServer.Controllers;

public class DefaultController : Controller
{
    private bool Accept(string format) => Request.Headers["Accept"].FirstOrDefault()?.Contains(format) ?? false;

    public async Task<IActionResult> Index()
    {
        if (Accept("text/html"))
        {
            return await IndexHtml();
        }
        else if (Accept("Application/json") || Accept("*/*"))
        {
            return await IndexJsonAsync();
        }
        else
        {
            return View();
        }
    }

    private async Task<IActionResult> IndexHtml()
    {
        return View("Index");
    }

    private readonly string[] _verbsWithForms = new string[] {"POST", "PUT"};
    private readonly string[] _contentTypeWithoutForms = new string[] {"application/json", null};
    private readonly string[] _contentJson = new string[] {"application/json"};

    private async Task<IActionResult> IndexJsonAsync()
    {
        var appName = Environment.GetEnvironmentVariable("APP_NAME");
        appName = string.IsNullOrWhiteSpace(appName) ? "Echo" : appName;

        var returnValue = new JObject
        {
            {"AppName", new JValue(appName)},
            {"Now", new JValue(DateTime.Now.ToString("o"))},
            {"UtcNow", new JValue(DateTime.UtcNow.ToString("o"))},
            {"Headers", JObject.FromObject(Request.Headers)},
            {"Path", new JValue(Request.Path)},
            {"MachineName", new JValue(Environment.MachineName)},
            {"Method", new JValue(Request.Method)},
            {"Scheme", new JValue(Request.Scheme)},
            {"QueryString", JArray.FromObject(Request.Query)},
            {"Forms", JArray.FromObject(_verbsWithForms.Contains(Request.Method)
                                        && _contentTypeWithoutForms.Contains(Request.ContentType)
                ? new FormCollection(null, null)
                : Request.Form)}
        };
        await SetPayload(returnValue);
        returnValue.Add("EnvironmentVariables", JObject.FromObject(Environment.GetEnvironmentVariables()));

        var returnJson = returnValue.ToString();

        return Content(returnJson, _contentJson.First());
    }

    private async Task SetPayload(JObject returnValue)
    {
        if (_contentJson.Contains(Request.ContentType))
        {
            Request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(Request.ContentLength)];
            await Request.Body.ReadAsync(buffer, 0, buffer.Length);
            var json = Encoding.UTF8.GetString(buffer);
            returnValue.Add("Payload", JObject.Parse(json));
        }
    }
}