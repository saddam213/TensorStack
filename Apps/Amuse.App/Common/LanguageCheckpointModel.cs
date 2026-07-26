using System.Collections.Generic;
using System.Text.Json.Serialization;
using TensorStack.WPF;

namespace Amuse.App.Common
{
    public sealed class LanguageCheckpointModel : BaseModel
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CheckpointComponent TextModel { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CheckpointComponent TextModel2 { get; set; }


        public bool IsValid()
        {
            foreach (var component in GetComponents())
            {
                if (!component.IsValid())
                    return false;
            }
            return true;
        }


        public bool IsInstalled(string modelDirectory, IReadOnlyCollection<ComponentModel> components)
        {
            foreach (var checkpointComponent in GetComponents())
            {
                if (!checkpointComponent.IsInstalled(modelDirectory, components))
                    return false;
            }
            return true;
        }


        public IEnumerable<CheckpointComponent> GetComponents()
        {
            if (TextModel != null) yield return TextModel;
            if (TextModel2 != null) yield return TextModel2;
        }


        public LanguageCheckpointModel DeepClone()
        {
            return new LanguageCheckpointModel
            {
                TextModel = TextModel?.DeepClone(),
                TextModel2 = TextModel2?.DeepClone()
            };
        }

    }
}
