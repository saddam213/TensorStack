using ColorCode;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Media;

namespace Amuse.App.Services
{
    internal static class MarkdownConverter
    {
        private static readonly MarkdownPipeline _pipeline;
        private static readonly HtmlClassFormatter _formatter;
        private static readonly string _htmlStyleSheet;
        private static readonly string _htmlJavascript;

        /// <summary>
        /// Initializes static members of the <see cref="MarkdownConverter"/> class.
        /// </summary>
        static MarkdownConverter()
        {
            _htmlStyleSheet = App.GetEmbeddedResource("Amuse.App.Controls.MarkdownElement.css");
            _htmlJavascript = App.GetEmbeddedResource("Amuse.App.Controls.MarkdownElement.js");
            _formatter = new HtmlClassFormatter();
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .DisableHtml()
                .UseBootstrap()
                .UseSmartyPants()
                .UseEmojiAndSmiley()
                .UseSoftlineBreakAsHardlineBreak()
                .Use(new ConversationExtension())
                .Use(new ColorCodeExtension(_formatter))
                .Build();
        }


        /// <summary>
        /// Builds the HTML.
        /// </summary>
        /// <param name="markdown">The markdown.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontFamily">The font family.</param>
        public static string BuildHtml(double fontSize, FontFamily fontFamily, bool isThinkingVisible)
        {
            return BuildHtmlPage(string.Empty, fontSize, fontFamily, isThinkingVisible);
        }


        public static string BuildBody(string markdown, bool isThinkingVisible)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            var markdownHtml = Markdown.ToHtml(markdown, _pipeline);
            return BuildHtmlBody(markdownHtml, isThinkingVisible);
        }


        public static string BuildFullHtml(string markdown, double fontSize, FontFamily fontFamily, bool isThinkingVisible)
        {
            var htmlBody = BuildBody(markdown, isThinkingVisible);
            return BuildHtmlPage(htmlBody, fontSize, fontFamily, isThinkingVisible);
        }


        /// <summary>
        /// Builds the HTML page.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontFamily">The font family.</param>
        private static string BuildHtmlPage(ReadOnlySpan<char> body, double fontSize, FontFamily fontFamily, bool isThinkingVisible)
        {
            const string htmlTemplate =
@"<!DOCTYPE html>
<html>
    <head>
        <meta charset=""UTF-8"">
        <script>
            {0}
        </script>
        <style>
            body,p,span,pre,li,th,td {{
                font-size:{2}px;
                font-family:""{3}"";
            }}
            {1}
        </style>
    </head>
    <body>
        {4}
    </body>
</html>";

            // 0 = style,  1 = font-size, 2 = family, script = 3, bode = 4
            var bodyContent = BuildHtmlBody(body, isThinkingVisible);
            return string.Format(htmlTemplate, _htmlJavascript, _htmlStyleSheet, fontSize, fontFamily, bodyContent);
        }


        private static string BuildHtmlBody(ReadOnlySpan<char> body, bool isThinkingVisible)
        {
            var bodyContent = body.ToString();
            if (isThinkingVisible) // TODO
                bodyContent = bodyContent.Replace("<details class=\"thinking-panel\">", "<details class=\"thinking-panel\" open>");

            return bodyContent;
        }
    }


    public class ConversationExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<ConversationBlockParser>())
                pipeline.BlockParsers.Insert(0, new ConversationBlockParser());
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                if (!htmlRenderer.ObjectRenderers.Contains<TagBlockRenderer>())
                    htmlRenderer.ObjectRenderers.Add(new TagBlockRenderer());
                if (!htmlRenderer.ObjectRenderers.Contains<ThinkBlockRenderer>())
                    htmlRenderer.ObjectRenderers.Add(new ThinkBlockRenderer());
            }
        }
    }


    public class ConversationBlockParser : BlockParser
    {
        private readonly HashSet<string> _stripTags;

        public ConversationBlockParser()
        {
            OpeningCharacters = ['<'];
            _stripTags = ["<assistant>", "</assistant>"];
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.Line.Match("<think>"))
            {
                processor.NewBlocks.Push(new ThinkBlock(this));
                processor.GoToColumn(processor.Line.End + 1);
                return BlockState.ContinueDiscard;
            }
            if (processor.Line.Match("<user>"))
            {
                processor.NewBlocks.Push(new TagBlock(this, "User", true));
                processor.GoToColumn(processor.Line.End + 1);
                return BlockState.ContinueDiscard;
            }
            if (processor.Line.Match("<system>"))
            {
                processor.NewBlocks.Push(new TagBlock(this, "System", true));
                processor.GoToColumn(processor.Line.End + 1);
                return BlockState.ContinueDiscard;
            }
            if (_stripTags.Any(tagline => processor.Line.Match(tagline)))
            {
                processor.GoToColumn(processor.Line.End + 1);
                return BlockState.None;
            }
            return BlockState.None;
        }

        public override BlockState TryContinue(BlockProcessor processor, Block block)
        {
            if (block is ThinkBlock && processor.Line.Match("</think>"))
            {
                processor.GoToColumn(processor.Line.End + 1);
                return BlockState.BreakDiscard;
            }
            if (block is TagBlock && processor.Line.Match("</user>"))
            {
                processor.GoToColumn(processor.Line.End + 1);
                return BlockState.BreakDiscard;
            }
            if (block is TagBlock && processor.Line.Match("</system>"))
            {
                processor.GoToColumn(processor.Line.End + 1);
                return BlockState.BreakDiscard;
            }
            return BlockState.Continue;
        }
    }


    public class ThinkBlock : ContainerBlock
    {
        public ThinkBlock(BlockParser parser)
            : base(parser) { }
    }



    public class ThinkBlockRenderer : HtmlObjectRenderer<ThinkBlock>
    {
        protected override void Write(HtmlRenderer renderer, ThinkBlock block)
        {
            renderer.WriteLine("<details class=\"thinking-panel\">");
            renderer.WriteLine($"<summary class=\"thinking-summary\">Thinking...</summary>");
            renderer.WriteLine("<div>");
            renderer.WriteChildren(block);
            renderer.WriteLine("</div>");
            renderer.Write("</details>");
        }
    }


    public class TagBlock : ContainerBlock
    {
        public TagBlock(BlockParser parser, string name, bool isVisible) : base(parser)
        {
            Name = name;
            IsVisible = isVisible;
            ClassName = $"message-{Name.ToLower()}";
        }

        public string Name { get; }
        public bool IsVisible { get; }
        public string ClassName { get; }
    }


    public class TagBlockRenderer : HtmlObjectRenderer<TagBlock>
    {
        protected override void Write(HtmlRenderer renderer, TagBlock block)
        {
            if (!block.IsVisible)
                return;

            Block lastProcessedBlock = null;
            renderer.WriteLine($"<div class=\"{block.ClassName}\">");
            renderer.Write("<p>");
            foreach (var subBlock in block.Descendants<Block>())
            {
                if (subBlock is CodeBlock codeBlock)
                {
                    if (lastProcessedBlock != null)
                        renderer.WriteLine("<br /><br />");

                    lastProcessedBlock = subBlock;
                    foreach (var line in codeBlock.Lines.Lines)
                    {
                        if (line.Slice.Length > 0)
                        {
                            renderer.WriteEscape(line.Slice);
                            if (line.NewLine != Markdig.Helpers.NewLine.None)
                                renderer.Write("<br />");
                        }
                    }
                    continue;
                }

                if (subBlock is LeafBlock leafBlock && leafBlock.Inline != null)
                {
                    if (lastProcessedBlock != null && lastProcessedBlock != subBlock)
                        renderer.WriteLine("<br />");

                    lastProcessedBlock = subBlock;
                    Inline inline = leafBlock.Inline;
                    while (inline != null)
                    {
                        RenderInlineFiltered(renderer, inline);
                        inline = inline.NextSibling;
                    }
                }
            }
            renderer.WriteLine("</p>");
            renderer.WriteLine("</div>");
        }


        private static void RenderInlineFiltered(HtmlRenderer renderer, Inline inline)
        {
            switch (inline)
            {
                case LiteralInline:
                case LinkInline:
                case AutolinkInline:
                    renderer.Write(inline);
                    break;
                case CodeInline codeInline:
                    renderer.WriteEscape(codeInline.Content);
                    break;
                case LineBreakInline:
                    renderer.WriteLine("<br />");
                    break;
                case ContainerInline container:
                    var child = container.FirstChild;
                    while (child != null)
                    {
                        RenderInlineFiltered(renderer, child);
                        child = child.NextSibling;
                    }
                    break;
            }
        }
    }


    public class ColorCodeExtension : IMarkdownExtension
    {
        private readonly HtmlClassFormatter _formatter;

        public ColorCodeExtension(HtmlClassFormatter formatter)
        {
            _formatter = formatter;
        }

        public void Setup(MarkdownPipelineBuilder pipeline) { }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                var defaultRenderer = htmlRenderer.ObjectRenderers.Find<CodeBlockRenderer>();
                if (defaultRenderer != null)
                {
                    htmlRenderer.ObjectRenderers.Remove(defaultRenderer);
                }
                htmlRenderer.ObjectRenderers.Add(new ColorCodeBlockRenderer(_formatter));
            }
        }
    }


    public class ColorCodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
    {
        private readonly HtmlClassFormatter _formatter;

        public ColorCodeBlockRenderer(HtmlClassFormatter formatter)
        {
            _formatter = formatter;
        }


        protected override void Write(HtmlRenderer renderer, CodeBlock block)
        {
            var codeContent = block.Lines.ToString();
            var codeLanguage = GetCodeLanguage(block);
            if (codeLanguage == null)
            {
                renderer.Write(FormatCode(WebUtility.HtmlEncode(codeContent), true));
                return;
            }

            var formattedHtmlSpan = _formatter.GetHtmlString(codeContent, codeLanguage).AsSpan();
            var codeSectionStart = formattedHtmlSpan.IndexOf("<pre>");
            var codeSectionEnd = formattedHtmlSpan.LastIndexOf("</pre>") + 6;
            if (codeSectionEnd > codeSectionStart)
                formattedHtmlSpan = FormatCode(formattedHtmlSpan[codeSectionStart..codeSectionEnd], false);

            renderer.Write(formattedHtmlSpan);
        }


        private static ILanguage GetCodeLanguage(CodeBlock codeBlock)
        {
            try
            {
                var language = string.Empty;
                if (codeBlock is FencedCodeBlock fencedCodeBlock && !string.IsNullOrEmpty(fencedCodeBlock.Info))
                    language = fencedCodeBlock.Info.Split(' ', '\t')[0].Trim();

                if (string.IsNullOrEmpty(language))
                    return null;

                if (AlternateLanguages.TryGetValue(language, out var alternateLanguage))
                    return Languages.FindById(alternateLanguage);

                return Languages.FindById(language);
            }
            catch (Exception)
            {
                return null;
            }
        }


        private static string FormatCode(ReadOnlySpan<char> content, bool isSimpleLayout)
        {
            if (isSimpleLayout)
                return $"<pre>{content}</pre>";

            const string codeTemplate =
            @"<div class=""copy-block"">
                <div class=""copy-content"">
                    {0}
                </div>
                <button class=""copy-code"">📋</button>
            </div>";
            return string.Format(codeTemplate, content.ToString());
        }


        /// <summary>
        /// The alternate language mapping
        /// </summary>
        private static readonly Dictionary<string, string> AlternateLanguages = new()
        {
            {"md", "markdown" },
            {"jsx", "html" },
            {"tsx", "html" },
            {"bash", "powershell" },
            {"kotlin", "csharp" },
            {"rust", "cplusplus" }
        };
    }
}
