using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EquipmentMonitor
{
    class clsTerm
    {
        public int m_Language = 0;

        public static string ERRCODE_DEFINE_ERROR = "Cant not get error code define, please defint it first!";
        public static string NO_LOG_FILE = "No log file";
        public static string NO_NEW_LOG = "No new log need to process";
        public static string FILE_SIZE_TOO_LARGE = "File Size too Large.Can not Processing.Please Check.";
        public static string GET_ERROR_MAPPING = "Get Error Code Mapping from DB, total count:";
        public static string GET_File_RECORD_COUNT = "Get File Record Count:";
        public static string INS_ERR_COUNT = "Insert to DB.Error Count:";
        public static string GET_LAST_PRO_TIME = "Get Last Processing Time:";
        public static string FILE_NOT_FOUND = "File not found :";
        public static string READ_FILE_FAIL = "Read file fail at line: ";
        public static string INS_DATA_FAIL = "Insert data fail.";
        public static string GET_LINE_INFO_FROM_MACHINE_DB_FAIL = "Get Line Info from Machine DB Fail:";
        public static string EQ_SN_NOT_FOUND = "Can't get EQ_SN From DB";
        public static string TIME_IS_WRONG = "Old log exists or system time is wrong ";
    }
}
