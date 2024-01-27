using System;
using System.Data;
using System.IO;

namespace EquipmentMonitor.Glue
{
    class PVA
    {
        DateTime m_TimeStamp;
        clsDB m_db;
        DataTable m_dtMachineLog;
        DataTable m_dtErrMapping;
        clsLog m_clsLog;
        clsParserMethod m_parser;

        internal string ParsingLog(clsLog log)
        {
            //0005
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


                //僅複製檔案,因為檔案會被機台程式lock
                result = clsCommon.CopyFile(sFilePath);


                //只要檔案重新命名之後,就應該將該檔案搬移到BAK中
                try
                {
                    //將log file 載入 DataTable中,並且僅留下要處理的資料
                    if (!ReadFileToTable(clsCommon.FilePath)) return "";


                    //將要處理的資料對應Error msg貼上記號
                    m_parser.processErrorMsgMapping(ref m_dtMachineLog, ref m_dtErrMapping);

                    //清除沒有包含Error Code的紀錄
                    m_parser.KeepOnlyErrorCodeRow(ref m_dtMachineLog);

                    //回存DB
                    if (!m_parser.saveResultToDB(m_dtMachineLog, m_clsLog, m_db)) return "";
                    m_parser.SetTimeStampFromDB("SETTimeStamp", m_db, ref m_TimeStamp, m_clsLog);
                }
                finally
                {
                    string strMsg = clsCommon.DelFile();
                    if (strMsg != string.Empty) m_clsLog.writeERRORLog(strMsg);
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
                DateTime date;
                //char delimiter = ',';
                string[] lines = File.ReadAllLines(sFilePath);
                lines[0] = " ";
                Array.Reverse(lines);
                foreach (string line in lines)
                {
                    if (line.Length > 10)
                    {
                  
                        string sTime = line.Substring(0, 4) + '-' + line.Substring(line.IndexOf('/') + 1, line.LastIndexOf('/') - line.IndexOf('/') - 1) + '-' + line.Substring(line.LastIndexOf('/') + 1, line.IndexOf("\t") - line.LastIndexOf("/") - 1) + ' ' + line.Substring(line.IndexOf("\t") + 1, line.IndexOf("\t", 12, 10) - line.IndexOf("\t") - 1);

                        //string sTime = line.Substring(line.LastIndexOf('/') + 1, 4) + '-' + line.Substring(0, line.IndexOf('/')) + '-' + line.Substring(line.IndexOf('/') + 1, line.LastIndexOf('/') - line.IndexOf('/') - 1) + ' ' + line.Substring(line.IndexOf("\t") + 1, line.IndexOf(' ') - line.IndexOf("\t") - 1) + ' ' + line.Substring(line.IndexOf(' ') + 1, 2);

                        // string sTime = line.Substring(line.LastIndexOf('/',0)+1, 4)+'-'+ line.Substring(0,line.IndexOf('/', 0)) + '-' + line.Substring(line.IndexOf('/',0), line.LastIndexOf('/',2)) + ' '+ line.Substring(line.IndexOf('t',9), line.IndexOf(' ',9)) + ' ' + line.Substring(line.IndexOf(' ', 9), 2);
                        date = DateTime.Parse(sTime);

                        string sError = line.Substring(line.LastIndexOf("\t"), line.Length - line.LastIndexOf("\t"));

                        //date = Convert.ToDateTime(sTime);
                        //sTime = date.ToString("yyyy-MM-dd HH:mm:ss");
                        //date = Convert.ToDateTime(sTime);
                        if (date < m_TimeStamp)
                        {
                            break;
                        }
                        dtTemp.Rows.Add(sTime, sError, "", clsCommon.EQ_SN);
                    }
                }

                string filter = "TransDateTime > '" + m_TimeStamp.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
                DataRow[] rows = dtTemp.Select(filter);

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


