using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;

namespace BluePointLilac.Methods
{
    public static class RichTextBoxExtension
    {
        /// <summary>RichTextBox中ini语法高亮</summary>
        /// <param name="iniStr">要显示的ini文本</param>
        public static void LoadIni(this RichTextBox box, string iniStr)
        {
            var lines = iniStr.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (var i = 0; i < lines.Length; i++)
            {
                var str = lines[i].Trim();
                if (str.StartsWith(";") || str.StartsWith("#"))
                {
                    box.AppendText(str, Color.SkyBlue);//注释
                }
                else if (str.StartsWith("["))
                {
                    if (str.Contains("]"))
                    {
                        var index = str.IndexOf(']');
                        box.AppendText(str[..(index + 1)], Color.DarkCyan, null, true);//section
                        box.AppendText(str[(index + 1)..], Color.SkyBlue);//section标签之后的内容视作注释
                    }
                    else box.AppendText(str, Color.SkyBlue);//section标签未关闭视作注释
                }
                else if (str.Contains("="))
                {
                    var index = str.IndexOf('=');
                    box.AppendText(str[..index], Color.DodgerBlue);//key
                    box.AppendText(str[index..], Color.DimGray);//value
                }
                else box.AppendText(str, Color.SkyBlue);//非section行和非key行视作注释
                if (i != lines.Length - 1) box.AppendText("\r\n");
            }
        }

        /// 代码原文：https://archive.codeplex.com/?p=xmlrichtextbox
        /// 本人（蓝点lilac）仅作简单修改，将原继承类改写为扩展方法
        /// <summary>RichTextBox中xml语法高亮</summary>
        /// <param name="xmlStr">要显示的xml文本</param>
        /// <remarks>可直接用WebBrowser的Url加载本地xml文件，但无法自定义颜色</remarks>
        public static void LoadXml(this RichTextBox box, string xmlStr)
        {
            var machine = new XmlStateMachine();
            if (xmlStr.StartsWith("<?"))
            {
                var declaration = machine.GetXmlDeclaration(xmlStr);
                try
                {
                    xmlStr = XDocument.Parse(xmlStr, LoadOptions.PreserveWhitespace).ToString().Trim();
                    if (string.IsNullOrEmpty(xmlStr) && declaration == string.Empty) return;
                }
                catch { throw; }
                xmlStr = declaration + "\r\n" + xmlStr;
            }

            var location = 0;
            var failCount = 0;
            var tokenTryCount = 0;
            while (location < xmlStr.Length)
            {
                var token = machine.GetNextToken(xmlStr[location..], out var ttype);
                var color = machine.GetTokenColor(ttype);
                var isBold = ttype is XmlTokenType.DocTypeName or XmlTokenType.NodeName;
                box.AppendText(token, color, null, isBold);
                location += token.Length;
                tokenTryCount++;

                // Check for ongoing failure
                if (token.Length == 0) failCount++;
                if (failCount > 10 || tokenTryCount > xmlStr.Length)
                {
                    var theRestOfIt = xmlStr[location..];
                    //box.AppendText(Environment.NewLine + Environment.NewLine + theRestOfIt); // DEBUG
                    box.AppendText(theRestOfIt);
                    break;
                }
            }
        }

        public static void AppendText(this RichTextBox box, string text, Color color = default, Font font = null, bool isBold = false)
        {
            var fontStyle = isBold ? FontStyle.Bold : FontStyle.Regular;
            box.SelectionFont = new Font(font ?? box.Font, fontStyle);
            box.SelectionColor = color != default ? color : box.ForeColor;
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }

        private sealed class XmlStateMachine
        {
            public XmlTokenType CurrentState = XmlTokenType.Unknown;
            private string subString = string.Empty;
            private string token = string.Empty;

            public string GetNextToken(string s, out XmlTokenType ttype)
            {
                ttype = XmlTokenType.Unknown;
                // skip past any whitespace (token added to it at the end of method)
                var whitespace = GetWhitespace(s);
                subString = s.TrimStart();
                token = string.Empty;
                if (CurrentState == XmlTokenType.CDataStart)
                {
                    // check for empty CDATA
                    if (subString.StartsWith("]]>"))
                    {
                        CurrentState = XmlTokenType.CDataEnd;
                        token = "]]>";
                    }
                    else
                    {
                        CurrentState = XmlTokenType.CDataValue;
                        var n = subString.IndexOf("]]>");
                        token = subString[..n];
                    }
                }
                else if (CurrentState == XmlTokenType.DocTypeStart)
                {
                    CurrentState = XmlTokenType.DocTypeName;
                    token = "DOCTYPE";
                }
                else if (CurrentState == XmlTokenType.DocTypeName)
                {
                    CurrentState = XmlTokenType.DocTypeDeclaration;
                    var n = subString.IndexOf("[");
                    token = subString[..n];
                }
                else if (CurrentState == XmlTokenType.DocTypeDeclaration)
                {
                    CurrentState = XmlTokenType.DocTypeDefStart;
                    token = "[";
                }
                else if (CurrentState == XmlTokenType.DocTypeDefStart)
                {
                    if (subString.StartsWith("]>"))
                    {
                        CurrentState = XmlTokenType.DocTypeDefEnd;
                        token = "]>";
                    }
                    else
                    {
                        CurrentState = XmlTokenType.DocTypeDefValue;
                        var n = subString.IndexOf("]>");
                        token = subString[..n];
                    }
                }
                else if (CurrentState == XmlTokenType.DocTypeDefValue)
                {
                    CurrentState = XmlTokenType.DocTypeDefEnd;
                    token = "]>";
                }
                else if (CurrentState == XmlTokenType.DoubleQuotationMarkStart)
                {
                    // check for empty attribute value
                    if (subString[0] == '\"')
                    {
                        CurrentState = XmlTokenType.DoubleQuotationMarkEnd;
                        token = "\"";
                    }
                    else
                    {
                        CurrentState = XmlTokenType.AttributeValue;
                        var n = subString.IndexOf("\"");
                        token = subString[..n];
                    }
                }
                else if (CurrentState == XmlTokenType.SingleQuotationMarkStart)
                {
                    // check for empty attribute value
                    if (subString[0] == '\'')
                    {
                        CurrentState = XmlTokenType.SingleQuotationMarkEnd;
                        token = "\'";
                    }
                    else
                    {
                        CurrentState = XmlTokenType.AttributeValue;
                        var n = subString.IndexOf("'");
                        token = subString[..n];
                    }
                }
                else if (CurrentState == XmlTokenType.CommentStart)
                {
                    // check for empty comment
                    if (subString.StartsWith("-->"))
                    {
                        CurrentState = XmlTokenType.CommentEnd;
                        token = "-->";
                    }
                    else
                    {
                        CurrentState = XmlTokenType.CommentValue;
                        token = ReadCommentValue(subString);
                    }
                }
                else if (CurrentState == XmlTokenType.NodeStart)
                {
                    CurrentState = XmlTokenType.NodeName;
                    token = ReadNodeName(subString);
                }
                else if (CurrentState == XmlTokenType.XmlDeclarationStart)
                {
                    CurrentState = XmlTokenType.NodeName;
                    token = ReadNodeName(subString);
                }
                else if (CurrentState == XmlTokenType.NodeName)
                {
                    if (subString[0] is not '/' and
                        not '>')
                    {
                        CurrentState = XmlTokenType.AttributeName;
                        token = ReadAttributeName(subString);
                    }
                    else
                    {
                        HandleReservedXmlToken();
                    }
                }
                else if (CurrentState == XmlTokenType.NodeEndValueStart)
                {
                    if (subString[0] == '<')
                    {
                        HandleReservedXmlToken();
                    }
                    else
                    {
                        CurrentState = XmlTokenType.NodeValue;
                        token = ReadNodeValue(subString);
                    }
                }
                else if (CurrentState == XmlTokenType.DoubleQuotationMarkEnd)
                {
                    HandleAttributeEnd();
                }
                else if (CurrentState == XmlTokenType.SingleQuotationMarkEnd)
                {
                    HandleAttributeEnd();
                }
                else
                {
                    HandleReservedXmlToken();
                }
                if (token != string.Empty)
                {
                    ttype = CurrentState;
                    return whitespace + token;
                }
                return string.Empty;
            }

            public Color GetTokenColor(XmlTokenType ttype)
            {
                return ttype switch
                {
                    XmlTokenType.NodeValue or XmlTokenType.EqualSignStart or XmlTokenType.EqualSignEnd or XmlTokenType.DoubleQuotationMarkStart or XmlTokenType.DoubleQuotationMarkEnd or XmlTokenType.SingleQuotationMarkStart or XmlTokenType.SingleQuotationMarkEnd => Color.DimGray,
                    XmlTokenType.XmlDeclarationStart or XmlTokenType.XmlDeclarationEnd or XmlTokenType.NodeStart or XmlTokenType.NodeEnd or XmlTokenType.NodeEndValueStart or XmlTokenType.CDataStart or XmlTokenType.CDataEnd or XmlTokenType.CommentStart or XmlTokenType.CommentEnd or XmlTokenType.AttributeValue or XmlTokenType.DocTypeStart or XmlTokenType.DocTypeEnd or XmlTokenType.DocTypeDefStart or XmlTokenType.DocTypeDefEnd => Color.DimGray,
                    XmlTokenType.CDataValue or XmlTokenType.DocTypeDefValue => Color.SkyBlue,
                    XmlTokenType.CommentValue => Color.SkyBlue,
                    XmlTokenType.DocTypeName or XmlTokenType.NodeName => Color.DarkCyan,
                    XmlTokenType.AttributeName or XmlTokenType.DocTypeDeclaration => Color.DodgerBlue,
                    _ => Color.DimGray,
                };
            }

            public string GetXmlDeclaration(string s)
            {
                var start = s.IndexOf("<?");
                var end = s.IndexOf("?>");
                if (start > -1 && end > start)
                {
                    return s.Substring(start, end - start + 2);
                }
                return string.Empty;
            }

            private void HandleAttributeEnd()
            {
                if (subString.StartsWith(">"))
                {
                    HandleReservedXmlToken();
                }
                else if (subString.StartsWith("/>"))
                {
                    HandleReservedXmlToken();
                }
                else if (subString.StartsWith("?>"))
                {
                    HandleReservedXmlToken();
                }
                else
                {
                    CurrentState = XmlTokenType.AttributeName;
                    token = ReadAttributeName(subString);
                }
            }

            private void HandleReservedXmlToken()
            {
                // check if state changer
                // <, >, =, </, />, <![CDATA[, <!--, -->
                if (subString.StartsWith("<![CDATA["))
                {
                    CurrentState = XmlTokenType.CDataStart;
                    token = "<![CDATA[";
                }
                else if (subString.StartsWith("<!DOCTYPE"))
                {
                    CurrentState = XmlTokenType.DocTypeStart;
                    token = "<!";
                }
                else if (subString.StartsWith("</"))
                {
                    CurrentState = XmlTokenType.NodeStart;
                    token = "</";
                }
                else if (subString.StartsWith("<!--"))
                {
                    CurrentState = XmlTokenType.CommentStart;
                    token = "<!--";
                }
                else if (subString.StartsWith("<?"))
                {
                    CurrentState = XmlTokenType.XmlDeclarationStart;
                    token = "<?";
                }
                else if (subString.StartsWith("<"))
                {
                    CurrentState = XmlTokenType.NodeStart;
                    token = "<";
                }
                else if (subString.StartsWith("="))
                {
                    CurrentState = XmlTokenType.EqualSignStart;
                    token = "=";
                }
                else if (subString.StartsWith("?>"))
                {
                    CurrentState = XmlTokenType.XmlDeclarationEnd;
                    token = "?>";
                }
                else if (subString.StartsWith(">"))
                {
                    CurrentState = XmlTokenType.NodeEndValueStart;
                    token = ">";
                }
                else if (subString.StartsWith("-->"))
                {
                    CurrentState = XmlTokenType.CommentEnd;
                    token = "-->";
                }
                else if (subString.StartsWith("]>"))
                {
                    CurrentState = XmlTokenType.DocTypeEnd;
                    token = "]>";
                }
                else if (subString.StartsWith("]]>"))
                {
                    CurrentState = XmlTokenType.CDataEnd;
                    token = "]]>";
                }
                else if (subString.StartsWith("/>"))
                {
                    CurrentState = XmlTokenType.NodeEnd;
                    token = "/>";
                }
                else if (subString.StartsWith("\""))
                {
                    if (CurrentState == XmlTokenType.AttributeValue)
                    {
                        CurrentState = XmlTokenType.DoubleQuotationMarkEnd;
                    }
                    else
                    {
                        CurrentState = XmlTokenType.DoubleQuotationMarkStart;
                    }
                    token = "\"";
                }
                else if (subString.StartsWith("'"))
                {
                    if (CurrentState == XmlTokenType.AttributeValue)
                    {
                        CurrentState = XmlTokenType.SingleQuotationMarkEnd;
                    }
                    else
                    {
                        CurrentState = XmlTokenType.SingleQuotationMarkStart;
                    }
                    token = "'";
                }
            }

            private string ReadNodeName(string s)
            {
                var nodeName = "";
                for (var i = 0; i < s.Length; i++)
                {
                    if (s[i] is '/' or ' ' or '>') return nodeName;
                    else nodeName += s[i].ToString();
                }
                return nodeName;
            }

            private string ReadAttributeName(string s)
            {
                var attName = "";
                var n = s.IndexOf('=');
                if (n != -1) attName = s[..n];
                return attName;
            }

            private string ReadNodeValue(string s)
            {
                var nodeValue = "";
                var n = s.IndexOf('<');
                if (n != -1) nodeValue = s[..n];
                return nodeValue;
            }

            private string ReadCommentValue(string s)
            {
                var commentValue = "";
                var n = s.IndexOf("-->");
                if (n != -1) commentValue = s[..n];
                return commentValue;
            }

            private string GetWhitespace(string s)
            {
                var whitespace = "";
                for (var i = 0; i < s.Length; i++)
                {
                    var c = s[i];
                    if (char.IsWhiteSpace(c)) whitespace += c;
                    else break;
                }
                return whitespace;
            }
        }

        private enum XmlTokenType
        {
            Whitespace, XmlDeclarationStart, XmlDeclarationEnd, NodeStart, NodeEnd, NodeEndValueStart, NodeName,
            NodeValue, AttributeName, AttributeValue, EqualSignStart, EqualSignEnd, CommentStart, CommentValue,
            CommentEnd, CDataStart, CDataValue, CDataEnd, DoubleQuotationMarkStart, DoubleQuotationMarkEnd,
            SingleQuotationMarkStart, SingleQuotationMarkEnd, DocTypeStart, DocTypeName, DocTypeDeclaration,
            DocTypeDefStart, DocTypeDefValue, DocTypeDefEnd, DocTypeEnd, DocumentEnd, Unknown
        }
    }
}