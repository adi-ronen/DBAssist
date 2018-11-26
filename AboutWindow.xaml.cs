using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DBAssist
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow(Window _Owner)
        {
            InitializeComponent();
            this.Owner = _Owner;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            aboutApp.Text = AssemblyProduct + " " + GetPlatform() + "\n";
            aboutApp.Inlines.Add(string.Format("Version {0}", AssemblyVersion) + "\n");
            aboutApp.Inlines.Add(AssemblyCopyright + "\n");
            aboutApp.Inlines.Add(AssemblyCompany);
            aboutDescription.Text = "This software allow to eligible students create Personal and Group new databases and obtain general information about existing database. Databases' names and users' access permissions will apply automatically suring new database creating, according to the metadata provided by supervisor in the configuration XML - file. to technical support contact by Phone: 08-6472196 or by e-Mail: support@som.bgu.ac.il.";
        }
   #region Assembly Attribute Accessors
        private string GetPlatform()
        {
            string Platform;
#if PLATFORM_64
            Platform = "64-bit";
#else
            Platform = "32/64-bit";
#endif
            return Platform;
        }

     

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
