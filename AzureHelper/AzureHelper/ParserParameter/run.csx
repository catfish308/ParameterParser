#r "..\external\FunctionSupport.dll"
using System.Net;
using FunctionSupport;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    
   
    // parse query parameter
    string parameterName = 
        req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "parameterName", true) == 0)
        .Value;

    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    // Set name to query string or body data
    string template = Convert.ToString( data);
    Master x = new Master(template);

    string  DefaultValue= x.GetDefaulValue(parameterName);
    string allowedValues = x.GetAllowedValues(parameterName);

    string jsonOut = @"{ ""DefaultValue"": """ + DefaultValue + @""" ,";
    if (string.IsNullOrEmpty(allowedValues))
    {
        allowedValues = "[]";
    }
    jsonOut += @"""allowedValues"": " + allowedValues + "}";
    jsonOut=jsonOut.Replace("\r\n", string.Empty);

    var response = req.CreateResponse(HttpStatusCode.OK);
    response.Content = new StringContent(jsonOut, System.Text.Encoding.UTF8, "application/json");

    return response;
 }