using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;

namespace CodeParser
{
    public static class comm
    {
        #region MSIL文件头结构
        /// <summary>
        /// 外部引用的程序集信息
        /// </summary>
        public static readonly string AssemblyExternMscorlib = @".assembly extern mscorlib{}";
        /// <summary>
        /// 定义程序集版本信息
        /// </summary>
        public static readonly string ProgramVersions = @".assembly FWC{.ver 0:9:1:9}";
        /// <summary>
        /// 定义本程序集的模块
        /// </summary>
        public static readonly string ProgramModule = @".module code.exe";

        #endregion
        #region MSIL语法结构
        /// <summary>
        /// 定义Main函数头结构
        /// </summary>
        public static readonly string MainFunctionHead = @".method static void  Main({0}) cil managed";
        /// <summary>
        /// 定义方法函数头结构
        /// </summary>
        public static readonly string otherFunctionHead = @".method static {0} {1}({2}) cil managed";
        /// <summary>
        /// 定义Main函数入口点
        /// </summary>
        public static readonly string MainFunctionEntryPoint = @".entrypoint";
        /// <summary>
        /// 执行方法时操作数堆栈上的项的最大数目
        /// </summary>
        public static readonly string FunctionMaxStack = @".maxstack {0}";
        /// <summary>
        /// 定义全局变量语法结构
        /// </summary>
        public static readonly string PublicVariable = @".field public static {0} {1}";
        /// <summary>
        /// 定义局部变量语法结构
        /// </summary>
        public static readonly string LocalsVariable = @".locals init ({0})";
        public static readonly string LVariableEnum = @"[{0}] {1} {2}";
        /// <summary>
        /// 调用Console::WriteLine
        /// </summary>
        public static readonly string CallFunc_WriteLine = @"void [mscorlib]System.Console::WriteLine({0})";
        /// <summary>
        /// 调用Console::Write
        /// </summary>
        public static readonly string CallFunc_Write = @"void [mscorlib]System.Console::Write({0})";
        /// <summary>
        /// 调用Console::ReadLine
        /// </summary>
        public static readonly string CallFunc_ReadLine = @"string [mscorlib]System.Console::ReadLine()";
        /// <summary>
        /// 调用Console::Read
        /// </summary>
        public static readonly string CallFunc_Read = @"int32 [mscorlib]System.Console::Read()";
        /// <summary>
        /// 调用ToString
        /// </summary>
        public static readonly string CallFunc_ToString = @"instance string {0}::ToString()";
        /// <summary>
        /// 调用自定义函数
        /// </summary>
        public static readonly string CallFunc_Default = @"{0} {1}({2})";

        #endregion

        #region 正则表达式
        /// <summary>
        /// 匹配当前行正则式
        /// 启用Singleline模式
        /// </summary>
        public static readonly string Reg_GetLineStr = @"[^;]+;";
        /// <summary>
        /// 匹配Int型正则式
        /// </summary>
        public static readonly string Reg_ReturnType_Int = @"^\d+$";
        /// <summary>
        /// 匹配Double型正则式
        /// </summary>
        public static readonly string Reg_ReturnType_Double = @"^\d+(?:\.)?\d+$";
        /// <summary>
        /// 匹配String型正则式
        /// </summary>
        public static readonly string Reg_ReturnType_String = @"^"".*""$";
        /// <summary>
        /// 匹配变量类型
        /// </summary>
        public static readonly string Reg_VT = @"^(?<VT>int|double|string|bool|object)(?:\[(?<ArrNum>[1-9]\d*)\])?$";
        /// <summary>
        /// 匹配变量名称
        /// </summary>
        public static readonly string Reg_VName = @"^(?<vName>[a-z|A-Z|_][a-z|A-Z|0-9|_]*)(?:\[(?<ArrNum>[^]]*)\])?$";
        /// <summary>
        /// 匹配赋值语句
        /// </summary>
        public static readonly string Reg_EqCode = @"(?<vName>[a-z|A-Z|_][a-z|A-Z|0-9|_]*)(?:\[(?<ArrNum>[^]]*)\])?(?:\s=\s(?<vValue>[^;]+));";
        /// <summary>
        /// 匹配全局变量正则式
        /// </summary>
        public static readonly string Reg_PublicVT = @"public\s(?<pvVT>int|double|string|bool|object)(?:\[(?<pvArrNum>[1-9]\d*)\])?\s(?<pvName>[a-z|A-Z|_][a-z|A-Z|0-9|_]*)(?:\s=\s(?<pvValue>[^;]+))?;";
        /// <summary>
        /// 匹配局部变量正则式
        /// </summary>
        public static readonly string Reg_LocalVT = @"(?<lvVT>int|double|string|bool|object)(?:\[(?<lvArrNum>[1-9]\d*)\])?\s(?<lvName>[a-z|A-Z|_][a-z|A-Z|0-9|_]*)(?:\s=\s(?<lvValue>[^;]+))?;";
        /// <summary>
        /// 匹配函数正则式
        /// (?<funcReturn>[a-z|A-Z|\[|\]]+)\s(?<funcName>\w+)\((?<funcCMD>[a-z|A-Z|0-9|\[|\]|,|\s]+)?\)\s+{\s+(?<funcCode>[^{}]+(?:(?:(?'Stack'{)[^{}]*)+(?:(?'-Stack'})[^{}]*)+)*(?(Stack)(?!)))?\s+}
        /// </summary>
        public static readonly string Reg_Function = @"(?<funcReturn>int\[\]|double\[\]|string\[\]|bool\[\]|object\[\]|int|double|string|bool|object|void)\s(?<funcName>[a-z|A-Z|_][a-z|A-Z|0-9|_]*)\((?<funcCMD>(?:int\[\]|double\[\]|string\[\]|bool\[\]|object\[\]|int|double|string|bool|object)\s[a-z|A-Z|_][a-z|A-Z|0-9|_]*(?:,\s?(?:int\[\]|double\[\]|string\[\]|bool\[\]|object\[\]|int|double|string|bool|object)\s[a-z|A-Z|_][a-z|A-Z|0-9|_]*)*)?\)\s+{\s+(?<funcCode>[^{}]+(?:(?:(?'Stack'{)[^{}]*)+(?:(?'-Stack'})[^{}]*)+)*(?(Stack)(?!)))?\s+}";
        #endregion

        #region 枚举与字典
        /// <summary>
        /// 设定变量枚举类型
        /// </summary>
        public enum FWCVariableType
        {
            //常用
            VT_int,
            VT_double,
            VT_string,
            VT_bool,
            VT_object,
            //一维数组
            VT_ints,
            VT_doubles,
            VT_strings,
            VT_bools,
            VT_objects,
            //（保留）
            VT_void
        }
        /// <summary>
        /// Code类型转化为枚举类型
        /// </summary>
        public static Dictionary<string, FWCVariableType> CVariableType = new Dictionary<string, FWCVariableType>()
        {
            {"int",FWCVariableType.VT_int},
            {"int[]",FWCVariableType.VT_ints},            
            {"int32",FWCVariableType.VT_int},
            {"int32[]",FWCVariableType.VT_ints},

            {"double",FWCVariableType.VT_double},
            {"double[]",FWCVariableType.VT_doubles},
            {"float64",FWCVariableType.VT_double},
            {"float64[]",FWCVariableType.VT_doubles},

            {"string",FWCVariableType.VT_string},
            {"string[]",FWCVariableType.VT_strings},

            {"bool",FWCVariableType.VT_bool},
            {"bool[]",FWCVariableType.VT_bools},

            {"object",FWCVariableType.VT_object},
            {"object[]",FWCVariableType.VT_objects},

            {"void",FWCVariableType.VT_void}

        };
        /// <summary>
        /// 枚举类型转MSIL中类型
        /// </summary>
        public static Dictionary<FWCVariableType, string> DVariableType = new Dictionary<FWCVariableType, string>()
         {             
             {FWCVariableType.VT_void,"void"},

             {FWCVariableType.VT_int,"int32"},
             {FWCVariableType.VT_double,"float64"},
             {FWCVariableType.VT_string,"string"},
             {FWCVariableType.VT_bool,"bool"},
             {FWCVariableType.VT_object,"object"},
             //一维数组
             {FWCVariableType.VT_ints,"int32[]"},
             {FWCVariableType.VT_doubles,"float64[]"},
             {FWCVariableType.VT_strings,"string[]"},
             {FWCVariableType.VT_bools,"bool[]"},
             {FWCVariableType.VT_objects,"object[]"}
         };
        public static Dictionary<FWCVariableType, string> _MSIL_VariableType = new Dictionary<FWCVariableType, string>()
        {
             {FWCVariableType.VT_int,"[mscorlib]System.Int32"},
             {FWCVariableType.VT_double,"[mscorlib]System.Double"},
             {FWCVariableType.VT_bool,"[mscorlib]System.Boolean"},
             {FWCVariableType.VT_string,"[mscorlib]System.String"},
             {FWCVariableType.VT_object,"[mscorlib]System.Object"},
             //newarr
             {FWCVariableType.VT_ints,"[mscorlib]System.Int32"},
             {FWCVariableType.VT_doubles,"[mscorlib]System.Double"},
             {FWCVariableType.VT_bools,"[mscorlib]System.Boolean"},
             {FWCVariableType.VT_strings,"[mscorlib]System.String"},
             {FWCVariableType.VT_objects,"[mscorlib]System.Object"}
        };
        /// <summary>
        /// 枚举MSIL指令
        /// </summary>
        public enum MSILCMD
        {
            /// <summary>
            /// 填充意义
            /// </summary>
            CMD_nop,
            /// <summary>
            /// 移除当前计算栈顶的值 用于返回void或其他
            /// </summary>
            CMD_pop,
            /// <summary>
            /// 从当前方法返回，将当前计算栈顶值（如果存在的话）返回给调用方计算栈顶
            /// </summary>
            CMD_ret,
            /// <summary>
            /// 调用方法
            /// </summary>
            CMD_call,
            /// <summary>
            /// 对对象调用后期绑定方法
            /// </summary>
            CMD_callvirt,
            /// <summary>
            /// 用来自栈顶的值替换静态字段的值 用于全局赋值
            /// </summary>
            CMD_stsfld,
            /// <summary>
            /// 用来自静态字段的值压入当前计算栈
            /// </summary>
            CMD_ldsfld,
            /// <summary>
            /// 用来自静态字段的变量地址压入当前计算栈
            /// </summary>
            CMD_ldsflda,
            /// <summary>
            /// 将int32值作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4,
            /// <summary>
            /// 将整数值0作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4_0,
            /// <summary>
            /// 将整数值1作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4_1,
            /// <summary>
            /// 将整数值2作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4_2,
            /// <summary>
            /// 将整数值3作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4_3,
            /// <summary>
            /// 将整数值4作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4_4,
            /// <summary>
            /// 将整数值5作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4_5,
            /// <summary>
            /// 将整数值6作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4_6,
            /// <summary>
            /// 将整数值7作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4_7,
            /// <summary>
            /// 将整数值8作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4_8,
            /// <summary>
            /// 将整数值-1作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4_m1,
            /// <summary>
            /// 将int8值作为int32类型值压栈
            /// </summary>
            CMD_ldc_i4_s,
            /// <summary>
            /// 将float64值作为float64类型值压栈
            /// </summary>
            CMD_ldc_r8,
            /// <summary>
            /// 对元数据中存储的字符串的新对象引用压栈
            /// </summary>
            CMD_ldstr,
            /// <summary>
            /// 将计算栈顶值赋值给局部变量表索引项
            /// </summary>
            CMD_stloc_s,
            /// <summary>
            /// 将计算栈顶值赋值给局部变量表索引0顶
            /// </summary>
            CMD_stloc_0,
            /// <summary>
            ///  将计算栈顶值赋值给局部变量表索引1顶
            /// </summary>
            CMD_stloc_1,
            /// <summary>
            ///  将计算栈顶值赋值给局部变量表索引2顶
            /// </summary>
            CMD_stloc_2,
            /// <summary>
            ///  将计算栈顶值赋值给局部变量表索引3顶
            /// </summary>
            CMD_stloc_3,
            /// <summary>
            /// 将特定索引处局部变量压栈
            /// </summary>
            CMD_ldloc_s,
            /// <summary>
            /// 将局部变量表索引0项压栈
            /// </summary>
            CMD_ldloc_0,
            /// <summary>
            /// 将局部变量表索引1项压栈
            /// </summary>
            CMD_ldloc_1,
            /// <summary>
            /// 将局部变量表索引2项压栈
            /// </summary>
            CMD_ldloc_2,
            /// <summary>
            /// 将局部变量表索引3项压栈
            /// </summary>
            CMD_ldloc_3,
            /// <summary>
            /// 将特定变量地址压栈
            /// </summary>
            CMD_ldloca_s,
            /// <summary>
            /// 装箱 值类型转引用类型
            /// </summary>
            CMD_box,
            /// <summary>
            /// 拆箱  将值类型的装箱形式还原成未装箱形式
            /// </summary>
            CMD_unbox_any,
            /// <summary>
            /// 把新创建的索引从零开始的一维数组的引用压栈
            /// </summary>
            CMD_newarr,
            /// <summary>
            /// 用栈顶上int8类型值替换数组索引处元素
            /// </summary>
            CMD_stelem_i1,
            /// <summary>
            /// 用栈顶上int32类型值替换数组索引处元素
            /// </summary>
            CMD_stelem_i4,
            /// <summary>
            /// 用栈顶上float64类型值替换数组索引处元素
            /// </summary>
            CMD_stelem_r8,
            /// <summary>
            /// 用栈顶上ref值替换数组索引处元素
            /// </summary>
            CMD_stelem_ref,
            /// <summary>
            /// 将数组索引处int8值作为int32类型值压栈
            /// </summary>
            CMD_ldelem_i1,
            /// <summary>
            /// 将数组索引处int32值作为int32类型值压栈
            /// </summary>
            CMD_ldelem_i4,
            /// <summary>
            /// 将数组索引处float64值作为float64类型值压栈
            /// </summary>
            CMD_ldelem_r8,
            /// <summary>
            /// 将数组索引处引用作为引用值压栈
            /// </summary>
            CMD_ldelem_ref,
            /// <summary>
            /// 将位于指定数组索引的数组元素的地址作为托管指针类型加载到计算堆栈的顶部
            /// </summary>
            CMD_ldelema,
            /// <summary>
            /// 无条件跳转至指定的目标指令
            /// </summary>
            CMD_br_s,
            /// <summary>
            /// 栈顶为真，则跳转至指定的目标指令
            /// </summary>
            CMD_brtrue_s,
            /// <summary>
            /// 栈顶为假，则跳转至指定的目标指令
            /// </summary>
            CMD_brfalse_s,
            /// <summary>
            /// 加
            /// </summary>
            CMD_add,
            /// <summary>
            /// 减
            /// </summary>
            CMD_sub,
            /// <summary>
            /// 乘
            /// </summary>
            CMD_mul,
            /// <summary>
            /// 除
            /// </summary>
            CMD_div,
            /// <summary>
            /// 取模
            /// </summary>
            CMD_rem,
            /// <summary>
            /// 比较两个值。如果这两个值相等，则将整数值 1 推送到计算堆栈上；否则，将 0 推送到计算堆栈上
            /// </summary>
            CMD_ceq,
            /// <summary>
            /// 比较两个值。如果第一个值大于第二个值，则将整数值 1  推送到计算堆栈上；反之，将 0 推送到计算堆栈上。
            /// </summary>
            CMD_cgt,
            /// <summary>
            /// 比较两个值。如果第一个值小于第二个值，则将整数值 1 推送到计算堆栈上；反之，将 0  推送到计算堆栈上
            /// </summary>
            CMD_clt,
            /// <summary>
            /// 栈顶值转换为 float64
            /// </summary>
            CMD_conv_r8,
            /// <summary>
            /// 取参数表某项压栈
            /// </summary>
            CMD_ldarg_s,
            /// <summary>
            /// 取参数表某项压栈
            /// </summary>
            CMD_ldarg_0,
            /// <summary>
            /// 取参数表某项压栈
            /// </summary>
            CMD_ldarg_1,
            /// <summary>
            /// 取参数表某项压栈
            /// </summary>
            CMD_ldarg_2,
            /// <summary>
            /// 取参数表某项压栈
            /// </summary>
            CMD_ldarg_3
        }
        /// <summary>
        /// 枚举转MSIL指令对应
        /// </summary>
        public static Dictionary<MSILCMD, string> DMSILCMD = new Dictionary<MSILCMD, string>()
        {
            {MSILCMD.CMD_nop,"nop"},
            {MSILCMD.CMD_pop,"pop"},
            {MSILCMD.CMD_ret,"ret"},
            {MSILCMD.CMD_call,"call"},
            {MSILCMD.CMD_callvirt,"callvirt"},
            {MSILCMD.CMD_stsfld,"stsfld"},
            {MSILCMD.CMD_ldsfld,"ldsfld"},
            {MSILCMD.CMD_ldsflda,"ldsflda"},
            {MSILCMD.CMD_ldc_i4,"ldc.i4"},
            {MSILCMD.CMD_ldc_i4_0,"ldc.i4.0"},
            {MSILCMD.CMD_ldc_i4_1,"ldc.i4.1"},
            {MSILCMD.CMD_ldc_i4_2,"ldc.i4.2"},
            {MSILCMD.CMD_ldc_i4_3,"ldc.i4.3"},
            {MSILCMD.CMD_ldc_i4_4,"ldc.i4.4"},
            {MSILCMD.CMD_ldc_i4_5,"ldc.i4.5"},
            {MSILCMD.CMD_ldc_i4_6,"ldc.i4.6"},
            {MSILCMD.CMD_ldc_i4_7,"ldc.i4.7"},
            {MSILCMD.CMD_ldc_i4_8,"ldc.i4.8"},
            {MSILCMD.CMD_ldc_i4_m1,"ldc.i4.m1"},
            {MSILCMD.CMD_ldc_i4_s,"ldc.i4.s"},            
            {MSILCMD.CMD_ldc_r8,"ldc.r8"},            
            {MSILCMD.CMD_ldstr,"ldstr"},
            {MSILCMD.CMD_stloc_s,"stloc.s"},
            {MSILCMD.CMD_stloc_0,"stloc.0"},
            {MSILCMD.CMD_stloc_1,"stloc.1"},
            {MSILCMD.CMD_stloc_2,"stloc.2"},
            {MSILCMD.CMD_stloc_3,"stloc.3"},
            {MSILCMD.CMD_ldloc_s,"ldloc.s"},
            {MSILCMD.CMD_ldloc_0,"ldloc.0"},
            {MSILCMD.CMD_ldloc_1,"ldloc.1"},
            {MSILCMD.CMD_ldloc_2,"ldloc.2"},
            {MSILCMD.CMD_ldloc_3,"ldloc.3"},
            {MSILCMD.CMD_ldloca_s,"ldloca.s"},
            {MSILCMD.CMD_box,"box"},
            {MSILCMD.CMD_unbox_any,"unbox.any"},
            {MSILCMD.CMD_newarr,"newarr"},
            {MSILCMD.CMD_stelem_i1,"stelem.i1"},
            {MSILCMD.CMD_stelem_i4,"stelem.i4"},
            {MSILCMD.CMD_stelem_r8,"stelem.r8"},
            {MSILCMD.CMD_stelem_ref,"stelem.ref"},
            {MSILCMD.CMD_ldelem_i1,"ldelem.i1"},
            {MSILCMD.CMD_ldelem_i4,"ldelem.i4"},
            {MSILCMD.CMD_ldelem_r8,"ldelem.r8"},
            {MSILCMD.CMD_ldelem_ref,"ldelem.ref"},
            {MSILCMD.CMD_ldelema,"ldelema"},
            {MSILCMD.CMD_br_s,"br.s"},
            {MSILCMD.CMD_brtrue_s,"brtrue.s"},
            {MSILCMD.CMD_brfalse_s,"brfalse.s"},
            {MSILCMD.CMD_add,"add"},
            {MSILCMD.CMD_sub,"sub"},
            {MSILCMD.CMD_mul,"mul"},
            {MSILCMD.CMD_div,"div"},
            {MSILCMD.CMD_rem,"rem"},
            {MSILCMD.CMD_ceq,"ceq"},
            {MSILCMD.CMD_clt,"clt"},
            {MSILCMD.CMD_cgt,"cgt"},
            {MSILCMD.CMD_conv_r8,"conv.r8"},
            {MSILCMD.CMD_ldarg_0,"ldarg.0"},
            {MSILCMD.CMD_ldarg_1,"ldarg.1"},
            {MSILCMD.CMD_ldarg_2,"ldarg.2"},
            {MSILCMD.CMD_ldarg_3,"ldarg.3"},
            {MSILCMD.CMD_ldarg_s,"ldarg.s"}
        };
        /// <summary>
        /// 取记号类型
        /// </summary>
        public enum TokenType
        {
            /// <summary>
            /// 无用的头标志
            /// </summary>
            Head,
            /// <summary>
            /// 无用的尾标志
            /// </summary>
            Tail,
            /// <summary>
            /// 操作符
            /// </summary>
            ExpOperator,
            /// <summary>
            /// 操作数
            /// </summary>
            ExpOperand,
            /// <summary>
            /// 功能函数
            /// </summary>
            ExpFuncKeyWord
        }

        /// <summary>
        /// 功能函数关键字
        /// </summary>
        public enum FuncKeyWord
        {
            printline,
            print,
            inputline,
            input,
            tostring,
            ret
        }
        /// <summary>
        /// 功能函数关键字与枚举格式对应
        /// </summary>
        public static Dictionary<string, FuncKeyWord> CallFunctionDic1 = new Dictionary<string, FuncKeyWord>()
        {
            {"print",FuncKeyWord.print},
            {"printline",FuncKeyWord.printline},
            {"input",FuncKeyWord.input},
            {"inputline",FuncKeyWord.inputline},
            {"tostring",FuncKeyWord.tostring},
            {"ret",FuncKeyWord.ret}
        };
        /// <summary>
        /// 关键词
        /// </summary>
        public enum ChkKeyWord
        {
            IF,
            ELSEIF,
            ELSE,
            FOR,
            WHILE,
            DO
        }
        /// <summary>
        /// 运算式操作符
        /// </summary>
        public enum ExpOperatorType
        {
            /// <summary>
            /// 加
            /// </summary>
            Plus,
            /// <summary>
            /// 减
            /// </summary>
            Subtract,
            /// <summary>
            /// 乘
            /// </summary>
            Multiply,
            /// <summary>
            /// 除
            /// </summary>
            Divide,
            /// <summary>
            /// 取模
            /// </summary>
            Mod,
            /// <summary>
            /// 左括号
            /// </summary>
            LBrace,
            /// <summary>
            /// 右括号
            /// </summary>
            RBrace,
            /// <summary>
            /// 小于
            /// </summary>
            LThan,
            /// <summary>
            /// 大于
            /// </summary>
            RThan,
            /// <summary>
            /// 等于=
            /// </summary>
            Equal,
            /// <summary>
            /// 否 ！
            /// </summary>
            Not,
            /// <summary>
            /// 与
            /// </summary>
            And,
            /// <summary>
            /// 或
            /// </summary>
            Or,
            /// <summary>
            ///逗号
            /// </summary>
            Coa
        }        
        /// <summary>
        /// 运算操作符与操作结构对应
        /// </summary>
        public static Dictionary<ExpOperatorType, ExpOperator> ExpOpTypeDry = new Dictionary<ExpOperatorType, ExpOperator>()
        {
            //算术运算
            {ExpOperatorType.Plus,new ExpOperator(ExpOperatorType.Plus,"+",6)},
            {ExpOperatorType.Subtract,new ExpOperator(ExpOperatorType.Subtract,"-",6)},
            {ExpOperatorType.Multiply,new ExpOperator(ExpOperatorType.Multiply,"*",7)},
            {ExpOperatorType.Divide,new ExpOperator(ExpOperatorType.Divide,"/",7)},
            {ExpOperatorType.Mod,new ExpOperator(ExpOperatorType.Mod,"%",7)},
            //逻辑运算
            {ExpOperatorType.Equal,new ExpOperator(ExpOperatorType.Equal,"=",2)},
            {ExpOperatorType.LThan,new ExpOperator(ExpOperatorType.LThan,"<",2)},
            {ExpOperatorType.RThan,new ExpOperator(ExpOperatorType.RThan,">",2)},
            {ExpOperatorType.Not,new ExpOperator(ExpOperatorType.Not,"!",8)},
            {ExpOperatorType.And,new ExpOperator(ExpOperatorType.And,"&",4)},
            {ExpOperatorType.Or,new ExpOperator(ExpOperatorType.Or,"|",3)},

            {ExpOperatorType.LBrace,new ExpOperator(ExpOperatorType.LBrace,"(",1)},
            {ExpOperatorType.RBrace,new ExpOperator(ExpOperatorType.RBrace,")",9)},

            {ExpOperatorType.Coa,new ExpOperator(ExpOperatorType.Coa,",",0)}
        };
        /// <summary>
        /// 有限状态自动机 
        /// </summary>
        public enum DFAState
        {
            /// <summary>
            /// 初态
            /// </summary>
            Start,
            /// <summary>
            /// 整数串：整型数
            /// </summary>
            IntStr,
            /// <summary>
            /// 浮点数串：浮点型数
            /// </summary>
            DoubleStr,
            /// <summary>
            /// 字符串 有引号：字符串值
            /// </summary>
            StringStr,
            /// <summary>
            /// 字符串 无引号：变量，布尔值，函数名
            /// </summary>
            CharStr,
            /// <summary>
            /// 操作符：运算符
            /// </summary>
            OperatorStr//,
            /// <summary>
            /// 逗号
            /// </summary>
            //Comma
        }        
        #endregion
    }

    public class commFunc
    {
        #region 进制
        /// <summary>
        /// 10进制转16进制
        /// </summary>
        /// <param name="decimalValue"></param>
        /// <returns></returns>
        public static string ConvertToHex(int decimalValue)
        {
            return Convert.ToString(decimalValue, 16);
        }
        /// <summary>
        /// 赋值时使用
        /// </summary>
        /// <param name="decimalValue"></param>
        /// <returns></returns>
        public static string GetHexValue(int decimalValue)
        {
            return string.Format("0x{0}", ConvertToHex(decimalValue));
        }
        /// <summary>
        /// MSIL序列号
        /// </summary>
        /// <param name="decimalValue"></param>
        /// <returns></returns>
        public static string GetMSILIndex(int decimalValue)
        {
            string tmp = ConvertToHex(decimalValue);
            if (tmp.Length < 4)
                tmp = "IL_" + new string('0', 4 - tmp.Length) + tmp;
            return tmp + ":";
        }
        public static string CgeMSILIndex(int decimalValue)
        {
            string tmp = ConvertToHex(decimalValue);
            if (tmp.Length < 4)
                tmp = "IL_" + new string('0', 4 - tmp.Length) + tmp;
            return tmp;
        }
        #endregion

        #region 读写文件
        /// <summary>
        /// 读文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ReadFileToEnd(string path)
        {
            string con = string.Empty;
            try
            {
                using (StreamReader fs = new StreamReader(path))
                {
                    con = fs.ReadToEnd();
                }
            }
            catch
            {
                throw;
            }
            return con;
        }
        /// <summary>
        /// 写文件
        /// </summary>
        /// <param name="con"></param>
        /// <param name="path"></param>
        public static void WriteMSILFile(string con, string path)
        {
            try
            {
                using (StreamWriter fs = new StreamWriter(path))
                {
                    fs.Write(con);
                }
            }
            catch
            {
                throw;
            }
        }
        #endregion

        #region 正则

        /// <summary>
        /// 匹配符合正则的所有项
        /// </summary>
        /// <param name="con"></param>
        /// <param name="regExp"></param>
        /// <returns></returns>
        public static MatchCollection RegexMatches(string con, string regExp)
        {
            Regex rgx = new Regex(regExp);

            return rgx.Matches(con);
        }
        /// <summary>
        /// 是否匹配
        /// </summary>
        /// <param name="con"></param>
        /// <param name="regExp"></param>
        /// <returns></returns>
        public static bool RegexIsMatch(string con, string regExp)
        {
            Regex rgx = new Regex(regExp);

            return rgx.IsMatch(con);
        }
        /// <summary>
        /// 匹配第一个符合正则规则的Str
        /// </summary>
        /// <param name="con"></param>
        /// <param name="regExp"></param>
        /// <param name="Result">成功后返回结果</param>
        /// <returns></returns>
        public static bool RegexMatch(string con, string regExp, ref string Result)
        {
            //Multiline模式
            Regex rgx = new Regex(regExp, RegexOptions.Multiline);
            return true;
        }


        #endregion

        #region 其他
        /// <summary>
        /// 根据值返回可能的类型，用于object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ReturnType(string value)
        {
            string type = string.Empty;
            if (value.ToLower() == "true" || value.ToLower() == "false")
            {
                type = "bool";
            }
            else if (RegexIsMatch(value, comm.Reg_ReturnType_Int))
            {
                type = "int";
            }
            else if (RegexIsMatch(value, comm.Reg_ReturnType_Double))
            {
                type = "double";
            }
            else if (RegexIsMatch(value, comm.Reg_ReturnType_String))
            {
                type = "string";
            }
            return type;
        }

        #endregion
    }
}
