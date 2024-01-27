using System;
using System.Data;
using System.Data.OleDb;
using System.IO;

namespace EquipmentMonitor.WaveSoldering
{
    class ERSA
    {
        DateTime m_TimeStamp;

        clsLog m_clsLog;
        clsParserMethod m_parser;

        clsDB m_db;
        OleDbConnection m_conn;
        DataTable m_dtMachineLog;
        DataTable m_dtErrMapping = new DataTable();
        //string FilePath = @"D:\D-文件\SOP\QIEMS\SampleFile\波峰焊\設備報警訊息\Meldungen01.mdb";

        internal string ParsingLog(clsLog log)
        {
            string result = "OK";

            m_db = new clsDB();
            m_clsLog = log;
            m_parser = new clsParserMethod();
            string sFilePath = "";
            try
            {
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

                //僅複製檔案,因為檔案會被機台程式lock
                result = clsCommon.CopyFile(sFilePath);

                //只要檔案重新命名之後,就應該將該檔案搬移到BAK中


                //將log file 載入 DataTable中,並且僅留下要處理的資料

                if (!ReadFileToTable(clsCommon.FilePath)) return "";


                //將要處理的資料對應Error Code貼上記號
                m_parser.processErrorCodeMapping(ref m_dtMachineLog, ref m_dtErrMapping);

                //清除沒有包含Error Code的紀錄

                m_parser.KeepOnlyErrorMsgRow(ref m_dtMachineLog);

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


        private bool ReadFileToTable(string sFilePath)    // .mdb (Access)
        {
            string msg = CreateConnection(sFilePath);    //Connect to the access file

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

            //Tag_aufgetreten 開始日期 Zeit_aufgetreten 開始時間
            //Tag_quittiert 結束日期  Zeit_quittiert 結束時間
            //Betriebsart Auto Model or Maintance Mode,正常是要Auto 生產模式
            string strSQL =
    "SELECT  SWITCH (Zeit_Aufgetreten = NULL,Format(Zeit_quittiert , 'yyyy/MM/dd HH:mm:ss'),Zeit_Aufgetreten <> NULL, Format(Zeit_Aufgetreten , 'yyyy/MM/dd HH:mm:ss')) AS [TransDateTime] ," + //  //
    "   Nummer AS [ErrCode], '' AS [ErrMsg], '" + clsCommon.EQ_SN + "' AS EQ_SN " +
    " FROM MonatMeldung " +
    " WHERE    (Zeit_Aufgetreten >= @LastParseDateTime AND Zeit_Aufgetreten <= @Now_DateTime) " +
    "       OR (Zeit_quittiert >=   @LastParseDateTime AND Zeit_quittiert   <= @Now_DateTime ) " +
    "  AND Typ IN (@Type_Crash_SC,@Type_Danger_SC,@Type_Crash_EN,@Type_Danger_EN) " +
    "  AND Betriebsart IN (@Mode_SC,@Mode_EN)";

            try
            {
                string sTime = m_TimeStamp.ToString("yyyy/MM/dd HH:mm:ss"); //bug ?? 0001/01/01 00:00:00
                string sTime2 = m_TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"); //bug ?? 0001/01/01 00:00:00
                string sTime_Now = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                if (m_conn.State == ConnectionState.Closed) { m_conn.Open(); }

                OleDbCommand oleCmd = new OleDbCommand(strSQL, m_conn);
                oleCmd.Parameters.AddWithValue("@LastParseDateTime", DbType.DateTime).Value = sTime;
                oleCmd.Parameters.AddWithValue("@Now_DateTime", DbType.DateTime).Value = sTime_Now;
                //oleCmd.Parameters.AddWithValue("@LastParseDate", DbType.DateTime).Value = DateTime.Parse("2018/06/20 00:00:00").ToString("yyyy/MM/dd");
                //oleCmd.Parameters.AddWithValue("@LastParseTime", DbType.DateTime).Value = DateTime.Parse("2018/06/20 00:00:00").ToString("hh:mm:ss");
                //避免未來時間在LOG中,所以設定區間 上次處理時間~現在
                oleCmd.Parameters.AddWithValue("@Type_Crash_SC", DbType.String).Value = "故障信息";
                oleCmd.Parameters.AddWithValue("@Type_Crash_EN", DbType.String).Value = "Failure message";
                oleCmd.Parameters.AddWithValue("@Type_Danger_SC", DbType.String).Value = "危险";
                oleCmd.Parameters.AddWithValue("@Type_Danger_EN", DbType.String).Value = "Danger";
                oleCmd.Parameters.AddWithValue("@Mode_SC", DbType.String).Value = "自动模式";
                oleCmd.Parameters.AddWithValue("@Mode_EN", DbType.String).Value = "Automatic mode";
                //Richard  DS說 維護模式不要放了
                //oleCmd.Parameters.AddWithValue("@Mode2_SC", DbType.String).Value = "维护模式";
                //oleCmd.Parameters.AddWithValue("@Mode2_EN", DbType.String).Value = "Maintenance mode";


                OleDbDataAdapter adapter = new OleDbDataAdapter(oleCmd);
                adapter.Fill(m_dtMachineLog);

                if (m_dtMachineLog != null && m_dtMachineLog.Rows.Count > 0)
                    m_clsLog.writeCOMMLog(clsTerm.GET_File_RECORD_COUNT + m_dtMachineLog.Rows.Count.ToString());

                foreach (DataRow dr in m_dtMachineLog.Rows)
                {
                    dr[0] = dr[0].ToString().Replace('-', '/').Replace('.', '/');
                    m_clsLog.writeCOMMLog(dr[0].ToString());
                }

                if (m_dtMachineLog != null && m_dtMachineLog.Rows.Count > 0)
                {
                    string filter = "TransDateTime >= '" + sTime + "'";
                    DataRow[] rows = m_dtMachineLog.Select(filter);
                    m_clsLog.writeCOMMLog("filter Str1=" + filter + ",Count=" + rows.Length.ToString());
                    if (rows.Length > 0)
                    {
                        m_dtMachineLog = rows.CopyToDataTable();
                        m_dtMachineLog.TableName = clsParserMethod.m_ERR_TABLE_NAME;

                        m_clsLog.writeCOMMLog("filter_" + clsTerm.GET_File_RECORD_COUNT + m_dtMachineLog.Rows.Count.ToString());
                    }
                    else
                    {
                        m_dtMachineLog.Rows.Clear();
                        //m_parser.InitDataTable(ref m_dtMachineLog);
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


        private string CreateConnection(string sFilePath)
        {
            string result = "OK";

            string strConn = "Provider=Microsoft.Jet.OLEDB.4.0;" + //"Data source=" + Path.Combine(clsCommon.FileDir, clsCommon.FileName);
                "Data source=" + sFilePath;

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
