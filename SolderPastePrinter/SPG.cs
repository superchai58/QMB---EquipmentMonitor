using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
namespace EquipmentMonitor.SolderPastePrinter
{
    class SPG
    {
        DateTime m_TimeStamp;
        clsDB m_db;
        DataTable m_dtMachineLog;
        DataTable m_dtErrMapping;
        clsLog m_clsLog;
        clsParserMethod m_parser;
        //DataTable m_dtEQSN;
        string MachineNo;

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

                //取得EQ SN Group
                //m_dtEQSN = m_db.Setting("GetNPM_EQSN");
                //if (m_dtEQSN != null && m_dtEQSN.Rows.Count == 0)
                //{
                //    m_clsLog.writeERRORLog(clsTerm.EQ_SN_NOT_FOUND);
                //}

                //從資料夾中,取得要處理的檔案名稱
                List<string> lsFileFullPath = m_parser.getProcessFileNameByDate("MEV", m_TimeStamp, m_clsLog);
                if (lsFileFullPath == null || lsFileFullPath.Count == 0)
                {
                    m_clsLog.writeERRORLog(clsTerm.NO_LOG_FILE);
                    return "";
                }

                foreach (string sPath in lsFileFullPath)
                {
                    sFilePath = sPath;
                    log.writeCOMMLog("Processing file:" + sFilePath);
                    long Filelength = new System.IO.FileInfo(sFilePath).Length;//in Byte
                    if (Filelength > clsCommon.LOG_SIZE_LIMIT_BYLE)
                    {
                        m_clsLog.writeERRORLog(clsTerm.FILE_SIZE_TOO_LARGE + sFilePath);
                        continue;
                    }

                    //將檔案先搬移並且重新命名
                    result = clsCommon.MoveFile(sFilePath);
                    if (!result.Equals("OK"))
                    {
                        m_clsLog.writeERRORLog(result);
                        return "";
                    }

                    //只要檔案重新命名之後,就應該將該檔案搬移到BAK中
                    try
                    {
                        //將log file 載入 DataTable中,並且僅留下要處理的資料
                        if (!ReadFileToTable(clsCommon.FilePath)) continue;


                        //將要處理的資料對應Error Code貼上記號
                        m_parser.processErrorCodeMapping(ref m_dtMachineLog, ref m_dtErrMapping);

                        //清除沒有包含Error Msg的紀錄
                        m_parser.KeepOnlyErrorMsgRow(ref m_dtMachineLog);

                        //m_dtMachineLog = updateEQ_SN();
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
                        //根据备份天数删除备份的log
                        if (clsCommon.DelBakLog == "Y")
                        {
                            string BakLogPath = Path.Combine(clsCommon.FileDir, "Bak");
                            int day = 7;
                            bool f = int.TryParse(clsCommon.DelBakLogByDay, out day);
                            m_parser.DeleteBakLogByDay(BakLogPath, day, m_clsLog);
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                m_clsLog.writeERRORLog(exp.ToString());
                return "";
            }
            finally
            {

            }
            return result;
        }

        //private DataTable updateEQ_SN()
        //{
        //    if (m_dtMachineLog == null || m_dtMachineLog.Rows.Count == 0)
        //    {
        //        return null;
        //    }
        //    DataTable m_dtEQSN2 = new DataTable();
        //    string filter = "Date > '" + m_TimeStamp.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
        //    filter = "Station = '" + clsCommon.SIDE + "Mounter" + MachineNo + "' ";
        //    DataRow[] drs5 = m_dtEQSN.Select(filter);
        //    m_dtEQSN2 = ToDataTable(drs5);

        //    DataTable tmpDT = new DataTable();
        //    InitDataTable(ref tmpDT);
        //    tmpDT.TableName = clsParserMethod.m_ERR_TABLE_NAME;

        //    if (m_dtEQSN2 == null || m_dtEQSN2.Rows.Count == 0)
        //    {
        //        return null;
        //    }

        //    tmpDT.Rows.Add(m_dtMachineLog.Rows[0][0].ToString(), m_dtMachineLog.Rows[0][1].ToString(), m_dtMachineLog.Rows[0][2].ToString(), m_dtEQSN2.Rows[0][0].ToString(), m_dtEQSN2.Rows[0][1].ToString(), m_dtEQSN2.Rows[0][2].ToString());
        //    return tmpDT;
        //}

        private DataTable ToDataTable(DataRow[] rows)
        {
            if (rows == null || rows.Length == 0) return null;
            DataTable tmp = rows[0].Table.Clone(); // 复制DataRow的表结构
            foreach (DataRow row in rows)
            {

                tmp.ImportRow(row); // 将DataRow添加到DataTable中
            }
            return tmp;
        }

        private void InitDataTable(ref DataTable table)
        {
            if (table.Columns.Count > 0) return;
            table.Columns.Add("TransDateTime", typeof(string));
            table.Columns.Add("ErrMsg", typeof(string));
            table.Columns.Add("ErrCode", typeof(string));
            table.Columns.Add("EQ_SN", typeof(string));
            table.Columns.Add("Line", typeof(string));
            table.Columns.Add("Station", typeof(string));
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
                DataTable dataTable = new DataTable("Element");
                dataTable.Columns.Add("MCNo", typeof(string));
                dataTable.Columns.Add("Date", typeof(string));
                dataTable.Columns.Add("MDLN", typeof(string));
                dataTable.Columns.Add("EventCode", typeof(string));
                dataTable.Columns.Add("EventSerial", typeof(string));
                dataTable.Columns.Add("EventDetailCode", typeof(string));
                dataTable.Columns.Add("Stage", typeof(string));
                dataTable.Columns.Add("GuiCode", typeof(string));
                dataTable.Columns.Add("Lane", typeof(string));
               
                dataTable.ReadXml(sFilePath);

                //replace datetime format
                foreach (DataRow dr in dataTable.Rows)
                {
                    dr["Date"] = dr["Date"].ToString().Replace("/", "-").Replace(",", " ");
                    MachineNo = dr["MCNo"].ToString().PadLeft(2, '0');
                }

                string filter = "Date > '" + m_TimeStamp.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
                DataRow[] rows = dataTable.Select(filter);
                foreach (DataRow r in dataTable.Select(filter))
                {
                    dtTemp.Rows.Add(r["Date"], "", r["EventDetailCode"], clsCommon.EQ_SN);
                }


                if (dtTemp.Rows.Count > 0)
                {
                    m_dtMachineLog = dtTemp;
                    m_dtMachineLog.TableName = clsParserMethod.m_ERR_TABLE_NAME;

                    m_clsLog.writeCOMMLog(clsTerm.GET_File_RECORD_COUNT + m_dtMachineLog.Rows.Count.ToString());
                }
                else
                {
                    m_clsLog.writeIMPORTLog(clsTerm.NO_NEW_LOG);
                    //m_dtMachineLog.Rows.Clear();//Richard 20190111 避免記憶體中還存著舊的資料

                    return false;
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
