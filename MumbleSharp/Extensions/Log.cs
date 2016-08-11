using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MumbleSharp.Extensions
{
    class Log
    {
        const bool enabled = true;

        public static void Write(string message) 
        {
            if (enabled)
                Console.WriteLine(message);
        }

        public static void Info(string message)
        {
            if (enabled)
                Log.Write("Info: " + message);
        }

        public static void Info(string message,object value)
        {
            if (enabled)
                Log.Write("Info: " + message + " - " + ObjectToString(value));
        }

        private static string ObjectToString(object value)
        {
            var stringPropertyNamesAndValues = value.GetType()
                .GetProperties()
                .Where(pi => pi.GetGetMethod() != null)
                .Select(pi => new
                {
                    Name = pi.Name,
                    Value = pi.GetGetMethod().Invoke(value, null)
                });

            StringBuilder builder = new StringBuilder();
            bool first = true;

            foreach (var pair in stringPropertyNamesAndValues)
            {
                if (first) first = false;
                else builder.Append(", ");
                if (pair.Value is string) builder.AppendFormat("{0}: \"{1}\"", pair.Name, pair.Value);
                else builder.AppendFormat("{0}: {1}", pair.Name, pair.Value);
            }

            return builder.ToString();
        }
    }
}
