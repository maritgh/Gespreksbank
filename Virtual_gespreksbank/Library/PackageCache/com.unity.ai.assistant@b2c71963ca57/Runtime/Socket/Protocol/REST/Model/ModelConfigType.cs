using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Unity.Ai.Assistant.Protocol.Model
{
    /// <summary>
    /// Type of model configuration option.
    /// </summary>
    /// <value>Type of model configuration option.</value>
    [JsonConverter(typeof(ModelConfigTypeConverter))]
    enum ModelConfigType
    {
        /// <summary>
        /// A value this package doesn't know. Parsed instead of failing so one new type can't drop the whole model list.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Enum Profile for value: profile
        /// </summary>
        [EnumMember(Value = "profile")]
        Profile = 1,

        /// <summary>
        /// Enum Model for value: model
        /// </summary>
        [EnumMember(Value = "model")]
        Model = 2
    }

}
