using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Unity.Ai.Assistant.Protocol.Model
{
    /// <summary>
    /// A group of model options, such as Ultra or Standard, as listed in the picker.
    /// </summary>
    [DataContract(Name = "ModelTierInfo")]
    partial class ModelTierInfo
    {
        [JsonConstructor]
        protected ModelTierInfo() { }

        public ModelTierInfo(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Identifier that options reference in their tier field.
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Display name of the tier.
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// One-line purpose of the tier, shown beside its name.
        /// </summary>
        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }
    }
}
