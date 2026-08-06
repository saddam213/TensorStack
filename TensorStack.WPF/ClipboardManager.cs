using System;
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

    }
}