using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeParser
{

    /// <summary>
    /// 词法分析
    /// </summary>
    public class IRLAS
    {
        #region 属性
        private char[] expCode;
        private string strCode;
        private Link Head = null;
        private Link Tail = null;
        #endregion
        #region 方法
        public IRLAS(string scode)
        {
            Token<comm.TokenType> tmpHead = new Token<comm.TokenType>(comm.TokenType.Head, comm.TokenType.Head);
            Head = new Link();
            Head.CurrentValue = tmpHead;

            Token<comm.TokenType> tmpTail = new Token<comm.TokenType>(comm.TokenType.Tail, comm.TokenType.Tail);
            Tail = new Link();
            Tail.CurrentValue = tmpTail;

            Head.NextLink = Tail;
            Tail.PrevLink = Head;

            strCode = scode;
            expCode = scode.Trim().ToCharArray();
        }
        /// <summary>
        /// 将Exp转化为链表形式
        /// </summary>
        /// <returns>返回链表头</returns>
        public Link Analyze()
        {
            comm.DFAState state = comm.DFAState.Start;
            int startPos = 0;
            int endPos = 0;

            for (int i = 0; i < expCode.Length; i++)
            {
                if (state == comm.DFAState.StringStr)
                {
                    if (expCode[i] == '"' && expCode[i - 1] != '\\')
                    {
                        endPos = i;
                        AddToLink(startPos, endPos, state);
                        state = comm.DFAState.Start;
                        startPos = i + 1;
                    }
                    else if (i + 1 == expCode.Length)
                    {
                        throw new Exception("DeBug:缺少“");
                    }
                }
                else if (Char.IsDigit(expCode[i]))
                {
                    if (state == comm.DFAState.Start)
                    {
                        state = comm.DFAState.IntStr;
                    }
                    else if (state != comm.DFAState.IntStr && state != comm.DFAState.DoubleStr
                        && state != comm.DFAState.CharStr && state != comm.DFAState.StringStr)
                    {
                        endPos = i - 1;
                        AddToLink(startPos, endPos, state);
                        state = comm.DFAState.IntStr;
                        startPos = i;
                    }
                }
                else if (expCode[i] == '.')
                {
                    if (state == comm.DFAState.IntStr)
                    {
                        state = comm.DFAState.DoubleStr;
                    }
                    else
                    {
                        throw new Exception("DeBug:小数点使用错误");
                    }
                }
                else if (Char.IsLetter(expCode[i]) || expCode[i] == '[' || expCode[i] == ']' || expCode[i] == '_')
                {
                    if (state == comm.DFAState.Start)
                    {
                        state = comm.DFAState.CharStr;
                    }
                    else if (state != comm.DFAState.CharStr && state != comm.DFAState.StringStr)
                    {
                        endPos = i - 1;
                        AddToLink(startPos, endPos, state);
                        state = comm.DFAState.CharStr;
                        startPos = i;
                    }
                }
                else if (expCode[i] == '"')
                {
                    if (i == expCode.Length - 1)
                    {
                        throw new Exception("DeBug:缺少“");
                    }
                    else if (state != comm.DFAState.Start)
                    {
                        endPos = i - 1;
                        AddToLink(startPos, endPos, state);
                        startPos = i;
                    }
                    state = comm.DFAState.StringStr;
                }
                else if (Char.IsWhiteSpace(expCode[i]) || expCode[i] == '\r' || expCode[i] == '\n' || expCode[i] == '\t')
                {//空白跳过                    
                }
                else if (expCode[i] == '(' || expCode[i] == ')' || expCode[i] == '+' || expCode[i] == '-' || expCode[i] == '*' ||
                    expCode[i] == '/' || expCode[i] == '%' || expCode[i] == '>' || expCode[i] == '<' || expCode[i] == '=' ||
                    expCode[i] == '&' || expCode[i] == '|' || expCode[i] == '!' || expCode[i] == ',')
                {
                    if (state != comm.DFAState.Start)
                    {
                        endPos = i - 1;
                        AddToLink(startPos, endPos, state);
                        startPos = i;
                    }
                    state = comm.DFAState.OperatorStr;
                }
                else
                {
                    throw new Exception(string.Format("DeBug:未识别字符{0}", expCode[i].ToString()));
                }
                //结束分词
                if (i == expCode.Length - 1)
                {
                    AddToLink(startPos, i, state);
                }
            }
            return Head;
        }

        private void AddToLink(int startPos, int endPos, comm.DFAState dfaState)
        {
            Link tmpLink = new Link();
            //连接节点
            Tail.PrevLink.NextLink = tmpLink;
            tmpLink.PrevLink = Tail.PrevLink;
            tmpLink.NextLink = Tail;
            Tail.PrevLink = tmpLink;
            //连接节点
            if (endPos >= startPos)
            {
                switch (dfaState)
                {
                    case comm.DFAState.StringStr:
                        tmpLink.CurrentValue = new Token<ExpOperand<string>>(comm.TokenType.ExpOperand, new ExpOperand<string>(comm.FWCVariableType.VT_string, strCode.Substring(startPos, endPos - startPos + 1), string.Empty, 0));
                        break;
                    case comm.DFAState.IntStr:
                        tmpLink.CurrentValue = new Token<ExpOperand<int>>(comm.TokenType.ExpOperand, new ExpOperand<int>(comm.FWCVariableType.VT_int, int.Parse(strCode.Substring(startPos, endPos - startPos + 1)), string.Empty, 0));
                        break;
                    case comm.DFAState.DoubleStr:
                        tmpLink.CurrentValue = new Token<ExpOperand<double>>(comm.TokenType.ExpOperand, new ExpOperand<double>(comm.FWCVariableType.VT_double, double.Parse(strCode.Substring(startPos, endPos - startPos + 1)), string.Empty, 0));
                        break;
                    case comm.DFAState.CharStr:
                        tmpLink.CurrentValue = new Token<ExpOperand<string>>(comm.TokenType.ExpOperand, new ExpOperand<string>(comm.FWCVariableType.VT_string, strCode.Substring(startPos, endPos - startPos + 1), string.Empty, 0));
                        break;
                    case comm.DFAState.OperatorStr:
                        string tmpOperStr = strCode.Substring(startPos, endPos - startPos + 1);
                        comm.ExpOperatorType expOpType = comm.ExpOperatorType.And;
                        switch (tmpOperStr.Trim())
                        {
                            case "+":
                                expOpType = comm.ExpOperatorType.Plus;
                                break;
                            case "-":
                                expOpType = comm.ExpOperatorType.Subtract;
                                break;
                            case "*":
                                expOpType = comm.ExpOperatorType.Multiply;
                                break;
                            case "/":
                                expOpType = comm.ExpOperatorType.Divide;
                                break;
                            case "%":
                                expOpType = comm.ExpOperatorType.Mod;
                                break;
                            case "=":
                                expOpType = comm.ExpOperatorType.Equal;
                                break;
                            case ">":
                                expOpType = comm.ExpOperatorType.RThan;
                                break;
                            case "<":
                                expOpType = comm.ExpOperatorType.LThan;
                                break;
                            case "&":
                                expOpType = comm.ExpOperatorType.And;
                                break;
                            case "|":
                                expOpType = comm.ExpOperatorType.Or;
                                break;
                            case "!":
                                expOpType = comm.ExpOperatorType.Not;
                                break;
                            case "(":
                                expOpType = comm.ExpOperatorType.LBrace;
                                break;
                            case ")":
                                expOpType = comm.ExpOperatorType.RBrace;
                                break;
                            case ",":
                                expOpType = comm.ExpOperatorType.Coa;
                                break;
                            default:
                                throw new Exception("DeBug:" + tmpOperStr);
                                break;
                        }
                        tmpLink.CurrentValue = new Token<ExpOperator>(comm.TokenType.ExpOperator, comm.ExpOpTypeDry[expOpType]);
                        break;
                    default:
                        throw new Exception("DeBug:" + strCode.Substring(startPos, endPos - startPos + 1));
                        break;
                }
            }
        }
        #endregion
        public string LinkToString(Link hd)
        {
            Link tmp = hd.NextLink;
            StringBuilder sber = new StringBuilder();
            while (tmp != null && tmp.CurrentValue != null)
            {
                IToken temp = tmp.CurrentValue;
                sber.AppendLine(temp.ToString());
                tmp = tmp.NextLink;
            }
            return sber.ToString();
        }
    }
}
