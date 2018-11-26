using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Xml;

namespace DBAssist
{ 
    public class Model
    {
        string UserName;
        public List<Course> Courses;
        public Model()
        {
            try
            {
                UserName = Environment.UserDomainName.ToLower() + "\\" + Environment.UserName.ToLower();
                Courses = new List<Course>();
                readXML();
            }
            catch
            {
                ShowError("system");
            }
        }
        private void readXML()
        {
            string year="", semester="";
            Course c = null;
            WebRequest request = null;
            string path = Directory.GetCurrentDirectory() + "\\DBAssistSettings.xml";
            if (File.Exists(path))
            {
                request = WebRequest.Create(path);
            }
            else
            {
                request = WebRequest.Create(Properties.Settings.Default.SettingLocation + "\\DBAssistSettings.xml");
            }
            request.Timeout = 5000;

                using (WebResponse response = request.GetResponse())
                using (XmlReader xmlreader = XmlReader.Create(response.GetResponseStream()))
                {

                    while (xmlreader.Read())
                    {
                        if (xmlreader.NodeType == XmlNodeType.Element)
                        {
                            switch (xmlreader.Name)
                            {
                                case "Year":
                                    year = xmlreader.GetAttribute("Value");
                                    break;
                                case "Semester":
                                    semester = xmlreader.GetAttribute("Value");
                                    break;
                                case "Course":
                                    c = new Course(year, semester, xmlreader.GetAttribute("Name"), xmlreader.GetAttribute("Number"), Convert.ToBoolean(xmlreader.GetAttribute("UseCourseNumberInDBName")));
                                    Courses.Add(c);
                                    break;
                                case "Faculty":
                                    xmlreader.Read();
                                    c._Faculty = xmlreader.Value.ToLower().Replace("\t", " ").Split(new[] { "\n", " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
                                    break;
                                case "Students":
                                    xmlreader.Read();
                                    c._Student = xmlreader.Value.ToLower().Replace("\t", " ").Split(new[] { "\n", " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
                                    break;
                                case "Groups":
                                    List<string> grp = new List<string>();
                                    string group_name = "";
                                    while (xmlreader.Read() && xmlreader.Name != "Groups")
                                    {
                                        if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "Group")
                                        {
                                            group_name = xmlreader.GetAttribute("Name");
                                            xmlreader.Read();
                                            c._Groups.Add(new Tuple<string, List<string>>(group_name, xmlreader.Value.ToLower().Replace("\t", " ").Split(new[] { "\n", " " }, StringSplitOptions.RemoveEmptyEntries).ToList()));
                                        }
                                    }
                                    break;
                            }
                        }
                    }

                }
        }
        /// <summary>
        /// saves shown database information into read only test file
        /// </summary>
        /// <param name="CourseName">Course Name</param>
        /// <param name="txt_server">Server Name</param>
        /// <param name="txt_db">Database Name</param>
        /// <param name="accounts">Authorized accounts</param>
        internal void SaveToFile(string CourseName, string txt_server, string txt_db, string[] accounts)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt|C# file (*.cs)|*.cs";
            saveFileDialog.FileName = "Database Info - " + Environment.UserName.ToLower() + "," + CourseName;
            if (saveFileDialog.ShowDialog() == true)
            {
                using (StreamWriter st = new StreamWriter(saveFileDialog.FileName, false, System.Text.Encoding.UTF8))
                {
                    st.WriteLine("Course Name: " + CourseName);
                    st.WriteLine(txt_server);
                    st.WriteLine(txt_db);
                    st.WriteLine("Database accessible for the follow authorized users:");
                    foreach (string name in accounts)
                    {
                        st.WriteLine(name);
                    }
                    FileInfo fileinfo = new FileInfo(saveFileDialog.FileName);
                    fileinfo.IsReadOnly = true;
                }
            }
        }
        /// <summary>
        /// get a list of 
        /// </summary>
        /// <param name="_server"></param>
        /// <param name="_db"></param>
        /// <returns></returns>
        internal string GetAuthorized(string _server, string _db)
        {
            Server srv = new Server(ConnectToServer(_server));
            Database db = srv.Databases[_db];
            if (db == null)
                return string.Empty;
            string[] ans = new string[db.Users.Count];
            for(int i=0; i<ans.Length; i++)
            {
                ans[i] = db.Users[i].Name;
            }
            return String.Join("\n", ans);
        }
        /// <summary>
        /// findes if the user have permission to open personal db in the chosen course
        /// </summary>
        /// <param name="course_index">course selection index</param>
        /// <returns>true if have permission and false if not</returns>
        internal bool HavePesonalPermissions(int course_index)
        {
            return Courses[course_index]._Student.Contains(UserName);
        }

        /// <summary>
        /// findes if the user have group permission in a course and what the name of his group
        /// </summary>
        /// <param name="course_index">course selection index</param>
        /// <returns>team name if have permmision or empty string if dose not have group permissions </returns>
        internal string HaveGroupPermissions(int course_index)
        {
            foreach (var g in Courses[course_index]._Groups)
            {
                if(g.Item2.Contains(UserName))
                {
                    return g.Item1;
                }
            }
            return string.Empty;
        }
        /// <summary>
        /// Creates a db based on the given details 
        /// </summary>
        /// <returns>database name craeted for the given details
        /// or an empty string if the creation faild</returns>
        internal string CreateDB(int course_index, bool is_personal, string name, string server_name)
        {
            if (name == string.Empty) name = UserName.Substring(UserName.LastIndexOf("\\") + 1);
            Server srv = new Server(ConnectToServer(server_name));
            Database db;
            string db_name = Courses[course_index].GetDBString(is_personal, name);
            db = new Database(srv, db_name);
            db.Create();
            db = srv.Databases[db_name];
            if (db == null) return string.Empty;
            if (is_personal) SetPersonalPermissions(course_index, db, server_name);
            else SetGroupPermissions(course_index, name, db, server_name);
            return "Server Name:\n" + srv.Name + "Database Name: " + db.Name;//get connaction string
        }
        /// <summary>
        ///  sets permissions for personal db.
        ///  permissions for faculty stuff for the given db based on the given information in the programs xml settings file
        /// </summary>
        private void SetPersonalPermissions(int course_index, Database db, string server_name)
        {
            Server srv = new Server(ConnectToServer(server_name));
            FacultyPermissions(course_index, db, srv);
            UserPermissions(db, srv, UserName); //db_owner
            //SetAsDatabaseOwner(db, srv, UserName); //database Owner (mode for Backup and Restore)
        }

        /// <summary>
        /// set student as database owner - this option is for enableing the student to create database Backups and Restore 
        /// </summary>
        private void SetAsDatabaseOwner(Database db, Server srv, string student_name)
        {
            if (!srv.Logins.Contains(student_name))
            {//Crate new Login instanse
                Login login = new Login(srv, student_name);
                login.LoginType = LoginType.WindowsUser;
                login.Create();
            }
            db.SetOwner(student_name,true);
        }

        /// <summary>
        ///  sets permissions for Group db.
        ///  permissions for faculty stuff for the given db based on the given information in the programs xml settings file
        /// </summary>
        private void SetGroupPermissions(int course_index, string group_name, Database db, string server_name)
        {
            Server srv = new Server(ConnectToServer(server_name));
            FacultyPermissions(course_index, db, srv);
            foreach (Tuple<string, List<string>> group in Courses[course_index]._Groups)
            {
                if (group.Item1 == group_name)
                {
                    bool dbOwner = false;
                    foreach (string student_name in group.Item2)
                    {
                        //if (!dbOwner)
                        //{
                        //    SetAsDatabaseOwner(db, srv, UserName);
                        //    dbOwner = true;
                        //}
                        //else
                            UserPermissions(db, srv, student_name);
                        
                    }
                }
            }
        }

        private static void UserPermissions(Database db, Server srv, string student_name)
        {
            if (!srv.Logins.Contains(student_name))
            {//Crate new Login instanse
                Login login = new Login(srv, student_name);
                login.LoginType = LoginType.WindowsUser;
                login.Create();
            }
            User user = new User(db, student_name);
            user.Login = student_name;
            user.Create();
            user.AddToRole("db_owner");
        }
        private void FacultyPermissions(int course_index, Database db, Server srv)
        {
            foreach (string faculty_name in Courses[course_index]._Faculty)
            {
                if (db.Users.Contains(faculty_name)) continue;
                if (!srv.Logins.Contains(faculty_name))
                {//Crate new Login instanse
                    Login login = new Login(srv, faculty_name);
                    login.LoginType = LoginType.WindowsUser;
                    login.Create();
                }
                User faculty = new User(db, faculty_name);
                faculty.Login = faculty_name;
                faculty.Create();
                faculty.AddToRole("db_backupoperator");
                faculty.AddToRole("db_ddladmin");
                faculty.AddToRole("db_datawriter");
                faculty.AddToRole("db_datareader");
            }
        }
        /// <summary>
        /// backup the selected database. 
        /// the program saves the backup file into tow locations:
        /// user choose location and backup folder in the server (writen in the Settings.settings file) 
        /// </summary>
        /*internal void Backup(string db_name, string server_name, string save_path)
        {
            try
            {
                string databaseName = db_name;
                Backup sqlBackup = new Backup();
                ////Specify the type of backup, the description, the name, and the database to be backed up.    
                sqlBackup.Action = BackupActionType.Database;
                sqlBackup.BackupSetDescription = "BackUp of:" + databaseName + "on" + DateTime.Now.ToShortDateString();
                sqlBackup.BackupSetName = "FullBackUp";
                sqlBackup.Database = databaseName;
                ////Declare a BackupDeviceItem    
                string backupfileName = db_name + ".bak";
                string source = @"\\" + server_name + "\\" + Properties.Settings.Default.BackupLocationServer;
                string local_dest = Properties.Settings.Default.BackupLocationClient;
                BackupDeviceItem deviceItemServer = new BackupDeviceItem(source + @"\" + backupfileName, DeviceType.File);
                ////Define Server connection    
                Server sqlServer = new Server(ConnectToServer(server_name));
                Database db = sqlServer.Databases[databaseName];
                sqlBackup.Devices.Add(deviceItemServer);  
                sqlBackup.SqlBackup(sqlServer);
                //File.Copy(source + @"\" + backupfileName, "C:\\SQ Backups\\"+ backupfileName, true);
                File.Copy(source + @"\" + backupfileName, save_path, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something Went Wrong in Backup: " + ex.Message);
            }
        */
        /// <returns>database name if there is a database for the given details
        /// or an empty string if there is no database</returns>
        internal string GetDB(int course_index, bool is_personal, string name, string server_name)
        {
            if (name == string.Empty) name = Environment.UserName.ToLower();
            Server srv = new Server(ConnectToServer(server_name));
            Database db;
            string a = Courses[course_index].GetDBString(is_personal, name);
            db = srv.Databases[a];
            if (db == null) return string.Empty;
            return db.Name;
        }
        /// <summary>
        /// asstablish a server connaction object to the given server name.
        /// the user name and the password is taken from the settings.settings file 
        /// </summary>
        private ServerConnection ConnectToServer(string serverName)
        {
            try
            {
                ServerConnection srvConn = new ServerConnection(serverName);
                srvConn.LoginSecure = false;
                srvConn.Login = Properties.Settings.Default.ConnectorName;
                srvConn.Password = Properties.Settings.Default.ConnectorPassword;
                return srvConn;
            }
            catch
            {
                ShowError("system");
                return null;
            }
        }
        internal bool ShowError(string error_type, string db_type="")
        {
            Mouse.OverrideCursor = Cursors.Arrow;
            switch (error_type)
            {
                case "permission": //User dose not have permissions to open data base in this specipic course and db_type
                    MessageBox.Show("You are not allowed to create " + db_type.ToLower() + " database in this course. Please contact the supervisor.", "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                case "system": //ping to server to see if the server is down or just the sql...
                    MessageBox.Show("System error occurred. Possible SQL server not accessible or XML configuration file not accessible or invalid. Please contact GGFBM Technical Support by Phone: 08-6472190 or by e-Mail: support@som.bgu.ac.il", "Permission Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                    return false;
                case "success": //data base was created successfuly
                    MessageBox.Show(db_type + " database was created successfully.", "Succsess", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return true;
                case "exists": //database already exists in this course 
                    MessageBoxResult r = MessageBox.Show(db_type + " database already exists and you cannot create it again. Please click OK to watch relevant information.", "Exists Error", MessageBoxButton.OKCancel, MessageBoxImage.Error,MessageBoxResult.Cancel); //== MessageBoxResult.OK;
                    return r == MessageBoxResult.OK;
                case "not exists": //you dont have db in this course in this type. click crate btn to create db
                    MessageBox.Show("In order to use \"Show Information\" button, you should first choose properly database type (Personal or Group) and create it. ", "Not Exists Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                default:
                    return false;
            }
        }
    }
    public class Course
    {
        public string _Year, _Semester, _Name, _Number;
        public bool _ShowCourseNumber;
        public List<string> _Faculty, _Student;
        public List<Tuple<string, List<string>>> _Groups;
        /// <summary>
        /// Course object constractor. fill in all the course details from the programs XML settings on the server.
        /// </summary>
        public Course(string Year, string Semester, string Name, string Number, bool ShowCourseNumber)
        {
            _Year = Year;
            _Semester = Semester;
            _Name = Name;
            _Number = Number;
            _ShowCourseNumber = ShowCourseNumber;
            _Student = new List<string>();
            _Faculty = new List<string>();
            _Groups = new List<Tuple<string, List<string>>>();      
        }
        /// <summary>
        /// use to get the default name of the database
        /// </summary>
        /// <param name="personal">is the database parsonal or group db?</param>
        /// <param name="db_name">the name of the user if personal db, or the name of the group if its group db</param>
        /// <returns>a string with the name of the requseted course type and username</returns>
        internal string GetDBString(bool personal, string db_name)
        {
            if (personal)
            {
                if (_ShowCourseNumber)
                    return _Year + "-" + _Semester + "_" + _Number.Replace(".", "") + "_" + db_name;
                else
                    return _Year + "-" + _Semester + "_" + db_name;
            }
            else
            {
                return _Year + "-" + _Semester + "_" + db_name;
            }
        }
        /// <summary>
        /// show message box with the type of error that occur
        /// </summary>
        /// <param name="error_type">the type of error</param>
        /// <param name="db_type">prsonal\group</param>
        
    }
}
