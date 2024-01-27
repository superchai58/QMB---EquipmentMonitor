using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace EquipmentMonitor
{
    class clsParserMethod
    {
        public const string m_ERR_TABLE_NAME = "ErrData";



        public void InitDataTable(ref DataTable table)
        {
            if (table.Columns.Count > 0) return;
            table.Columns.Add("TransDateTime", typeof(string));
            table.Columns.Add("ErrMsg", typeof(string));
            table.Columns.Add("ErrCode", typeof(string));
            table.Columns.Add("EQ_SN", typeof(string));
        }
        //CM/DT信息全部收集
        public void InitCMDTDataTable(ref DataTable table)
        {
            if (table.Columns.Count > 0) return;
            table.Columns.Add("Data", typeof(string));
            table.Columns.Add("MainCode", typeof(string));
            table.Columns.Add("SubCode", typeof(string));
            table.Columns.Add("MainSubject", typeof(string));
            table.Columns.Add("SubSubject", typeof(string));
        }


        public void processErrorCodeMapping(ref DataTable dtMachineLog, ref DataTable dtErrMapping)
        {
            if (dtMachineLog == null || dtMachineLog.Rows.Count == 0) return;

            string s1, s2;
            foreach (DataRow rowMsg in dtMachineLog.Rows)
            {
                System.Threading.Thread.Sleep(1);
                s1 = rowMsg["ErrCode"].ToString().Trim().ToUpper();
                foreach (DataRow rowErr in dtErrMapping.Rows)
                {
                    s2 = rowErr["MachineErrorCode"].ToString().Trim().ToUpper();

                    if (s1.Contains(s2))
                    {
                        rowMsg["ErrCode"] = rowErr["SFErrorCode"];
                        rowMsg["ErrMsg"] = rowErr["SFErrorMsg"];
                        break;
                    }
                }
            }
        }
        public void processErrorCodeMapping_Fuji(ref DataTable dtMachineLog, ref DataTable dtErrMapping)
        {
            if (dtMachineLog == null || dtMachineLog.Rows.Count == 0) return;
            dtErrMapping.PrimaryKey = new DataColumn[] { dtErrMapping.Columns["MachineErrorCode"] };

            string s1 = "";
            foreach (DataRow rowMsg in dtMachineLog.Rows)
            {
                s1 = rowMsg["ErrCode"].ToString().Trim().ToUpper();
                System.Threading.Thread.Sleep(1);
                DataRow foundRow = dtErrMapping.Rows.Find(s1);
                if (foundRow != null)
                {
                    rowMsg["ErrCode"] = foundRow["SFErrorCode"];
                    rowMsg["ErrMsg"] = foundRow["SFErrorMsg"];
                }
            }
        }
        public void processErrorMsgMapping(ref DataTable dtMachineLog, ref DataTable dtErrMapping)
        {
            if (dtMachineLog == null || dtMachineLog.Rows.Count == 0) return;
            if (dtErrMapping == null || dtErrMapping.Rows.Count == 0) return;
            string s1, s2;
            foreach (DataRow rowMsg in dtMachineLog.Rows)
            {
                s1 = rowMsg["ErrMsg"].ToString().Trim().ToUpper();
                System.Threading.Thread.Sleep(1);
                foreach (DataRow rowErr in dtErrMapping.Rows)
                {
                    s2 = rowErr["MachineErrorMsg"].ToString().Trim().ToUpper();
                    if (s1.Contains(s2))
                    {
                        rowMsg["ErrMsg"] = rowErr["SFErrorMsg"];
                        rowMsg["ErrCode"] = rowErr["SFErrorCode"];
                        break;
                    }
                }
            }
        }

        public Boolean getProcessFileName(ref string sFilePath)
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
        //获取子目录的所有文件
        public List<string> getDirectoryFile(clsLog log)
        {
            List<string> lsFilePath = new List<string>();
            try
            {
                string[] Files = Directory.GetFiles(clsCommon.MPath, "*" + clsCommon.FileName + "*");
                foreach (string file in Files)
                {
                    lsFilePath.Add(file);
                }
                return lsFilePath;
            }
            catch (Exception e)
            {
                log.writeERRORLog(e.ToString());
                return lsFilePath;
            }
        }
        //获取所有子目录
        public List<string> getAllPaths(clsLog log)
        {
            List<string> lsMPath = new List<string>();
            try
            {
                string[] FilePaths = Directory.GetDirectories(clsCommon.FileDir);
                foreach (string FilePath in FilePaths)
                {
                    lsMPath.Add(FilePath);
                }
                return lsMPath;
            }
            catch (Exception e)
            {
                log.writeERRORLog(e.ToString());
                return lsMPath;
            }
        }

        public List<string> getProcessFileNameByDate(string Ext, DateTime timStamp, clsLog log)
        {
            List<string> lsFilePath = new List<string>();
            try
            {
                DirectoryInfo info = new DirectoryInfo(clsCommon.FileDir);
                FileInfo[] files = info.GetFiles().OrderBy(p => p.CreationTime).ToArray(); ;
                foreach (FileInfo file in files)
                {
                    if (!file.Extension.ToUpper().Contains(Ext.ToUpper())) continue;
                    if (file.CreationTime < timStamp)
                    {
                        log.writeERRORLog(clsTerm.TIME_IS_WRONG);  //0006
                        break;
                    }
                    lsFilePath.Add(file.FullName);
                }
                return lsFilePath;
            }
            catch (Exception e)
            {
                log.writeERRORLog(e.ToString());
                return lsFilePath;
            }
        }
        /// <summary>
        /// 根据timStamp抓取最新的log
        /// </summary>
        /// <param name="Ext">log格式</param>
        /// <param name="timStamp">时间戳</param>
        /// <param name="log">记录log</param>
        /// <returns></returns>
        public List<string> getProcessFileNameByWriteTime(string Ext, DateTime timStamp, clsLog log)
        {
            List<string> lsFilePath = new List<string>();
            try
            {
                //将路径下所有文件抓取出来
                DirectoryInfo info = new DirectoryInfo(clsCommon.FileDir);
                //根据文件创建时间排序
                FileInfo[] files = info.GetFiles().OrderBy(p => p.LastWriteTime).ToArray();
                Array.Reverse(files);
                foreach (FileInfo file in files)
                {
                    if (!file.Extension.ToUpper().Contains(Ext.ToUpper())) continue;
                    if (!file.ToString().ToUpper().Contains(clsCommon.FileName.ToUpper())) continue;
                    if (file.LastWriteTime < timStamp) break;
                    lsFilePath.Add(file.FullName);
                }
                //sFilePath = lsFilePath[0].ToString();
                return lsFilePath;
            }
            catch (Exception e)
            {
                log.writeERRORLog(e.ToString());
                return lsFilePath;
            }
        }

        public Boolean KeepOnlyErrorMsgRow(ref DataTable dtMachineLog)
        {
            DataTable dt = new DataTable();
            //如果前面讀取到的LOG是空的,就直接返回
            if (dtMachineLog == null || dtMachineLog.Columns.Count == 0)
                return false;
            DataRow[] rows = dtMachineLog.Select("ErrMsg <>'' ");
            if (rows.Length == 0)
            {
                InitDataTable(ref dt);
                dt.TableName = clsParserMethod.m_ERR_TABLE_NAME;
                dtMachineLog = dt;
                return false;
            }

            dtMachineLog = rows.CopyToDataTable();
            dtMachineLog.TableName = m_ERR_TABLE_NAME;
            return true;
        }
        public Boolean KeepOnlyErrorCodeRow(ref DataTable dtMachineLog)
        {
            DataTable dt = new DataTable();
            //如果前面讀取到的LOG是空的,就直接返回
            if (dtMachineLog == null || dtMachineLog.Columns.Count == 0)
                return false;
            DataRow[] rows = dtMachineLog.Select("ErrCode <>'' ");
            if (rows.Length == 0)
            {
                InitDataTable(ref dt);
                dt.TableName = clsParserMethod.m_ERR_TABLE_NAME;
                dtMachineLog = dt;
                return false;
            }

            dtMachineLog = rows.CopyToDataTable();
            dtMachineLog.TableName = m_ERR_TABLE_NAME;
            return true;
        }

        public void GetTimeStampFromDB(string sActionType, clsDB m_db, ref DateTime TimeStamp, clsLog log)
        {
            string sLine = "", sStation = "";
            DataTable dt;
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
            dt = m_db.Setting(sActionType);
            if (dt != null && dt.Rows.Count > 0)
            {
                //Get Time Stamp的時候會拿Line跟Station,並且變更顯示,其他不拿
                string sLastParseLogTime = dt.Rows[0][0].ToString();
                if (dt.Columns.Contains("Line"))
                    sLine = dt.Rows[0]["Line"].ToString();
                if (dt.Columns.Contains("Station"))
                    sStation = dt.Rows[0]["Station"].ToString();

                if (!clsCommon.isAIMEX)
                {
                    if (!sLine.Equals(clsCommon.Line))
                    {
                        log.writeIMPORTLog("Change Line :" + clsCommon.Line + " to " + sLine);
                        clsCommon.Line = sLine;
                    }
                    if (!sStation.Equals(clsCommon.Station))
                    {
                        log.writeIMPORTLog("Change Station :" + clsCommon.Station + " to " + sStation);
                        clsCommon.Station = sStation;
                    }
                    clsCommon.WriteRegInfo(clsCommon.EQ_SN, clsCommon.Site, clsCommon.Factory, clsCommon.Line,
                        clsCommon.Station, clsCommon.MachineType, clsCommon.MachineName, clsCommon.MachineSWVer,
                        clsCommon.FileDir, clsCommon.FileName, clsCommon.Remark, clsCommon.SIDE, clsCommon.DelBakLogByDay, clsCommon.DelBakLog);
                }
                TimeStamp = DateTime.Parse(sLastParseLogTime);
            }
        }
        public void SetTimeStampFromDB(string sActionType, clsDB m_db, ref DateTime TimeStamp, clsLog log)
        {
            DataTable dt;
            System.Threading.Thread.Sleep(1000);        //superchai add 20231002

            //---------superchai set interval by line 20231002 (B)------------
            //ConnectDBSMT oCon = new ConnectDBSMT();
            //SqlCommand cmd = new SqlCommand();
            //DataTable dtTmp = new DataTable();

            //cmd.CommandText = "Select Interval From IntervalByLine With(nolock) Where Line = @Line";
            //cmd.Parameters.Add(new SqlParameter("@Line", clsCommon.Line.ToString().Trim()));
            //dtTmp = oCon.Query(cmd);
            //if (dt.Rows.Count > 0)SetStatus
            //{
            //    System.Threading.Thread.Sleep(int.Parse(dt.Rows[0]["Interval"].ToString().Trim()));
            //    //m_ParserInterval = int.Parse(dt.Rows[0]["Interval"].ToString().Trim());
            //}
            //else
            //{
            //    System.Threading.Thread.Sleep(1000);
            //}
            //---------superchai set interval by line 20231002 (E)------------
            dt = m_db.Setting(sActionType);           
            if (dt != null && dt.Rows.Count > 0)
            {
                string sLastParseLogTime = dt.Rows[0][0].ToString();
                TimeStamp = DateTime.Parse(sLastParseLogTime);
            }
        }

        public Boolean saveResultToDB(DataTable MachineLog, clsLog log, clsDB db)
        {
            string result = "";
            if (MachineLog == null || MachineLog.Rows.Count == 0)
            {
                log.writeIMPORTLog(clsTerm.NO_NEW_LOG);
                return true;
            }

            //打印LOG出來
            log.writeCOMMLog("find error log total count:" + MachineLog.Rows.Count.ToString());
            foreach (DataRow row in MachineLog.Rows)
            {
                log.writeWARNINGLog("Error:" + row["ErrCode"] + ",ErrorMsg:" + row["ErrMsg"]);
            }
            //如果沒有錯誤,就存入DB
            try
            {
                result = db.InsErrDataToDB(MachineLog);
                if (!result.Contains("OK"))
                {
                    log.writeERRORLog(result);
                    return false;
                }
                else
                {
                    log.writeIMPORTLog(clsTerm.INS_ERR_COUNT + MachineLog.Rows.Count.ToString());
                }
                return true;
            }
            catch (Exception ex)
            {
                log.writeERRORLog(clsTerm.INS_DATA_FAIL + ex.ToString());
                return false;
            }
        }

        public Boolean saveCMDTDataToDB(DataTable MachineLog, clsLog log, clsDB db)
        {
            string result = "";
            if (MachineLog == null || MachineLog.Rows.Count == 0)
            {
                log.writeIMPORTLog(clsTerm.NO_NEW_LOG);
                return true;
            }

            //打印LOG出來
            //log.writeCOMMLog("find error log total count:"+MachineLog.Rows.Count.ToString());
            //foreach (DataRow row in MachineLog.Rows)
            //{
            //    log.writeWARNINGLog("Error:" + row["ErrCode"] + ",ErrorMsg:" + row["ErrMsg"]);
            //}
            //如果沒有錯誤,就存入DB
            try
            {
                result = db.InsCMDTDataToDB(MachineLog);
                if (!result.Contains("OK"))
                {
                    log.writeERRORLog(result);
                    return false;
                }
                else
                {
                    log.writeIMPORTLog(clsTerm.INS_ERR_COUNT + MachineLog.Rows.Count.ToString());
                }
                return true;
            }
            catch (Exception ex)
            {
                log.writeERRORLog(clsTerm.INS_DATA_FAIL + ex.ToString());
                return false;
            }
        }

        public void DeleteBakLogByDay(string directoryPath, int day, clsLog log)
        {

            try
            {
                //获取指定目录下的所有子目录
                string[] dirs = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
                //设定备份log的时间   可由前台控制
                //int day = 7;
                DateTime dt = DateTime.Now.AddDays(-day);
                //遍历每个子目录文件夹
                foreach (string dir in dirs)
                {
                    //获取目录下所有文件
                    DirectoryInfo info = new DirectoryInfo(dir);
                    FileInfo[] files = info.GetFiles().OrderBy(p => p.LastWriteTime).ToArray();
                    //遍历所有文件，判断文件写入时间是否小于备份时间  小于则删除文件
                    foreach (var f in files)
                    {
                        if (f.LastWriteTime > dt)
                        {
                            break;
                        }
                        else
                        {
                            File.Delete(f.FullName);
                            log.writeCOMMLog("Delete back log file:" + f.FullName);
                        }

                    }
                    //在这里再判断下文件夹是否为空了  为空就删除此文件夹
                    if (Directory.GetDirectories(dir).Length == 0 && Directory.GetFiles(dir).Length == 0)
                    {
                        //删除空文件夹
                        Directory.Delete(dir);
                    }

                }
            }
            catch (Exception e)
            {
                log.writeERRORLog(e.ToString());
            }
        }
    }
}
