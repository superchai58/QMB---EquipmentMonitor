using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EquipmentMonitor
{
    /// <summary>
    /// 寫LOG到RichTextBox中,
    /// 但是new clsLog()物件時,
    ///   需要傳入clsLog(RichTextBox _txbox)或 setTextCompon(RichTextBox _txbox)
    /// </summary>
    class clsLog
    {
        public const int COMM = 0;
        public const int IMPORT = 1;
        public const int WARNING = 2;
        public const int ERROR = 3;

        RichTextBox txbox;
        clsDB m_db;
        private clsLog() { }

        public clsLog(RichTextBox _txbox,clsDB db)
        {
            txbox = _txbox;
            m_db = db;
        }
        public void setTextCompon(RichTextBox _txbox)
        {
            txbox = _txbox;
        }
        private void writeLog(string logStr, int level)
        {
            txbox.Invalidate();
            try
            {
                switch (level)
                {
                    case COMM:
                        txbox.SelectionColor = Color.Black;
                        txbox.SelectionBackColor = Color.White;
                        break;
                    case IMPORT:
                        txbox.SelectionColor = Color.Black;
                        txbox.SelectionBackColor = Color.PaleGreen;
                        break;
                    case WARNING:
                        txbox.SelectionColor = Color.OrangeRed;
                        txbox.SelectionBackColor = Color.White;
                        break;
                    case ERROR:
                        txbox.SelectionColor = Color.Red;
                        txbox.SelectionBackColor = Color.Pink;
                        break;
                    default:
                        txbox.SelectionColor = Color.Red;
                        txbox.SelectionBackColor = Color.Pink;
                        break;
                }
                txbox.AppendText(Environment.NewLine+DateTime.Now.ToString("[HH:mm:ss] ") + logStr);
                txbox.SelectionStart = txbox.Text.Length;
            }
            catch(Exception ex)
            {
                txbox.AppendText(Environment.NewLine + ex.ToString());
            }
            finally
            {
                txbox.ScrollToCaret();
                txbox.Update();
            }
        }

        internal void cleanLog()
        {
            //避免LOG太多
            int iLen = txbox.TextLength;
            while (txbox.Lines.Length > 100) //保留100行
            {
                txbox.SelectionStart = 0;
                txbox.SelectionLength = txbox.Text.IndexOf("\n", 0) + 1;
                txbox.SelectedText = "";
            }
            //if (iLen > 3000)
            //{
            //    string sss = txbox.Text.Substring(iLen - 3000, 3000);
            //    txbox.Text = sss;
            //}
        }

        public void writeCOMMLog(string logstr)
        {
            writeLog(logstr, COMM);
        }
        public void writeIMPORTLog(string logstr)
        {
            writeLog(logstr, IMPORT);
        }
        public void writeWARNINGLog(string logstr)
        {
            writeLog(logstr, WARNING);
        }
        public void writeERRORLog(string logstr)
        {
            writeLog(logstr, ERROR);
            System.Threading.Thread.Sleep(1000);        //superchai add 20231002

            //---------superchai set interval by line 20231002 (B)------------
            //ConnectDBSMT oCon = new ConnectDBSMT();
            //SqlCommand cmd = new SqlCommand();
            //DataTable dtTmp = new DataTable();

            //cmd.CommandText = "Select Interval From IntervalByLine With(nolock) Where Line = @Line";
            //cmd.Parameters.Add(new SqlParameter("@Line", clsCommon.Line.ToString().Trim()));
            //dtTmp = oCon.Query(cmd);
            //if (dt.Rows.Count > 0)
            //{
            //    System.Threading.Thread.Sleep(int.Parse(dt.Rows[0]["Interval"].ToString().Trim()));
            //    //m_ParserInterval = int.Parse(dt.Rows[0]["Interval"].ToString().Trim());
            //}
            //else
            //{
            //    System.Threading.Thread.Sleep(1000);
            //}
            //---------superchai set interval by line 20231002 (E)------------
            m_db.QIEMS_Setting_Status(clsCommon.SetStatus,DateTime.Now.ToString("[MM/dd HH:mm:ss] ") + logstr);
        }
    }
}
