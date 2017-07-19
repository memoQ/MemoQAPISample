using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Channels;

using MQS.Security;

namespace ApiClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            const string baseUrl = "/memoqservices";
            const string apiKey = "";
            using (Service<ISecurityService> svcSecurity = new Service<ISecurityService>(baseUrl, apiKey))
            {
                var users = svcSecurity.Proxy.ListUsers();
                var groups = svcSecurity.Proxy.ListGroups();
            }
        }
    }
}
