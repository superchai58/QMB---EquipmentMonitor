using System;
using System.Collections;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace EquipmentMonitor
{
    static class Program
    {
        public static String Line;
        public static String Station;
        public static String CONN;
        public static String IP;
        public static String UserName;
        //public static String BU;
        public static String AppName;
        public static ArrayList UserRight = null;
        private static bool IsInDesign = true;
        /// <summary>
        /// 應用程式的主要進入點。



        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string strArgs = string.Empty;


#if DEBUG
            //程序修改记录请添加在ProgramVersion中。

            //MessageBox.Show("This is debug Mode,please do not use it on porduct line and Contact QMS!", "Error Message", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            //strArgs = "<CONN=Network Library=DBMSSOCN;UID=MunSFUser;SERVER=192.168.20.39;DATABASE=SMT;PWD=is6<2g;CONNECT TIMEOUT=30;Application Name=EquipmentMonitor><APPNAME=EquipmentMonitor><LINE=A12><STATION=EquipmentMonitor><SERVERIP=192.168.20.38><BU=QCI><SERVERBU=QCI><FACTORY=T2><USERID=admin><RIGHT=Login,>";
            ////strArgs = "<CONN=Network Library=DBMSSOCN;UID=sa;SERVER=172.26.12.2;DATABASE=SMT;PWD=East#86;CONNECT TIMEOUT=30;Application Name=EquipmentMonitor><APPNAME=EquipmentMonitor><LINE=A12><STATION=EquipmentMonitor><SERVERIP=172.26.12.2><BU=QCI><SERVERBU=QCI><FACTORY=T2><USERID=admin><RIGHT=Login,>";
            //strArgs = "<CONN=Network Library=DBMSSOCN;UID=sa;SERVER=172.26.16.4;DATABASE=SMT;PWD=East#86;CONNECT TIMEOUT=30;Application Name=EquipmentMonitor><APPNAME=EquipmentMonitor><LINE=K35><STATION=EquipmentMonitor><SERVERIP=172.26.16.4><BU=NB6><SERVERBU=NB6><FACTORY=F5><USERID=admin><RIGHT=Login,>";
            //strArgs = "<CONN=Network Library=DBMSSOCN;UID=sa;SERVER=10.18.3.21;DATABASE=SMT;PWD=East#86;CONNECT TIMEOUT=30;Application Name=EquipmentMonitor><APPNAME=EquipmentMonitor><LINE=F11><STATION=EquipmentMonitor><SERVERIP=10.18.3.21><BU=NB5><SERVERBU=NB5><FACTORY=F1><USERID=admin><RIGHT=Login,>";
            //strArgs = "<CONN=Network Library=DBMSSOCN;UID=sa;SERVER=172.26.21.5;DATABASE=SMT;PWD=East#86;CONNECT TIMEOUT=30;Application Name=EquipmentMonitor><APPNAME=EquipmentMonitor><LINE=K35><STATION=EquipmentMonitor><SERVERIP=172.26.21.5><BU=NB1><SERVERBU=NB1><FACTORY=F1><USERID=admin><RIGHT=Login,>";

#endif
            if (!IsInDesign)
            {
                strArgs = "<CONN=Network Library=DBMSSOCN;UID=MunSFUser;SERVER=10.94.7.11;DATABASE=SMT;PWD=is6<2g;CONNECT TIMEOUT=30;Application Name=EquipmentMonitor><APPNAME=EquipmentMonitor><LINE=V177><STATION=EquipmentMonitor><SERVERIP=10.94.7.15><BU=PU9><SERVERBU=QMB><FACTORY=F7><USERID=admin><RIGHT=Login,>";
                //strArgs = "<CONN=Network Library=DBMSSOCN;UID=sa;SERVER=10.226.32.101;DATABASE=SMT;PWD=qms7sa;CONNECT TIMEOUT=30;Application Name=EquipmentMonitor><APPNAME=EquipmentMonitor><LINE=A12><STATION=EquipmentMonitor><SERVERIP=10.18.11.11><BU=PU11><SERVERBU=NB3><FACTORY=T2A><USERID=admin><RIGHT=Login,>";
                //strArgs = "<CONN=Network Library=DBMSSOCN;UID=sa;SERVER=10.18.8.11;DATABASE=SMT;PWD=East#86;CONNECT TIMEOUT=30;Application Name=EquipmentMonitor><APPNAME=EquipmentMonitor><LINE=I23><STATION=EquipmentMonitor><SERVERIP=10.18.8.11><BU=NB4><SERVERBU=NB4><FACTORY=F2><USERID=admin><RIGHT=Login,>";
            }
            if (args.Length > 0)
            {
                foreach (string v in args)
                {
                    strArgs += v.ToString() + " ";
                }
            }

            if (String.IsNullOrEmpty(strArgs))
            {
                MessageBox.Show("Please run program from MainMenu!");
                Application.Exit();
                return;
            }

            if (GetArgumentsFromMainMenu(strArgs) == false)
            {
                MessageBox.Show("Incorrect arguments from MainMenu!");
                Application.Exit();
                return;
            }

            QMSSDK.Db.Connections.CreateCn(CONN);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            GetParametersFromRegistry();
            if (clsCommon.MachineName.Equals(string.Empty))
            {
                Application.Run(new FormSetting());
            }
            
            Application.Run(new FormLogParser());
        }

        private static void GetParametersFromRegistry()
        {
            clsCommon.Site = clsCommon.ReadRegKey("Site");
            clsCommon.Factory = clsCommon.ReadRegKey("Factory");
            clsCommon.Line = clsCommon.ReadRegKey("Line");
            clsCommon.Station = clsCommon.ReadRegKey("Station");
            clsCommon.MachineType = clsCommon.ReadRegKey("MachineType");
            clsCommon.MachineName = clsCommon.ReadRegKey("MachineName");
            clsCommon.MachineSWVer = clsCommon.ReadRegKey("MachineSoftVer");
            clsCommon.FileDir = clsCommon.ReadRegKey("FileDir");
            clsCommon.DB_Conn2 = clsCommon.ReadRegKey("FileDir");
            clsCommon.FileName = clsCommon.ReadRegKey("FileName");
            clsCommon.Remark = clsCommon.ReadRegKey("Remark");
            clsCommon.EQ_SN = clsCommon.ReadRegKey("EQ_SN");
            clsCommon.SIDE = clsCommon.ReadRegKey("SIDE");
        }

        static bool GetArgumentsFromMainMenu(String strCMD)
        {
            string strCONN;
            UserRight = new ArrayList(); 

            if (strCMD.IndexOf("<LINE=") > -1)
            {
                strCONN = strCMD.Substring(strCMD.IndexOf("<LINE="), strCMD.Length - strCMD.IndexOf("<LINE="));
                Line = strCONN.Substring(strCONN.IndexOf("<LINE="), strCONN.Length).Substring("<LINE=".Length, strCONN.IndexOf(">") - "<LINE=".Length);
            }

            if (strCMD.IndexOf("<STATION=") > -1)
            {
                strCONN = strCMD.Substring(strCMD.IndexOf("<STATION="), strCMD.Length - strCMD.IndexOf("<STATION="));
                Station = strCONN.Substring(strCONN.IndexOf("<STATION="), strCONN.Length).Substring("<STATION=".Length, strCONN.IndexOf(">") - "<STATION=".Length);
            }
            if (strCMD.IndexOf("<CONN=") > -1)
            {
                strCONN = strCMD.Substring(strCMD.IndexOf("<CONN="), strCMD.Length - strCMD.IndexOf("<CONN="));
                CONN = strCONN.Substring(strCONN.IndexOf("<CONN="), strCONN.Length).Substring("<CONN=".Length, strCONN.IndexOf(">") - "<CONN=".Length);
                CONN = CONN.Replace("PROVIDER=SQLOLEDB;", "");
                CONN = CONN.Replace("NetworkLibrary", "Network Library");  //20131030 add by Scofield for MBU mainmenu(0002)
            }
            if (strCMD.IndexOf("SERVER=") > -1)
            {
                strCONN = strCMD.Substring(strCMD.IndexOf("SERVER="), strCMD.Length - strCMD.IndexOf("SERVER="));
                IP = strCONN.Substring(strCONN.IndexOf("SERVER="), strCONN.Length).Substring("SERVER=".Length, strCONN.IndexOf(";") - "SERVER=".Length);
            }
            if (strCMD.IndexOf("DATA SOURCE=") > -1)
            {
                strCONN = strCMD.Substring(strCMD.IndexOf("DATA SOURCE="), strCMD.Length - strCMD.IndexOf("DATA SOURCE="));
                IP = strCONN.Substring(strCONN.IndexOf("DATA SOURCE="), strCONN.Length).Substring("DATA SOURCE=".Length, strCONN.IndexOf(";") - "DATA SOURCE=".Length);
            }
            if (strCMD.IndexOf("<USERID=") > -1)
            {
                string strUID = strCMD.Substring(strCMD.IndexOf("<USERID="), strCMD.Length - strCMD.IndexOf("<USERID="));
                UserName = strUID.Substring(strUID.IndexOf("<USERID="), strUID.Length).Substring("<USERID=".Length, strUID.IndexOf(">") - "<USERID=".Length);
            }
            if (strCMD.IndexOf("RIGHT=") > -1)
            {
                string right = strCMD.Substring(strCMD.IndexOf("<RIGHT="), strCMD.Length - strCMD.IndexOf("<RIGHT="));
                right = right.Substring("<RIGHT=".Length, right.IndexOf(">") - "<RIGHT=".Length);
                string[] struserright = right.Split(',');
                for (int i = 0; i < struserright.Length; i++)
                {
                    if (struserright[i] != "")
                    {
                        UserRight.Add(struserright[i].ToString());
                    }
                }
            }
            if (strCMD.IndexOf("<APPNAME=") > -1)
            {
                string strApp = strCMD.Substring(strCMD.IndexOf("<APPNAME="), strCMD.Length - strCMD.IndexOf("<APPNAME="));
                AppName = strApp.Substring(strApp.IndexOf("<APPNAME="), strApp.Length).Substring("<APPNAME=".Length, strApp.IndexOf(">") - "<APPNAME=".Length);
            }
            if (String.IsNullOrEmpty(Line) || String.IsNullOrEmpty(Station) || String.IsNullOrEmpty(CONN))
            {
                return false;
            }
            if (String.IsNullOrEmpty(AppName))
            {
                AppName = Station;
            }

            return true;
        }
    }
}
