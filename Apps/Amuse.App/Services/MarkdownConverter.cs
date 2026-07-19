using ColorCode;
using HtmlAgilityPack;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Media;

namespace Amuse.App.Services
{
    internal static class MarkdownConverter
    {
        private static readonly string _header;
        private static readonly MarkdownPipeline _pipeline;
        private static readonly HtmlClassFormatter _formatter;

        /// <summary>
        /// Initializes static members of the <see cref="MarkdownConverter"/> class.
        /// </summary>
        static MarkdownConverter()
        {
            _header = BuildHeader();
            _formatter = new HtmlClassFormatter();
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .DisableHtml()
                .UseBootstrap()
                .UseSmartyPants()
                .UseEmojiAndSmiley()
                .Use(new ThinkExtension())
                .Build();
        }


        /// <summary>
        /// Builds the HTML.
        /// </summary>
        /// <param name="markdown">The markdown.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontFamily">The font family.</param>
        public static string BuildHtml(string markdown, double fontSize, FontFamily fontFamily, bool isThinkingVisible)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return BuildHtmlPage(string.Empty, fontSize, fontFamily);

            var markdownHtml = Markdown.ToHtml(markdown, _pipeline);
            var processedHtml = ProcessCodeSegments(markdownHtml, isThinkingVisible);
            return BuildHtmlPage(processedHtml, fontSize, fontFamily);
        }


        /// <summary>
        /// Builds the HTML header.
        /// </summary>
        private static string BuildHeader()
        {
            var styleSheet = App.GetEmbeddedResource("Amuse.App.Controls.MarkdownElement.css");
            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append($@"<meta charset=""UTF-8"">");
            htmlBuilder.Append($@"<style>{styleSheet}</style>");
            return htmlBuilder.ToString();
        }


        /// <summary>
        /// Builds the HTML page.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontFamily">The font family.</param>
        private static string BuildHtmlPage(ReadOnlySpan<char> body, double fontSize, FontFamily fontFamily)
        {
            var htmlBuilder = new StringBuilder(_header);

            // Fonts
            htmlBuilder.Append($@"<style>
                body,p,span,pre,li,th,td {{
                    font-size:{fontSize}px;
                    font-family:""{fontFamily}"";
                }}
            </style>");

            // Script
            htmlBuilder.Append(@"
            <script>
            document.addEventListener('click', function(e) {
                const thinking = e.target.id === 'thinking-summary'
                if(thinking)
                    e.preventDefault();
                window.chrome.webview.postMessage({
                    X: e.clientX,
                    Y: e.clientY,
                    Element: e.target.id,
                    ToggleThinking: thinking
                });
            }, true);
            </script>");

            return $"<html><head>{htmlBuilder}</head><body>{body}</body></html>";
        }


        /// <summary>
        /// Processes the code segments.
        /// </summary>
        /// <param name="markdownHtml">The markdown HTML.</param>
        private static string ProcessCodeSegments(string markdownHtml, bool isThinkingVisible)
        {
            try
            {
                var document = new HtmlDocument();
                document.LoadHtml(markdownHtml);

                // Show/Hide thinking section
                var thinkingNode = document.GetElementbyId("thinking-panel");
                if (thinkingNode != null)
                {
                    if (isThinkingVisible)
                        thinkingNode.Attributes.Add("open", "");
                    else
                        thinkingNode.Attributes.Remove("open");
                }

                // Format code segments
                foreach (var codeSegment in document.DocumentNode.SelectNodes("//pre") ?? Enumerable.Empty<HtmlNode>())
                {
                    var codeBlock = codeSegment.SelectSingleNode("./code");
                    if (codeBlock == null)
                        continue;

                    var language = "";
                    var classAttr = codeBlock.GetAttributeValue("class", "");
                    if (classAttr.StartsWith("language-"))
                        language = classAttr["language-".Length..];

                    var codeContent = WebUtility.HtmlDecode(codeBlock.InnerText);
                    var replacementSegment = GetCodeSegment(language, codeContent);
                    codeSegment.ParentNode.ReplaceChild(replacementSegment, codeSegment);
                }
                return document.DocumentNode.OuterHtml;
            }
            catch (Exception)
            {
                return markdownHtml;
            }
        }


        /// <summary>
        /// Gets the code segment.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <param name="content">The content.</param>
        private static HtmlNode GetCodeSegment(string language, ReadOnlySpan<char> content)
        {
            try
            {
                var languageId = Languages.FindById(GetLanguageCode(language));
                if (languageId == null)
                    return FormatCode(content);

                var codeContent = content.Trim().ToString();
                var formattedHtml = _formatter.GetHtmlString(codeContent, languageId).AsSpan();
                var preStart = formattedHtml.IndexOf("<pre>");
                var preEnd = formattedHtml.LastIndexOf("</pre>") + 6;
                return FormatCode(formattedHtml[preStart..preEnd]);
            }
            catch (Exception)
            {
                return FormatCode(content);
            }
        }


        /// <summary>
        /// Formats the code.
        /// </summary>
        /// <param name="content">The content.</param>
        private static HtmlNode FormatCode(ReadOnlySpan<char> content)
        {
            return HtmlNode.CreateNode($"{content}");
        }


        /// <summary>
        /// Gets the language code.
        /// </summary>
        /// <param name="codeLang">The code language.</param>
        private static string GetLanguageCode(string codeLang)
        {
            if (codeLang == "md")
                return "markdown";
            else if (codeLang == "jsx" || codeLang == "tsx")
                return "html";
            return codeLang;
        }
    }


    public class ThinkBlock : ContainerBlock
    {
        public ThinkBlock(BlockParser parser, string description) : base(parser)
        {
            Description = description;
        }

        public string Description { get; }
    }


    public class ThinkBlockParser : BlockParser
    {
        private readonly string _tagOpen;
        private readonly string _tagClose;
        private readonly string _description;

        public ThinkBlockParser(string description, string tagOpen, string tagClose)
        {
            _tagOpen = tagOpen;
            _tagClose = tagClose;
            _description = description;
            OpeningCharacters = new[] { '<' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (!processor.Line.Match(_tagOpen))
                return BlockState.None;

            processor.NewBlocks.Push(new ThinkBlock(this, _description));
            return BlockState.ContinueDiscard;
        }

        public override BlockState TryContinue(BlockProcessor processor, Block block)
        {
            if (processor.Line.Match(_tagClose))
            {
                return BlockState.BreakDiscard;
            }
            return BlockState.Continue;
        }
    }


    public class ThinkBlockRenderer : HtmlObjectRenderer<ThinkBlock>
    {
        protected override void Write(HtmlRenderer renderer, ThinkBlock block)
        {
            renderer.WriteLine("<details id=\"thinking-panel\">");
            renderer.WriteLine($"<summary id=\"thinking-summary\">{block.Description}</summary>");
            renderer.WriteLine("<div>");
            renderer.WriteChildren(block);
            renderer.WriteLine("</div>");
            renderer.Write("</details>");
        }
    }


    public class ThinkExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<ThinkBlockParser>())
            {
                pipeline.BlockParsers.Insert(0, new ThinkBlockParser("Thinking...", "<think>", "</think>"));
            }
        }


        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                if (!htmlRenderer.ObjectRenderers.Contains<ThinkBlockRenderer>())
                {
                    htmlRenderer.ObjectRenderers.Insert(0, new ThinkBlockRenderer());
                }
            }
        }
    }
}
