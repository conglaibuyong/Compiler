using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

namespace CodeParser
{
    /// <summary>
    /// 编译IL
    /// </summary>
    public class CodeParser
    {
        #region 成员
        /// <summary>
        /// 存储全局变量
        /// </summary>
        private static Dictionary<string, PublicValueList> cp_PublicList = new Dictionary<string, PublicValueList>();
        public static Dictionary<string, PublicValueList> Cp_PublicList
        {
            get { return CodeParser.cp_PublicList; }
        }
        /// <summary>
        /// 存储函数体
        /// </summary>
        private static Dictionary<string, FunctionList> cp_FunctionList = new Dictionary<string, FunctionList>();
        public static Dictionary<string, FunctionList> Cp_FunctionList
        {
            get { return CodeParser.cp_FunctionList; }
        }
        
        #endregion

        #region Public_Func
        /// <summary>
        /// 读取代码文件，并转换存储
        /// </summary>
        /// <param name="path"></param>
        public static bool ReadCodeFile(string path)
        {
            string tmp = string.Empty;
            try
            {
                tmp = commFunc.ReadFileToEnd(path);
                //
                //检测全局变量和函数结构语法
                //
                if (tmp.Length > 0)
                {
                    MatchCollection mcP = commFunc.RegexMatches(tmp, comm.Reg_PublicVT);
                    foreach (Match m in mcP)
                    {
                        PublicValueList pv = new PublicValueList();
                        pv.pvValue = m.Groups["pvValue"].Value;
                        pv.pvName = m.Groups["pvName"].Value;
                        string arrTemp = m.Groups["pvArrNum"].Value;
                        if (arrTemp.Length > 0)
                        {
                            pv.pvArrNum = int.Parse(m.Groups["pvArrNum"].Value);
                            pv.pvVT = comm.CVariableType[m.Groups["pvVT"].Value.ToLower() + "[]"];
                        }
                        else
                        {
                            pv.pvVT = comm.CVariableType[m.Groups["pvVT"].Value.ToLower()];
                        }
                        cp_PublicList.Add(pv.pvName, pv);
                    }

                    MatchCollection mcF = commFunc.RegexMatches(tmp, comm.Reg_Function);
                    foreach (Match m in mcF)
                    {
                        FunctionList func = new FunctionList();
                        func.funcReturn = comm.CVariableType[m.Groups["funcReturn"].Value.ToLower()];
                        func.funcName = m.Groups["funcName"].Value.ToLower();
                        func.funcCMD = m.Groups["funcCMD"].Value;
                        func.funcCode = m.Groups["funcCode"].Value.Trim();
                        cp_FunctionList.Add(func.funcName, func);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex.Message);
                return false;
            }
        }
        /// <summary>
        /// 1.IL文件头结构信息
        /// </summary>
        /// <returns></returns>
        public static string ILFileHead()
        {
            StringBuilder sber = new StringBuilder();
            sber.AppendLine(comm.AssemblyExternMscorlib);
            sber.AppendLine(comm.ProgramVersions);
            sber.AppendLine(comm.ProgramModule);
            return sber.ToString();
        }
        /// <summary>
        /// 2.编译定义全局变量
        /// </summary>
        /// <returns></returns>
        public static string CompileILDefinePublic()
        {
            StringBuilder sber = new StringBuilder();
            PublicStruct ps = new PublicStruct();
            foreach (PublicValueList pv in cp_PublicList.Values)
            {
                ps.AddPublicsVariable(pv.pvVT, pv.pvName);
            }
            return ps.ToString();
        }
        /// <summary>
        /// 3.1在主函数中初始化全局变量赋值
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string CompileILMainFunc_PublicValue(ref int index)
        {
            StringBuilder sber = new StringBuilder();
            EvalStruct es = new EvalStruct();
            try
            {
                foreach (PublicValueList pv in cp_PublicList.Values)
                {
                    if (pv.pvValue.Length > 0)
                    {
                        if (pv.pvVT == comm.FWCVariableType.VT_int ||
                            pv.pvVT == comm.FWCVariableType.VT_double ||
                            pv.pvVT == comm.FWCVariableType.VT_string||
                            pv.pvVT == comm.FWCVariableType.VT_bool)
                        {
                            es.PushStackValue(pv.pvVT, pv.pvValue);
                            es.EvalStackPublicValue(pv.pvVT, pv.pvName);
                        }
                        else if (pv.pvVT == comm.FWCVariableType.VT_object)
                        {
                            string tmp = commFunc.ReturnType(pv.pvValue);
                            if (tmp.Length>0)
                            {
                                es.PushStackValue(comm.CVariableType[tmp], pv.pvValue);
                                if (tmp != "string")
                                {
                                    es.BoxValueType(comm.CVariableType[tmp]);
                                }
                            }
                            else
                            {
                                throw new Exception(string.Format("Error:未知类型的值不能赋值使用。"));
                            }
                            es.EvalStackPublicValue(comm.FWCVariableType.VT_object, pv.pvName);
                        }
                    }
                    if (pv.pvArrNum > 0)
                    {
                        es.PushStackValue(comm.FWCVariableType.VT_int, pv.pvArrNum.ToString());
                        es.CreateNewArr(pv.pvVT);
                        es.EvalStackPublicValue(pv.pvVT, pv.pvName);
                        if (pv.pvValue.Length > 2)
                        {
                            string[] arrValue = pv.pvValue.Substring(1, pv.pvValue.Length - 2).Split(',');
                            for (int i=0;i<(arrValue.Length>pv.pvArrNum?pv.pvArrNum:arrValue.Length);i++)
                            {
                                es.PushStackPublicVTValue(pv.pvVT, pv.pvName);
                                es.PushStackValue(comm.FWCVariableType.VT_int, i.ToString());
                                switch (pv.pvVT)
                                {
                                    case comm.FWCVariableType.VT_ints:
                                        es.PushStackValue(comm.FWCVariableType.VT_int, arrValue[i]);
                                        break;
                                    case comm.FWCVariableType.VT_doubles:
                                        es.PushStackValue(comm.FWCVariableType.VT_double, arrValue[i]);
                                        break;
                                    case comm.FWCVariableType.VT_bools:
                                        es.PushStackValue(comm.FWCVariableType.VT_bool, arrValue[i]);
                                        break;
                                    case comm.FWCVariableType.VT_strings:
                                        es.PushStackValue(comm.FWCVariableType.VT_string, arrValue[i]);
                                        break;
                                    case comm.FWCVariableType.VT_objects:
                                        string tmp = commFunc.ReturnType(arrValue[i]);
                                        if (tmp.Length>0)
                                        {
                                            es.PushStackValue(comm.CVariableType[tmp], arrValue[i]);
                                            if (tmp != "string")
                                            {
                                                es.BoxValueType(comm.CVariableType[tmp]);
                                             }
                                         }
                                        else
                                        {
                                            throw new Exception(string.Format("Error:未知类型的值不能赋值使用。"));
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                es.EvalIndexArr(pv.pvVT);
                            }
                        }
                    }
                }
                sber.Append(es.GetToString(ref index,true));
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
            return sber.ToString(); 
        }
        /// <summary>
        /// 3.编译主函数Main
        /// </summary>
        /// <returns></returns>
        public static string CompileILMainFunc()
        {
            int index = 0;
            FunctionList mainFunc = cp_FunctionList["main"];
            StringBuilder sber = new StringBuilder();
            sber.AppendLine(string.Format(comm.MainFunctionHead,mainFunc.funcCMD));
            sber.AppendLine("{");
            sber.AppendLine(comm.MainFunctionEntryPoint);
            sber.AppendLine(string.Format(comm.FunctionMaxStack, 8));
            //在主函数中初始化全局变量赋值
            string tmpPV = CompileILMainFunc_PublicValue(ref index);
            //声明使用
            CodeCompiler cc = new CodeCompiler(cp_PublicList, cp_FunctionList["main"],index);
            string tmpSave = cc.CompileProgram(0,mainFunc.funcCode.Length);
            //此处局部变量列表
            sber.AppendLine(cc.Ls.ToString());
            sber.Append(tmpPV);
            //代码块
            sber.AppendLine(tmpSave);
            //代码块
            //ret
            //FuncStruct fs = new FuncStruct();
            //fs.Func_ReturnValue(false);
            //index = sber.ToString().IndexOf('\n')+1;
            sber.AppendLine("IL_ffff: ret");//fs.GetToString(ref index,false));
            //ret
            sber.AppendLine("}");
            //
            //其他函数
            foreach (FunctionList dicfl in cp_FunctionList.Values)
            {
                if (dicfl.funcName.ToUpper() != "MAIN")
                {
                    CodeCompiler tmp = new CodeCompiler(cp_PublicList, cp_FunctionList[dicfl.funcName], 0);
                    sber.AppendLine(tmp.CompileILFunction());
                }
            }
            //
            return sber.ToString();
        }

        #endregion

    }

}
