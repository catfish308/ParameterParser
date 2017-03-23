#r "..\external\FunctionSupport.dll"
using FunctionSupport;
using System.Net;



public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    ARMRestHelper myRestHelp = new ARMRestHelper();

    // parse query parameter
    string subId = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "subId", true) == 0).Value;
    string RGName = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "RGName", true) == 0).Value;
    string DeployName = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "DeployName", true) == 0).Value;
    string Token = req.GetQueryNameValuePairs().FirstOrDefault(q => string.Compare(q.Key, "Token", true) == 0).Value;
    HttpStatusCode X;
    string content;
    
    try
    {
        content = myRestHelp.TagResources(subId, RGName, DeployName, "provider", "D445ED96-C1B1-434D-9E46-D2929964E0B1", Token);
        X = HttpStatusCode.OK;
      
    }
    catch (System.Exception e)
    {

        X = HttpStatusCode.InternalServerError;
        content = "{\"error\":\""+  e.Message  +"\"}";
    }
    var response = req.CreateResponse(X);
    response.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");

    return response;
}