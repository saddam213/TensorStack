using Amuse.Common;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using TensorStack.WPF;

namespace Amuse.App.Common
{
    public sealed record ConversationModel : BaseRecord
    {
        private ConversationRole _role;
        private string _content;
        private List<int> _imageIndex;

        public ConversationRole Role
        {
            get { return _role; }
            set { SetProperty(ref _role, value); }
        }

        public string Content
        {
            get { return _content; }
            set { SetProperty(ref _content, value); }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<int> ImageIndex
        {
            get { return _imageIndex; }
            set { SetProperty(ref _imageIndex, value); }
        }


        public bool Equals(ConversationModel other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
