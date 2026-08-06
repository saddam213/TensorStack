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
        private string _thinking;
        private Dictionary<int, string> _imageIndex;
        private Dictionary<int, string> _audioIndex;
        private Dictionary<int, string> _videoIndex;

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
        public string Thinking
        {
            get { return _thinking; }
            set { SetProperty(ref _thinking, value); }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<int, string> ImageIndex
        {
            get { return _imageIndex; }
            set { SetProperty(ref _imageIndex, value); }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<int, string> AudioIndex
        {
            get { return _audioIndex; }
            set { SetProperty(ref _audioIndex, value); }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<int, string> VideoIndex
        {
            get { return _videoIndex; }
            set { SetProperty(ref _videoIndex, value); }
        }

        public bool Equals(ConversationModel other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
