
using System.Text;

namespace MarkSharp;

/// <summary>
/// Class that parse the md content
/// </summary>
public class Markdown
{
    /// <summary>
    /// Root node of the syntax tree
    /// </summary>
    private readonly MarkdownNode root;
    /// <summary>
    /// Filename of the Markdown file
    /// </summary>
    private readonly string filename;
    /// <summary>
    /// StringBuilder for building the html content string
    /// </summary>
    private readonly StringBuilder sb;
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="filename">Filename of the Markdown file</param>
    public Markdown(string filename)
    {
        this.filename = filename;
        sb = new StringBuilder();
        root = new MarkdownNode(MarkdownNodeType.NULL);
    }
    /// <summary>
    /// Read and process the md content by line
    /// </summary>
    public void Transform()
    {
        using var reader = new StreamReader(filename);
        var line = "";
        var isCodeBlock = false;                                // Determine if this line is in the code block

        while ((line = reader.ReadLine()) is not null)          // Read by line
        {
            line = line.TrimStart(' ');                         // Trim the blank

            if (!isCodeBlock && string.IsNullOrEmpty(line))     // if line is empty or code, skip
                continue;

            if (!isCodeBlock && IsLine(line))                   // if line is not code and equals to <hr/>, create a hr node
            {
                root.Children.Add(new MarkdownNode(MarkdownNodeType.HorizontalRule));
                continue;
            }

            var lineType = ParseType(line);                     // Determine the type of the line

            if (lineType.Key == MarkdownNodeType.BlockCode)     // Code Block start
            {
                if (!isCodeBlock)
                    root.Children.Add(new MarkdownNode(MarkdownNodeType.BlockCode));

                isCodeBlock = !isCodeBlock;
                continue;
            }
            if (isCodeBlock)                                    // if the line still in the code block, append to the codeblock node content
            {
                root.Children[^1].Content += line;
                root.Children[^1].Content += '\n';
                continue;
            }

            if (lineType.Key == MarkdownNodeType.Paragraph)     // Paragrah
            {
                root.Children.Add(new MarkdownNode(MarkdownNodeType.Paragraph));
                Insert(root.Children[^1], line[lineType.Value..]);
                continue;
            }

            if (lineType.Key >= MarkdownNodeType.Heading1 && lineType.Key <= MarkdownNodeType.Heading6) // H1 to H6
            {
                root.Children.Add(new MarkdownNode(lineType.Key));
                Insert(root.Children[^1], line[lineType.Value..]);
                continue;
            }

            if (lineType.Key == MarkdownNodeType.UnorderedList) // ul
            {
                if (!root.Children.Any() || root.Children[^1].Type != MarkdownNodeType.UnorderedList) // if the last item in the syntax tree is not ul, add a new ul node, otherwise, not.
                    root.Children.Add(new MarkdownNode(MarkdownNodeType.UnorderedList));

                root.Children[^1].Children.Add(new MarkdownNode(MarkdownNodeType.ListItem));
                Insert(root.Children[^1].Children[^1], line[lineType.Value..]);
                continue;
            }

            if (lineType.Key == MarkdownNodeType.OrderedList) // ol
            {
                if (!root.Children.Any() || root.Children[^1].Type != MarkdownNodeType.OrderedList) // same as above
                    root.Children.Add(new MarkdownNode(MarkdownNodeType.OrderedList));

                root.Children[^1].Children.Add(new MarkdownNode(MarkdownNodeType.ListItem));
                Insert(root.Children[^1].Children[^1], line[lineType.Value..]);
                continue;
            }

            if (lineType.Key == MarkdownNodeType.Blockquote)    // Block quote
            {
                root.Children.Add(new MarkdownNode(MarkdownNodeType.Blockquote));
                Insert(root.Children[^1], line[lineType.Value..]);
                continue;
            }
        }

        DFS(root);                                              // tranvers the syntax tree and parse each node to html tag
    }
    /// <summary>
    /// tranvers the syntax tree and parse each node to html tag
    /// </summary>
    /// <param name="root">Root or sub root of the syntax tree</param>
    private void DFS(MarkdownNode root)
    {
        sb.Append(HTMLTag.BeginTag[(int)root.Type]);

        // link and img need a spacial process
        if (root.Type == MarkdownNodeType.Link)                 
            sb.Append($"<a href=\"{root.Uri}\">{root.Content}</a>");

        else if (root.Type == MarkdownNodeType.Image)
            sb.Append($"<img src=\"{root.Uri}\" alt=\"{root.Content}\"/>");

        else
            sb.Append(root.Content);

        // deep first
        root.Children.ForEach((item) => DFS(item));

        sb.Append(HTMLTag.EndTag[(int)root.Type]);
    }
    /// <summary>
    /// process the inline node
    /// </summary>
    /// <param name="child">node need to be processed</param>
    /// <param name="content">content that could has inline node</param>
    private static void Insert(MarkdownNode child, string content)
    {
        var inCode = false;
        var inStrong = false;
        var inItalic = false;
        var inDelLine = false;

        child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));
        int size = content.Length;

        for (var i = 0; i < size; i++)                          // process by char
        {
            if (content[i] == '`')                              // ``
            {
                if (inCode)
                    child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));
                else
                    child.Children.Add(new MarkdownNode(MarkdownNodeType.Code));

                inCode = !inCode;
                continue;
            }

            if (content[i] == '*' && i + 1 < size && content[i + 1] == '*' && !inCode) // **strong**
            {
                if (inStrong)
                    child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));
                else
                    child.Children.Add(new MarkdownNode(MarkdownNodeType.Strong));

                inStrong = !inStrong;
                i++;
                continue;
            }

            if (content[i] == '_' && !inCode)                                          // _Italic_
            {
                if (inItalic)
                    child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));
                else
                    child.Children.Add(new MarkdownNode(MarkdownNodeType.Italic));

                inItalic = !inItalic;
                continue;
            }

            if (content[i] == '!' && i + 1 < size && content[i + 1] == '[')             // img ![content](uri)
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

            if (content[i] == '[' && !inCode)             // link
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

            if (content[i] == '~' && i + 1 < size && content[i + 1] == '~') // delete line
            {
                if (inDelLine)
                    child.Children.Add(new MarkdownNode(MarkdownNodeType.DeleteLine));
                else
                    child.Children.Add(new MarkdownNode(MarkdownNodeType.NULL));

                inDelLine = !inDelLine;
                i++;
                continue;
            }

            child.Children[^1].Content += content[i];   // add content
        }
    }
    /// <summary>
    /// Determin line
    /// </summary>
    /// <param name="content">content</param>
    /// <returns>true if is line , otherwise false</returns>
    private static bool IsLine(string content)
    {
        return content == "---";
    }
    /// <summary>
    /// return the true start position of the content
    /// </summary>
    /// <param name="content"></param>
    /// <returns>Type and position</returns>
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
    /// <summary>
    /// Return html content
    /// </summary>
    /// <returns>Html content</returns>
    public string GetHtmlContent()
    {
        Transform();
        return sb.ToString();
    }
    /// <summary>
    /// Get syntax tree
    /// </summary>
    /// <returns>Root of the syntax tree</returns>
    public MarkdownNode GetSyntaxTree()
    {
        return root;
    }
}
