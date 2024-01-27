using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace EquipmentMonitor.Mounter
{
    /// <summary>
    /// 這個是吃XML LOG , 廢棄使用
    /// </summary>
    class NXT_I
    {
        const string m_ERR_TABLE_NAME = "ErrData";
        DateTime m_TimeStamp;
        //string FilePath = @"";
        clsDB db;
        DataTable m_dtMachineLog;
        DataTable m_dtErrMapping;

        internal string ParsingLog()
        {
            db = new clsDB();
            string result = "OK";
            string sFilePath = "";
            try
            {
                //Get Time Stamp
                updateTimeStampFromDB("GETTimeStamp");

                //讀取Error Code Mapping
                m_dtErrMapping = db.execQIEMS_GetErrorMapping("GET");
                if (m_dtErrMapping != null && m_dtErrMapping.Rows.Count == 0)
                    return clsTerm.ERRCODE_DEFINE_ERROR;

                //從資料夾中,取得要處理的檔案名稱
                if (!getProcessFileName(ref sFilePath)) return clsTerm.NO_LOG_FILE;

                //將檔案先搬移並且重新命名
                //result = clsCommon.MoveFile(sFilePath);
                //if (!result.Equals("OK")) return result;

                //將log file 載入 datatable中,並且僅留下要處理的資料
                m_dtMachineLog = ReadFileToTable(sFilePath);//clsCommon.FilePath

                //將要處理的資料對應Error Code貼上記號
                processErrorCodeMapping(ref m_dtMachineLog, ref m_dtErrMapping);

                //清除沒有包含Error Code的紀錄
                m_dtMachineLog = KeepOnlyErrorCodeRow();

                //回存DB
                result = saveResultToDB();

                //將剛剛處理的檔案搬移到備份資料夾
                if (!clsCommon.BakFile(ref result)) return result;

                updateTimeStampFromDB("SETTimeStamp");

            }
            catch (Exception exp)
            {
                result = exp.ToString();
            }
            finally
            {

            }
            return result;
        }

        private static Boolean getProcessFileName(ref string sFilePath)
        {
            try
            {
                string[] Files = Directory.GetFiles(clsCommon.FileDir, "*" + clsCommon.FileName + "*");
                foreach (string file in Files)
                {
                    sFilePath = file;
                }
                if (sFilePath.Equals(String.Empty))
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private DataTable KeepOnlyErrorCodeRow()
        {
            DataTable dt = new DataTable();
            //如果前面讀取到的LOG是空的,就直接返回
            if (m_dtMachineLog == null || m_dtMachineLog.Columns.Count == 0)
                return dt;
            DataRow[] rows = m_dtMachineLog.Select("ErrCode <>'' ");
            if (rows.Length == 0)
                return dt;

            dt = rows.CopyToDataTable();
            dt.TableName = m_ERR_TABLE_NAME;
            return dt;
        }

        private string saveResultToDB()
        {
            string result = "";
            if (m_dtMachineLog.Columns.Count == 0)
            {
                result = clsTerm.NO_NEW_LOG;
                return result;
            }
            //如果是發生錯誤,就抓第一個錯誤訊息

            if (m_dtMachineLog.Columns[0].ColumnName.Equals("ErrorMsg"))
            {
                result = m_dtMachineLog.Rows[0][0].ToString();
                return result;
            }

            //如果沒有錯誤,就存入DB
            try
            {
                result = db.InsErrDataToDB(m_dtMachineLog);
                return result;
            }
            catch (Exception ex)
            {
                result = "Insert data fail. Error Message:\n" + ex.Message + "\n" + ex.StackTrace;
            }
            return result;
        }

        private void updateTimeStampFromDB(string sActionType)
        {
            DataTable dt;
            dt = db.Setting(sActionType);
            if (dt != null && dt.Rows.Count > 0)
            {
                String sLastParseLogTime = dt.Rows[0][0].ToString();
                m_TimeStamp = DateTime.Parse(sLastParseLogTime);
            }
        }

        private void processErrorCodeMapping(ref DataTable dtMachineLog, ref DataTable dtErrMapping)
        {
            string s1, s2;
            foreach (DataRow rowMsg in dtMachineLog.Rows)
            {
                s1 = rowMsg["ErrMsg"].ToString();
                foreach (DataRow rowErr in dtErrMapping.Rows)
                {
                    s2 = rowErr["MachineErrorMsg"].ToString();

                    if (s1.Contains(s2))
                    {
                        rowMsg["ErrCode"] = rowErr["SFErrorCode"];
                        break;
                    }
                }
            }
        }

        private void InitDataTable(ref DataTable table)
        {
            if (table.Columns.Count > 0) return;
            table.Columns.Add("TransDateTime", typeof(string));
            table.Columns.Add("ErrMsg", typeof(string));
            table.Columns.Add("ErrCode", typeof(string));
        }

        private DataTable ReadFileToTable(string sFilePath)
        {
            DataTable dtTemp = new DataTable();
            DataTable dtResult = new DataTable();
            if (!File.Exists(sFilePath))
            {
                FillErrorMsgInDataTable("File not found : " + Path.Combine(clsCommon.FileDir, clsCommon.FileName));
                return dtResult;
            }

            InitDataTable(ref dtTemp);
            try
            {

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(sFilePath);
                //可將xml裡的<Flash>元素都存到nodelist裡
                
                //抓時間,雖然有不同machine，但時間理論上是一樣的,所以抓第一筆就夠了
                string STime = xmlDoc.SelectNodes("//Report/Result/Machines/Machine/Recipes/Recipe/StartTime")[0].InnerText;
                string ETime = xmlDoc.SelectNodes("//Report/Result/Machines/Machine/Recipes/Recipe/EndTime")[0].InnerText;

                XmlNodeList nodelist = xmlDoc.SelectNodes("//Report/Result/Machines/Machine");

                foreach (XmlNode aNode in nodelist)
                {
                    string sMachineName = aNode["Name"].InnerText;
                    XmlNodeList bNode = aNode.SelectNodes("Recipes/Recipe/Items/Item");
                    foreach (XmlNode cNode in bNode)
                    {
                        string sErrorCode = cNode["ErrorCode"].InnerText;
                    }
                }   
                 

                string filter = "TransDateTime > '" + m_TimeStamp.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
                DataRow[] rows = dtTemp.Select(filter);

                if (rows.Length > 0)
                {
                    dtResult = rows.CopyToDataTable();
                    dtResult.TableName = "ErrData";
                }
            }
            catch (Exception ex)
            {
                FillErrorMsgInDataTable("Read file fail at line: " + (m_dtMachineLog.Rows.Count + 1).ToString() + " Error message:\n" + ex.Message + "\n" + ex.StackTrace);
            }
            
            return dtResult;
        }

        private void FillErrorMsgInDataTable(string msg)
        {
            m_dtMachineLog = new DataTable();

            DataColumn column;
            DataRow row;

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "ErrorMsg";
            m_dtMachineLog.Columns.Add(column);

            row = m_dtMachineLog.NewRow();
            row["ErrorMsg"] = msg;
            m_dtMachineLog.Rows.Add(row);
        }
    }

}
