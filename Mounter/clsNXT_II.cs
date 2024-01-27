using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace EquipmentMonitor.Mounter
{
    class clsNXT_II
    {
        DateTime m_TimeStamp;
        clsDB m_db;
        DataTable m_dtMachineLog;
        DataTable m_dtErrMapping;
        DataTable m_dtEQSN;
        clsLog m_clsLog;
        clsParserMethod m_parser;
        clsDB_Machine m_sql;
        
        internal string ParsingLog(clsLog log)
        {
            m_db = new clsDB();
            m_clsLog = log;
            m_parser = new clsParserMethod();
            string result = "OK";
            try
            {
                m_sql = new clsDB_Machine(clsCommon.DB_Conn2);
                
                //Get Time Stamp
                m_parser.GetTimeStampFromDB("GETTimeStamp", m_db, ref m_TimeStamp, m_clsLog);
                m_clsLog.writeCOMMLog("Line:" + clsCommon.Line+","+clsTerm.GET_LAST_PRO_TIME + m_TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));

                //讀取Error Code Mapping, Only first time
                if (m_dtErrMapping == null || m_dtErrMapping.Rows.Count == 0)
                {
                    //取得EQ SN Group
                    m_dtEQSN = m_db.Setting("GetAIMEX_EQSN");
                    if (m_dtEQSN != null && m_dtEQSN.Rows.Count == 0)
                    {
                        m_clsLog.writeERRORLog(clsTerm.EQ_SN_NOT_FOUND);
                    }

                    m_dtErrMapping = m_db.execQIEMS_GetErrorMapping("GET");
                    if (m_dtErrMapping != null && m_dtErrMapping.Rows.Count == 0)
                    {
                        m_clsLog.writeERRORLog(clsTerm.ERRCODE_DEFINE_ERROR);
                        return "";
                    }
                    m_clsLog.writeCOMMLog(clsTerm.GET_ERROR_MAPPING + m_dtErrMapping.Rows.Count.ToString());
                }

                try{
                    //將log file 載入 datatable中,並且僅留下要處理的資料
                    if(!ReadSQLToTable()) return "";
                    m_clsLog.writeCOMMLog(clsTerm.GET_File_RECORD_COUNT + m_dtMachineLog.Rows.Count.ToString());

                    //將要處理的資料對應Error Code貼上記號
                    m_parser.processErrorCodeMapping_Fuji(ref m_dtMachineLog, ref m_dtErrMapping);

                    //清除沒有包含Error Code的紀錄
                    m_parser.KeepOnlyErrorMsgRow(ref m_dtMachineLog);

                    m_dtMachineLog = updateEQ_SN();
                    
                    //回存DB
                    if (!m_parser.saveResultToDB(m_dtMachineLog, m_clsLog, m_db)) return "";

                    m_parser.SetTimeStampFromDB("SETTimeStamp", m_db, ref m_TimeStamp, m_clsLog);
                }
                finally
                {
                    
                }
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

        private DataTable updateEQ_SN()
        {
            if (m_dtMachineLog == null || m_dtMachineLog.Rows.Count == 0)
            {
                return null;
            }

            var newDT =                
                from log in m_dtMachineLog.AsEnumerable()
                join eq in m_dtEQSN.AsEnumerable()
                on new { c1 = log.Field<string>("Line"), c2 = log.Field<string>("Station") }
                    equals new { c1 = eq.Field<string>("Line"), c2 = eq.Field<string>("Station") }                   
                select new
                {
                    TransDateTime = log.Field<string>("TransDateTime"),
                    ErrMsg = log.Field<string>("ErrMsg"),
                    ErrCode = log.Field<string>("ErrCode"),
                    EQ_SN = eq.Field<string>("EQ_SN"),
                    Line = log.Field<string>("Line"),
                    Station = log.Field<string>("Station")                                        
                };

            DataTable tmpDT = new DataTable();
            InitDataTable(ref tmpDT);
            tmpDT.TableName = clsParserMethod.m_ERR_TABLE_NAME;

            foreach (var item in newDT)
            {
                tmpDT.Rows.Add(item.TransDateTime, item.ErrMsg, item.ErrCode, item.EQ_SN, item.Line, item.Station);
            }
            return tmpDT;
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
        private Boolean ReadSQLToTable()
        {
            if (m_dtMachineLog == null) m_dtMachineLog = new DataTable();
            try
            {
                //Query Data and filter by timestamp
                m_dtMachineLog = m_sql.QueryMCALARMON(m_TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"));
                m_dtMachineLog.TableName = clsParserMethod.m_ERR_TABLE_NAME;
                return true;
            }
            catch (Exception ex)
            {
                m_clsLog.writeERRORLog("Read file fail at line: " + (m_dtMachineLog.Rows.Count + 1).ToString() + " Error message:\n" + ex.Message + "\n" + ex.StackTrace);                
                return false;
            }
        }

  
    }
}
