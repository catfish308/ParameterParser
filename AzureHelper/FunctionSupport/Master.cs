using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace FunctionSupport
{
    public class Master
    {
        JObject myTemplate;
        public Master(string jsonTemplate)
        {
            myTemplate = JObject.Parse(jsonTemplate);

           
        }


        public string GetDefaulValue(string paramName)
        {
            string value = "";
            try
            {
                value = myTemplate["parameters"][paramName]["defaultValue"].ToString();
            }
            catch (Exception X)
            {
                Trace.TraceError(X.Message);
            }
            return value;
        }

        public string GetAllowedValues(string paramName)
        {
            string values = "";
            try
            {
                values= myTemplate["parameters"][paramName]["allowedValues"].ToString();
            }
            catch (Exception X)
            {
                Trace.TraceError(X.Message);
            
            }
            return values;
        }

    }
}
