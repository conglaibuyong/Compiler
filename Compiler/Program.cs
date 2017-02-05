using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CodeParser;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            StringBuilder sber = new StringBuilder();
            if (true)//args.Length == 2)
            {
                try
                {
                    if (CodeParser.CodeParser.ReadCodeFile(@"E:\code.txt"))//E:\code.txt\\args[0]
                    {
                        sber.AppendLine(CodeParser.CodeParser.ILFileHead());
                        sber.AppendLine(CodeParser.CodeParser.CompileILDefinePublic());
                        sber.AppendLine(CodeParser.CodeParser.CompileILMainFunc());
                        Console.WriteLine(sber.ToString());
                        commFunc.WriteMSILFile(sber.ToString(), @"E:\code.il");//E:\code.il\\args[1]
                        Console.WriteLine("IL_Out_OK!");
                    }
                    else
                    {
                        Console.WriteLine("读取文件失败！");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
            else
            {
                Console.WriteLine("Compiler DesignBy:FWC");
                Console.WriteLine("Anykey to EXIT...");   
            }
            Console.ReadKey();
        }
    }
}
