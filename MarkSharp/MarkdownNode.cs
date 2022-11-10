using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkSharp
{
    public class MarkdownNode
    {
        public MarkdownNodeType Type { get; set; }
        public List<MarkdownNode> Children { get; set; }
        public string Content { get; set; }
        public string Uri { get; set; }
        public MarkdownNode(MarkdownNodeType type)
        {
            Type = type;
            Children = new List<MarkdownNode>();
            Content = "";
            Uri = "";
        }
    }
}
