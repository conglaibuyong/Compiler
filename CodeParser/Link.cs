using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeParser
{
    /// <summary>
    /// 运算式操作数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ExpOperand<T> : IExpOperand
    {
        private comm.FWCVariableType _type;
        private T _value;
        private string _name;
        private int _arrIndex;

        public ExpOperand(comm.FWCVariableType fVT, T tValue, string vName, int aIndex)
        {
            _type = fVT;
            _value = tValue;
            _name = vName;
            _arrIndex = aIndex;
        }

        public comm.FWCVariableType Type
        {
            get { return _type; }
        }
        public T Value
        {
            get { return _value; }
        }
        public object oValue
        {
            get { return _value; }
        }
        public string Name
        {
            get { return _name; }
        }
        public int ArrIndex
        {
            get { return _arrIndex; }
        }

        public override string ToString()
        {
            return string.Format("Type:{0},Value:{1},Name:{2},ArrIndex:{3}.", Type.ToString(), Value.ToString(), Name, ArrIndex.ToString());
        }

    }
    public interface IExpOperand
    {
        comm.FWCVariableType Type { get; }
        object oValue { get; }
        string Name { get; }
        int ArrIndex { get; }
    }
    /// <summary>
    /// 运算式操作符
    /// </summary>
    public struct ExpOperator
    {
        private comm.ExpOperatorType _type;
        private string _name;
        private int _priority;

        public ExpOperator(comm.ExpOperatorType type, string name, int pri)
        {
            _type = type;
            _name = name;
            _priority = pri;
        }

        public comm.ExpOperatorType Type
        {
            get { return _type; }
        }
        public string Name
        {
            get { return _name; }
        }
        public int Priority
        {
            get { return _priority; }
        }

        public override string ToString()
        {
            return string.Format("Type:{0},Name:{1},Priority:{2}.", Type.ToString(), Name, Priority.ToString());
        }
    }
    /// <summary>
    /// 取记号结构体
    /// </summary>
    /// <typeparam name="T">操作数或操作符或功能关键词</typeparam>
    public class Token<T> : IToken
    {
        private comm.TokenType _type;
        private T _value;

        public Token(comm.TokenType type, T value)
        {
            _type = type;
            _value = value;
        }

        public comm.TokenType Type
        {
            get { return _type; }
        }
        public T Value
        {
            get { return _value; }
        }
        public object oValue
        {
            get { return _value; }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
    public interface IToken
    {
        comm.TokenType Type { get; }
        object oValue { get; }
    }

    /// <summary>
    /// 链表项
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Link
    {
        #region 私有
        private IToken currentValue;
        private Link prevLink = null;
        private Link nextLink = null;
        #endregion

        #region 外部
        public IToken CurrentValue
        {
            get { return currentValue; }
            set { currentValue = value; }
        }
        public Link PrevLink
        {
            get { return prevLink; }
            set
            {
                if (value != this)
                {
                    prevLink = value;
                }
            }
        }
        public Link NextLink
        {
            get { return nextLink; }
            set
            {
                if (value != this)
                {
                    nextLink = value;
                }
            }
        }
        #endregion
    }

}
