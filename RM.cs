using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;

namespace ResxPropertiesBuilder
{
    class RM
    {
        private static RM loader;
        private ResourceManager resources;

        internal RM()
        {
            Assembly assembly = this.GetType().Assembly;
            this.resources = new ResourceManager(assembly.GetName().Name + ".Cultures.Resources", assembly);
        }

        private static RM GetLoader()
        {
            if (RM.loader == null)
            {
                RM sr = new RM();
                Interlocked.CompareExchange<RM>(ref RM.loader, sr, (RM)null);
            }
            return RM.loader;
        }

        private static CultureInfo Culture
        {
            get => CultureInfo.CurrentCulture;
        }

        public static ResourceManager Resources => RM.GetLoader().resources;

        public static string GetString(string name, params object[] args)
        {
            RM loader = RM.GetLoader();

            if (loader == null)
                return (string)null;

            string format = loader.resources.GetString(name, RM.Culture);

            if (args == null || args.Length == 0)
                return format;

            for (int index = 0; index < args.Length; ++index)
            {
                if (args[index] is string str1 && str1.Length > 1024)
                    args[index] = (object)(str1.Substring(0, 1021) + "...");
            }

            return string.Format((IFormatProvider)CultureInfo.CurrentCulture, format, args);
        }

        public static string GetString(string name) => RM.GetLoader()?.resources.GetString(name, RM.Culture);

        public static string GetString(string name, out bool usedFallback)
        {
            usedFallback = false;
            return RM.GetString(name);
        }

        public static object GetObject(string name) => RM.GetLoader()?.resources.GetObject(name, RM.Culture);
    }
}
