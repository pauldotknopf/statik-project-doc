using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using YamlDotNet.Serialization;

namespace StatikProject.Services.Impl
{
    public class MarkdownParser : IMarkdownParser
    {
        public MarkdownParseResult Parse(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return new MarkdownParseResult(null, null);
            }

            markdown = Regex.Replace(markdown, @"(\r\n)|(\n\r)|(\n\r)|(\r)", Environment.NewLine);
            
            var builder = new MarkdownPipelineBuilder();
            builder.Extensions.Add(new Markdig.Extensions.Yaml.YamlFrontMatterExtension());
            var pipeline = builder.Build();
            var document = Markdown.Parse(markdown, pipeline);
            var yamlBlocks = document.Descendants<Markdig.Extensions.Yaml.YamlFrontMatterBlock>()
                .ToList();
            
            if(yamlBlocks.Count == 0)
            {
                return new MarkdownParseResult(null, markdown);
            }

            if(yamlBlocks.Count > 1)
            {
                throw new InvalidOperationException();
            }

            var yamlBlock = yamlBlocks.First();

            var yamlBlockIterator = yamlBlock.Lines.ToCharIterator();
            var yamlString = new StringBuilder();
            while (yamlBlockIterator.CurrentChar != '\0')
            {
                yamlString.Append(yamlBlockIterator.CurrentChar);
                yamlBlockIterator.NextChar();
            }

            var yamlDeserializer = new DeserializerBuilder().Build();
            var yamlObject = yamlDeserializer.Deserialize(new StringReader(yamlString.ToString()));

            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();

            dynamic yaml = Newtonsoft.Json.JsonConvert.DeserializeObject(serializer.Serialize(yamlObject));

            markdown = markdown.Substring(yamlBlock.Span.End + 1);
            if(markdown.StartsWith(Environment.NewLine))
                markdown = markdown.Substring(Environment.NewLine.Length);

            return new MarkdownParseResult(yaml, markdown);
        }
    }
}