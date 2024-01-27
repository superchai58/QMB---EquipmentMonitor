using EquipmentMonitor.SolderPastePrinter;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;

namespace EquipmentMonitor
{
    public partial class FormLogParser : Form
    {
        //private const int m_ParserInterval = 300000;  //Parse log的間隔時間
        public int m_ParserInterval = 0;  //Parse log的間隔時間
        private int m_iCount = 0;
        private string m_msg;
        clsDB m_db = new clsDB();
        clsLog m_clslog;

        //declare here, only query error code mapping one time,
        //if want to query each time, class must declare in switch case.
        Mounter.clsNXT_II m_nx = new Mounter.clsNXT_II();
        SolderPastePrinter.GPX gpx = new SolderPastePrinter.GPX();
        SolderPastePrinter.GPX_C gpx_c = new SolderPastePrinter.GPX_C();
        SolderPastePrinter.SPG spg = new SolderPastePrinter.SPG();

        public FormLogParser()
        {
            InitializeComponent();

            //---------superchai set interval by line 20231002 (B)------------
            ConnectDBSMT oCon = new ConnectDBSMT();
            SqlCommand cmd = new SqlCommand();
            DataTable dtTmp = new DataTable();

            cmd.CommandText = "Select Interval From IntervalByLine With(nolock) Where Line = @Line";
            cmd.Parameters.Add(new SqlParameter("@Line", clsCommon.Line.ToString().Trim()));
            dtTmp = oCon.Query(cmd);
            if (dtTmp.Rows.Count > 0)
            {
                //System.Threading.Thread.Sleep(int.Parse(dtTmp.Rows[0]["Interval"].ToString().Trim()));
                m_ParserInterval = int.Parse(dtTmp.Rows[0]["Interval"].ToString().Trim());                
            }
            else
            {
                m_ParserInterval = 300000;
            }
            //---------superchai set interval by line 20231002 (E)------------ 

            //*********************************************
            //*************改版 版號放在這邊******************
            //*********************************************
            //clsCommon.ClientVersion = "2019-08-06_v1";
            clsCommon.ClientVersion = "2023-10-02_v1";      //superchai add 20231002
            this.Text = "Log Parser "+clsCommon.ClientVersion;

            //Create Log Object
            m_clslog = new clsLog(txbox,m_db);
            lb_EQ_SN.Text = clsCommon.EQ_SN;
            txtSite.Text = clsCommon.Site;
            txtFactory.Text = clsCommon.Factory;
            txtLine.Text = clsCommon.Line;
            txtStation.Text = clsCommon.Station;
            txtMachineType.Text = clsCommon.MachineType;
            txtMachine.Text = clsCommon.MachineName;
            txtMachineSWVer.Text = clsCommon.MachineSWVer;
            if (clsCommon.FileDir.ToUpper().StartsWith("NETWORK LIBRARY"))
            {
                txtFileDir.Text = clsCommon.FileDir.Substring(0, clsCommon.FileDir.IndexOf("PWD") - 1);
            }
            else
            {
                txtFileDir.Text = clsCommon.FileDir;
            }
            
            txtFile.Text = clsCommon.FileName;
            //有删除log则在Remark显示Backup天数
            if (clsCommon.DelBakLog == "Y")
            {
                txtRemark.Text = "Backup Day:" + clsCommon.DelBakLogByDay + ';' + clsCommon.Remark;
            }
            else
            {
                txtRemark.Text = clsCommon.Remark;
            }

            if (txtMachine.Text== "NXT_I" ||
                txtMachine.Text == "NXT_II" ||
                txtMachine.Text == "NXT_III" ||
                txtMachine.Text == "AIMEX")
            {
                clsCommon.isAIMEX = true;
            }

            TimerParser.Interval = m_ParserInterval;
            TimerParser.Enabled = true;
            m_clslog.writeCOMMLog("Ready");
            m_clslog.writeCOMMLog("Schedule:" + (m_ParserInterval / 1000).ToString() + " Second.");            
        }

        private void TimerParser_Tick(object sender, EventArgs e)
        {
            try
            {
                m_iCount++;
                if (m_iCount % 5 == 0)
                {
                    m_clslog.cleanLog();
                }

                if (m_iCount > 13)
                {
                    m_nx = new Mounter.clsNXT_II();
                    gpx = new SolderPastePrinter.GPX();
                    gpx_c = new SolderPastePrinter.GPX_C();
                    spg = new SolderPastePrinter.SPG();
                    m_iCount = 0;
                }

                TimerParser.Enabled = false;
                try
                {
                    runEachModelParseMethod();

                    writeLog(m_msg);
                }
                catch (Exception ex)
                {
                    writeLog(ex.ToString());
                }
                finally
                {
                    TimerParser.Enabled = true;
                }

                //可能變更線別跟站別



                txtLine.Text = clsCommon.Line;
                txtStation.Text = clsCommon.Station;
            }
            catch (Exception ex)
            {
                writeLog(ex.ToString());
            }
            finally
            {
                TimerParser.Enabled = true;
            }
        }

        private void runEachModelParseMethod()
        {
            try
            {
                switch (txtMachine.Text.ToUpper())
                {
                    #region 印刷机




                    case "HORIZON_03IX":
                        DEK_Horizon_03ix DEK = new DEK_Horizon_03ix();
                        m_msg = DEK.ParsingLog(m_clslog);
                        break;
                    case "MPM":
                        SolderPastePrinter.MPM mpm = new SolderPastePrinter.MPM();
                        m_msg = mpm.ParsingLog(m_clslog);
                        break;
                    case "GPX":
                        m_msg = gpx.ParsingLog(m_clslog);
                        break;
                    case "GPX_C":
                        m_msg = gpx_c.ParsingLog(m_clslog);
                        break;
                    case "SPG":
                        m_msg = spg.ParsingLog(m_clslog);
                        break;
                    case "VERSAPRINT":
                        SolderPastePrinter.Versaprint ve = new SolderPastePrinter.Versaprint();
                        m_msg = ve.ParsingLog(m_clslog);
                        break;


                    #endregion

                    #region 回焊炉


                    case "VXS944_REHM":
                    case "VXP944_REHM":
                    case "VXP945_REHM":         
                        Reflow.VXP945_Rehm vx = new Reflow.VXP945_Rehm();
                        m_msg = vx.ParsingLog(m_clslog);
                        break;
                    case "HELLER-1826MK5":
                        Reflow.HELLER_1826MK5 he = new Reflow.HELLER_1826MK5();
                        m_msg = he.ParsingLog(m_clslog);
                        break;
               

                    #endregion

                    #region 裁板机




                    case "EM5700":
                    case "ER6000":                        
                        Router.EM5700 el = new Router.EM5700();
                        m_msg = el.ParsingLog(m_clslog);
                        break;
                    case "WT-500L":
                        Router.WT_500L wt = new Router.WT_500L();
                        m_msg = wt.ParsingLog(m_clslog);
                        break;
                    case "EXE880M":
                        Router.EXE880M exe = new Router.EXE880M();
                        m_msg = exe.ParsingLog(m_clslog);
                        break;
                    #endregion

                    #region 选择焊/波峰焊/回焊炉(ERSA)

                    case "VERSAFLOW_3_SERIALS": //选择焊

                    case "POWERFLOW_N2/XL": //波峰焊

                    case "HOTFLOW 3/20":    //回焊炉(ERSA)
                    case "HOTFLOW 3/26XL":    //回焊炉(ERSA)   
                    case "VERSAFLOW 3/45": //选择焊(ERSA)
                    case "VERSAFLOW 3/66"://选择焊(ERSA)
                    case "ECOSELECT2"://选择焊(ERSA)
                        WaveSoldering.ERSA ersa = new WaveSoldering.ERSA();
                        m_msg = ersa.ParsingLog(m_clslog);
                        break;
                    case "VERSAFLOW W3": //选择焊

                        WaveSoldering.ERSAW3 ersa3 = new WaveSoldering.ERSAW3();
                        m_msg = ersa3.ParsingLog(m_clslog);
                        break;

                    #endregion

                    #region 压合机



                    case "CBP-5T":
                    case "MEP-6T-TYCO":
                        PressFit.MEP_6T_TYCO mep_6t_tyco = new PressFit.MEP_6T_TYCO();
                        m_msg = mep_6t_tyco.ParsingLog(m_clslog);
                        break;
                    case "LPM1":
                        PressFit.LPM1 lpm1 = new PressFit.LPM1();
                        m_msg = lpm1.ParsingLog(m_clslog);
                        break;
                    case "SX_PF03":
                        PressFit.SX_PF03 SX_PF03 = new PressFit.SX_PF03();
                        m_msg = SX_PF03.ParsingLog(m_clslog);
                        break;
                    #endregion

                    #region 贴片机


                    case "NXT_I":
                    case "NXT_II":
                    case "NXT_III":
                    case "AIMEX":
                        m_msg = m_nx.ParsingLog(m_clslog);
                        break;
                    case "NPM":
                    case "CNPM":
                    case "SNPM":
                        Mounter.NPM npm = new Mounter.NPM();
                        m_msg = npm.ParsingLog(m_clslog);
                        break;
                    case "NPM_PANACIM":
                    case "SNPM_PANACIM":
                    case "CNPM_PANACIM":
                        Mounter.NPM_PANACIM Npm_PanaCIM = new Mounter.NPM_PANACIM();
                        m_msg = Npm_PanaCIM.ParsingLog(m_clslog);
                        break;
                    case "CMDT-S":
                    case "CMDT-C":
                    case "SP60":
                        Mounter.CMDT cmdt = new Mounter.CMDT();
                        m_msg = cmdt.ParsingLog(m_clslog);
                        break;
                    #endregion

                    #region 镭雕机

                    case "SHINE-F1":
                    case "SPARK-CD":
                        Laser.Jutze jz = new Laser.Jutze();
                        m_msg = jz.ParsingLog(m_clslog);
                        break;
                    case "LCB10C":
                    case "LAA10C":
                    case "LMP10C":
                    case "LAA20F":
                    case "LCD10C":
                    case "LCB-V":
                        Laser.HuaGong lc = new Laser.HuaGong();
                        m_msg = lc.ParsingLog(m_clslog);
                        break;
                    #endregion


                    #region 点胶机

                    case "FX-D":
                        Glue.Camalot ca = new Glue.Camalot();
                        m_msg = ca.ParsingLog(m_clslog);
                        break;
                    case "AD-16-DSWH2":
                        Glue.Anda ad = new Glue.Anda();
                        m_msg = ad.ParsingLog(m_clslog);
                        break;
                    case "S2-910/920":
                        Glue.Nordson nor = new Glue.Nordson();
                        m_msg = nor.ParsingLog(m_clslog);
                        break;
                    case "AU77S":
                        Glue.AU77S au = new Glue.AU77S();
                        m_msg = au.ParsingLog(m_clslog);
                        break;
                    case "EM-5701N":
                        Glue.EM5701N em = new Glue.EM5701N();
                        m_msg = em.ParsingLog(m_clslog);
                        break;
                    case "DELTA6/8":
                        Glue.PVA pva = new Glue.PVA();
                        m_msg = pva.ParsingLog(m_clslog);
                        break;

                    #endregion

                    default:
                        m_clslog.writeERRORLog("Can not find machine process method");
                        break;
                }
            }
            catch (Exception e)
            {
                WriteErrorFile(e.ToString());
            }
        }
        private void writeLog(string sLog)
        {
            if (m_msg == "") return;

            WriteErrorFile(sLog);

            sLog = DateTime.Now.ToString(Environment.NewLine+"[HH:mm:ss] ") + sLog;
            txbox.AppendText(sLog);
            txbox.ScrollToCaret();
        }


        private static void WriteErrorFile(string strcontent)
        {
            try
            {
                string strfilepath = CheckFolder() + "\\QEMS_Log_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";

                using (StreamWriter sw = new StreamWriter(strfilepath))   //小寫TXT     
                {
                    sw.WriteLine(strcontent);
                }
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.  
                MessageBox.Show(e.ToString());
                Console.WriteLine(e.Message);
            }
        }

        private static string CheckFolder()
        {
            string stryear = DateTime.Now.Year.ToString();
            string strmonth = DateTime.Now.Month.ToString();
            string strday = DateTime.Now.Day.ToString();

            string strFilePath = Application.StartupPath + "\\" + stryear + "\\" + strmonth + "\\" + strday;

            if (!Directory.Exists(strFilePath))
            {
                Directory.CreateDirectory(strFilePath);
            }

            return strFilePath;
        }

        private void cleanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Do you want to Reset Config?", "Reset", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                clsCommon.WriteRegInfo("", "", "", "",
                    "", "", "", "",
                    clsCommon.FileDir, clsCommon.FileName, clsCommon.Remark,clsCommon.SIDE, "", "");
                Application.Exit();
            }
            else if (dialogResult == DialogResult.No)
            {
                
            }
            
        }

        private void changeTimerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dlgInputBox Dialog = new dlgInputBox("Change Detect Interval","Min");
            if (Dialog.ShowDialog(this) == DialogResult.OK)
            {
                float x =5.0f;//預設五分鐘


                float.TryParse(Dialog.m_Value, out x);
                
                if (x < 1) x = 5;
                if (x > 5) x = 5;
                if (Dialog.m_Value == "debug") x = 0.1f;

                TimerParser.Stop();
                TimerParser.Interval = (int)(x * 1000f * 60f);
                TimerParser.Start();
                m_clslog.writeIMPORTLog("Change Detect Interval to " + x + " Mins");                
            }
            else
            {

            }
            Dialog.Dispose();
        }

        private void FormLogParser_FormClosing(object sender, FormClosingEventArgs e)
        {
            SignIn LoginIn = new SignIn();
            LoginIn.ShowDialog();
            if (LoginIn.DialogResult == DialogResult.OK)
            {
                System.Environment.Exit(0);
                e.Cancel = false;
            }
            else if (LoginIn.DialogResult == DialogResult.Cancel)
            {
                MessageBox.Show("没有权限关闭，或者密码输入错误！");
                e.Cancel = true;
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
