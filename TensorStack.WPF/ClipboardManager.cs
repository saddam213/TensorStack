using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TensorStack.WPF
{
    public static class ClipboardManager
    {
        public static async Task SetTextAsync(string text)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        Clipboard.Clear();
                        return;
                    }

                    Clipboard.SetText(text);
                    return;
                }
                catch (Exception)
                {
                    await Task.Delay(100);
                }
            }
        }


        public static FileInfo GetFileDrop(this DragEventArgs dragEvent)
        {
            if (dragEvent == null)
                return null;

            var filename = ((string[])dragEvent.Data.GetData(DataFormats.FileDrop))?.FirstOrDefault();
            if (!File.Exists(filename))
                return null;

            return new FileInfo(filename);
        }

    }
}