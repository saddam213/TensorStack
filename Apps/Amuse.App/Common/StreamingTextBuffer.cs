using System.Text;

namespace Amuse.App.Common
{
    public class StreamingTextBuffer
    {
        private readonly StringBuilder _buffer = new();

        public void Append(string token)
        {
            lock (_buffer)
            {
                _buffer.Append(token);
            }
        }

        public string Flush()
        {
            lock (_buffer)
            {
                if (_buffer.Length == 0)
                    return null;

                var text = _buffer.ToString();
                _buffer.Clear();
                return text;
            }
        }

        public void Clear()
        {
            lock (_buffer)
            {
                _buffer.Clear();
            }
        }
    }
}
