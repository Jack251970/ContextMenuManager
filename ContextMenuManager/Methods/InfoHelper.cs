using System;
using System.Reflection;

namespace ContextMenuManager.Methods
{
    internal class InfoHelper
    {
        public static Version ProductVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version();

        public static string CompanyName
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                return attributes.Length == 0 ? string.Empty : ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
    }
}
