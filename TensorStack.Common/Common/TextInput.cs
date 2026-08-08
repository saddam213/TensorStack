using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TensorStack.Common
{
    public class TextInput
    {
        private readonly string _sourceFile;
        public TextInput() { }
        public TextInput(string textInput, string filename = null)
        {
            Text = textInput;
            _sourceFile = filename;
        }

        public TextInput(string filename, Encoding encoding)
            : this(File.ReadAllText(filename, encoding), filename) { }

        public string Text { get; set; }

        public string SourceFile => _sourceFile;
        public int Length => Text?.Length ?? 0;


        public int Beam { get; set; }
        public float Score { get; set; }
        public float PenaltyScore { get; set; }
        public int TokenCount { get; set; }



        public void Save(string filename)
        {
            File.WriteAllText(filename, Text);
        }

        public async Task SaveAsync(string filename)
        {
            await File.WriteAllTextAsync(filename, Text);
        }

        public static async Task<TextInput> CreateAsync(string filename, Encoding encoding = default, CancellationToken cancellationToken = default)
        {
            encoding ??= Encoding.UTF8;
            return new TextInput(await File.ReadAllTextAsync(filename, encoding, cancellationToken));
        }
    }

}
