using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkSharp
{
    class HTMLTag
    {
        public static readonly string[] BeginTag =
            {"", "<h1>", "<h2>", "<h3>", "<h4>", "<h5>", "<h6>", "<p>", "<strong>",
        "<em>", "<strike>", "<blockquote>", "<ol>", "<ul>", "<li>", "<code>", "<pre><code>",
        "<hr/>", "", ""};
        public static readonly string[] EndTag =
            {"", "</h1>", "</h2>", "</h3>", "</h4>", "</h5>", "</h6>", "</p>", "</strong>",
        "</em>", "</strike>", "</blockquote>", "</ol>", "</ul>", "</li>", "</code>", "</code></pre>",
        "<hr/>", "", ""};
    }
}
