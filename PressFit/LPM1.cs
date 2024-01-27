using System;
using System.Data;
using System.Data.OleDb;
using System.IO;

namespace EquipmentMonitor.PressFit
{
    class LPM1
    {
        OleDbConnection m_conn;

        DateTime m_TimeStamp;
        clsDB m_db;
        DataTable m_dtMachineLog;
        DataTable m_dtErrMapping;
        clsLog m_clsLog;
        clsParserMethod m_parser;
        //string FilePath = @"D:\D-文件\SOP\QIEMS\SampleFile\波峰焊\設備報警訊息\Meldungen01.mdb";

        internal string ParsingLog(clsLog log)
        {
            m_db = new clsDB();
            m_clsLog = log;
            m_parser = new clsParserMethod();
            string result = "OK";
            string sFilePath = "";

            //Get Time Stamp
            m_parser.GetTimeStampFromDB("GETTimeStamp", m_db, ref m_TimeStamp, m_clsLog);
            m_clsLog.writeCOMMLog(clsTerm.GET_LAST_PRO_TIME + m_TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));

            //讀取Error Code Mapping
            m_dtErrMapping = m_db.execQIEMS_GetErrorMapping("GET");
            if (m_dtErrMapping != null && m_dtErrMapping.Rows.Count == 0)
            {
                m_clsLog.writeERRORLog(clsTerm.ERRCODE_DEFINE_ERROR);
                return "";
            }
            m_clsLog.writeCOMMLog(clsTerm.GET_ERROR_MAPPING + m_dtErrMapping.Rows.Count.ToString());

            //從資料夾中,取得要處理的檔案名稱
            if (!m_parser.getProcessFileName(ref sFilePath))
            {
                m_clsLog.writeERRORLog(clsTerm.NO_LOG_FILE);
                return "";
            }

            long Filelength = new System.IO.FileInfo(sFilePath).Length;//in Byte
            if (Filelength > clsCommon.LOG_SIZE_LIMIT_BYLE)
            {
                m_clsLog.writeERRORLog(clsTerm.FILE_SIZE_TOO_LARGE + sFilePath);
                return "";
            }

            //僅複製檔案,因為檔案會被機台程式lock,或不會自動再產生
            result = clsCommon.CopyFile(sFilePath);

            //只要檔案重新命名之後,就應該將該檔案搬移到BAK中
            try
            {
                //將log file 載入 DataTable中,並且僅留下要處理的資料
                if (!ReadFileToTable(clsCommon.FilePath)) return "";
                

                //將要處理的資料對應Error Code貼上記號
                m_parser.processErrorMsgMapping(ref m_dtMachineLog, ref m_dtErrMapping);

                //清除沒有包含Error Code的紀錄
                m_parser.KeepOnlyErrorCodeRow(ref m_dtMachineLog);

                //回存DB
                if (!m_parser.saveResultToDB(m_dtMachineLog, m_clsLog, m_db)) return "";
                m_parser.SetTimeStampFromDB("SETTimeStamp", m_db, ref m_TimeStamp, m_clsLog);
            }
            finally
            {
                if (m_conn.State != ConnectionState.Closed) { m_conn.Close(); }
                string strMsg = clsCommon.DelFile();
                //if (!strMsg.Equals(string.Empty)) m_clsLog.writeERRORLog(strMsg);
            }               
            return result;
        }

 

        private Boolean ReadFileToTable(string sFilePath)
        {
            string msg = CreateConnection();    //Connect to the access file

            if (msg.Equals("OK"))
            {
                GetErrData();
                return true;
            }
            else
            {
                m_clsLog.writeERRORLog(msg);
                return false;
            } 
        }

        private void GetErrData()
        {
            m_dtMachineLog = new DataTable();
            m_dtMachineLog.TableName = clsParserMethod.m_ERR_TABLE_NAME;
 

            string strSQL =
                "SELECT Format(TagDataTime, 'yyyy/MM/dd hh:mm:ss') AS [TransDateTime], '' AS [ErrCode], Msg AS [ErrMsg], '" + clsCommon.EQ_SN + "' AS EQ_SN " +
                "FROM PSlot "+
                "WHERE TagDataTime >= @LastParseTime";

            try
            {
                string sTime = m_TimeStamp.ToString("yyyy/MM/dd HH:mm:ss");
                if (m_conn.State == ConnectionState.Closed) { m_conn.Open(); }

                OleDbCommand oleCmd = new OleDbCommand(strSQL, m_conn);
                oleCmd.Parameters.AddWithValue("@LastParseTime", DbType.DateTime).Value = sTime;
                //oleCmd.Parameters.AddWithValue("@LastParseTime", DbType.DateTime).Value = DateTime.Parse("2017/10/20 00:00:00").ToString("yyyy/MM/dd hh:mm:ss");

                //oleCmd.Parameters.AddWithValue("@PressError", DbType.String).Value = "Press Error.";
                //oleCmd.Parameters.AddWithValue("@DistanceError", DbType.String).Value = "Distance Error.";

                OleDbDataAdapter adapter = new OleDbDataAdapter(oleCmd);
                adapter.Fill(m_dtMachineLog);

                if (m_dtMachineLog != null && m_dtMachineLog.Rows.Count > 0)
                    m_clsLog.writeCOMMLog(clsTerm.GET_File_RECORD_COUNT + m_dtMachineLog.Rows.Count.ToString());

                foreach (DataRow dr in m_dtMachineLog.Rows)
                {
                    dr[0] = dr[0].ToString().Replace('-', '/').Replace('.', '/');
                }

                if (m_dtMachineLog != null && m_dtMachineLog.Rows.Count > 0)
                {
                    string filter = "TransDateTime >= '" + sTime + "'";
                    DataRow[] rows = m_dtMachineLog.Select(filter);
                    if (rows.Length > 0)
                    {
                        m_dtMachineLog = rows.CopyToDataTable();
                        m_dtMachineLog.TableName = clsParserMethod.m_ERR_TABLE_NAME;

                        m_clsLog.writeCOMMLog(clsTerm.GET_File_RECORD_COUNT + m_dtMachineLog.Rows.Count.ToString());
                    }
                }
 
            }
            catch (Exception ex)
            {
                m_clsLog.writeERRORLog("Can not read file. Error message:\n" + ex.Message + "\n" + ex.StackTrace);
                return;
            }
            finally
            {
                if (m_conn.State != ConnectionState.Closed) { m_conn.Close(); }
                
            }
        }

        private string CreateConnection()
        {
            string result = "OK";

            string strConn = "Provider=Microsoft.Jet.OLEDB.4.0;" + //"Data source=" + Path.Combine(clsCommon.FileDir, clsCommon.FileName);
                "Data source=" + clsCommon.FilePath;

            m_conn = new System.Data.OleDb.OleDbConnection(strConn);

            try
            {
                m_conn.Open();
                m_conn.Close();
            }
            catch (Exception ex)
            {
                result = "Can not read file. Error message:\n" + ex.Message + "\n" + ex.StackTrace;
            }

            return result;
        }
    }
}
