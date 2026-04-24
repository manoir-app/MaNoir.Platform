using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MaNoir.Core.Contracts.Models.Mesh;

[JsonConverter(typeof(StringEnumConverter))]
public enum CommunicationMode
{
    RestApi
}