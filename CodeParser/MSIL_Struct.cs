using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeParser
{
    #region MSIL结构体
    /// <summary>
    /// MSIL代码结构
    /// </summary>
    public struct MSIL_xxxx
    {
        public MSIL_xxxx(string index,comm.MSILCMD msilcmd,string value,string note)
        {
            Index = index;
            MSILCMD = msilcmd;
            Value = value;
            Note = note;
        }

        public string Index;
        public comm.MSILCMD MSILCMD;
        public string Value;
        public string Note;
    }
    /// <summary>
    /// 全局变量列表结构
    /// </summary>
    public struct PublicValueList
    {
        public comm.FWCVariableType pvVT;
        public int pvArrNum;
        public string pvName;
        public string pvValue;
    }
    /// <summary>
    /// 局部变量列表结构
    /// </summary>
    public struct LocalValueList
    {
        public int lvIndex;
        public comm.FWCVariableType lvVT;
        //public int lvArrNum;
        public string lvName;
        public string lvValue;
    }
    /// <summary>
    /// 函数体列表结构
    /// </summary>
    public struct FunctionList
    {
        public comm.FWCVariableType funcReturn;
        public string funcName;
        public string funcCMD;
        public string funcCode;
    }
    #endregion
    #region MSIL生成
    /// <summary>
    /// 基类
    /// </summary>
    public class BaseStruct
    {
        #region 私有成员
        private List<MSIL_xxxx> bsList = new List<MSIL_xxxx>();
        #endregion

        #region 方法
        public void Add(comm.MSILCMD msilcmd,string value,string note)
        {
            MSIL_xxxx tmp = new MSIL_xxxx("", msilcmd, value, note);
            bsList.Add(tmp);
        }

        public string GetToString(ref int index, bool cls)
        {
            StringBuilder sber = new StringBuilder();
            foreach (MSIL_xxxx tmp in bsList)
            {
                string ix = commFunc.GetMSILIndex(index++);
                sber.AppendLine(string.Format("{0} {1} {2} {3}", ix, comm.DMSILCMD[tmp.MSILCMD], tmp.Value, tmp.Note));
            }
            if (cls == true)
                bsList.Clear();
            return sber.ToString();
        }
        /// <summary>
        /// 重载ToString方法，使之直接输出MSIL相关格式代码
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sber = new StringBuilder();
            int index = 0;
            int len = bsList.Count - 1;
            foreach (MSIL_xxxx tmp in bsList)
            {
                sber.AppendFormat("{0} {1} {2} {3}", tmp.Index, comm.DMSILCMD[tmp.MSILCMD], tmp.Value, tmp.Note);
                if (index < len)
                    sber.Append("\r\n");
                index++;
            }
            return sber.ToString();
        }
        #endregion
    }
    /// <summary>
    /// 全局变量MSIL结构
    /// </summary>
    public class PublicStruct
    {
        private List<PublicValueList> publics = new List<PublicValueList>();

        public void AddPublicsVariable(comm.FWCVariableType vt,string vn)
        {
            PublicValueList pvlist = new PublicValueList();
            pvlist.pvVT = vt;
            pvlist.pvName = vn;
            publics.Add(pvlist);
        }
        public bool IsExistPV(string vn)
        {
            PublicValueList pvList = publics.Find(
                delegate(PublicValueList tmp)
                {
                    return tmp.pvName == vn;
                }
            );
            if (pvList.pvName != vn)
                return false;
            else
                return true;

        }
        //public comm.FWCVariableType ReturnTypeByName(string vn)
        //{
        //    PublicValueList pvlist = publics.Find(
        //    delegate(PublicValueList tmp)
        //    {
        //        return tmp.pvName == vn;
        //    }
        //    );
        //    if (pvlist.pvName != vn)
        //        return comm.FWCVariableType.VT_object;
        //    else
        //        return pvlist.pvVT;
        //}
        /// <summary>
        /// 重载ToString方法，使之直接输出MSIL相关格式代码
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sber = new StringBuilder();
            foreach (PublicValueList pvlist in publics)
            {
                sber.AppendLine(string.Format(comm.PublicVariable,comm.DVariableType[pvlist.pvVT],pvlist.pvName));
            }
            return sber.ToString();
        }
    }
    /// <summary>
    /// 局部变量 参数变量 MSIL结构
    /// </summary>
    public class LocalStruct
    {
        private List<LocalValueList> locals = new List<LocalValueList>();
        private int index = 0;//索引值
        /// <summary>
        /// 增加comm.Enum项
        /// </summary>
        /// <param name="vt"></param>
        /// <param name="vn"></param>
        public void AddLocalsVariable(comm.FWCVariableType vt, string vn)
        {
            LocalValueList lvlist = new LocalValueList();
            lvlist.lvIndex = index++;
            lvlist.lvVT = vt;
            lvlist.lvName = vn;
            locals.Add(lvlist);
        }
        public int IsExistLV(string vn)
        {
            LocalValueList lvlist = locals.Find(
                delegate(LocalValueList tmp)
                {
                    return tmp.lvName == vn;
                }
            );
            if (lvlist.lvName != vn)
                return -1;
            else
                return lvlist.lvIndex;

        }
        public comm.FWCVariableType ReturnTypeByName(string vn)
        {
            LocalValueList lvlist = locals.Find(
            delegate(LocalValueList tmp)
            {
                 return tmp.lvName == vn;
            }
            );
            if (lvlist.lvName != vn)
                return comm.FWCVariableType.VT_void;
            else
                return lvlist.lvVT;
        }
        public comm.FWCVariableType[] ReturnTypeArr()
        {
            List<comm.FWCVariableType> tmp = new List<comm.FWCVariableType>();
            foreach (LocalValueList lvl in locals)
            {
                tmp.Add(lvl.lvVT);
            }
            return tmp.ToArray();
        }
        /// <summary>
        /// 重载ToString方法，使之直接输出MSIL相关格式代码
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sber = new StringBuilder();
            int tmpIndex = 0;
            int tmpLen = locals.Count - 1;
            foreach (LocalValueList lvlist in locals)
            {
                sber.Append(string.Format(comm.LVariableEnum, lvlist.lvIndex, comm.DVariableType[lvlist.lvVT], lvlist.lvName));
                if (tmpIndex < tmpLen)
                    sber.Append(",");
                tmpIndex++;
            }
            if (tmpIndex > 0)
            {
                return string.Format(comm.LocalsVariable, sber.ToString());
            }
            else
            {
                return string.Empty;
            }
        }
        public string GetToString()
        {
            StringBuilder sber = new StringBuilder();
            int tmpIndex = 0;
            int tmpLen = locals.Count - 1;
            foreach (LocalValueList lvlist in locals)
            {
                sber.Append(string.Format(@"{0} {1}", comm.DVariableType[lvlist.lvVT], lvlist.lvName));
                if (tmpIndex < tmpLen)
                    sber.Append(",");
                tmpIndex++;
            }
            return sber.ToString();
        }
    }
    /// <summary>
    /// 变量赋值取值MSIL结构
    /// </summary>
    public class EvalStruct : BaseStruct
    {
        /// <summary>
        /// 将计算栈顶值赋值给全局变量
        /// </summary>
        /// <param name="vt">全局变量类型</param>
        /// <param name="pvName">全局变量名称</param>
        public void EvalStackPublicValue(comm.FWCVariableType vt, string pvName)
        {
            Add(comm.MSILCMD.CMD_stsfld, comm.DVariableType[vt] + " " + pvName,string.Empty);
        }
        /// <summary>
        /// 将计算栈顶值赋值给局部变量
        /// </summary>
        /// <param name="index">局部变量列表索引值</param>
        /// <param name="pvName">局部变量名称，仅当索引值大于3有效</param>
        public void EvalStackLocalValue(int index, string pvName)
        {
            comm.MSILCMD MSILCMD = comm.MSILCMD.CMD_nop;
            string Value = string.Empty;
            switch (index)
            {
                case 0:
                    MSILCMD = comm.MSILCMD.CMD_stloc_0;
                    break;
                case 1:
                    MSILCMD = comm.MSILCMD.CMD_stloc_1;
                    break;
                case 2:
                    MSILCMD = comm.MSILCMD.CMD_stloc_2;
                    break;
                case 3:
                    MSILCMD = comm.MSILCMD.CMD_stloc_3;
                    break;
                default:
                    MSILCMD = comm.MSILCMD.CMD_stloc_s;
                    Value = pvName;
                    break;
            }
            Add(MSILCMD,Value,string.Empty);
        }
        /// <summary>
        /// 将类型值压栈
        /// </summary>
        /// <param name="vt"></param>
        /// <param name="value"></param>
        public void PushStackValue(comm.FWCVariableType vt, string value)
        {
            comm.MSILCMD MSILCMD = comm.MSILCMD.CMD_nop;
            string Value = string.Empty;
            if (vt == comm.FWCVariableType.VT_int)
            {
                switch (value)
                {
                    case "0":
                        MSILCMD = comm.MSILCMD.CMD_ldc_i4_0;
                        break;
                    case "1":
                        MSILCMD = comm.MSILCMD.CMD_ldc_i4_1;
                        break;
                    case "2":
                        MSILCMD = comm.MSILCMD.CMD_ldc_i4_2;
                        break;
                    case "3":
                        MSILCMD = comm.MSILCMD.CMD_ldc_i4_3;
                        break;
                    case "4":
                        MSILCMD = comm.MSILCMD.CMD_ldc_i4_4;
                        break;
                    case "5":
                        MSILCMD = comm.MSILCMD.CMD_ldc_i4_5;
                        break;
                    case "6":
                        MSILCMD = comm.MSILCMD.CMD_ldc_i4_6;
                        break;
                    case "7":
                        MSILCMD = comm.MSILCMD.CMD_ldc_i4_7;
                        break;
                    case "8":
                        MSILCMD = comm.MSILCMD.CMD_ldc_i4_8;
                        break;
                    case "-1":
                        MSILCMD = comm.MSILCMD.CMD_ldc_i4_m1;
                        break;
                    default:
                        int tmpValue = Convert.ToInt32(value);
                        if (tmpValue > -129 && tmpValue < 128)
                        {
                            MSILCMD = comm.MSILCMD.CMD_ldc_i4_s;
                            Value = value;
                        }
                        else
                        {
                            MSILCMD = comm.MSILCMD.CMD_ldc_i4;
                            Value = commFunc.GetHexValue(tmpValue);
                        }
                        break;
                }
            }
            else if (vt == comm.FWCVariableType.VT_bool)
            {
                if (value.ToUpper() == "TRUE" || value == "1")
                {
                    MSILCMD = comm.MSILCMD.CMD_ldc_i4_1;
                }
                else if (value.ToUpper() == "FALSE" || value == "0")
                {
                    MSILCMD = comm.MSILCMD.CMD_ldc_i4_0;
                }
            }
            else if (vt == comm.FWCVariableType.VT_double)
            {
                MSILCMD = comm.MSILCMD.CMD_ldc_r8;
                Value = value;
            }
            else if (vt == comm.FWCVariableType.VT_string)
            {
                MSILCMD = comm.MSILCMD.CMD_ldstr;
                //Value = string.Format("\"{0}\"", value);
                Value = value;
            }
            Add(MSILCMD, Value, string.Empty);
        }
        /// <summary>
        /// 全局变量值压栈
        /// </summary>
        /// <param name="vt"></param>
        /// <param name="vtName"></param>
        public void PushStackPublicVTValue(comm.FWCVariableType vt, string vtName)
        {
            Add(comm.MSILCMD.CMD_ldsfld, string.Format("{0} {1}", comm.DVariableType[vt], vtName),string.Empty);
        }
        /// <summary>
        /// 全局 值类型 变量 地址压栈
        /// </summary>
        /// <param name="vt"></param>
        /// <param name="vtName"></param>
        public void PushStackPublicVTAddr(comm.FWCVariableType vt, string vtName)
        {
            Add(comm.MSILCMD.CMD_ldsflda, string.Format("{0} {1}", comm.DVariableType[vt], vtName),string.Empty);
        }
        /// <summary>
        /// 局部变量值压栈
        /// </summary>
        /// <param name="index"></param>
        /// <param name="vtName"></param>
        public void PushStackLocalVTValue(int index, string vtName)
        {
            comm.MSILCMD MSILCMD = comm.MSILCMD.CMD_nop;
            string Value = string.Empty;
            switch (index)
            {
                case 0:
                    MSILCMD = comm.MSILCMD.CMD_ldloc_0;
                    break;
                case 1:
                    MSILCMD = comm.MSILCMD.CMD_ldloc_1;
                    break;
                case 2:
                    MSILCMD = comm.MSILCMD.CMD_ldloc_2;
                    break;
                case 3:
                    MSILCMD = comm.MSILCMD.CMD_ldloc_3;
                    break;
                default:
                    MSILCMD = comm.MSILCMD.CMD_ldloc_s;
                    Value = vtName;
                    break;
            }
            Add(MSILCMD,Value,string.Empty);
        }
        /// <summary>
        /// 局部 值类型 变量地址压栈
        /// </summary>
        /// <param name="vtName"></param>
        public void PushStackLocalVTAddr(string vtName)
        {
            Add(comm.MSILCMD.CMD_ldloca_s,vtName,string.Empty);
        }
        /// <summary>
        /// 参数值压栈
        /// </summary>
        /// <param name="index"></param>
        /// <param name="cName"></param>
        public void PushStackCmdValue(int index, string cName)
        {
            comm.MSILCMD MSILCMD = comm.MSILCMD.CMD_nop;
            string Value = string.Empty;
            switch (index)
            {
                case 0:
                    MSILCMD = comm.MSILCMD.CMD_ldarg_0;
                    break;
                case 1:
                    MSILCMD = comm.MSILCMD.CMD_ldarg_1;
                    break;
                case 2:
                    MSILCMD = comm.MSILCMD.CMD_ldarg_2;
                    break;
                case 3:
                    MSILCMD = comm.MSILCMD.CMD_ldarg_3;
                    break;
                default:
                    MSILCMD = comm.MSILCMD.CMD_ldarg_s;
                    Value = cName;
                    break;
            }
            Add(MSILCMD, Value, string.Empty);
        }
        /// <summary>
        /// 装箱
        /// </summary>
        /// <param name="vt"></param>
        public void BoxValueType(comm.FWCVariableType vt)
        {
            Add(comm.MSILCMD.CMD_box, comm._MSIL_VariableType[vt],string.Empty);
        }
        /// <summary>
        /// 将栈顶值类型已装箱的形式转化为未装箱的形式
        /// unbox.any
        /// </summary>
        /// <param name="vt"></param>
        public void UnBoxValueType(comm.FWCVariableType vt)
        {
            Add(comm.MSILCMD.CMD_unbox_any, comm._MSIL_VariableType[vt],string.Empty);
        }
        /// <summary>
        /// 创建新一维数组索引从零开始，引用压栈：1.维数压栈，2.newarr创建数组，引用压栈*，3.栈顶值赋给变量
        /// </summary>
        /// <param name="vt"></param>
        public void CreateNewArr(comm.FWCVariableType vt)
        {
            Add(comm.MSILCMD.CMD_newarr, comm._MSIL_VariableType[vt],string.Empty);
        }
        /// <summary>
        /// 给数组索引处元素赋值：1.自静态字段的值压入当前计算栈，2.压索引值，3.压类型值，4.索引元素赋值*
        /// </summary>
        /// <param name="vt"></param>
        public void EvalIndexArr(comm.FWCVariableType vt)
        {
            comm.MSILCMD MSILCMD = comm.MSILCMD.CMD_nop;
            if (vt == comm.FWCVariableType.VT_ints)
            {
                MSILCMD = comm.MSILCMD.CMD_stelem_i4;
            }
            else if (vt == comm.FWCVariableType.VT_doubles)
            {
                MSILCMD = comm.MSILCMD.CMD_stelem_r8;
            }
            else if (vt == comm.FWCVariableType.VT_bools)
            {
                MSILCMD = comm.MSILCMD.CMD_stelem_i1;
            }
            else if (vt == comm.FWCVariableType.VT_objects || vt == comm.FWCVariableType.VT_strings)
            {
                MSILCMD = comm.MSILCMD.CMD_stelem_ref;
            }
            Add(MSILCMD,string.Empty,string.Empty);
        }
        /// <summary>
        /// 将数组索引处元素值压栈：1.自静态字段的值压入当前计算栈，2.压索引值，3.压索引处元素值*
        /// </summary>
        /// <param name="vt"></param>
        public void PushStackIndexArr(comm.FWCVariableType vt)
        {
            comm.MSILCMD MSILCMD = comm.MSILCMD.CMD_nop;
            if (vt == comm.FWCVariableType.VT_int)
            {
                MSILCMD = comm.MSILCMD.CMD_ldelem_i4;
            }
            else if (vt == comm.FWCVariableType.VT_double)
            {
                MSILCMD = comm.MSILCMD.CMD_ldelem_r8;
            }
            else if (vt == comm.FWCVariableType.VT_bool)
            {
                MSILCMD = comm.MSILCMD.CMD_ldelem_i1;
            }
            else if (vt == comm.FWCVariableType.VT_object || vt == comm.FWCVariableType.VT_string)
            {
                MSILCMD = comm.MSILCMD.CMD_ldelem_ref;
            }
            Add(MSILCMD,string.Empty,string.Empty);
        }
        /// <summary>
        /// 将数组索引处 值类型 元素地址压栈：1.自静态字段的值压入当前计算栈，2.压索引值，3.压索引处元素地址*
        /// </summary>
        /// <param name="vt"></param>
        public void PushStackIndexArrAddr(comm.FWCVariableType vt)
        {
            Add(comm.MSILCMD.CMD_ldelema, comm._MSIL_VariableType[vt],string.Empty);
        }


    }
    /// <summary>
    /// 函数功能MSIL结构
    /// </summary>
    public class FuncStruct : BaseStruct
    {
        /// <summary>
        /// 输出指令 string:"nop","pop","ret"
        /// </summary>
        /// <param name="cmd"></param>
        public void PrintSleMSILCMD(string cmd)
        {
            comm.MSILCMD tmp;
            switch (cmd.ToLower())
            {
                case "nop":
                    tmp = comm.MSILCMD.CMD_nop;
                    break;
                case "pop":
                    tmp = comm.MSILCMD.CMD_pop;
                    break;
                case "ret":
                    tmp = comm.MSILCMD.CMD_ret;
                    break;
                default:
                    return;
            }
            Add(tmp, string.Empty, string.Empty);           
        }
        /// <summary>
        /// 将栈顶值输出控制台：1.调用Write* 2.nop
        /// </summary>
        /// <param name="vt"></param>
        public void CallFunc_Write(comm.FWCVariableType vt)
        {
            Add(comm.MSILCMD.CMD_call, string.Format(comm.CallFunc_Write, comm.DVariableType[vt]),string.Empty);
            //PrintSleMSILCMD("nop");
        }
        /// <summary>
        /// 将栈顶值输出控制台： 1.调用WriteLine* 2.nop
        /// </summary>
        /// <param name="vt"></param>
        public void CallFunc_WriteLine(comm.FWCVariableType vt)
        {
            Add(comm.MSILCMD.CMD_call, string.Format(comm.CallFunc_WriteLine, comm.DVariableType[vt]),string.Empty);
            //PrintSleMSILCMD("nop");
        }
        /// <summary>
        /// 将控制台输入值压栈: Read返回int32
        /// </summary>
        public void CallFunc_Read()
        {
            Add(comm.MSILCMD.CMD_call, comm.CallFunc_Read,string.Empty);
        }
        /// <summary>
        /// 将控制台输入值压栈：ReadLine返回string
        /// </summary>
        public void CallFunc_ReadLine()
        {
            Add(comm.MSILCMD.CMD_call, comm.CallFunc_ReadLine, string.Empty);
        }
        /// <summary>
        /// 变量转字符串类型：1.特定变量地址压栈，2.将特定类型变量.toString()
        /// </summary>
        /// <param name="vt"></param>
        public void CallFunc_ToString(comm.FWCVariableType vt)
        {
            switch (vt)
            {
                case comm.FWCVariableType.VT_strings:
                case comm.FWCVariableType.VT_string:
                case comm.FWCVariableType.VT_objects:
                case comm.FWCVariableType.VT_object:
                    Add(comm.MSILCMD.CMD_callvirt, string.Format(comm.CallFunc_ToString, comm._MSIL_VariableType[comm.FWCVariableType.VT_object]),string.Empty);
                    break;
                case comm.FWCVariableType.VT_ints:
                case comm.FWCVariableType.VT_int:
                case comm.FWCVariableType.VT_doubles:
                case comm.FWCVariableType.VT_double:
                case comm.FWCVariableType.VT_bools:
                case comm.FWCVariableType.VT_bool:
                    Add(comm.MSILCMD.CMD_call, string.Format(comm.CallFunc_ToString, comm._MSIL_VariableType[vt]),string.Empty);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 调用函数
        /// </summary>
        /// <param name="funcName">函数名称</param>
        /// <param name="returnType">返回值类型</param>
        /// <param name="cmdType">参数类型</param>
        public void CallFunc_Default(string funcName,comm.FWCVariableType returnType,comm.FWCVariableType[] cmdType)
        {
            List<string> tmp = new List<string>();
            int count = cmdType.Length;
            if (count >0)
            {
                foreach(comm.FWCVariableType fvt in cmdType)
                {
                    tmp.Add(comm.DVariableType[fvt]);
                }
            }
            string value = string.Format(comm.CallFunc_Default,comm.DVariableType[returnType],funcName,string.Join(",",tmp));
            Add(comm.MSILCMD.CMD_call,value, string.Empty);
        }
        /// <summary>
        /// 函数返回
        /// </summary>
        /// <param name="RtnStackValue">是否返回栈顶值</param>
        public void Func_ReturnValue(bool RtnStackValue)
        {
            if (RtnStackValue == true)
            {
                PrintSleMSILCMD("pop");
            }
            PrintSleMSILCMD("ret");
        }
    }
    /// <summary>
    /// 跳转语句MSIL结构:if、for、foreach、while、dowhile
    /// </summary>
    public class BrCmdStruct : BaseStruct
    {
        /*       for 示例：(foreach同)
         *  for (int i=0; i<len; i++)
         *  {
         *      printline;
         *  }
         *  
         * IL_****: int i=0;    //条件准备
         * IL_****: br.s    IL__####
         * IL_@@@@: nop
         * IL_****: ｛｝内代码编译
         * IL_****: nop
         * IL_****: i++;    //准备下一循环条件
         * IL_####: nop
         * IL_****: 判断i<len，结果以布尔类型压栈
         * IL_****: brtrue.s    IL_@@@@
         */
        /*       while do_while 示例：
         *  while (条件)
         *  {
         *      printline;
         *  }
         *  
         * IL_****: br.s    IL__####    //do while循环没有这行
         * IL_@@@@: nop
         * IL_****: ｛｝内代码编译
         * IL_####: nop
         * IL_****: 判断i<len，结果以布尔类型压栈
         * IL_****: brtrue.s    IL_@@@@
         */
        /*       if 示例
         *  if (条件一)
         *  {
         *      语句块一
         *  }
         *  else if (条件二)
         *  {
         *      语句块二
         *  }
         *  else
         *  {
         *      语句块三
         *  }
         *  
         * IL_****: 条件一
         * IL_****: brtrue.s    IL_####
         * IL_****: 语句块一
         * IL_****: br.s    IL_$$$$
         * IL_####: nop
         * IL_****: 条件二
         * IL_****: brtrue.s    IL_@@@@
         * IL_****: 语句块二
         * IL_****: br.s    IL_$$$$
         * IL_@@@@: nop
         * IL_****: 语句块三
         * IL_$$$$: nop
         */
        #region Private Ms
        #endregion
        #region Public Ms
        
        /// <summary>
        /// 增加跳转指令
        /// </summary>
        /// <param name="expoprtype"></param>
        public void AddJmpOperator(comm.MSILCMD jmpcmd, int index)
        {
            comm.MSILCMD tmp;
            switch (jmpcmd)
            {
                case comm.MSILCMD.CMD_br_s:
                case comm.MSILCMD.CMD_brtrue_s:
                case comm.MSILCMD.CMD_brfalse_s:
                    tmp = jmpcmd;
                    break;
                default:
                    throw new Exception(string.Format("Debug:{0}指令错误。",jmpcmd.ToString()));
            }
            Add(tmp, commFunc.CgeMSILIndex(index), string.Empty);
        }
        #endregion
    }
    /// <summary>
    /// 运算式MSIL结构
    /// </summary>
    public class ExpStruct : BaseStruct
    {
        /// <summary>
        /// 增加操作符对应MSIL指令
        /// </summary>
        /// <param name="expoprtype"></param>
        public void AddExpOperator(comm.ExpOperatorType expoprtype)
        {
            comm.MSILCMD tmp;
            switch (expoprtype)
            {
                case comm.ExpOperatorType.Plus:
                    tmp = comm.MSILCMD.CMD_add;
                    break;
                case comm.ExpOperatorType.Subtract:
                    tmp = comm.MSILCMD.CMD_sub;
                    break;
                case comm.ExpOperatorType.Multiply:
                    tmp = comm.MSILCMD.CMD_mul;
                    break;
                case comm.ExpOperatorType.Divide:
                    tmp = comm.MSILCMD.CMD_div;
                    break;
                case comm.ExpOperatorType.Mod:
                    tmp = comm.MSILCMD.CMD_rem;
                    break;
                case comm.ExpOperatorType.RThan:
                    tmp = comm.MSILCMD.CMD_cgt;
                    break;
                case comm.ExpOperatorType.LThan:
                    tmp = comm.MSILCMD.CMD_clt;
                    break;
                case comm.ExpOperatorType.Equal:
                    tmp = comm.MSILCMD.CMD_ceq;
                    break;
                default:
                    return;
            }
            Add(tmp, string.Empty, string.Empty);
        }
    }
    #endregion
}
