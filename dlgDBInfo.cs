using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EquipmentMonitor
{
    public partial class dlgDBInfo : Form
    {
        internal string m_conn;
        public dlgDBInfo()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if(txtIP.Text.Length <5 || txtAccount.Text.Length < 2)
            {
                MessageBox.Show("please input");
                return;
            }
            if(txtIP.Text.Length <5)
            {
                MessageBox.Show("please input IP");
                return;
            }
            getConn();
            try
            {
                //Get Line
                clsDB_Machine sql = new clsDB_Machine(m_conn);

                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(clsTerm.GET_LINE_INFO_FROM_MACHINE_DB_FAIL + ex.Message);
            }
            
        }

        private void getConn()
        {
            m_conn = "Network Library=DBMSSOCN;" +
                "SERVER=" + txtIP.Text.Trim() + ";" +
                "UID=" + txtAccount.Text.Trim() + ";" +                
                "DATABASE=" + cb_DB.Text.Trim() + ";" +
                "PWD=" + txtPWD.Text.Trim() + ";" +
                "CONNECT TIMEOUT=30;" +
                "Application Name=EquipmentMonitor;;";
        }

        private void KeyPressEvent(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 13) return;//Must press ENTER key
            switch (((TextBox)sender).Name)
            {
                case "txtIP":
                    txtAccount.Focus();
                    break;
                case "txtAccount":
                    txtPWD.Focus();
                    break;
                case "txtPWD":
                    btn_Test.Focus();
                    break;
                default:
                    break;
            }
                    
        }

        private void btn_Test_Click(object sender, EventArgs e)
        {
            try
            {
                getConn();
                clsDB_Machine sql = new clsDB_Machine(m_conn);

                DataTable dt = sql.Get_DB_List();
                if (dt == null || dt.Rows.Count == 0) return;

                cb_DB.Items.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    cb_DB.Items.Add(row["name"].ToString());
                }

                btnSave.Enabled = true;
            }
            catch (Exception ex)
            {
                btnSave.Enabled = false;
                MessageBox.Show("Connection Error :" + ex.Message);
            }
        }
    }
}
