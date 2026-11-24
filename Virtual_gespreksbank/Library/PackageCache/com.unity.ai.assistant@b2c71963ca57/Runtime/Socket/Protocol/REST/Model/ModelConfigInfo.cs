using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace Unity.Ai.Assistant.Protocol.Model
{
    /// <summary>
    /// Information about a single model configuration option.
    /// </summary>
    [DataContract(Name = "ModelConfigInfo")]
    partial class ModelConfigInfo
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelConfigInfo" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected ModelConfigInfo() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelConfigInfo" /> class.
        /// </summary>
        /// <param name="name">The name to use in the model_settings field of chat requests.</param>
        /// <param name="type">Type of the model configuration.</param>
        public ModelConfigInfo(string name, ModelConfigType type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Identifier for the model option.
        /// </summary>
        [DataMember(Name = "id", IsRequired = true, EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or Sets Type
        /// </summary>
        [DataMember(Name = "type", IsRequired = true, EmitDefaultValue = true)]
        public ModelConfigType Type { get; set; }

        /// <summary>
        /// The name to use in the model_settings field of chat requests.
        /// </summary>
        /// <value>The name to use in the model_settings field of chat requests.</value>
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = true)]
        public string Name { get; set; }

        /// <summary>
        /// Optional tooltip text describing the model option.
        /// </summary>
        [DataMember(Name = "tooltip", EmitDefaultValue = false)]
        public string Tooltip { get; set; }

        /// <summary>
        /// Id of the tier this option belongs to, from the response's tier list.
        /// </summary>
        [DataMember(Name = "tier", EmitDefaultValue = false)]
        public string Tier { get; set; }

        /// <summary>
        /// The option's standing within its tier, such as "recommended" or "preview".
        /// </summary>
        [DataMember(Name = "status", EmitDefaultValue = false)]
        public string Status { get; set; }

        /// <summary>
        /// One-line description shown under the option's name in the picker.
        /// </summary>
        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        /// Whether this is the option the app starts on.
        /// </summary>
        [DataMember(Name = "isDefault", EmitDefaultValue = false)]
        public bool IsDefault { get; set; }

        /// <summary>
        /// Whether the option was released recently enough to carry a "New" label.
        /// </summary>
        [DataMember(Name = "isNew", EmitDefaultValue = false)]
        public bool IsNew { get; set; }

        /// <summary>
        /// Thinking levels the option accepts, such as "quick", "balanced" and "deep".
        /// </summary>
        [DataMember(Name = "thinkingLevels", EmitDefaultValue = false)]
        public List<string> ThinkingLevels { get; set; }

        /// <summary>
        /// The thinking level the option runs at when none is sent.
        /// </summary>
        [DataMember(Name = "defaultThinkingLevel", EmitDefaultValue = false)]
        public string DefaultThinkingLevel { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class ModelConfigInfo {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Tooltip: ").Append(Tooltip).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Tier: ").Append(Tier).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  IsDefault: ").Append(IsDefault).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }
    }

}
