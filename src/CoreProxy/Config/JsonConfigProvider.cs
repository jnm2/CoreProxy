using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace jnm2.CoreProxy.Config
{
    public sealed class JsonConfigProvider : IConfigProvider
    {
        private readonly string path;

        public JsonConfigProvider(string path)
        {
            this.path = path;
        }

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters =
            {
                new StringEnumConverter { CamelCaseText = true },
                new EndPointConfigurationConverter()
            }
        };

        public CoreProxyServiceConfiguration Load()
        {
            using (var jsonReader = new JsonTextReader(File.OpenText(path)))
                return JsonSerializer.Create(SerializerSettings).Deserialize<CoreProxyServiceConfiguration>(jsonReader);
        }

        private sealed class EndPointConfigurationConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(IEndPointConfiguration);
            public override bool CanWrite => false;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var obj = (JObject)JToken.ReadFrom(reader);
                IEndPointConfiguration r;

                switch ((string)obj["protocol"])
                {
                    case "tcp":
                        r = new TcpConfiguration();
                        break;
                    case "tls":
                        r = new TlsConfiguration();
                        break;
                    default:
                        throw new InvalidDataException($"Unknown protocol '{(string)obj["protocol"]}'.");
                }

                r.EndPoint = new IPEndPoint(IPAddress.Parse((string)obj["ip"]), (ushort)obj["port"]);
                serializer.Populate(obj.CreateReader(), r);
                return r;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }
        }
    }
}