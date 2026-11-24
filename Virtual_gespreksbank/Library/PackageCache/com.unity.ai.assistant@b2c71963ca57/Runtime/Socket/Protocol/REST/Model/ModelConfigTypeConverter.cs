using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Unity.Ai.Assistant.Protocol.Model
{
    /// <summary>
    /// Reads <see cref="ModelConfigType"/> as a string and maps a value this package doesn't know to
    /// <see cref="ModelConfigType.Unknown"/>.
    /// </summary>
    class ModelConfigTypeConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                return base.ReadJson(reader, objectType, existingValue, serializer);
            }
            catch (JsonSerializationException)
            {
                return ModelConfigType.Unknown;
            }
        }
    }
}
