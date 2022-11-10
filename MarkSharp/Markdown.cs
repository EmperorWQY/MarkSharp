using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkSharp
{
    public class Markdown
    {
        private readonly MarkdownNode root;
        private readonly string filename;
        private readonly StringBuilder sb;
        public Markdown(string filename)
        {
            this.filename = filename;
            sb = new StringBuilder();
            root = new MarkdownNode(MarkdownNodeType.NULL);
        }
        public void Transform()
        {
            using var reader = new StreamReader(filename);
            var line = "";
            var isCodeBlock = false;
            
            while ((line = reader.ReadLine()) is not null)
            {
                line = line.TrimStart(' ');

                if (!isCodeBlock && string.IsNullOrEmpty(line))
                    continue;

                if (!isCodeBlock && IsLine(line))
                {
                    root.Children.Add(new MarkdownNode(MarkdownNodeType.HorizontalRule));
                    continue;
                }

                var lineType = ParseType(line);

                if (lineType.Key == MarkdownNodeType.Code)
                {
                    if (!isCodeBlock)
                        root.Children.Add(new MarkdownNode(MarkdownNodeType.BlockCode));

                    isCodeBlock = !isCodeBlock;
                    continue;
                }
                if (isCodeBlock)
                {
                    root.Children[^1].Content += line;
                    root.Children[^1].Content += '\n';
                    continue;
                }

                if (lineType.Key == MarkdownNodeType.Paragraph)
                {
                    root.Children.Add(new MarkdownNode(MarkdownNodeType.Paragraph));
                    Insert(root.Children[^1], line[lineType.Value..]);
                    continue;
                }

                if (lineType.Key >= MarkdownNodeType.Heading1 && lineType.Key <= MarkdownNodeType.Heading6)
                {
                    root.Children.Add(new MarkdownNode(lineType.Key));
                    Insert(root.Children[^1], line[lineType.Value..]);
                    continue;
                }

                if (lineType.Key == MarkdownNodeType.UnorderedList)
                {
                    if (!root.Children.Any() || root.Children[^1].Type != MarkdownNodeType.UnorderedList)
                        root.Children.Add(new MarkdownNode(MarkdownNodeType.UnorderedList));

                    root.Children[^1].Children.Add(new MarkdownNode(MarkdownNodeType.ListItem));
                    Insert(root.Children[^1].Children[^1], line[lineType.Value..]);
                    continue;
                }

                if (lineType.Key == MarkdownNodeType.UnorderedList)
                {
                    if (!root.Children.Any() || root.Children[^1].Type != MarkdownNodeType.OrderedList)
                        root.Children.Add(new MarkdownNode(MarkdownNodeType.OrderedList));

                    root.Children[^1].Children.Add(new MarkdownNode(MarkdownNodeType.ListItem));
                    Insert(root.Children[^1].Children[^1], line[lineType.Value..]);
                    continue;
                }

                if (lineType.Key == MarkdownNodeType.Blockquote)
                {
                    root.Children.Add(new MarkdownNode(MarkdownNodeType.Blockquote));
                    Insert(root.Children[^1], line[lineType.Value..]);
                    continue;
                }
            }

            DFS(root);
        }
        private void DFS(MarkdownNode root)
        {
            sb.Append(HTMLTag.BeginTag[(int)root.Type]);

            if (root.Type == MarkdownNodeType.Link)
            {
                sb.Append("<a href=\"");
                sb.Append(root.Uri);
                sb.Append("\">");
                sb.Append(root.Content);
                sb.Append("</a>");
            }
            else if (root.Type == MarkdownNodeType.Image)
            {
                sb.Append("<img src=\"");
                sb.Append(root.Uri);
                sb.Append("\" alt=\"");
                sb.Append(root.Content);
                sb.Append("\"/>");
            }
            else
                sb.Append(root.Content);

            root.Children.ForEach((item) =>
            {
                DFS(item);
            });

            sb.Append(HTMLTag.EndTag[(int)root.Type]);
        }
        private static void Insert(MarkdownNode child, string content)
        {
            var inCode = false;
            var inStrong = false;
            var inItalic = false;
            var inDelLine = false;

            child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));
            int size = content.Length;

            for (var i = 0; i < size; i++)
            {
                if (content[i] == '`')
                {
                    if (inCode)
                        child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));
                    else
                        child.Children.Add(new MarkdownNode(MarkdownNodeType.Code));

                    inCode = !inCode;
                    continue;
                }

                if (content[i] == '*' && i + 1 < size && content[i + 1] == '*' && !inCode)
                {
                    if (inStrong)
                        child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));
                    else
                        child.Children.Add(new MarkdownNode(MarkdownNodeType.Strong));

                    inStrong = !inStrong;
                    i++;
                    continue;
                }

                if (content[i] == '_' && !inCode)
                {
                    if (inItalic)
                        child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));
                    else
                        child.Children.Add(new MarkdownNode(MarkdownNodeType.Italic));

                    inItalic = !inItalic;
                    continue;
                }

                if (content[i] == '!' && i + 1 < size && content[i + 1] == '[')
                {
                    child.Children.Add(new MarkdownNode(MarkdownNodeType.Image));
                    i += 2;
                    for (; i < size && content[i] != ']'; i++)
                        child.Children[^1].Content += content[i];

                    i += 2;
                    for (; i < size && content[i] != ')'; i++)
                        child.Children[^1].Uri += content[i];

                    child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));
                    continue;
                }

                if (content[i] == '!' && i + 1 < size && content[i + 1] == '[')
                {
                    child.Children.Add(new MarkdownNode(MarkdownNodeType.Link));
                    i++;
                    for (; i < size && content[i] != ']'; i++)
                        child.Children[^1].Content += content[i];

                    i += 2;
                    for (; i < size && content[i] != ')'; i++)
                        child.Children[^1].Uri += content[i];

                    child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));
                    continue;
                }

                if (content[i] == '~' && i + 1 < size && content[i + 1] == '~')
                {
                    if (inDelLine)
                        child.Children.Add(new MarkdownNode(MarkdownNodeType.DeleteLine));
                    else
                        child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));

                    inDelLine = !inDelLine;
                    i++;
                    continue;
                }

                child.Children[^1].Content += content[i];
            }
        }
        private static bool IsLine(string content)
        {
            return content == "---";
        }
        private static KeyValuePair<MarkdownNodeType, int> ParseType(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new KeyValuePair<MarkdownNodeType, int>();

            var pos = 0;
            var count = 0;
            
            while (content[pos] == '#' && pos < content.Length)
            {
                pos++;
                count++;
            }

            if (content[pos] == ' ' && count > 0 && count <= 6)
                return new KeyValuePair<MarkdownNodeType, int>(MarkdownNodeType.Heading1 + count - 1, pos + 1);

            pos = 0;

            if (content.StartsWith("```"))
                return new KeyValuePair<MarkdownNodeType, int>(MarkdownNodeType.Code, 3);

            if (content.StartsWith("- "))
                return new KeyValuePair<MarkdownNodeType, int>(MarkdownNodeType.UnorderedList, 2);

            if (content[pos] > '0' && content[pos] < '9')
            {
                while (content[pos] > '0' && content[pos] < '9')
                    pos++;
                if (content[pos] == '.' && pos < content.Length)
                {
                    pos++;
                    if (content[pos] == ' ' && pos < content.Length)
                        return new KeyValuePair<MarkdownNodeType, int>(MarkdownNodeType.OrderedList, pos + 1);
                }
            }

            pos = 0;

            if (content.StartsWith("> "))
                return new KeyValuePair<MarkdownNodeType, int>(MarkdownNodeType.Blockquote, pos + 2);

            return new KeyValuePair<MarkdownNodeType, int>(MarkdownNodeType.Paragraph, pos);
        }

        public string GetHtmlContent()
        {
            Transform();
            return sb.ToString();
        }
    }
}
