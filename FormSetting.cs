using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace EquipmentMonitor
{
    public partial class FormSetting : Form
    {
        clsDB m_db = new clsDB();
        DataTable m_dtQIEMS_Define_EQ_SN;
        List<ListBox> m_lsBoxList = new List<ListBox>();
        public FormSetting()
        {
            InitializeComponent();
            cbMachineType.Items.Add("印刷机");
            cbMachineType.Items.Add("回焊炉");
            cbMachineType.Items.Add("压合机");
            cbMachineType.Items.Add("波峰焊");
            cbMachineType.Items.Add("裁板机");
            cbMachineType.Items.Add("选择焊");
            cbMachineType.Items.Add("贴片机");
            cbMachineType.Items.Add("镭雕机");
            cbMachineType.Items.Add("点胶机");

            m_dtQIEMS_Define_EQ_SN = m_db.Setting("GETBasicInfo");
            //--superchai add 20231002 (B)--
            DataTable dt = m_dtQIEMS_Define_EQ_SN.DefaultView.ToTable(true, "Site");
            if (dt.Rows.Count > 0)
            {
                BindingComboBoxWithDataTable(cbSite, dt, "Site", "");
            }           
            //--superchai add 20231002 (E)--
            //DataTable dt = m_dtQIEMS_Define_EQ_SN.DefaultView.ToTable(true, "Site");
            //BindingComboBoxWithDataTable(cbSite, dt, "Site", "");

            //如果是第二次設定,就自動帶出上次存在regedit裡面的路徑

            txtFileDir.Text = clsCommon.FileDir;
            txtFile.Text = clsCommon.FileName;

            toolTip1.SetToolTip(txtFile, "File Name KeyWord");

            SetAutoRun(true, Application.ProductName, Application.StartupPath + @"\EquipmentMonitor.exe");      //superchai Add 20230705
        }
        /// <summary>
        /// 定義目前有開發的機型
        /// </summary>
        /// 

        //--------------superchai Add 20230705 (B)--------------------
        public void SetAutoRun(bool Started, string name, string path)
        {
            RegistryKey hkml = Registry.CurrentUser;
            RegistryKey Run = hkml.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            if (Started == true)
            {
                Run.SetValue(name, path);
                hkml.Close();
            }
            else
            {
                try
                {
                    if (Run.GetValue(name) != null)
                    {
                        Run.DeleteSubKey(name, false);
                        hkml.Close();
                    }
                }
                catch (Exception)
                {
                }

            }
        }
        //--------------superchai Add 20230705 (E)--------------------

        private void cbStation_SelectedValueChanged(object sender, EventArgs e)
        {
            switch (cbMachineType.Text)
            {
                case "印刷机":
                    addMachineComboBox(new[] { "HORIZON_03IX", "MPM", "GPX", "SPG", "SP60", "Versaprint" });
                    break;
                case "回焊炉":
                    addMachineComboBox(new[] { "HOTFLOW 3/20", "VXP944_REHM", "VXS944_REHM", "VXP945_REHM", "HOTFLOW 3/26XL", "HELLER-1826MK5" });
                    break;
                case "压合机":
                    addMachineComboBox(new[] { "LPM1", "MEP-6T-TYCO", "CBP-5T", "SX_PF03" });
                    break;
                case "波峰焊":
                    addMachineComboBox(new[] { "POWERFLOW_N2/XL" });
                    break;
                case "裁板机":
                    addMachineComboBox(new[] { "EM5700", "ER6000", "WT-500L", "EXE880M" });
                    //addMachineComboBox(new[] { "ER6000" });                    
                    break;
                case "选择焊":
                    addMachineComboBox(new[] { "VERSAFLOW_3_SERIALS", "VERSAFLOW W3", "VERSAFLOW 3/66", "VERSAFLOW 3/45", "ECOSELECT2" });
                    break;
                case "贴片机":
                    if(cbFactory.Text.Trim()=="F2")
                        addMachineComboBox(new[] { "AIMEX", "NXT_I", "NXT_II", "NXT_III", "NPM" , "CMDT-S", "CMDT-C", "CNPM_PANACIM", "SNPM_PANACIM" });
                    else
                        addMachineComboBox(new[] { "AIMEX", "NXT_I", "NXT_II", "NXT_III", "NPM", "CNPM", "SNPM", "CMDT-S", "CMDT-C", "CNPM_PANACIM", "SNPM_PANACIM" });
                    break;
                case "镭雕机":
                    addMachineComboBox(new[] { "SPARK-CD" , "SHINE-F1","TP350", "LCB10C", "LAA10C" , "LMP10C", "LAA20F", "LCD10C", "LCB-V" });
                    break;
                case "点胶机":
                    addMachineComboBox(new[] { "FX-D", "AD-16-DSWH2", "S2-910/920", "AU77S", "EM-5701N", "DELTA6/8" });
                    break;
                default:
                    cbMachineType.Items.Clear();
                    break;
            }
            showEQSN();
        }

        private void cbMachine_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cbMachineType.Text)
            {
                case "HORIZON_03IX":
                    addMachineSoftVerComboBox(new[] { "1.0" });
                    break;
                default:
                    addMachineSoftVerComboBox(new[] { "1.0" });
                    break;
            }
        }

        private void BindingComboBoxWithDataTable(ComboBox cb, DataTable dt, string colName, string defaultValue)
        {
            cb.DisplayMember = colName;
            cb.ValueMember = colName;
            cb.DataSource = dt;
            cb.SelectedValue = defaultValue;
        }

        private void btnExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void btnDir_Click(object sender, System.EventArgs e)
        {
            //如果是贴片机,就只要連線資料,直接連到資料庫去
            if (cbMachine.Text == "NXT_I" ||
                cbMachine.Text == "NXT_II" ||
                cbMachine.Text == "NXT_III" ||
                cbMachine.Text == "AIMEX")
            {
                if (cbFactory.Text == String.Empty)
                {
                    MessageBox.Show("Input Factory");
                    return;
                }
                clsCommon.Factory = cbFactory.Text.Trim();
                dlgDBInfo Dialog = new dlgDBInfo();
                if (Dialog.ShowDialog(this) == DialogResult.OK)
                {
                    clsCommon.DB_Conn2 = Dialog.m_conn;
                    txtFileDir.Text = clsCommon.DB_Conn2.Substring(0, clsCommon.DB_Conn2.IndexOf("PWD") - 1); ;
                    txtFile.Text = "DataBase Connection";
                }
                else
                {

                }
                Dialog.Dispose();
            }
            else
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "Select file";
                dialog.InitialDirectory = ".\\";
                dialog.Filter = "files (*.*)|";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    
                    string sFileName = Path.GetFileName(dialog.FileName);
                    string sFilePath = Path.GetDirectoryName(dialog.FileName);
                    if (cbMachine.Text == "CMDT-S" || cbMachine.Text == "CMDT-C" || cbMachine.Text == "SP60")
                    {
                        System.IO.DirectoryInfo topDir = System.IO.Directory.GetParent(sFilePath);
                        txtFileDir.Text = topDir.FullName;
                    }
                    else
                    {
                        txtFileDir.Text = sFilePath;
                    }
                    txtFile.Text = sFileName;
                }
            }
        }

        private void btnSave_Click(object sender, System.EventArgs e)
        {
            clsCommon.EQ_SN = lb_EQ_SN.Text;
            clsCommon.Site = cbSite.Text;
            clsCommon.Factory = cbFactory.Text;
            clsCommon.Line = cbLine.Text;
            clsCommon.Station = cbStation.Text;
            clsCommon.MachineType = cbMachineType.Text;
            string msg = CheckInput();
            if (msg.Equals("OK"))
            {
                msg = SaveSetting();
                var MsgBoxIcon = msg.Equals("OK") ? MessageBoxIcon.Information : MessageBoxIcon.Error;
                MessageBox.Show(msg, this.Text + "-", MessageBoxButtons.OK, MsgBoxIcon);
            }
            else
            {
                MessageBox.Show(msg, this.Text + "-", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            //System.Threading.Thread.Sleep(3000);        //superchai add 20231002
            this.Close();       //superchai add 20231002
        }

        private string SaveSetting()
        {
            string msg = "OK";

            try
            {
                if (clsCommon.DB_Conn2.ToUpper().StartsWith("NETWORK LIBRARY"))
                {
                    clsCommon.FileDir = clsCommon.DB_Conn2;
                }
                else
                {
                    clsCommon.FileDir = txtFileDir.Text.Trim();
                }

                clsCommon.EQ_SN = lb_EQ_SN.Text;
                clsCommon.Site = cbSite.Text;
                clsCommon.Factory = cbFactory.Text;                
                clsCommon.Station = cbStation.Text;
                clsCommon.MachineType = cbMachineType.Text;
                string MachingName = cbMachine.Text;
                if (cbFactory.Text == "PU11-F1" || cbFactory.Text == "F3" || cbFactory.Text == "BGM" || cbFactory.Text == "NB6-3" || cbFactory.Text == "NB6-2" || cbFactory.Text == "NB6-1" || cbFactory.Text == "PU10" || cbFactory.Text == "PU11")  //00001 针对PU11 NPM做的修改 新增NB6 PU10 
                {
                    if (cbMachine.Text == "CNPM" || cbMachine.Text == "SNPM")
                    {
                        clsCommon.SIDE = cbMachine.Text.Substring(0, 1);
                        clsCommon.MachineName = MachingName.Substring(1, MachingName.Length - 1);
                        cbMachine.Text = cbMachine.Text.Substring(1, cbMachine.Text.Length - 1);
                    }
   
                   else if  (cbMachine.Text == "CNPM_PANACIM" || cbMachine.Text == "SNPM_PANACIM")
                    {
                        clsCommon.SIDE = cbMachine.Text.Substring(0, 1);
                        clsCommon.MachineName = MachingName.Substring(1, MachingName.Length-1);
                        cbMachine.Text = cbMachine.Text.Substring(1, cbMachine.Text.Length- 1);
                    }
                    else
                    {
                        clsCommon.MachineName = cbMachine.Text;
                        clsCommon.SIDE = "";
                    }

                }
                else if(cbFactory.Text=="F2" && cbMachine.Text=="NPM")
                {
                    clsCommon.MachineName = cbMachine.Text;
                    clsCommon.SIDE = cbStation.Text.Substring(0,1);        
                } 

                else 
                {
                    clsCommon.MachineName = cbMachine.Text;
                    clsCommon.SIDE = "";
                }                
                clsCommon.MachineSWVer = cbMachineSoftVer.Text;
                clsCommon.FileDir = txtFileDir.Text;

                if (clsCommon.FileDir.ToUpper().StartsWith("NETWORK LIBRARY"))
                {
                    clsCommon.FileDir = clsCommon.DB_Conn2;
                }
                clsCommon.FileName = txtFile.Text;
                clsCommon.Remark = txtRemark.Text;

                clsCommon.Line = cbLine.Text;
                m_db.Setting("SET");

                txtFileDir.Text = clsCommon.FileDir;
                txtFile.Text = clsCommon.FileName;

                if (cBDelFlag.Checked)
                {
                    //Y=delete Bak log.
                    clsCommon.DelBakLog = "Y";
                    //确保输入的信息可转换成int类型
                    if (txtConvertToInt(txtBakDay.Text))
                    {
                        clsCommon.DelBakLogByDay = txtBakDay.Text;
                    }
                }
                else
                {
                    //N=no delete Bak log.
                    clsCommon.DelBakLog = "N";
                    clsCommon.DelBakLogByDay = "7";
                }

                clsCommon.WriteRegInfo(lb_EQ_SN.Text,cbSite.Text, cbFactory.Text, cbLine.Text,
                    cbStation.Text, cbMachineType.Text, cbMachine.Text, cbMachineSoftVer.Text,
                    clsCommon.FileDir, txtFile.Text, txtRemark.Text,clsCommon.SIDE, clsCommon.DelBakLogByDay, clsCommon.DelBakLog);

            }
            catch (Exception ex)
            {
                msg = ex.Message + "\n" + ex.StackTrace;
            }

            return msg;
        }

        private string CheckInput()
        {
            string msg = "OK";
            if (cbSite.Text.Trim().Equals(string.Empty) || cbFactory.Text.Trim().Equals(string.Empty)
                || cbLine.Text.Trim().Equals(string.Empty) || cbMachineType.Text.Trim().Equals(string.Empty)
                || cbMachine.Text.Trim().Equals(string.Empty) || cbMachineSoftVer.Text.Trim().Equals(string.Empty)
                || txtFileDir.Text.Trim().Equals(string.Empty) || txtFile.Text.Trim().Equals(string.Empty))
            {
                msg = "Please check the values in the fields: [Site], [Factory], [Line], [Station], [Machine], [SW Ver], [Directory] and [FileName].";
            }

            //檢查是否有在定義中
            //--superchai add 20231002 (B)--
            string result = "";
            DataTable dt = new DataTable();
            dt = m_db.Setting("CheckLineMachine");
            if (dt.Rows.Count > 0)
            {
                result = dt.Rows[0][0].ToString();
            }
            //result = m_db.Setting("CheckLineMachine").Rows[0][0].ToString();
            //--superchai add 20231002 (E)--

            if (result != "OK") msg = result;

            if (msg.Equals("OK"))
            {
                cbSite.Text = cbSite.Text.ToUpper();
                cbFactory.Text = cbFactory.Text.ToUpper();
                cbLine.Text = cbLine.Text.ToUpper();
                cbMachineType.Text = cbMachineType.Text.ToUpper();
                cbMachine.Text = cbMachine.Text.ToUpper();
            }

            return msg;
        }
        private void addMachineComboBox(string[] arrStr)
        {
            cbMachine.Items.Clear();
            foreach (string str in arrStr)
            {
                cbMachine.Items.Add(str);
            }
        }
        private void addMachineSoftVerComboBox(string[] arrStr)
        {
            cbMachineSoftVer.Items.Clear();
            foreach (string str in arrStr)
            {
                cbMachineSoftVer.Items.Add(str);
            }
            //Default select first one.
            if (cbMachineSoftVer.Items.Count > 0)
            {
                cbMachineSoftVer.SelectedIndex = 0;
            }
        }

        private void cbFactory_SelectionChangeCommitted(object sender, EventArgs e)
        {
            string sSIte = cbSite.SelectedValue.ToString();
            string sFactory = cbFactory.SelectedValue.ToString();
            if (sFactory == string.Empty) return;
            string filter = "Site = '" + cbSite.Text + "' and Factory= '" + sFactory + "'";
            DataRow[] rows = m_dtQIEMS_Define_EQ_SN.Select(filter);
            DataTable newdt = rows.CopyToDataTable().DefaultView.ToTable(true, "Line");
            newdt.DefaultView.Sort = "Line";
            BindingComboBoxWithDataTable(cbLine, newdt, "Line", clsCommon.Line);
        }

        private void cbSite_SelectionChangeCommitted(object sender, EventArgs e)
        {
            string sSIte = cbSite.SelectedValue.ToString();
            if (sSIte == string.Empty) return;
            
            string[] columnNames = new string[] { "Factory" };
            string filter = "Site = '" + sSIte + "' ";
            DataRow[] rows = m_dtQIEMS_Define_EQ_SN.Select(filter);
            DataTable newdt = rows.CopyToDataTable().DefaultView.ToTable(true, "Factory");
            newdt.DefaultView.Sort = "Factory";
            BindingComboBoxWithDataTable(cbFactory, newdt, "Factory", clsCommon.Factory);
        }

        private void cbLine_SelectionChangeCommitted(object sender, EventArgs e)
        {
            string sSIte = cbSite.SelectedValue.ToString();
            string sFactory = cbFactory.SelectedValue.ToString();
            string sLine = cbLine.SelectedValue.ToString();

            if (sLine == string.Empty) return;
            string filter = "Site = '" + cbSite.Text + "' and Factory= '" + sFactory + "' and Line = '"+sLine+"'";
            DataRow[] rows = m_dtQIEMS_Define_EQ_SN.Select(filter);
            DataTable newdt = rows.CopyToDataTable().DefaultView.ToTable(true, "Station");
            //newdt.DefaultView.Sort = "Station";
            BindingComboBoxWithDataTable(cbStation, newdt, "Station", clsCommon.Station);

            showEQSN();
            
        }

        private void showEQSN()
        {
            string sSIte = cbSite.SelectedValue.ToString();
            string sFactory = cbFactory.SelectedValue.ToString();
            string sLine = cbLine.SelectedValue.ToString();
            string sStation = "";
            if (cbStation.SelectedValue != null)
                sStation = cbStation.SelectedValue.ToString();            

            string sfilter = "Site = '" + cbSite.Text + "' and Factory= '" + sFactory + "' and Line = '" + sLine + "' and Station = '" + sStation + "'";
            string sSort = "EQ_SN asc";
            DataRow[] rows = m_dtQIEMS_Define_EQ_SN.Select(sfilter,sSort);
            if (rows.Length == 0) return;
            DataTable newdt = rows.CopyToDataTable().DefaultView.ToTable(true, "EQ_SN");
            if (newdt.Rows.Count > 0)
            {
                lb_EQ_SN.Text = newdt.Rows[0][0].ToString();
            }
        }

        private void btn_add_EqSN_Click(object sender, EventArgs e)
        {
            dlgEqu_SN Dialog = new dlgEqu_SN();
            Dialog.setting(cbMachine.Text.Trim());
            if (Dialog.ShowDialog(this) == DialogResult.OK)
            {
                m_lsBoxList = Dialog.m_lsBoxList;                
            }
            else
            {

            }
            Dialog.Dispose();

        }
        public static bool txtConvertToInt(string txt)
        {
            bool f  = true;
            int result = 0;
             f = int.TryParse(txt, out result);
            if (!f)
            {
                MessageBox.Show("请输入数字，谢谢！");
            }
            return f;
        }


    }
}
