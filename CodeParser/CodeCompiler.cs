using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

namespace CodeParser
{
    /// <summary>
    /// 语法分析语义分析
    /// </summary>
    public class CodeCompiler
    {
        #region 私有成员
        private Dictionary<string, PublicValueList> DicPV;//全局变量表
        private LocalStruct ls = new LocalStruct();//局部变量表
        private LocalStruct cs = new LocalStruct();//参数表
        private FunctionList Func;//当前函数
        private int Index;
        #endregion 
        #region 公共方法
        public CodeCompiler(Dictionary<string, PublicValueList> dic, FunctionList func,int index)
        {
            Index = index;
            Func = func;
            DicPV = dic;
            //格式化参数表
            if (Func.funcCMD.Length > 0)
            {
                string[] tmpArr = Func.funcCMD.Split(',');
                foreach (string tmpcmd in tmpArr)
                {
                    string tmp_cType = tmpcmd.Trim().Split(' ')[0];
                    string tmp_cName = tmpcmd.Trim().Split(' ')[1];
                    cs.AddLocalsVariable(comm.CVariableType[tmp_cType], tmp_cName);
                }
            }
        }
        public LocalStruct Ls
        {
            get { return ls; }
        }//封装字段
       
        public string CompileILFunction()
        {
            StringBuilder sber = new StringBuilder();
            sber.AppendLine(string.Format(comm.otherFunctionHead, comm.DVariableType[Func.funcReturn], Func.funcName, cs.GetToString()));
            sber.AppendLine("{");
            sber.AppendLine(string.Format(comm.FunctionMaxStack, 8));
            //临时存放
            string tmpsav = CompileProgram(0, Func.funcCode.Length);
            //局部变量
            sber.AppendLine(ls.ToString());
            //代码块
            sber.AppendLine(tmpsav);
            //处理返回值
            sber.AppendLine("IL_ffff: ret");
            sber.AppendLine("}");
            return sber.ToString();
        }

        public string CompileProgram(int start, int length)
        {
            int CurrentPoint = start;
            int CodeLen = length;
            StringBuilder sber = new StringBuilder();
            string tmpFToken = string.Empty;
            int end = 0;

            while (CurrentPoint < CodeLen - 1)
            {
                tmpFToken = GetFirstToken(CurrentPoint,CodeLen,ref end);
                if (end > 0)
                {
                    sber.AppendLine(SingleLineCode(tmpFToken,Func.funcCode.Substring(CurrentPoint, end - CurrentPoint + 1).Trim()));
                }
                else if (end == -1)
                {
                    if (tmpFToken.ToUpper() == comm.ChkKeyWord.IF.ToString())
                    {
                        sber.AppendLine(IFCodes(CurrentPoint, CodeLen, ref end));
                    }
                    else if (tmpFToken.ToUpper() == comm.ChkKeyWord.FOR.ToString())
                    {
                        sber.AppendLine(FORCodes(CurrentPoint, CodeLen, ref end));
                    }
                }
                else
                {
                    break;
                }
                if (tmpFToken.ToUpper() == comm.FuncKeyWord.ret.ToString().ToUpper() 
                    || end < CurrentPoint)
                {
                    break;
                }
                CurrentPoint = end + 1;
            }

            return sber.ToString();
        }        

        private string SingleLineCode(string token, string sleLine)
        {
            EvalStruct es = new EvalStruct();
            StringBuilder sber = new StringBuilder();
            if (commFunc.RegexIsMatch(token,comm.Reg_VT))//局部变量定义与赋值表达式
            {
                MatchCollection mcn = commFunc.RegexMatches(sleLine, comm.Reg_LocalVT);
                if (mcn.Count == 0)
                {
                    throw new Exception(string.Format("Debug:{0}，格式错误。",sleLine));
                }
                Match mTmp = mcn[0];
                //添加至局部变量表
                string tmp_lv_name = mTmp.Groups["lvName"].Value.ToString();
                string tmp_lv_type = mTmp.Groups["lvVT"].Value.ToString();
                string tmp_lv_arrNum = mTmp.Groups["lvArrNum"].Value.ToString();
                if (ls.IsExistLV(tmp_lv_name) > -1)
                {
                    throw new Exception(string.Format("Debug:存在同名局部变量{0}",tmp_lv_name));
                }
                if (tmp_lv_arrNum.Length > 0)
                {
                    tmp_lv_type = tmp_lv_type + "[]";                    
                }                
                ls.AddLocalsVariable(comm.CVariableType[tmp_lv_type], tmp_lv_name);
                //处理数组与赋值
                if (tmp_lv_arrNum.Length > 0)
                {
                    es.PushStackValue(comm.FWCVariableType.VT_int, tmp_lv_arrNum);
                    es.CreateNewArr(comm.FWCVariableType.VT_ints);
                    es.EvalStackLocalValue(ls.IsExistLV(tmp_lv_name), tmp_lv_name);
                    sber.Append(es.GetToString(ref Index, true));
                    #region 识别并赋值
                    string arrValue = mTmp.Groups["lvValue"].Value.ToString();
                    Stack<char> arrValueStack = new Stack<char>();
                    int flagPos = 1,arrNum = 0;
                    for (int curPos = 1; curPos < arrValue.Length-1;curPos++)
                    {
                        if (arrValue[curPos] == '(')
                        {
                            arrValueStack.Push(arrValue[curPos]);
                        }
                        else if (arrValue[curPos] == ')')
                        {
                            if (arrValueStack.Count > 0)
                            {
                                arrValueStack.Pop();
                            }
                            else
                            {
                                throw new Exception(string.Format("Debug:{0}结构错误",arrValue));
                            }
                        }
                        else if (arrValue[curPos] == ',')
                        {
                            sber.Append(SingleLineCode(string.Format("{0}[{1}]", tmp_lv_name, arrNum), string.Format("{0}[{1}] = {2};", tmp_lv_name, arrNum, arrValue.Substring(flagPos, curPos - flagPos))));
                            flagPos = curPos + 1;
                            arrNum++;
                        }
                        if (curPos == arrValue.Length - 2 && flagPos <= curPos)
                        {
                            sber.Append(SingleLineCode(string.Format("{0}[{1}]", tmp_lv_name, arrNum), string.Format("{0}[{1}] = {2};", tmp_lv_name, arrNum, arrValue.Substring(flagPos, curPos - flagPos + 1))));     
                        }
                    }
                    #endregion
                }
                //处理赋值或表达式
                else
                {
                    if (mTmp.Groups["lvValue"].Value.ToString().Length > 0)
                    {
                        comm.FWCVariableType rType = comm.FWCVariableType.VT_void;
                        sber.Append(ComputeArcExp(mTmp.Groups["lvValue"].Value.ToString(), ref rType));
                        es.EvalStackLocalValue(ls.IsExistLV(tmp_lv_name), tmp_lv_name);
                    }
                }
                //返回结果
                //return mTmp.Groups["lvValue"].Value.ToString();
                sber.Append(es.GetToString(ref Index,true));
            }
            else if (comm.CallFunctionDic1.ContainsKey(token.ToLower()) || CodeParser.Cp_FunctionList.ContainsKey(token.ToLower()))
            {
                comm.FWCVariableType rType = comm.FWCVariableType.VT_void;
                string tmp_sleLine = sleLine.EndsWith(";") ? sleLine.Substring(0, sleLine.Length - 1) : sleLine;
                sber.Append(ComputeArcExp(tmp_sleLine, ref rType));
            }
            else//最后一种情况：赋值
            {
                #region 赋值
                MatchCollection mc = commFunc.RegexMatches(sleLine, comm.Reg_EqCode);
                if (mc.Count == 1)
                {
                    string tmp_v_name = mc[0].Groups["vName"].Value.ToString();
                    string tmp_v_arrNum = mc[0].Groups["ArrNum"].Value.ToString();
                    string tmp_v_value = mc[0].Groups["vValue"].Value.ToString();

                    comm.FWCVariableType rType = comm.FWCVariableType.VT_void;                    
                    int clpFlag = -1;
                    comm.FWCVariableType typeFlag = ReturnTypeByName(tmp_v_name, ref clpFlag);
                    if (clpFlag == 1)
                    {
                        if (tmp_v_arrNum == string.Empty)
                        {
                            sber.Append(ComputeArcExp(tmp_v_value, ref rType));
                            es.EvalStackLocalValue(ls.IsExistLV(tmp_v_name), tmp_v_name);
                            sber.Append(es.GetToString(ref Index, true));                            
                        }
                        else
                        {
                            es.PushStackLocalVTValue(ls.IsExistLV(tmp_v_name), tmp_v_name);
                            sber.Append(es.GetToString(ref Index, true));
                            comm.FWCVariableType tmpDG = comm.FWCVariableType.VT_int;
                            sber.Append(ComputeArcExp(tmp_v_arrNum, ref tmpDG));
                            sber.Append(ComputeArcExp(tmp_v_value, ref rType));
                            es.EvalIndexArr(typeFlag);
                            sber.Append(es.GetToString(ref Index, true));
                        }
                    }
                    else if (clpFlag == 2)
                    {
                        if (tmp_v_arrNum == string.Empty)
                        {
                            sber.Append(ComputeArcExp(tmp_v_value, ref rType));
                            es.EvalStackPublicValue(typeFlag,tmp_v_name);
                            sber.Append(es.GetToString(ref Index, true));
                        }
                        else
                        {
                            es.PushStackPublicVTValue(typeFlag, tmp_v_name);
                            sber.Append(es.GetToString(ref Index, true));
                            comm.FWCVariableType tmpDG = comm.FWCVariableType.VT_int;
                            sber.Append(ComputeArcExp(tmp_v_arrNum, ref tmpDG));
                            sber.Append(ComputeArcExp(tmp_v_value, ref rType));
                            es.EvalIndexArr(typeFlag);
                            sber.Append(es.GetToString(ref Index, true));
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("Debug:错误的使用变量{0}",tmp_v_name));
                    }
                }
                else
                {
                    throw new Exception(string.Format("Debug:{0}未识别的语句",sleLine));
                }
                #endregion
            }
            return sber.ToString();
        }

        private string IFCodes(int CurrentPoint,int CodeLen, ref int end)
        {
            StringBuilder sber = new StringBuilder();
            end = CodeLen;
            int cp = CurrentPoint;
            Stack<int> myStack = new Stack<int>();
            List<int> myRecordPoint = new List<int>();//记录位置
            List<string> myRecodeMSILCode = new List<string>();//临时存放
            for (; cp < CodeLen; cp++)
            {
                #region 空白跳过
                if (Char.IsWhiteSpace(Func.funcCode[cp]) || Func.funcCode[cp] == '\r' || Func.funcCode[cp] == '\n' || Func.funcCode[cp] == '\t')
                {
                    continue;
                }
                #endregion
                #region IF(){}ELSEIF(){}
                if (GetFirstToken(cp,CodeLen,ref end).ToUpper() == comm.ChkKeyWord.IF.ToString() 
                    || GetFirstToken(cp,CodeLen,ref end).ToUpper() == comm.ChkKeyWord.ELSEIF.ToString())
                {
                    for (; cp < CodeLen; cp++)
                    {
                        if (Func.funcCode[cp] == '(')
                        {
                            myStack.Push(cp);
                        }
                        else if (Func.funcCode[cp] == ')')
                        {
                            if (myStack.Count > 1)
                            {
                                myStack.Pop();
                            }
                            else if (myStack.Count == 1)
                            {
                                //处理IF条件
                                int tmp = myStack.Pop();
                                comm.FWCVariableType tmprt = comm.FWCVariableType.VT_void;
                                string tmp_sav = Func.funcCode.Substring(tmp + 1, cp - tmp - 1);
                                myRecodeMSILCode.Add(ComputeArcExp(tmp_sav, ref tmprt));
                                //记录点
                                myRecordPoint.Add(Index++);
                                break;
                            }
                        }
                        else if (cp == CodeLen-1)
                        {
                            throw new Exception("Debug:条件错误！");
                        }
                    }
                    for (; cp < CodeLen; cp++)
                    {
                        if (Func.funcCode[cp] == '{')
                        {
                            myStack.Push(cp);
                        }
                        else if (Func.funcCode[cp] == '}')
                        {
                            if (myStack.Count > 1)
                            {
                                myStack.Pop();
                            }
                            else if (myStack.Count == 1)
                            {
                                //处理语句块
                                int s = myStack.Pop() +1;
                                int e = cp - 1 ;
                                myRecodeMSILCode.Add(CompileProgram(s, e));
                                //记录点
                                myRecordPoint.Add(Index++);
                                myRecordPoint.Add(Index++);
                                break;
                            }
                        }
                        else if (cp == CodeLen - 1)
                        {
                            throw new Exception("Debug:语句块错误！");
                        }
                    }
                }
                #endregion
                #region ELSE{}
                else if (GetFirstToken(cp, CodeLen, ref end).ToUpper() == comm.ChkKeyWord.ELSE.ToString())
                {
                    for (; cp < CodeLen; cp++)
                    {
                        if (Func.funcCode[cp] == '{')
                        {
                            myStack.Push(cp);
                        }
                        else if (Func.funcCode[cp] == '}')
                        {
                            if (myStack.Count > 1)
                            {
                                myStack.Pop();
                            }
                            else if (myStack.Count == 1)
                            {
                                //处理IF条件
                                int s = myStack.Pop() + 1;
                                int e = cp - 1;
                                myRecodeMSILCode.Add(CompileProgram(s, e));
                                //记录点
                                myRecordPoint.Add(Index++);
                                break;
                            }
                        }
                        else if (cp == CodeLen - 1)
                        {
                            throw new Exception("Debug:语句块错误！");
                        }
                    }
                    end = cp;
                    break;
                }
                #endregion
                else
                {
                    end = cp - 1 ;
                    break;
                }
                if (cp == CodeLen - 1)
                {
                    end = CodeLen - 1;
                }
            }
            #region 组织代码
            int ifstrLen = myRecodeMSILCode.Count;
            BrCmdStruct brs = new BrCmdStruct();
            FuncStruct funcs = new FuncStruct();
            int tmpIndex = 0;
            for (int i = 0; i < ifstrLen / 2; i++)
            {
                sber.Append(myRecodeMSILCode[2 * i]);//条件i
                tmpIndex = myRecordPoint[3 * i + 2];//brfalse.s至nop i
                brs.AddJmpOperator(comm.MSILCMD.CMD_brfalse_s, tmpIndex);
                tmpIndex = myRecordPoint[3 * i];
                sber.Append(brs.GetToString(ref tmpIndex, true));
                sber.Append(myRecodeMSILCode[2 * i + 1]);//语句块i
                tmpIndex = myRecordPoint[myRecordPoint.Count-1];//br.s至nop最后
                brs.AddJmpOperator(comm.MSILCMD.CMD_br_s, tmpIndex);
                tmpIndex = myRecordPoint[3 * i+1];
                sber.Append(brs.GetToString(ref tmpIndex, true));
                tmpIndex = myRecordPoint[3 * i + 2];//nop i
                funcs.PrintSleMSILCMD("nop");
                sber.Append(funcs.GetToString(ref tmpIndex, true));
                if (ifstrLen%2==1&&i == ifstrLen / 2 - 1)//处理else部分
                {
                    sber.Append(myRecodeMSILCode[ifstrLen-1]);//else语句块                
                    tmpIndex = myRecordPoint[myRecordPoint.Count-1];//nop最后
                    funcs.PrintSleMSILCMD("nop");
                    sber.Append(funcs.GetToString(ref tmpIndex, true));
                }
            }


            #endregion
            return sber.ToString();
        }
        private string FORCodes(int CurrentPoint, int CodeLen, ref int end)
        {
            StringBuilder sber = new StringBuilder();
            end = CodeLen;
            int cp = CurrentPoint;
            Stack<int> myStack = new Stack<int>();
            List<int> myRecordPoint = new List<int>();//记录位置
            List<string> myRecodeMSILCode = new List<string>();//临时存放
            List<string> myRecodeCode = new List<string>();//临时存放
            int s = 0,e = 0;
            int st = 0, et = 0;
            for (; cp < CodeLen; cp++)
            {
                #region 空白跳过
                if (Char.IsWhiteSpace(Func.funcCode[cp]) || Func.funcCode[cp] == '\r' || Func.funcCode[cp] == '\n' || Func.funcCode[cp] == '\t')
                {
                    continue;
                }
                #endregion
                #region FOR(){}
                if (GetFirstToken(cp, CodeLen, ref end).ToUpper() == comm.ChkKeyWord.FOR.ToString())
                {
                    for (; cp < CodeLen; cp++)
                    {
                        if (Func.funcCode[cp] == '(')
                        {
                            myStack.Push(cp);
                        }
                        else if (Func.funcCode[cp] == ')')
                        {
                            if (myStack.Count > 1)
                            {
                                myStack.Pop();
                            }
                            else if (myStack.Count == 1)
                            {
                                //处理条件
                                st = myStack.Pop() + 1;
                                et = cp - 1;
                                break;
                            }
                        }
                        else if (myStack.Count == 1 && Func.funcCode[cp] == ';')
                        {
                            myRecordPoint.Add(cp);
                        }
                        else if (cp == CodeLen - 1)
                        {
                            throw new Exception("Debug:条件错误！");
                        }
                    }
                    for (; cp < CodeLen; cp++)
                    {
                        if (Func.funcCode[cp] == '{')
                        {
                            myStack.Push(cp);
                        }
                        else if (Func.funcCode[cp] == '}')
                        {
                            if (myStack.Count > 1)
                            {
                                myStack.Pop();
                            }
                            else if (myStack.Count == 1)
                            {
                                //处理语句块
                                s = myStack.Pop() + 1;
                                e = cp - 1;
                                break;
                            }
                        }
                        else if (cp == CodeLen - 1)
                        {
                            throw new Exception("Debug:语句块错误！");
                        }
                    }
                }
                #endregion
                else
                {
                    end = cp - 1;
                    break;
                }
                if (cp == CodeLen - 1)
                {
                    end = CodeLen - 1;
                }
            }
            #region 组织代码
            int tmp_1 = myRecordPoint[0], tmp_2 = myRecordPoint[1], tmp_end=0;
            myRecordPoint.Clear();
            comm.FWCVariableType tmprt = comm.FWCVariableType.VT_void;
            myRecodeMSILCode.Add(SingleLineCode(GetFirstToken(st,et-st+1,ref tmp_end),Func.funcCode.Substring(st,tmp_1-st+1)));//预置为空
            myRecordPoint.Add(Index++);
            myRecordPoint.Add(Index++);
            myRecodeMSILCode.Add(CompileProgram(s, e));
            myRecodeMSILCode.Add(SingleLineCode(GetFirstToken(tmp_2 + 1, et - tmp_2, ref tmp_end), Func.funcCode.Substring(tmp_2 + 1, et - tmp_2)+";"));
            myRecordPoint.Add(Index++);
            myRecodeMSILCode.Add(ComputeArcExp(Func.funcCode.Substring(tmp_1+1,tmp_2-tmp_1-1), ref tmprt));
            myRecordPoint.Add(Index++);

            int ifstrLen = myRecodeMSILCode.Count;
            BrCmdStruct brs = new BrCmdStruct();
            FuncStruct funcs = new FuncStruct();
            int tmpIndex = 0;

            sber.Append(myRecodeMSILCode[0]);
            tmpIndex = myRecordPoint[2];
            brs.AddJmpOperator(comm.MSILCMD.CMD_br_s, tmpIndex);
            tmpIndex = myRecordPoint[0];
            sber.Append(brs.GetToString(ref tmpIndex, true));
            tmpIndex = myRecordPoint[1];
            funcs.PrintSleMSILCMD("nop");
            sber.Append(funcs.GetToString(ref tmpIndex, true));
            sber.Append(myRecodeMSILCode[1]);
            sber.Append(myRecodeMSILCode[2]);
            tmpIndex = myRecordPoint[2];
            funcs.PrintSleMSILCMD("nop");
            sber.Append(funcs.GetToString(ref tmpIndex, true));
            sber.Append(myRecodeMSILCode[3]);
            tmpIndex = myRecordPoint[1];
            brs.AddJmpOperator(comm.MSILCMD.CMD_brtrue_s, tmpIndex);
            tmpIndex = myRecordPoint[3];
            sber.Append(brs.GetToString(ref tmpIndex, true));
            #endregion

            return sber.ToString();
        }

        #endregion
        #region 私有方法
        /// <summary>
        /// 匹配
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="endP"></param>
        /// <returns></returns>
        private string MatchLRPAndCon(char s, char e, ref int endP)
        {
            return string.Empty;
        }
        /// <summary>
        /// 格式化参数表
        /// </summary>
        /// <param name="cmdstr"></param>
        /// <returns></returns>
        private comm.FWCVariableType[] ReturnArrByCMDStr(string cmdstr)
        {
            List<comm.FWCVariableType> tmp = new List<comm.FWCVariableType>();
            if (cmdstr.Length > 0)
            {
                string[] tmpArr = cmdstr.Split(',');
                foreach (string tmpcmd in tmpArr)
                {
                    string tmp_cType = tmpcmd.Trim().Split(' ')[0];
                    //string tmp_cName = tmpcmd.Trim().Split(' ')[1];
                    //cs.AddLocalsVariable(comm.CVariableType[tmp_cType], tmp_cName);
                    tmp.Add(comm.CVariableType[tmp_cType]);
                }
            }
            return tmp.ToArray();
        }
        /// <summary>
        /// 匹配关键词
        /// </summary>
        /// <param name="curToken"></param>
        /// <param name="ckword"></param>
        /// <returns></returns>
        private bool Match_ChkKeyWord(string curToken, comm.ChkKeyWord ckword)
        {
            bool retValue = false;
            if (curToken.ToUpper() == ckword.ToString())
            {
                retValue = true;
            }
            return retValue;
        }
        /// <summary>
        /// 返回当前首Token
        /// </summary>
        /// <param name="CurrentPoint"></param>
        /// <param name="CodeLen"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private string GetFirstToken(int CurrentPoint, int CodeLen, ref int end)
        {
            string tmp = string.Empty;
            end = -9;
            for (int i = CurrentPoint; i < CodeLen; i++)
            {
                if (char.IsLetterOrDigit(Func.funcCode, i) || Func.funcCode[i] == '_' || Func.funcCode[i] == '[' || Func.funcCode[i] == ']')
                {
                }
                else if (tmp == string.Empty)
                {
                    tmp = Func.funcCode.Substring(CurrentPoint, i - CurrentPoint).Trim();
                    foreach (string keyWord in Enum.GetNames(typeof(comm.ChkKeyWord)))
                    {
                        if (keyWord.ToUpper().Equals(tmp.ToUpper()))
                        {
                            end = -1;
                            return tmp;
                        }
                    }
                }
                else if (Func.funcCode[i] == ';')
                {
                    end = i;
                    break;
                }
            }
            return tmp;
        }
        /// <summary>
        /// 根据变量名返回变量类型
        /// </summary>
        /// <param name="vName">变量名</param>
        /// <param name="type">返回：0参数1局部2全局</param>
        /// <returns></returns>
        private comm.FWCVariableType ReturnTypeByName(string vName,ref int type)
        {
            comm.FWCVariableType returnValue = comm.FWCVariableType.VT_void;
            returnValue = cs.ReturnTypeByName(vName);
            type = -1;
            if (returnValue != comm.FWCVariableType.VT_void)
            {
                type = 0;
                return returnValue;
            }
            returnValue = ls.ReturnTypeByName(vName);
            if (returnValue != comm.FWCVariableType.VT_void)
            {
                type = 1;
                return returnValue;
            }
            if (DicPV.ContainsKey(vName))
            {
                type = 2;
                returnValue = DicPV[vName].pvVT;
            }
            return returnValue;
        }
        /// <summary>
        /// 根据Token节点类型返回对应枚举类型
        /// </summary>
        /// <param name="currentLink">Token节点</param>
        /// <returns></returns>
        private comm.FWCVariableType ReturnTypeByLinkCurrentValue(Link currentLink)
        {
            //很奇怪吧。先这样搞，虽然很挫。。
            //obj = ((Token<IExpOperand>)currentLink.CurrentValue).Value.Type; 竟然出错。
            comm.FWCVariableType tmp = comm.FWCVariableType.VT_void;
            if (currentLink.CurrentValue.Type == comm.TokenType.ExpOperand)
            {
                try
                {
                    tmp = ((Token<ExpOperand<string>>)currentLink.CurrentValue).Value.Type;
                }
                catch 
                {
                    try
                    {
                        tmp = ((Token<ExpOperand<double>>)currentLink.CurrentValue).Value.Type;
                    }
                    catch
                    {
                        try
                        {
                            tmp = ((Token<ExpOperand<int>>)currentLink.CurrentValue).Value.Type;
                        }
                        catch
                        {
                            throw new Exception(string.Format("DeBug:当前节点属于未知类型！{0}",currentLink.CurrentValue.ToString()));
                        }
                    }
                }
            }
            return tmp;
        }
        /// <summary>
        /// 重置返回数据类型
        /// </summary>
        /// <param name="pre"></param>
        /// <param name="res"></param>
        private void ReSetReturnType(ref comm.FWCVariableType pre,comm.FWCVariableType res)
        {
            #region
            Dictionary<comm.FWCVariableType, int> tmp = new Dictionary<comm.FWCVariableType, int>()
            {
                {comm.FWCVariableType.VT_void,0},
                {comm.FWCVariableType.VT_bool,3},
                {comm.FWCVariableType.VT_int,1},
                {comm.FWCVariableType.VT_double,2},
                {comm.FWCVariableType.VT_string,3},
                {comm.FWCVariableType.VT_object,4}
            };
            #endregion
            if (tmp[res] > tmp[pre])
            {
                pre = res;
            }
            else
            {
                //不处理，保持原样
                //throw new Exception("Debug：前后操作类型不一致/类型转换失败");
            }
        }
        #endregion

        #region 语义处理
        public string ComputeArcExp(string exp, ref comm.FWCVariableType returnType)
        {
            IRLAS per = new IRLAS(exp);
            Link expHead = per.Analyze();
            Link expTail = null;
            Link tmpLink = expHead;            
            Stack<Link> myStack = new Stack<Link>();
            while (tmpLink.NextLink != null && tmpLink.CurrentValue != null)
            {
                //处理表达式（）匹配
                if (tmpLink.CurrentValue.Type == comm.TokenType.ExpOperator)
                {
                    if (((Token<ExpOperator>)tmpLink.CurrentValue).Value.Type == comm.ExpOperatorType.LBrace)
                    {
                        myStack.Push(tmpLink);
                    }
                    else if (((Token<ExpOperator>)tmpLink.CurrentValue).Value.Type == comm.ExpOperatorType.RBrace)
                    {
                        if (myStack.Count > 0)
                        {
                            myStack.Pop();
                        }
                        else
                        {
                            throw new Exception("DeBug:缺少对应(!");
                        }
                    }
                }
                //处理链表类型,只将变量归位
                else
                {
                }
                //

                tmpLink = tmpLink.NextLink;
            }
            if (myStack.Count != 0)
            {
                throw new Exception("DeBug:缺少对应)!");
            }
            //
            expTail = tmpLink;
            return ComExp(expHead, expTail, ref returnType);
        }
        #region 测试使用
        /// <summary>
        /// 算术处理,处理整型或双精度
        /// </summary>
        /// <param name="head"></param>
        /// <param name="tail"></param>
        /// <returns></returns>
        private string ComputeArcExp(Link head, Link tail,ref comm.FWCVariableType returnType)
        {
            StringBuilder sber = new StringBuilder();
            Link tmpHead = head.NextLink;
            Stack<Link> myStack = new Stack<Link>();
            comm.FWCVariableType returnT = comm.FWCVariableType.VT_void;
            while (tmpHead != tail && tmpHead.CurrentValue != null)
            {                
                #region 处理函数块               
                if (tmpHead.CurrentValue.Type == comm.TokenType.ExpOperand 
                    && ReturnTypeByLinkCurrentValue(tmpHead) == comm.FWCVariableType.VT_string)
                {
                    if (comm.CallFunctionDic1.ContainsKey(((Token<ExpOperand<string>>)tmpHead.CurrentValue).Value.Value.ToLower())
                        || CodeParser.Cp_FunctionList.ContainsKey(((Token<ExpOperand<string>>)tmpHead.CurrentValue).Value.Value.ToLower()))
                    {
                        myStack.Push(tmpHead);
                        Link funHead = tmpHead;
                        Link funTail = null;
                        while (tmpHead != tail && tmpHead.CurrentValue != null)
                        {
                            if (tmpHead.CurrentValue.Type == comm.TokenType.ExpOperator)
                            {
                                if (((Token<ExpOperator>)tmpHead.CurrentValue).Value.Type == comm.ExpOperatorType.LBrace)
                                {
                                    myStack.Push(tmpHead);
                                }
                                else if (((Token<ExpOperator>)tmpHead.CurrentValue).Value.Type == comm.ExpOperatorType.RBrace)
                                {
                                    if (myStack.Count > 1)
                                    {
                                        myStack.Pop();
                                    }
                                    else
                                    {
                                        throw new Exception("Debug:()不对称");
                                    }
                                    if (myStack.Count == 1)
                                    {
                                        funTail = tmpHead;
                                        comm.FWCVariableType tmp = comm.FWCVariableType.VT_void;
                                        sber.AppendLine(ComputeFunction(funHead, funTail, ref tmp));
                                    }
                                }
                            }
                            tmpHead = tmpHead.NextLink;
                        }
                    }
                }
                #endregion

                sber.AppendLine(tmpHead.CurrentValue.ToString());

                if (tmpHead != tail)
                {
                    tmpHead = tmpHead.NextLink;
                }
            }
            returnT = comm.FWCVariableType.VT_string;//返回表达式结果类型
            returnType = returnT;
            return sber.ToString();
        }
        /// <summary>
        /// 函数关键词运算
        /// </summary>
        /// <param name="head">函数名开头</param>
        /// <param name="tail">）结尾</param>
        /// <returns></returns>
        private string ComputeFunction(Link head, Link tail,ref comm.FWCVariableType returnType)
        {
            StringBuilder sber = new StringBuilder();
            Link tmpHead = head;
            string funName = ((Token<ExpOperand<string>>)tmpHead.CurrentValue).Value.Value;//函数名
            comm.FWCVariableType tmpvt = comm.FWCVariableType.VT_void;//来自参数表达式的传入类型
            Stack<Link> myStack = new Stack<Link>();
            myStack.Push(tmpHead);//函数名部分压栈
            tmpHead = tmpHead.NextLink;//从（开始
            Link expHead = null, expTail = null;//标志参数段开始与结束
            while (tmpHead != tail.NextLink && tmpHead.CurrentValue != null)
            {
                if (expHead == null && tmpHead.NextLink.CurrentValue.Type == comm.TokenType.ExpOperand)
                {
                    expHead = tmpHead.NextLink;
                }
                if (tmpHead.CurrentValue.Type == comm.TokenType.ExpOperator)
                {
                    if (((Token<ExpOperator>)tmpHead.CurrentValue).Value.Type == comm.ExpOperatorType.LBrace)
                    {
                        myStack.Push(tmpHead);
                    }
                    else if (((Token<ExpOperator>)tmpHead.CurrentValue).Value.Type == comm.ExpOperatorType.RBrace)
                    {
                        myStack.Pop();
                        if (expHead != null)
                        {
                            expTail = tmpHead;                            
                            sber.AppendLine(ComputeArcExp(expHead.PrevLink, expTail, ref tmpvt));
                        }
                    }
                    else if (((Token<ExpOperator>)tmpHead.CurrentValue).Value.Type == comm.ExpOperatorType.Coa)
                    {
                        if (myStack.Count == 2)
                        {
                            expTail = tmpHead;
                            sber.AppendLine(ComputeArcExp(expHead.PrevLink, expTail, ref tmpvt));
                            expHead = tmpHead.NextLink;
                        }
                    }
                }
                tmpHead = tmpHead.NextLink;
            }
            #region 处理返回类型——返回MSIL
            FuncStruct fs = new FuncStruct();
            if (comm.CallFunctionDic1.ContainsKey(funName.ToLower()))
            {
                switch(comm.CallFunctionDic1[funName.ToLower()])
                {
                    case comm.FuncKeyWord.print:
                        fs.CallFunc_Write(tmpvt);
                        returnType = comm.FWCVariableType.VT_void;
                        break;
                    case comm.FuncKeyWord.printline:
                        fs.CallFunc_WriteLine(tmpvt);
                        returnType = comm.FWCVariableType.VT_void;
                        break;
                    case comm.FuncKeyWord.input:
                        fs.CallFunc_Read();
                        returnType = comm.FWCVariableType.VT_int;
                        break;
                    case comm.FuncKeyWord.inputline:
                        fs.CallFunc_ReadLine();
                        returnType = comm.FWCVariableType.VT_string;
                        break;
                    case comm.FuncKeyWord.tostring:
                        fs.CallFunc_ToString(tmpvt);
                        returnType = comm.FWCVariableType.VT_string;
                        break;
                    default:
                        throw new Exception(string.Format("Debug:{0}函数未定义",funName));
                        break;
                }
            }
            else
            {
                if (CodeParser.Cp_FunctionList.ContainsKey(funName))
                {
                    FunctionList tmpFuncList = CodeParser.Cp_FunctionList[funName];
                    LocalStruct cmdLS = new LocalStruct();
                    //格式化参数表
                    if (tmpFuncList.funcCMD.Length > 0)
                    {
                        string[] tmpArr = tmpFuncList.funcCMD.Split(',');
                        foreach (string tmpcmd in tmpArr)
                        {
                            string tmp_cType = tmpcmd.Trim().Split(' ')[0];
                            string tmp_cName = tmpcmd.Trim().Split(' ')[1];
                            cmdLS.AddLocalsVariable(comm.CVariableType[tmp_cType], tmp_cName);
                        }
                    }
                    fs.CallFunc_Default(funName, tmpFuncList.funcReturn, cmdLS.ReturnTypeArr());
                    returnType = tmpFuncList.funcReturn;
                }
                else
                {
                    throw new Exception(string.Format("Debug:{0}函数未定义", funName));
                }
            }
            sber.AppendLine(fs.GetToString(ref Index,true));
            #endregion
            return sber.ToString();
        }
        #endregion
        /// <summary>
        /// 转后缀并编译MSIL
        /// </summary>
        /// <param name="Head"></param>
        /// <param name="Tail"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        private string ComExp(Link Head, Link Tail, ref comm.FWCVariableType returnType)
        {
            StringBuilder sber = new StringBuilder();
            Link head = Head;
            Stack<Link> myStack = new Stack<Link>();
            List<Link> myList = new List<Link>();
            returnType = comm.FWCVariableType.VT_void;//默认返回
            #region 转后缀表达式
            while (head != Tail && head.CurrentValue != null)
            {
                #region 测试
                //if (head.CurrentValue.Type == comm.TokenType.ExpOperator)
                //{
                //    sber.AppendLine(comm.ExpOpTypeDry[((Token<ExpOperator>)head.CurrentValue).Value.Type].ToString());
                //}
                #endregion
                if (head.CurrentValue.Type == comm.TokenType.ExpOperand)
                {                    
                    comm.FWCVariableType tmp_head_value_type = ReturnTypeByLinkCurrentValue(head);
                    if (tmp_head_value_type == comm.FWCVariableType.VT_string)
                    {
                        string tmp_head_value = ((Token<ExpOperand<string>>)head.CurrentValue).Value.Value;
                        //函数名压栈
                        if (comm.CallFunctionDic1.ContainsKey(tmp_head_value) == true || CodeParser.Cp_FunctionList.ContainsKey(tmp_head_value) == true)
                        {
                            myStack.Push(head);
                        }
                        //字符串值。变量名。布尔值
                        else
                        {
                            myList.Add(head);
                        }
                    }
                    //整型数值，双精度数值
                    else if (tmp_head_value_type == comm.FWCVariableType.VT_int || tmp_head_value_type == comm.FWCVariableType.VT_double)
                    {
                        myList.Add(head);
                    }
                }
                else if (head.CurrentValue.Type == comm.TokenType.ExpOperator)
                {
                    comm.ExpOperatorType tmp_expopr_type = ((Token<ExpOperator>)head.CurrentValue).Value.Type;
                    //左括号压栈
                    if (tmp_expopr_type == comm.ExpOperatorType.LBrace)
                    {
                        myStack.Push(head);
                    }
                    //右括号弹出相关至队列
                    else if (tmp_expopr_type == comm.ExpOperatorType.RBrace)
                    {
                        while (myStack.Count != 0)
                        {
                            Link tmp_from_stack = myStack.Pop();
                            if (((Token<ExpOperator>)tmp_from_stack.CurrentValue).Value.Type != comm.ExpOperatorType.LBrace)
                            {
                                myList.Add(tmp_from_stack);
                            }
                            else
                            {
                                break;
                            }
                        }
                        //函数括号特殊处理
                        if (myStack.Count != 0 && myStack.Peek().CurrentValue.Type == comm.TokenType.ExpOperand 
                            && ReturnTypeByLinkCurrentValue(myStack.Peek()) == comm.FWCVariableType.VT_string)
                        {
                            string tmp_stacktop_isfunction = ((Token<ExpOperand<string>>)myStack.Peek().CurrentValue).Value.Value;
                            if (comm.CallFunctionDic1.ContainsKey(tmp_stacktop_isfunction) == true || CodeParser.Cp_FunctionList.ContainsKey(tmp_stacktop_isfunction))
                            {
                                myList.Add(myStack.Pop());
                            }
                        }
                    }
                    else
                    {
                        if (tmp_expopr_type == comm.ExpOperatorType.Coa)
                        {
                            //while (myStack.Count > 0)
                            //{
                            //    if (myStack.Peek().CurrentValue.Type == comm.TokenType.ExpOperator
                            //        && ((Token<ExpOperator>)myStack.Peek().CurrentValue).Value.Type != comm.ExpOperatorType.LBrace)
                            //    {
                            //        myList.Add(myStack.Pop());
                            //        break;
                            //    }
                            //}
                        }//逗号跳过
                        else
                        {
                            //取栈顶操作符，如果有则比较优先级。如果没有直接入栈
                            if (myStack.Count == 0 || myStack.Peek().CurrentValue.Type != comm.TokenType.ExpOperator)
                            {
                                myStack.Push(head);
                            }
                            else
                            {
                                ExpOperator tmp_head_expopr_value = ((Token<ExpOperator>)head.CurrentValue).Value;
                                ExpOperator tmp_stacktop_expopr_value = ((Token<ExpOperator>)myStack.Peek().CurrentValue).Value;
                                //栈顶操作符优先级高于链表当前：出栈-压栈
                                if (tmp_head_expopr_value.Priority <= tmp_stacktop_expopr_value.Priority)
                                {
                                    if (tmp_head_expopr_value.Type == comm.ExpOperatorType.Equal &&
                                        (tmp_stacktop_expopr_value.Type == comm.ExpOperatorType.LThan
                                        || tmp_stacktop_expopr_value.Type == comm.ExpOperatorType.RThan
                                        || tmp_stacktop_expopr_value.Type == comm.ExpOperatorType.Equal))
                                    {
                                        myStack.Push(head);
                                    }
                                    else
                                    {
                                        myList.Add(myStack.Pop());
                                        myStack.Push(head);
                                        //while (myStack.Count != 0 && myStack.Peek().CurrentValue.Type == comm.TokenType.ExpOperator
                                        //    && tmp_head_expopr_value.Priority <= ((Token<ExpOperator>)myStack.Peek().CurrentValue).Value.Priority)
                                        //{
                                        //    myList.Add(myStack.Pop());      
                                        //}
                                        //myStack.Push(head);
                                    }
                                }
                                else
                                {
                                    myStack.Push(head);
                                }
                            }
                        }
                    }
                }
                head = head.NextLink;
            }
            //处理栈剩余项
            while (myStack.Count != 0)
            {
                //路过（）
                Link tmp_cls_stack = myStack.Pop();
                if (tmp_cls_stack.CurrentValue.Type == comm.TokenType.ExpOperator
                    &&(((Token<ExpOperator>)tmp_cls_stack.CurrentValue).Value.Type == comm.ExpOperatorType.LBrace || ((Token<ExpOperator>)tmp_cls_stack.CurrentValue).Value.Type == comm.ExpOperatorType.RBrace))
                { }
                else
                {
                    myList.Add(tmp_cls_stack);
                }
            }
            #endregion

            #region 译为MSIL
            //此处可优化表达式
            //前置引用类Struct
            EvalStruct evals = new EvalStruct();//处理赋值
            ExpStruct exps = new ExpStruct();//处理运算符
            FuncStruct funcs = new FuncStruct();//处理函数
            BrCmdStruct brcmd = new BrCmdStruct();//跳转
            Link myList_Link = null;
            for (int i = 0; i < myList.Count(); i++)
            {
                myList_Link = myList[i];
                if (myList_Link.CurrentValue.Type == comm.TokenType.ExpOperand)
                {
                    switch (ReturnTypeByLinkCurrentValue(myList_Link))
                    {
                        case comm.FWCVariableType.VT_int:
                            evals.PushStackValue(comm.FWCVariableType.VT_int, (((Token<ExpOperand<int>>)myList_Link.CurrentValue).Value.Value).ToString());
                            ReSetReturnType(ref returnType, comm.FWCVariableType.VT_int);
                            break;
                        case comm.FWCVariableType.VT_double:
                            evals.PushStackValue(comm.FWCVariableType.VT_double, (((Token<ExpOperand<double>>)myList_Link.CurrentValue).Value.Value).ToString());
                            ReSetReturnType(ref returnType, comm.FWCVariableType.VT_double);
                            break;
                        case comm.FWCVariableType.VT_string:
                            //处理 布尔值，函数，变量名
                            string tmp_str = ((Token<ExpOperand<string>>)myList_Link.CurrentValue).Value.Value;
                            if (tmp_str.ToUpper().Equals("TRUE") && tmp_str.ToUpper().Equals("FALSE"))
                            {
                                evals.PushStackValue(comm.FWCVariableType.VT_bool, tmp_str);
                                ReSetReturnType(ref returnType, comm.FWCVariableType.VT_bool);
                            }
                            else if (tmp_str[0] == '"' && tmp_str[tmp_str.Length-1] == '"')
                            {
                                evals.PushStackValue(comm.FWCVariableType.VT_string, tmp_str);
                                ReSetReturnType(ref returnType, comm.FWCVariableType.VT_string);
                            }
                            else if (comm.CallFunctionDic1.ContainsKey(tmp_str))
                            {
                                switch (comm.CallFunctionDic1[tmp_str])
                                {
                                    case comm.FuncKeyWord.input:
                                        funcs.CallFunc_Read();
                                        returnType = comm.FWCVariableType.VT_int;
                                        break;
                                    case comm.FuncKeyWord.inputline:
                                        funcs.CallFunc_ReadLine();
                                        returnType = comm.FWCVariableType.VT_string;
                                        break;
                                    case comm.FuncKeyWord.print:
                                        funcs.CallFunc_Write(returnType);
                                        returnType = comm.FWCVariableType.VT_void;
                                        break;
                                    case comm.FuncKeyWord.printline:
                                        funcs.CallFunc_WriteLine(returnType);
                                        returnType = comm.FWCVariableType.VT_void;
                                        break;
                                    case comm.FuncKeyWord.tostring://???
                                        funcs.CallFunc_ToString(returnType);
                                        returnType = comm.FWCVariableType.VT_string;
                                        break;
                                }
                                sber.Append(funcs.GetToString(ref Index, true));
                                continue;
                            }
                            else if (CodeParser.Cp_FunctionList.ContainsKey(tmp_str))//自定义函数项
                            {
                                FunctionList fltmp = CodeParser.Cp_FunctionList[tmp_str];
                                funcs.CallFunc_Default(tmp_str, fltmp.funcReturn,ReturnArrByCMDStr(fltmp.funcCMD));
                                returnType = fltmp.funcReturn;
                                sber.Append(funcs.GetToString(ref Index, true));
                                continue;
                            }
                            else//处理变量
                            {
                                comm.FWCVariableType rtnType = comm.FWCVariableType.VT_void;
                                comm.FWCVariableType rtnType_tmp = comm.FWCVariableType.VT_void;
                                string tmp_vname = string.Empty;
                                string tmp_varrnum = string.Empty;
                                int rtnNum = -1;
                                MatchCollection mc = commFunc.RegexMatches(tmp_str, comm.Reg_VName);
                                if (mc.Count == 1)
                                {
                                    tmp_vname = mc[0].Groups["vName"].Value.ToString();
                                    tmp_varrnum = mc[0].Groups["ArrNum"].Value.ToString().Length > 0 ? mc[0].Groups["ArrNum"].Value : string.Empty;
                                    rtnType = ReturnTypeByName(tmp_vname, ref rtnNum);
                                    rtnType_tmp = tmp_varrnum == string.Empty ? rtnType : comm.CVariableType[comm.DVariableType[rtnType].Substring(0, comm.DVariableType[rtnType].Length - 2)];
                                    //根据变量名取值压栈
                                    switch (rtnNum)
                                    {
                                        case 0://参数，返回值暂不予以支持数组
                                            evals.PushStackCmdValue(ls.IsExistLV(tmp_vname), tmp_vname);
                                            break;
                                        case 1:
                                            if (tmp_varrnum == string.Empty)
                                            {
                                                evals.PushStackLocalVTValue(ls.IsExistLV(tmp_vname), tmp_vname);
                                            }
                                            else
                                            {
                                                evals.PushStackLocalVTValue(ls.IsExistLV(tmp_vname), tmp_vname);
                                                sber.Append(evals.GetToString(ref Index, true));
                                                comm.FWCVariableType tmpDG = comm.FWCVariableType.VT_int;
                                                sber.Append(ComputeArcExp(tmp_varrnum, ref tmpDG));
                                                evals.PushStackIndexArr(rtnType_tmp);
                                            }
                                            break;
                                        case 2:
                                            if (tmp_varrnum == string.Empty)
                                            {
                                                evals.PushStackPublicVTValue(rtnType, tmp_vname);
                                            }
                                            else
                                            {
                                                evals.PushStackPublicVTValue(rtnType, tmp_vname);
                                                sber.Append(evals.GetToString(ref Index, true));
                                                comm.FWCVariableType tmpDG = comm.FWCVariableType.VT_int;
                                                sber.Append(ComputeArcExp(tmp_varrnum, ref tmpDG));
                                                evals.PushStackIndexArr(rtnType_tmp);
                                            }
                                            break;
                                        default:
                                            throw new Exception(string.Format("Debug:不存在的变量{0}", tmp_vname));
                                            break;
                                    }
                                    ReSetReturnType(ref returnType, rtnType_tmp);
                                    //
                                }
                                else
                                {
                                    throw new Exception(string.Format("Debug:无效的变量名称{0}", tmp_str));
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    sber.Append(evals.GetToString(ref Index,true));
                }
                else if (myList_Link.CurrentValue.Type == comm.TokenType.ExpOperator)
                {
                    comm.ExpOperatorType expOerType = ((Token<ExpOperator>)myList_Link.CurrentValue).Value.Type;
                    switch (expOerType)
                    {
                        case comm.ExpOperatorType.Plus:
                        case comm.ExpOperatorType.Subtract:
                        case comm.ExpOperatorType.Multiply:
                        case comm.ExpOperatorType.Divide:
                        case comm.ExpOperatorType.Mod:
                            exps.AddExpOperator(expOerType);
                            break;
                        case comm.ExpOperatorType.RThan:
                            //当>=时，转制成<!
                            if (i < myList.Count() - 1
                                && myList[i + 1].CurrentValue.Type == comm.TokenType.ExpOperator
                                && ((Token<ExpOperator>)myList[i + 1].CurrentValue).Value.Type == comm.ExpOperatorType.Equal)
                            {
                                exps.AddExpOperator(comm.ExpOperatorType.LThan);
                                sber.Append(exps.GetToString(ref Index, true));
                                evals.PushStackValue(comm.FWCVariableType.VT_bool, "false");
                                sber.Append(evals.GetToString(ref Index, true));
                                exps.AddExpOperator(comm.ExpOperatorType.Equal);
                                sber.Append(exps.GetToString(ref Index, true));
                                i++;
                                continue;
                            }
                            else
                            {
                                exps.AddExpOperator(expOerType);
                            }
                            returnType = comm.FWCVariableType.VT_bool;
                            break;
                        case comm.ExpOperatorType.LThan:
                            //当<=时，转制成>!
                            if (i < myList.Count() - 1
                                && myList[i + 1].CurrentValue.Type == comm.TokenType.ExpOperator
                                && ((Token<ExpOperator>)myList[i + 1].CurrentValue).Value.Type == comm.ExpOperatorType.Equal)
                            {
                                exps.AddExpOperator(comm.ExpOperatorType.RThan);
                                sber.Append(exps.GetToString(ref Index, true));
                                evals.PushStackValue(comm.FWCVariableType.VT_bool, "false");
                                sber.Append(evals.GetToString(ref Index, true));
                                exps.AddExpOperator(comm.ExpOperatorType.Equal);
                                sber.Append(exps.GetToString(ref Index, true));
                                i++;
                                continue;
                            }
                            else
                            {
                                exps.AddExpOperator(expOerType);
                            }
                            returnType = comm.FWCVariableType.VT_bool;
                            break;
                        case comm.ExpOperatorType.Equal:
                            if (i < myList.Count() - 1
                                && myList[i + 1].CurrentValue.Type == comm.TokenType.ExpOperator
                                && ((Token<ExpOperator>)myList[i + 1].CurrentValue).Value.Type == comm.ExpOperatorType.RThan)
                            {
                                exps.AddExpOperator(comm.ExpOperatorType.LThan);
                                sber.Append(exps.GetToString(ref Index, true));
                                evals.PushStackValue(comm.FWCVariableType.VT_bool, "false");
                                sber.Append(evals.GetToString(ref Index, true));
                                exps.AddExpOperator(comm.ExpOperatorType.Equal);
                                sber.Append(exps.GetToString(ref Index, true));
                                i++;
                                continue;
                            }
                            else if (i < myList.Count() - 1
                                && myList[i + 1].CurrentValue.Type == comm.TokenType.ExpOperator
                                && ((Token<ExpOperator>)myList[i + 1].CurrentValue).Value.Type == comm.ExpOperatorType.LThan)
                            {
                                exps.AddExpOperator(comm.ExpOperatorType.RThan);
                                sber.Append(exps.GetToString(ref Index, true));
                                evals.PushStackValue(comm.FWCVariableType.VT_bool, "false");
                                sber.Append(evals.GetToString(ref Index, true));
                                exps.AddExpOperator(comm.ExpOperatorType.Equal);
                                sber.Append(exps.GetToString(ref Index, true));
                                i++;
                                continue;
                            }
                            else if (i < myList.Count() - 1
                                && myList[i + 1].CurrentValue.Type == comm.TokenType.ExpOperator
                                && ((Token<ExpOperator>)myList[i + 1].CurrentValue).Value.Type == comm.ExpOperatorType.Equal)
                            {
                                exps.AddExpOperator(expOerType);
                                i++;
                            }
                            returnType = comm.FWCVariableType.VT_bool;
                            break;                            
                        case comm.ExpOperatorType.Not:
                            //栈压 false---比较等否ceq
                            evals.PushStackValue(comm.FWCVariableType.VT_bool, "false");
                            sber.Append(evals.GetToString(ref Index, true));
                            exps.AddExpOperator(comm.ExpOperatorType.Equal);
                            returnType = comm.FWCVariableType.VT_bool;
                            break;
                        case comm.ExpOperatorType.And:
                            #region
                            //evals.PushStackValue(comm.FWCVariableType.VT_bool, "true");
                            //sber.Append(evals.GetToString(ref Index, true));
                            //exps.AddExpOperator(comm.ExpOperatorType.Equal);
                            //sber.Append(exps.GetToString(ref Index, true));
                            //brcmd.AddJmpOperator(comm.MSILCMD.CMD_brfalse_s, Index + 4);
                            //sber.Append(brcmd.GetToString(ref Index, true));
                            //evals.PushStackValue(comm.FWCVariableType.VT_bool,"true");
                            //sber.Append(evals.GetToString(ref Index, true));
                            //exps.AddExpOperator(comm.ExpOperatorType.Equal);
                            //sber.Append(exps.GetToString(ref Index, true));
                            //brcmd.AddJmpOperator(comm.MSILCMD.CMD_br_s, Index + 2);
                            //sber.Append(brcmd.GetToString(ref Index, true));
                            //evals.PushStackValue(comm.FWCVariableType.VT_bool,"false");
                            //sber.Append(evals.GetToString(ref Index, true));
                            #endregion
                            break;
                        case comm.ExpOperatorType.Or:
                            break;
                    }
                    sber.Append(exps.GetToString(ref Index, true));
                }
            }

            #endregion

            return sber.ToString();
        }
        #endregion
    }
}
