using System.Runtime.CompilerServices;
using TensorStack.WPF;

namespace Amuse.App.Common
{
    public sealed record ConversationModel : BaseRecord
    {
        private string _role;
        private string _content;

        public string Role
        {
            get { return _role; }
            set { SetProperty(ref _role, value); }
        }

        public string Content
        {
            get { return _content; }
            set { SetProperty(ref _content, value); }
        }

        public bool Equals(ConversationModel other) => ReferenceEquals(this, other);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
