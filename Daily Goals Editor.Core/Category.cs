using System.Text.Json.Serialization;

namespace Daily_Goals_Editor.Core;

[JsonConverter(typeof(JsonStringEnumConverter<Category>))]
public enum Category
{
    Product,
    Engineering,
    Design,
    Operations
}
