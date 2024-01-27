using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace EquipmentMonitor
{
    class clsCommon
    {
        public const int LOG_SIZE_LIMIT_BYLE = 10000000;

        #region Parameters
        private static string _eq_sn = "";
        public static string EQ_SN
        {
            get
            {
                if (_eq_sn == null)
                    return "";
                else
                    return _eq_sn;
            }
            set
            {
                _eq_sn = value;
            }
        }
        private static string _side = "";
        public static string SIDE
        {
            get
            {
                if (_side == null)
                    return "";
                else
                    return _side;
            }
            set
            {
                _side = value;
            }
        }

        public static string Site { get; set; }
        public static string Factory { get; set; }
        public static string Line { get; set; }
        public static string Station { get; set; }
        public static string MachineType { get; set; }
        public static string MachineName { get; set; }
        //public static string Side { get; set; }  ///for QSMC发

        public static string MachineSWVer { get; set; }
        private static string _FileDir = "";
        public static string FileDir
        {
            get
            {
                if (_FileDir == null)
                    return "";
                else
                    return _FileDir;
            }
            set
            {
                _FileDir = value;
            }
        }
        private static string _FileName = "";
        public static string FileName
        {
            get
            {
                if (_FileName == null)
                    return "";
                else
                    return _FileName;
            }
            set
            {
                _FileName = value;
            }
        }
        public static string Remark { get; set; }
        private static string _ClientVer = "";
        public static string ClientVersion {
            get
            {
                if (_ClientVer == null)
                    return "";
                else
                    return _ClientVer;
            }
            set
            {
                _ClientVer = value;
            }
        }
        public static string FilePath { get; set; }
        public static string MPath { get; set; }
        public static DataTable dtMachine { get; set; }
        public static string DB_Conn2 { get; set; }
        public static Boolean isAIMEX { get; set; }
        private static string _DelBakLog = "";
        public static string DelBakLog {
            get
            {
                if (_DelBakLog == null)
                    return "";
                else
                    return _DelBakLog;
            }
            set
            {
                _DelBakLog = value;
            }

        }

        private static string _DelBakLogByDay = "";

        public static string DelBakLogByDay {
            get
            {
                if (_DelBakLogByDay == null)
                    return "";
                else
                    return _DelBakLogByDay;
            }
            set
            {
                _DelBakLogByDay = value;
            }

        }
        #endregion

        public static string SetStatus = "SetStatus";

        #region Read/Write Key

        internal static string ReadRegKey(string strKeyName)
        {
            RegistryKey regKey;
            
            try
            {
                regKey = Registry.CurrentUser.OpenSubKey("Software\\SFS\\EquipmentMonitor");
                return regKey.GetValue(strKeyName, "").ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static void WriteRegKey(string strKeyName, string strKeyValue)
        {
            RegistryKey regKey = Registry.CurrentUser.CreateSubKey("Software\\SFS\\EquipmentMonitor");
            regKey.SetValue(strKeyName, strKeyValue);
        }
        internal static void WriteRegInfo(string EQ_SN, string site,string factory,string Line,string station,string machineTpye,string machine,
            string softVer,string fileDir,string file, string remark,string SIDE, string day, string delFlag)
        {
            clsCommon.WriteRegKey("EQ_SN", EQ_SN);
            clsCommon.WriteRegKey("Site", site);
            clsCommon.WriteRegKey("Factory", factory);
            clsCommon.WriteRegKey("Line", Line);
            clsCommon.WriteRegKey("Station", station);
            clsCommon.WriteRegKey("MachineType", machineTpye);
            clsCommon.WriteRegKey("MachineName", machine);
            clsCommon.WriteRegKey("MachineSoftVer", softVer);
            clsCommon.WriteRegKey("FileDir", fileDir);
            clsCommon.WriteRegKey("FileName", file);
            clsCommon.WriteRegKey("Remark", remark);
            clsCommon.WriteRegKey("SIDE", SIDE);
            clsCommon.WriteRegKey("Day", day);
            clsCommon.WriteRegKey("DelFlag", delFlag);
        }

        #endregion

        #region File action

        internal static string MoveFile()
        {
            string result = "OK";
            string sourcePath = Path.Combine(clsCommon.FileDir, clsCommon.FileName);
            if (!File.Exists(sourcePath))
            {
                result = "File not found. (File path = " + sourcePath + ")";
            }
            else
            {
                string extension = Path.GetExtension(sourcePath);
                string timestamp = DateTime.Now.ToString("yyyyMMddhhmmss");
                string destPath = Path.Combine(clsCommon.FileDir, clsCommon.FileName.Replace(extension, "_") + timestamp + extension);

                try
                {
                    File.Move(sourcePath, destPath);
                    clsCommon.FilePath = destPath;
                }
                catch (Exception ex)
                {
                    result = "Move file fail.\n" + ex.Message + "\n" + ex.StackTrace;
                }
            }

            return result;
        }
        /// <summary>
        /// 將原本檔名 xxx.ext 改為 xxx_yyyyMMddhhmmss.ext
        /// </summary>
        /// <param name="sFileFullPath">要重新命名的檔案完整路徑名稱</param>
        /// <returns></returns>
        internal static string CopyFile(string sFileFullPath)
        {
            string result = "OK";
            if (!File.Exists(sFileFullPath))
            {
                return "File not found. (File path = " + sFileFullPath + ")";
            }

            string extension = Path.GetExtension(sFileFullPath);
            string timestamp = DateTime.Now.ToString("yyyyMMddhhmmss");
            string destPath = sFileFullPath.Replace(extension, "_") + timestamp + extension;
            //string destPath = sFileFullPath.Replace(extension, "_") + "_Copy_" + extension;

            try
            {
                File.Copy(sFileFullPath, destPath,true);
                clsCommon.FilePath = destPath;
            }
            catch
            {
                //如果無法搬移檔案,就不搬移,直接處理
                clsCommon.FilePath = sFileFullPath;
                //result = "Move file fail.\n" + ex.Message + "\n" + ex.StackTrace;
            }
            return result;
        }
        /// <summary>
        /// 將原本檔名 xxx.ext 改為 xxx_yyyyMMddhhmmss.ext
        /// </summary>
        /// <param name="sFileFullPath">要重新命名的檔案完整路徑名稱</param>
        /// <returns></returns>
        internal static string MoveFile(string sFileFullPath)
        {
            string result = "OK";
            if (!File.Exists(sFileFullPath))
            {
                return "File not found. (File path = " + sFileFullPath + ")";
            }

            string extension = Path.GetExtension(sFileFullPath);
            string timestamp = DateTime.Now.ToString("yyyyMMddhhmmss");
            //將原本檔名 xxx.log 改為 xxx_20171222101010.log
            string destPath = sFileFullPath.Replace(extension, "_") + timestamp + extension;

            try
            {
                File.Move(sFileFullPath, destPath);
                clsCommon.FilePath = destPath;
            }
            catch
            {
                //如果無法搬移檔案,就不搬移,直接處理
                clsCommon.FilePath = sFileFullPath;
                //result = "Move file fail.\n" + ex.Message + "\n" + ex.StackTrace;
            }
            return result;
        }

        internal static void BakFile()
        {
            string now, yyyy, mm, dd;
            now = DateTime.Now.ToString("yyyyMMdd");
            yyyy = now.Substring(0, 4); mm = now.Substring(4, 2); dd = now.Substring(6, 2);

            string BakPath = Path.Combine(clsCommon.FileDir, "Bak\\" + yyyy + "\\" + mm + "\\" + dd);
            try
            {
                if (!Directory.Exists(BakPath)) { Directory.CreateDirectory(BakPath); }
                File.Move(clsCommon.FilePath, Path.Combine(BakPath, Path.GetFileName(clsCommon.FilePath)));
            }
            catch
            {
                //ignore                
            }
        }

        internal static Boolean BakFile(ref string msg)
        {
            string now, yyyy, mm, dd;
            now = DateTime.Now.ToString("yyyyMMdd");
            yyyy = now.Substring(0, 4); mm = now.Substring(4, 2); dd = now.Substring(6, 2);

            string BakPath = Path.Combine(clsCommon.FileDir, "Bak\\" + yyyy + "\\" + mm + "\\" + dd);
            try
            {
                if (!Directory.Exists(BakPath))
                {
                    msg = msg+Environment.NewLine+" Create folder:" + BakPath;
                    Directory.CreateDirectory(BakPath);
                }
                File.Move(clsCommon.FilePath, Path.Combine(BakPath, Path.GetFileName(clsCommon.FilePath)));
                return true;
            }
            catch (Exception ex)
            {
                msg = msg + Environment.NewLine + " Backup file fail.\n" + ex.Message + "\n" + ex.StackTrace;
                return false;
            }
        }
        internal static Boolean BakCMDTFile(ref string msg)
        {
            string now, yyyy, mm, dd;
            now = DateTime.Now.ToString("yyyyMMdd");
            yyyy = now.Substring(0, 4); mm = now.Substring(4, 2); dd = now.Substring(6, 2);

            string BakPath = Path.Combine(clsCommon.MPath, "Bak\\" + yyyy + "\\" + mm + "\\" + dd);
            try
            {
                if (!Directory.Exists(BakPath))
                {
                    msg = msg + Environment.NewLine + " Create folder:" + BakPath;
                    Directory.CreateDirectory(BakPath);
                }
                File.Move(clsCommon.FilePath, Path.Combine(BakPath, Path.GetFileName(clsCommon.FilePath)));
                return true;
            }
            catch (Exception ex)
            {
                msg = msg + Environment.NewLine + " Backup file fail.\n" + ex.Message + "\n" + ex.StackTrace;
                return false;
            }
        }

        internal static string DelFile()
        {
            try
            {
                File.Delete(clsCommon.FilePath);
                if (File.Exists(clsCommon.FilePath))
                {
                    return "File delete fail";
                }
                return "";
            }
            catch(Exception e)
            {                
                return e.ToString();
            }
        }
        #endregion
    }
}
