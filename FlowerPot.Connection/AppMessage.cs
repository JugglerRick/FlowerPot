using System;
using Windows.Foundation.Collections;
using System.Text;

namespace FlowerPot.Connection
{
    public class AppMessage
    {
        private readonly static string commandName = "Command";
        private readonly static string paramName = "Param";

        public enum CommandType
        {
            Unknown,
            Error,
            Play,
            Stop,
            Media,
            State
        };


        public CommandType Command { get; set; }
        public string Param { get; set; }

        public AppMessage() : this(CommandType.Unknown, ""){ }
        public AppMessage(CommandType c) : this(c, "") { }
        public AppMessage(CommandType c, string p)
        {
            Command = c;
            Param = p;
        }

        public void Copy(AppMessage lc)
        {
            Command = lc.Command;
            Param = lc.Param;
        }

        public static AppMessage FromValueSet(ValueSet values)
        {
            AppMessage lc = new AppMessage();

            if (values.ContainsKey(commandName))
            {
                lc.Command = (CommandType)Enum.Parse(typeof(CommandType), values[commandName].ToString());
            }
            if (values.ContainsKey(paramName))
            {
                lc.Param = (string)values[paramName];
            }
            return lc;
        }

 
        public ValueSet ToValueSet()
        {
            ValueSet ret = new ValueSet
            {
                { commandName, Command.ToString() }
            };
            if (!string.IsNullOrEmpty(Param))
            {
                ret.Add(paramName, Param);
            }
            return ret;
        }

        //public void AddToValueSet(out ValueSet set)
        //{
        //    set.Add(commandName, Command.ToString());
        //    if (!string.IsNullOrEmpty(Param))
        //    {
        //        set.Add(paramName, Param);
        //    }
        //}

        public override string ToString()
        {
            StringBuilder jsonStr = new StringBuilder($"{{ {commandName}: {Command.ToString()}");
            if(!string.IsNullOrEmpty(Param))
            {
                jsonStr.Append($", {paramName} : {Param}");
            }
            jsonStr.Append(" }");
            return jsonStr.ToString();
        }
    }

}
