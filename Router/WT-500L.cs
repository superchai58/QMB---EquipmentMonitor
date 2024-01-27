using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace EquipmentMonitor.Router
{
    class WT_500L
    {
        DateTime m_TimeStamp;
        clsDB m_db;
        DataTable m_dtMachineLog;
        DataTable m_dtErrMapping;
        clsLog m_clsLog;
        clsParserMethod m_parser;

        internal string ParsingLog(clsLog log)
        {
            m_db = new clsDB();
            m_clsLog = log;
            m_parser = new clsParserMethod();
            string result = "OK";
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

                //將檔案先搬移並且重新命名
                //result = clsCommon.MoveFile(sFilePath);
                result = clsCommon.CopyFile(sFilePath);
                if (!result.Equals("OK"))
                {
                    m_clsLog.writeERRORLog(result);
                    return "";
                }


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
                    //將剛剛處理的檔案搬移到備份資料夾
                    if (!clsCommon.BakFile(ref result))
                    {
                        m_clsLog.writeERRORLog(result);
                    }
                    if (clsCommon.DelBakLog == "Y")
                    {
                        string BakLogPath = Path.Combine(clsCommon.FileDir, "Bak");
                        int day = 7;
                        bool f = int.TryParse(clsCommon.DelBakLogByDay, out day);
                        m_parser.DeleteBakLogByDay(BakLogPath, day, m_clsLog);
                    }
                }
            }
            catch (Exception exp)
            {
                m_clsLog.writeERRORLog(exp.ToString());
            }
            finally
            {

            }
            return result;
        }


        private Boolean ReadFileToTable(string sFilePath)
        {
            DataTable dtTemp = new DataTable();
            DataTable dtResult = new DataTable();
            if (m_dtMachineLog != null)
            {
                m_dtMachineLog.Rows.Clear();//Richard 20190111 避免記憶體中還存著舊的資料
            }

            if (!File.Exists(sFilePath))
            {
                m_clsLog.writeERRORLog(clsTerm.FILE_NOT_FOUND + Path.Combine(clsCommon.FileDir, clsCommon.FileName));
                return false;
            }

            m_parser.InitDataTable(ref dtTemp);

            try
            {
                string[] lines = File.ReadAllLines(sFilePath, System.Text.Encoding.Default);
                foreach (string line in lines)
                {
                    /*
                     *  s1 = rowMsg["ErrMsg"].ToString().Trim().ToUpper();
                System.Threading.Thread.Sleep(1);
                foreach (DataRow rowErr in dtErrMapping.Rows)
                {                    
                    s2 = rowErr["MachineErrorMsg"].ToString().Trim().ToUpper();
                    if (s1.Contains(s2))
                     * 
                    */

                    //if (line.Contains("Alarm Code="))
                    //{
                    //    string sTime = line.Substring(0, 4) + "-" + line.Substring(4, 2) + "-" + line.Substring(6, 2) + " " + line.Substring(8, 2) + ":" + line.Substring(10, 2) + ":" + line.Substring(12, 2);

                    //    string sError = line.Substring(26, line.Length - 26);  //将时间去掉
                    //    sError = sError.Substring(0, sError.IndexOf(";", 2)); //将Errorcode后面多余的去掉
                    //    dtTemp.Rows.Add(sTime, sError, "", clsCommon.EQ_SN);
                    //}
                    if (line.Contains("PRO:")) //20190817
                    {
                        string sTime = line.Substring(0, 4) + "-" + line.Substring(5, 2) + "-" + line.Substring(8, 2) + " " + line.Substring(11, 2) + ":" + line.Substring(14, 2) + ":" + line.Substring(17, 2);

                        string sError = line.Substring(29, line.Length - 29);  //将时间去掉
                        sError = sError.Substring(4, sError.Length - 4); //将Errorcode前面多余的去掉
                        dtTemp.Rows.Add(sTime, sError, "", clsCommon.EQ_SN);
                    }

                }

                string filter = "TransDateTime > '" + m_TimeStamp.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
                DataRow[] rows = dtTemp.Select(filter);
                //int i = dtTemp.Rows.Count;
                if (rows.Length > 0)
                {
                    m_dtMachineLog = rows.CopyToDataTable();
                    m_dtMachineLog.TableName = clsParserMethod.m_ERR_TABLE_NAME;

                    m_clsLog.writeCOMMLog(clsTerm.GET_File_RECORD_COUNT + m_dtMachineLog.Rows.Count.ToString());
                }
                else
                {
                    m_clsLog.writeIMPORTLog(clsTerm.NO_NEW_LOG);
                    //m_dtMachineLog.Rows.Clear();//Richard 20190111 避免記憶體中還存著舊的資料

                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                m_clsLog.writeERRORLog(clsTerm.READ_FILE_FAIL + ex.ToString());
                return false;
            }
        }

    }
}
