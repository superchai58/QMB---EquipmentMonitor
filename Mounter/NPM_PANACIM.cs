using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Data.OleDb;


namespace EquipmentMonitor.Mounter
{
    class NPM_PANACIM
    {
        DateTime m_TimeStamp;
        clsDB m_db;
        DataTable m_dtMachineLog;
        DataTable m_dtErrMapping;
        clsLog m_clsLog;
        clsParserMethod m_parser;
        DataTable m_dtEQSN;
        string MachineNo;
        string strDate;
        DateTime dtDate;
        string ErrorCode;

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
                m_dtEQSN = m_db.Setting("GetNPM_EQSN");
                if (m_dtEQSN != null && m_dtEQSN.Rows.Count == 0)
                {
                    m_clsLog.writeERRORLog(clsTerm.EQ_SN_NOT_FOUND);
                }

                //從資料夾中,取得要處理的檔案名稱
                List<string> lsFileFullPath = m_parser.getProcessFileNameByDate("csv", m_TimeStamp, m_clsLog);
                if (lsFileFullPath == null || lsFileFullPath.Count == 0)
                {
                    m_clsLog.writeERRORLog(clsTerm.NO_LOG_FILE);
                    return "";
                }

                foreach (string sPath in lsFileFullPath)
                {
                    sFilePath = sPath;
                    log.writeCOMMLog("Processing file:"+sFilePath);
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

                        m_dtMachineLog = updateEQ_SN();
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

        private DataTable updateEQ_SN()
        {
            if (m_dtMachineLog == null || m_dtMachineLog.Rows.Count == 0)
            {
                return null;
            }
            DataTable m_dtEQSN2 = new DataTable();
            string filter = "Date > '" + m_TimeStamp.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
            filter = "Station = '" + clsCommon.SIDE + "Mounter" + MachineNo + "' ";
            DataRow[] drs5 = m_dtEQSN.Select(filter);
            m_dtEQSN2 = ToDataTable(drs5);

            DataTable tmpDT = new DataTable();
            InitDataTable(ref tmpDT);
            tmpDT.TableName = clsParserMethod.m_ERR_TABLE_NAME;

            if (m_dtEQSN2 == null || m_dtEQSN2.Rows.Count == 0)
            {
                return null;
            }

            tmpDT.Rows.Add(m_dtMachineLog.Rows[0][0].ToString(), m_dtMachineLog.Rows[0][1].ToString(), m_dtMachineLog.Rows[0][2].ToString(), m_dtEQSN2.Rows[0][0].ToString(), m_dtEQSN2.Rows[0][1].ToString(), m_dtEQSN2.Rows[0][2].ToString());
            return tmpDT;
        }

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
            DataTable dt = new DataTable();
            DataTable dtTemp = new DataTable();
            DataTable dtResult = new DataTable();
            if (!File.Exists(sFilePath))
            {
                m_clsLog.writeERRORLog(clsTerm.FILE_NOT_FOUND + Path.Combine(clsCommon.FileDir, clsCommon.FileName));
                return false;
            }

            m_parser.InitDataTable(ref dtTemp);

            try
            {
                System.Text.Encoding encoding = GetType(sFilePath); //Encoding.ASCII;//
                System.IO.FileStream fs = new System.IO.FileStream(sFilePath, System.IO.FileMode.Open, 
                    System.IO.FileAccess.Read);
                System.IO.StreamReader sr = new System.IO.StreamReader(fs, encoding);
 
                //记录每次读取的一行记录
                string strLine = "";
                //记录每行记录中的各字段内容
                string[] aryLine = null;
                string[] tableHead = null;
                //标示列数
                int columnCount = 0;
                //标示是否是读取的第一行
                bool IsFirst = true;
                //逐行读取CSV中的数据
                while ((strLine = sr.ReadLine()) != null)
                {
                    if (IsFirst == true)
                    {
                        tableHead = strLine.Split(',');
                        IsFirst = false;
                        columnCount = tableHead.Length;
                        //创建列
                        for (int i = 0; i < columnCount; i++)
                        {
                            DataColumn dc = new DataColumn(tableHead[i]);
                            dt.Columns.Add(dc);
                        }
                    }
                    else
                    {
                        aryLine = strLine.Split(',');
                        DataRow dr = dt.NewRow();
                        for (int j = 0; j < columnCount; j++)
                        {
                            dr[j] = aryLine[j];
                        }
                        dt.Rows.Add(dr);
                    }
                }
                if (aryLine != null && aryLine.Length > 0)
                {
                    dt.DefaultView.Sort = tableHead[0] + " " + "asc";
                }
                sr.Close();
                fs.Close();
                //replace datetime format
                foreach (DataRow dr in dt.Rows)
                {
                    dr["EVENTTIME"] = Convert.ToDateTime(dr["EVENTTIME"]).ToString("yyyy-MM-dd HH:mm:ss");
                    strDate = Convert.ToDateTime(dr["EVENTTIME"]).ToString("yyyy-MM-dd HH:mm:ss");
                    MachineNo = dr["CELL"].ToString().Substring(dr["CELL"].ToString().Length - 1, 1);
                    char[] c = MachineNo.ToCharArray();
                    int IntNo = Convert.ToInt32(c[0]) - 64;
                    MachineNo= Convert.ToString(IntNo).PadLeft(2, '0');
                    ErrorCode = dr["EVENTCODE"].ToString();
                    ErrorCode = ErrorCode.Substring(ErrorCode.Length - 6);
                }

                string filter = "EVENTTIME > '" + m_TimeStamp.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
                DataRow[] rows = dt.Select(filter);
                dtTemp.Rows.Add(strDate, "", ErrorCode, clsCommon.EQ_SN);

                if (dtTemp.Rows.Count > 0)
                {
                    m_dtMachineLog = dtTemp;
                    m_dtMachineLog.TableName = clsParserMethod.m_ERR_TABLE_NAME;
                    m_clsLog.writeCOMMLog(clsTerm.GET_File_RECORD_COUNT + m_dtMachineLog.Rows.Count.ToString());
                }
                else
                {
                    m_clsLog.writeIMPORTLog(clsTerm.NO_NEW_LOG);
                    m_dtMachineLog.Rows.Clear();//Richard 20190111 避免記憶體中還存著舊的資料

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
        public static System.Text.Encoding GetType(string FILE_NAME)
        {
            System.IO.FileStream fs = new System.IO.FileStream(FILE_NAME, System.IO.FileMode.Open, 
                System.IO.FileAccess.Read);
            System.Text.Encoding r = GetType(fs);
            fs.Close();
            return r;
        }
 
/// 通过给定的文件流，判断文件的编码类型
/// <param name="fs">文件流</param>
/// <returns>文件的编码类型</returns>
        public static System.Text.Encoding GetType(System.IO.FileStream fs)
        {
            byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM
            System.Text.Encoding reVal = System.Text.Encoding.Default;

            System.IO.BinaryReader r = new System.IO.BinaryReader(fs, System.Text.Encoding.Default);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
            {
                reVal = System.Text.Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                reVal = System.Text.Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                reVal = System.Text.Encoding.Unicode;
            }
            r.Close();
            return reVal;
        } 
/// 判断是否是不带 BOM 的 UTF8 格式
/// <param name="data"></param>
/// <returns></returns>
        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1;　 //计算当前正分析的字符应还有的字节数
            byte curByte; //当前分析的字节.
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X　
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式");
            }
            return true;
        }
    }
}
