using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FunctionSupport
{
    public static class ARMCommands
    {
        public static string GetDeployment = "subscriptions/{0}/resourcegroups/{1}/providers/Microsoft.Resources/deployments/{2}?api-version=2016-09-01";
        public static string GetProvider = "https://management.azure.com/subscriptions/{0}/providers/{1}?api-version=2016-09-01";
        public static string GetResource = "https://management.azure.com/subscriptions/{0}/resourcegroups/{1}/providers/{2}?api-version={3}";
    }
  
    public class ARMRestHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tenant_id"></param>
        /// <param name="client_id"></param>
        /// <param name="client_secret"></param>
        /// <param name="managementUrl"></param>
        /// <param name="loginUrl"></param>
        /// <returns></returns>
        public async Task<string> GetToken(string tenant_id, string client_id, string client_secret, string managementUrl, string loginUrl)
        {
            string myToken = null;
            var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]{
                new KeyValuePair<string, string>("resource", managementUrl),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", client_id),
                new KeyValuePair<string, string>("client_secret",client_secret) });
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.PostAsync(loginUrl + tenant_id + "/oauth2/token", content).Result;
                string stringR = await response.Content.ReadAsStringAsync();
                JObject jsonR = JObject.Parse(stringR);
                myToken = jsonR.SelectToken("access_token").ToString();
            }
            return myToken;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="managementUrl"></param>
        /// <param name="myToken"></param>
        /// <returns></returns>
        public async Task<string> ExecuteGet(string command, string managementUrl, string myToken)
        {
            string stringR = null;
            using (var client = new HttpClient())
            {
                //Common headers
                client.BaseAddress = new Uri(managementUrl);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer  " + myToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await client.GetAsync(command);
                stringR = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(response.StatusCode + " " + stringR);
                }
            }
            return stringR;
        }
        /// <summary>
        /// Execute HTTP POST 
        /// </summary>
        /// <param name="command">path call</param>
        /// <param name="myContent">body content</param>
        /// <param name="managementUrl">API URL</param>
        /// <param name="myToken">Bearer Token</param>
        /// <returns></returns>
        public async Task<string> ExecuteHttpPost(string command, HttpContent myContent, string managementUrl, string myToken)
        {
            string stringR = null;
            using (var client = new HttpClient())
            {
                //Common headers
                client.BaseAddress = new Uri(managementUrl);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer  " + myToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.PutAsync(command, myContent).Result;
                stringR = await response.Content.ReadAsStringAsync();
            }
            return stringR;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="DeployName"></param>
        /// <param name="tagKey"></param>
        /// <param name="tagValue"></param>
        /// <param name="Token"></param>
        public string TagResources(string subscriptionId, string resourceGroupName, string DeployName, string tagKey, string tagValue, string Token)
        {
            //Get Deployment
            string command = String.Format(ARMCommands.GetDeployment,subscriptionId,resourceGroupName,DeployName);
            var rString = ExecuteGet(command, "https://management.azure.com/", Token).Result;
            JObject myResponse = JObject.Parse(rString);
            
            // Outpur resorces of deployment
            var myResList = myResponse["properties"]["outputResources"];

            foreach (var xResource in myResList)
            {
                string[] rId = xResource["id"].ToString().Split('/');
                string provider = rId[0];

                //Get Provider informations
                command = string.Format(ARMCommands.GetProvider,subscriptionId,provider);
                var json = ExecuteGet(command, "https://management.azure.com/", Token).Result;
                JObject providerInfo = JObject.Parse(json);


                var xPRovider = providerInfo["resourceTypes"].Where(x => x["resourceType"].ToString() == rId[1]);
                //Provider API Information
                string apiversionX = xPRovider.FirstOrDefault()["apiVersions"].FirstOrDefault().ToString();

                //Get resource information
                command = string.Format(ARMCommands.GetResource,subscriptionId,resourceGroupName,xResource["id"].ToString(),apiversionX);
                json = ExecuteGet(command, "https://management.azure.com/", Token).Result;
                JObject myResource = JObject.Parse(json);

                var myTag=JObject.Parse("{\""+ tagKey +"\": \""+ tagValue +"\"}");

                //Check TAGS
                if (myResource["tags"] == null)
                {
                    //ADD tag to resoruce
                    myResource.Add("tags", myTag);

                }
                else
                {
                    //Add TAGG to a existing tag list
                    Trace.TraceInformation("It has tags");
                    if (myResource["tags"].HasValues)
                    {
                        myResource["tags"][tagKey] = tagValue;
                    }
                    else
                    {
                        myResource["tags"] = myTag;
                    }
                }

                //Update Resource to add TAG
                string jsonUpdated = JsonConvert.SerializeObject(myResource);
                var myContent = new StringContent(jsonUpdated, Encoding.UTF8, "application/json");
                json = ExecuteHttpPost(command, myContent, "https://management.azure.com/", Token).Result;

                xResource["id"] = string.Format("/subscriptions/{0}/resourceGroups/{1}/providers/{2}",subscriptionId,resourceGroupName, xResource["id"]);
                Trace.TraceInformation("Updated Resorce {0}", xResource["id"].ToString());
            }

            return JsonConvert.SerializeObject(myResList);
        }
    }
}
