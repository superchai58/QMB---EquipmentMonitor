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
    /// <summary>
    /// 存入 設備管理系統的SN QRCode
    /// 
    /// </summary>
    public partial class dlgEqu_SN : Form
    {
        public string m_MachineName = "";
        public List<string> m_StrArr = new List<string>();
        
        public List<ListBox> m_lsBoxList = new List<ListBox>();

        public dlgEqu_SN()
        {
            InitializeComponent();
        }
        public void setting(string MachineName)
        {
            m_MachineName = MachineName;

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            m_lsBoxList.Add(ls_SN1);
            this.DialogResult = DialogResult.OK;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtSN1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != Convert.ToChar(13)) return;
            ls_SN1.Items.Add(txtSN1.Text.Trim());
            txtSN1.Text = "";
            txtSN1.Focus();
        }

        private void ls_SN1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            for (int i = ls_SN1.SelectedIndices.Count-1; i >= 0; i--)
            {
                ls_SN1.Items.RemoveAt(ls_SN1.SelectedIndices[i]);
            }
        }
    }
}
