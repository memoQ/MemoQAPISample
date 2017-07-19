using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ApiClient
{
    public class Service<T> : IDisposable
    {
        private readonly ChannelFactory<T> factory;
        private readonly T proxy;

        public Service(string baseUrl, string apiKey)
        {
            var name = typeof(T).Name.Substring(1).Replace("Service", "");
            var url = baseUrl + "/" + name.ToLowerInvariant() + "/" + name + "Service";
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
            var header = AddressHeader.CreateAddressHeader("ApiKey", "", apiKey);
            var address = new EndpointAddress(new Uri(url), header);
            factory = new ChannelFactory<T>(binding, address);
            proxy = factory.CreateChannel();
        }

        public T Proxy { get { return proxy;  } }

        public void Dispose()
        {
            try { factory.Close(); }
            catch { }
        }
    }
}
