using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DBAssist
{
    /// <summary>
    /// Interaction logic for InformationWindow.xaml
    /// </summary>
    public partial class InformationWindow : Window
    {
        Model m_model;
        List<string> hidden;
        public List<string> SystemAccounts;
        string[] seperator = new string[] { "\n" };
        string CourseName;
        public InformationWindow(Window _owner, string _server, string _db, Model _model, string course_name)
        {
            InitializeComponent();
            CourseName = course_name;
            SystemAccounts = new List<string>() { "sys", "guest", "INFORMATION_SCHEMA", "dbo" };
            hidden = new List<string>();
            this.Owner = _owner;
            txt_server.Text += " " + _server;
            txt_db.Text += " " + _db;
            m_model = _model;
            db_authorized.Text = m_model.GetAuthorized(_server,_db);
            CheckBox_Click(this, new RoutedEventArgs());
        }
        /// <summary>
        /// on checking "hide system accounts" this will hide the system accounts
        /// from "SystemAccounts" list writen in this window constracture and show them when unchecked 
        /// </summary>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            string[] accounts = db_authorized.Text.Split(seperator, System.StringSplitOptions.RemoveEmptyEntries);
            if ((hide_sys_account).IsChecked.Value)
            {
                hidden.Clear();
                for (int i=0;i<accounts.Length;i++)
                {
                    if(SystemAccounts.Contains(accounts[i]))
                    {
                        hidden.Add(accounts[i]);
                        accounts[i] = string.Empty; 
                    }
                }
                accounts = accounts.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            }
            else
            {
                foreach (string name in accounts)
                {
                    hidden.Add(name);
                }
                accounts = hidden.ToArray();
            }
            db_authorized.Text = String.Join("\n", accounts);
        }
        /// <summary>
        /// saves shown database information into read only test file
        /// </summary>
        private void SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            m_model.SaveToFile(CourseName, txt_server.Text, txt_db.Text, db_authorized.Text.Split(seperator, StringSplitOptions.RemoveEmptyEntries));
            this.Close();
        }
    }
}
