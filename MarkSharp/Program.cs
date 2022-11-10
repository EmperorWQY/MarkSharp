// See https://aka.ms/new-console-template for more information

using MarkSharp;

var markdown = new Markdown(@"C:\Users\Emper\Desktop\2021\2021 883 C语言程序设计编程题.md");
var html = markdown.GetHtmlContent();

using var writer = new StreamWriter(@"C:\Users\Emper\Desktop\output.html");
writer.Write(html);
