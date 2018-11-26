using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DBAssist
{
    /// <summary>
    /// Main window
    /// </summary>
    public partial class MainWindow : Window
    {
        public Model m_model;
        private string db_type = "";
        public MainWindow()
        {
            InitializeComponent();
            m_model = new Model();
            CourseNamesItemsSource();
            server_name.Text = Properties.Settings.Default.SQLServerName;
        }
        /// <summary>
        /// get the list of courseses 
        /// </summary>
        private void CourseNamesItemsSource()
        {
            List<string> courseses = new List<string>();
            Dictionary<string, string> semesters = new Dictionary<string, string>() { {"A","סתיו"}, { "B", "אביב" } , { "C", "קיץ" } };
            foreach (Course c in m_model.Courses)
            {
                courseses.Add(c._Name + " (" + c._Number + ") - סמסטר " + semesters[c._Semester] + " " + c._Year);
            }
            course_name.ItemsSource = courseses;
            course_name.SelectedIndex = 0;
        }
        /// <summary>
        /// creates a new personal db for the student in the selected course in the selected server.
        /// </summary>
        private bool NewPersonalDB_Click(object sender, RoutedEventArgs e)
        {
            if (m_model.HavePesonalPermissions(course_name.SelectedIndex))
            {
                return CreateDB();
            }
            else
            {
                 return m_model.ShowError("permission", db_type);
            }
        }
        /// <summary>
        /// creates a new group db for the student in the selected course in the selected server.
        /// </summary>
        private bool NewGroupDB_Click(object sender, RoutedEventArgs e)
        {
            string group_name = m_model.HaveGroupPermissions(course_name.SelectedIndex);
            if (group_name != string.Empty)
            {
                return CreateDB(group_name);
            }
            else
            {
                 return m_model.ShowError("permission",db_type);
            }
        }
       
        /// <summary>
        /// creates new database for the user. based on the given server name, course and db type personal or group.
        /// </summary>
        /// <param name="group_name">if its a group, the name of the group else empty string</param>
        private bool CreateDB(string group_name="")
        {
            Mouse.OverrideCursor = Cursors.Wait;
            bool ans;
            string connectionString = m_model.GetDB(course_name.SelectedIndex, rbtn_personal.IsChecked.Value, group_name, server_name.Text);
            if (connectionString == string.Empty)
            {
                connectionString = m_model.CreateDB(course_name.SelectedIndex, rbtn_personal.IsChecked.Value, group_name, server_name.Text);
                if (connectionString == string.Empty)
                {
                    ans = m_model.ShowError("system", db_type);
                }
                else
                    ans = m_model.ShowError("success", db_type);
            }
            else
                ans= m_model.ShowError("exists", db_type);
            Mouse.OverrideCursor = Cursors.Arrow;
            return ans;
        }
        /// <summary>
        /// closes the environment
        /// </summary>
        private void X_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        /// <summary>
        /// opens a new window with programs information and description
        /// </summary>
        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow(this);
            about.Show();
        }
        /// <summary>
        /// change create tooltip with change in the selected course 
        /// </summary>
        private void course_name_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gbx_creating.ToolTip = "Create new SQL database in course name:\n" + m_model.Courses[course_name.SelectedIndex]._Name;
            if(rbtn_group.IsChecked.Value)
                ChangeToolTips("group");
            if (rbtn_personal.IsChecked.Value)
                ChangeToolTips("personal");
        }
        private void CreateDB_Click(object sender, RoutedEventArgs e)
        {
            bool ok = false;
            try
            {
                if (rbtn_personal.IsChecked.Value)
                    ok = NewPersonalDB_Click(sender, new RoutedEventArgs());
                else if (rbtn_group.IsChecked.Value)
                    ok = NewGroupDB_Click(sender, new RoutedEventArgs());
                if (ok)
                    ShowInformation_Click(sender, e);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }
        private void Personal_Checked(object sender, RoutedEventArgs e)
        {
            EnableButtons("personal");
        }
        private void Group_Checked(object sender, RoutedEventArgs e)
        {
            EnableButtons("group");
        }
        private void EnableButtons(string type)
        {
            ChangeToolTips(type);
            btn_create.IsEnabled = true;
            btn_showInformation.IsEnabled = true;
            db_type = type;
        }
        private void ChangeToolTips(string type)
        {
            //btn_backup.ToolTip = "Save a backup of " + type + " SQL database in course name:\n" + m_model.Courses[course_name.SelectedIndex]._Name;
            btn_create.ToolTip = "Create new " + type + " SQL database in course name:\n" + m_model.Courses[course_name.SelectedIndex]._Name;
            btn_showInformation.ToolTip = "Show " + type + " SQL database information in course name:\n" + m_model.Courses[course_name.SelectedIndex]._Name;
        }
        /// <summary>
        /// opens a new Information Window with the connactions information based on the given server name and selected course
        /// </summary>
        private void ShowInformation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                string group_name = "";
                if (rbtn_group.IsChecked.Value)
                    group_name = m_model.HaveGroupPermissions(course_name.SelectedIndex);
                string db_name = m_model.GetDB(course_name.SelectedIndex, rbtn_personal.IsChecked.Value, group_name, server_name.Text);
                if (db_name == string.Empty)
                {
                    m_model.ShowError("not exists", db_type);
                    return;
                }
                InformationWindow iw = new InformationWindow(this, server_name.Text, db_name, m_model, course_name.SelectedItem.ToString());
                Mouse.OverrideCursor = Cursors.Arrow;
                iw.Show();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }
        /* private void Backup_Click(object sender, RoutedEventArgs e)
{
    Mouse.OverrideCursor = Cursors.Wait;
    string group_name = "";
    if (rbtn_group.IsChecked.Value)
        group_name = m_model.HaveGroupPermissions(course_name.SelectedIndex);
    string db_name = m_model.GetDB(course_name.SelectedIndex, rbtn_personal.IsChecked.Value, group_name, server_name.Text);
    if (db_name == string.Empty)
    {
        ShowError("not exists");
        return;
    }
    Mouse.OverrideCursor = Cursors.Arrow;
    SaveFileDialog saveFileDialog = new SaveFileDialog();
    saveFileDialog.Filter = "Backup file (*.bak)|*.bak";
    saveFileDialog.FileName = db_name + " Backup";
    if (saveFileDialog.ShowDialog() == true)
    {
        Mouse.OverrideCursor = Cursors.Wait;
        m_model.Backup(db_name, server_name.Text, saveFileDialog.FileName);
        Mouse.OverrideCursor = Cursors.Arrow;
    }
}*/
    }
}
