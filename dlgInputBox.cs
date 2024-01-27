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
    public partial class dlgInputBox : Form
    {
        internal string m_Value = "";
        public dlgInputBox()
        {
            InitializeComponent();
        }
        public dlgInputBox(string sFormName,string sLabel)
        {
            InitializeComponent();
            this.Text = sFormName;
            lb_Value.Text = sLabel;
            txtValue.Focus();
        }
        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            m_Value = txtValue.Text.Trim();
            this.DialogResult = DialogResult.OK;
        }
    }
}
