using Amuse.App.Common;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TensorStack.Common;

namespace Amuse.App
{
    public static class Extensions
    {
        private static readonly SearchValues<char> InvalidPathChars = SearchValues.Create(Path.GetInvalidPathChars());

        /// <summary>
        /// Gets the file extesnion.
        /// </summary>
        /// <param name="mediaType">Type of the media.</param>
        public static string GetExtension(this MediaType mediaType)
        {
            return mediaType switch
            {
                MediaType.Text => "txt",
                MediaType.Audio => "wav",
                MediaType.Video => "mp4",
                MediaType.Image => "png",
                _ => throw new NotSupportedException()
            };
        }


        public static int GetIndex(this MemoryProfile profile, int deviceMemory)
        {
            int bestIndex = -1;
            int bestValue = int.MinValue;

            for (int i = 0; i < profile.MemoryModes.Length; i++)
            {
                int value = profile.MemoryModes[i];
                if (value <= deviceMemory && value >= bestValue)
                {
                    bestValue = value;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
                bestIndex = 0;

            return bestIndex;
        }


        public static bool HasChanged(this IReadOnlyList<LoraAdapterModel> existingAdapters, IReadOnlyList<LoraAdapterModel> newAdapters)
        {
            if (ReferenceEquals(existingAdapters, newAdapters))
                return false;

            if (existingAdapters == null || newAdapters == null)
                return true;

            if (existingAdapters.Count != newAdapters.Count)
                return true;

            for (int i = 0; i < existingAdapters.Count; i++)
            {
                if (!string.Equals(existingAdapters[i]?.Key, newAdapters[i]?.Key, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }



        public static SchedulerInputOptions[] Copy(this SchedulerInputOptions[] collection)
        {
            if (collection.IsNullOrEmpty())
                return null;

            return collection.Select(x => x with
            {
                ScaleFactors = x.ScaleFactors?.ToList(),
                StageRange = x.StageRange?.ToList(),
                DisableCorrector = x.DisableCorrector?.ToList(),
            }).ToArray();
        }


        public static MemoryProfile[] Copy(this MemoryProfile[] collection)
        {
            if (collection.IsNullOrEmpty())
                return [];

            return collection.Select(x => new MemoryProfile
            {
                QualityMode = x.QualityMode,
                MemoryModes = x.MemoryModes.ToArray(),
            }).ToArray();
        }


        public static SizeOption[] Copy(this SizeOption[] collection)
        {
            if (collection.IsNullOrEmpty())
                return [];

            return collection.Select(x => new SizeOption
            {
                Height = x.Height,
                Width = x.Width,
                IsDefault = x.IsDefault
            }).ToArray();
        }


        public static bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            return path.AsSpan().IndexOfAny(InvalidPathChars) == -1;
        }


        public static bool AddIfNotNull<TSource>(this IList<TSource> source, TSource item)
        {
            if (item is null)
                return false;

            source.Add(item);
            return true;
        }


        public static int RemoveAll<T>(this IList<T> collection, Predicate<T> condition)
        {
            var removed = 0;
            for (int i = collection.Count - 1; i >= 0; i--)
            {
                if (condition(collection[i]))
                {
                    collection.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }


        public static int NextId<T>(this Collection<T> collection) where T : IDownloadModel
        {
            return collection.NextId(x => x.Id);
        }


        public static int NextId<T>(this Collection<T> collection, Func<T, int> selector)
        {
            var nextId = Utils.FixedIdRange + 1;
            if (collection.IsNullOrEmpty())
                return nextId;

            return Math.Max(nextId, collection.Max(selector) + 1);
        }


        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                    yield return t;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
    }

    public static partial class Utils
    {
        public const int FixedIdRange = 1000;


        public static bool HasThinkingText(string content, string tagOpen = "<think>", string tagClose = "</think>")
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            return content.StartsWith(tagOpen, StringComparison.OrdinalIgnoreCase)
                && content.Contains(tagClose, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Gets the thinking text.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="tagOpen">The tag open.</param>
        /// <param name="tagClose">The tag close.</param>
        public static string GetThinkingText(string content, string tagOpen = "<think>", string tagClose = "</think>")
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            if (content.StartsWith(tagOpen, StringComparison.OrdinalIgnoreCase))
            {
                var start = tagOpen.Length;
                var end = content.IndexOf(tagClose, StringComparison.OrdinalIgnoreCase);
                if (end > start)
                    return content[start..end].Trim();
            }
            return string.Empty;
        }


        /// <summary>
        /// Gets the response text.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="tagOpen">The tag open.</param>
        /// <param name="tagClose">The tag close.</param>
        /// <returns>System.String.</returns>
        public static string GetResponseText(string content, string tagOpen = "<think>", string tagClose = "</think>")
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            if (content.StartsWith(tagOpen, StringComparison.OrdinalIgnoreCase))
            {
                var start = content.IndexOf(tagClose, StringComparison.OrdinalIgnoreCase);
                if (start > 0)
                    return content[(start + tagClose.Length)..].Trim();
            }
            return content;
        }

    }



    public static class FontOptions
    {
        public static FontWeight[] FontWeightList { get; } = new[]
           {
            FontWeights.Thin,
            FontWeights.ExtraLight,
            FontWeights.Light,
            FontWeights.Normal,
            FontWeights.Medium,
            FontWeights.SemiBold,
            FontWeights.Bold,
            FontWeights.ExtraBold,
            FontWeights.Black
        };


        public static FontStyle[] FontStyleList { get; } = new[]
        {
            FontStyles.Normal,
            FontStyles.Italic,
            FontStyles.Oblique
        };


        public static ICollection<FontFamily> FontFamilies { get; } = System.Windows.Media.Fonts.SystemFontFamilies;
    }


    public static class BrushOptions
    {
        public static IEnumerable<Brush> AllBrushes { get; } =
            typeof(Brushes).GetProperties()
                .Where(p => p.PropertyType == typeof(Brush))
                .Select(p => (Brush)p.GetValue(null));
    }
}
