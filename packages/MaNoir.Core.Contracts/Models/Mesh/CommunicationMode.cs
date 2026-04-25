using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MaNoir.Core.Contracts.Models.Mesh;

[JsonConverter(typeof(StringEnumConverter))]
/// <summary>
/// Describes how a mesh capability is exposed to other components.
/// </summary>
public enum CommunicationMode
{
    /// <summary>
    /// The capability is exposed through an HTTP REST API.
    /// </summary>
    RestApi
}