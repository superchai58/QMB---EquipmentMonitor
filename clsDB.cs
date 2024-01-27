using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace EquipmentMonitor
{
    /// <summary>
    /// QMS DB專用
    /// </summary>
    class clsDB : QMSSDK.Db.WinForm
    {
        DataSet ds = new DataSet();

        internal DataTable Setting(string action)
        {
            this.spName = "QIEMS_Setting"; 
            SqlParameter[] sp = new SqlParameter[13];

            sp[0] = new SqlParameter("@Action", SqlDbType.VarChar);
            sp[0].Value = action;
            sp[1] = new SqlParameter("@EQ_SN", SqlDbType.VarChar);
            sp[1].Value = clsCommon.EQ_SN;
            sp[2] = new SqlParameter("@Site", SqlDbType.VarChar);
            sp[2].Value = clsCommon.Site;
            sp[3] = new SqlParameter("@Factory", SqlDbType.VarChar);
            sp[3].Value = clsCommon.Factory;
            sp[4] = new SqlParameter("@Line", SqlDbType.VarChar);
            sp[4].Value = clsCommon.Line;
            sp[5] = new SqlParameter("@Station", SqlDbType.NVarChar);
            sp[5].Value = clsCommon.Station;
            sp[6] = new SqlParameter("@MachineType", SqlDbType.NVarChar);
            sp[6].Value = clsCommon.MachineType;
            sp[7] = new SqlParameter("@MachineName", SqlDbType.NVarChar);
            sp[7].Value = clsCommon.MachineName;
            sp[8] = new SqlParameter("@MachineSoftVer", SqlDbType.VarChar);
            sp[8].Value = clsCommon.MachineSWVer;
            sp[9] = new SqlParameter("@FileDir", SqlDbType.NVarChar);
            sp[9].Value = clsCommon.FileDir;
            sp[10] = new SqlParameter("@FileName", SqlDbType.NVarChar);
            sp[10].Value = clsCommon.FileName;
            sp[11] = new SqlParameter("@ClientVersion", SqlDbType.NVarChar);
            sp[11].Value = clsCommon.ClientVersion;
            sp[12] = new SqlParameter("@Remark", SqlDbType.NVarChar);
            sp[12].Value = clsCommon.Remark;

            ds = this.Execute(sp);


            return ds.Tables[0];
        }
        internal DataTable QIEMS_Setting_Status(string action, string remark)
        {
            this.spName = "QIEMS_Setting";
            SqlParameter[] sp = new SqlParameter[13];

            sp[0] = new SqlParameter("@Action", SqlDbType.VarChar);
            sp[0].Value = action;
            sp[1] = new SqlParameter("@EQ_SN", SqlDbType.VarChar);
            sp[1].Value = clsCommon.EQ_SN;
            sp[2] = new SqlParameter("@Site", SqlDbType.VarChar);
            sp[2].Value = clsCommon.Site;
            sp[3] = new SqlParameter("@Factory", SqlDbType.VarChar);
            sp[3].Value = clsCommon.Factory;
            sp[4] = new SqlParameter("@Line", SqlDbType.VarChar);
            sp[4].Value = clsCommon.Line;
            sp[5] = new SqlParameter("@Station", SqlDbType.NVarChar);
            sp[5].Value = clsCommon.Station;
            sp[6] = new SqlParameter("@MachineType", SqlDbType.NVarChar);
            sp[6].Value = clsCommon.MachineType;
            sp[7] = new SqlParameter("@MachineName", SqlDbType.NVarChar);
            sp[7].Value = clsCommon.MachineName;
            sp[8] = new SqlParameter("@MachineSoftVer", SqlDbType.VarChar);
            sp[8].Value = clsCommon.MachineSWVer;
            sp[9] = new SqlParameter("@FileDir", SqlDbType.NVarChar);
            sp[9].Value = "";
            sp[10] = new SqlParameter("@FileName", SqlDbType.NVarChar);
            sp[10].Value = "";
            sp[11] = new SqlParameter("@ClientVersion", SqlDbType.NVarChar);
            sp[11].Value = clsCommon.ClientVersion;
            if (remark.Length > 200)
                remark = remark.Substring(0, 200);
            sp[12] = new SqlParameter("@Remark", SqlDbType.NVarChar);
            sp[12].Value = remark;

            ds = this.Execute(sp);
            return ds.Tables[0];
        }
        internal DataTable execQIEMS_GetErrorMapping(string action)
        {
            this.spName = "QIEMS_MangErrorMapping";
            SqlParameter[] sp = new SqlParameter[8];

            sp[0] = new SqlParameter("@Action", SqlDbType.VarChar);
            sp[0].Value = action;
            sp[1] = new SqlParameter("@Site", SqlDbType.VarChar);
            sp[1].Value = clsCommon.Site;
            sp[2] = new SqlParameter("@Factory", SqlDbType.VarChar);
            sp[2].Value = clsCommon.Factory;
            sp[3] = new SqlParameter("@Line", SqlDbType.VarChar);
            sp[3].Value = clsCommon.Line;
            sp[4] = new SqlParameter("@Station", SqlDbType.NVarChar);
            sp[4].Value = clsCommon.Station;
            sp[5] = new SqlParameter("@MachineType", SqlDbType.NVarChar);
            sp[5].Value = clsCommon.MachineType;
            sp[6] = new SqlParameter("@MachineName", SqlDbType.NVarChar);
            sp[6].Value = clsCommon.MachineName;
            sp[7] = new SqlParameter("@MachineSoftVer", SqlDbType.VarChar);
            sp[7].Value = clsCommon.MachineSWVer;

            ds = this.Execute(sp);
            return ds.Tables[0];
        }
        internal string InsErrDataToDB(DataTable dt)
        {
            StringWriter sw = new StringWriter();
            dt.WriteXml(sw, true);

            this.spName = "QIEMS_InsertData";
            SqlParameter[] sp = new SqlParameter[6];

            sp[0] = new SqlParameter("@Site", SqlDbType.VarChar);
            sp[0].Value = clsCommon.Site;
            sp[1] = new SqlParameter("@Factory", SqlDbType.VarChar);
            sp[1].Value = clsCommon.Factory;
            sp[2] = new SqlParameter("@Line", SqlDbType.VarChar);
            sp[2].Value = clsCommon.Line;
            sp[3] = new SqlParameter("@Station", SqlDbType.NVarChar);
            sp[3].Value = clsCommon.Station;
            sp[4] = new SqlParameter("@MachineName", SqlDbType.NVarChar);
            sp[4].Value = clsCommon.MachineName;
            sp[5] = new SqlParameter("@ErrData", SqlDbType.Xml);
            sp[5].Value = sw.ToString();

            ds = this.Execute(sp);
            return ds.Tables[0].Rows[0][0].ToString();
        }

        internal string InsCMDTDataToDB(DataTable dt)
        {
            StringWriter sw = new StringWriter();
            dt.WriteXml(sw, true);

            this.spName = "QIEMS_InsertCMDTData";
            SqlParameter[] sp = new SqlParameter[7];

            sp[0] = new SqlParameter("@Site", SqlDbType.VarChar);
            sp[0].Value = clsCommon.Site;
            sp[1] = new SqlParameter("@Factory", SqlDbType.VarChar);
            sp[1].Value = clsCommon.Factory;
            sp[2] = new SqlParameter("@Line", SqlDbType.VarChar);
            sp[2].Value = clsCommon.Line;
            sp[3] = new SqlParameter("@Station", SqlDbType.NVarChar);
            sp[3].Value = clsCommon.Station;
            sp[4] = new SqlParameter("@MachineName", SqlDbType.NVarChar);
            sp[4].Value = clsCommon.MachineName;
            sp[5] = new SqlParameter("@FilePath", SqlDbType.NVarChar);
            sp[5].Value = clsCommon.MPath;
            sp[6] = new SqlParameter("@ErrData", SqlDbType.Xml);
            sp[6].Value = sw.ToString();

            ds = this.Execute(sp);
            return ds.Tables[0].Rows[0][0].ToString();
        }
        internal string ChkLoginOn(string Username, string Password)
        {
            this.spName = "ChkEquipmentMonitorLogOn";
            SqlParameter[] sp = new SqlParameter[2];

            sp[0] = new SqlParameter("@Username", SqlDbType.VarChar);
            sp[0].Value = Username;
            sp[1] = new SqlParameter("@Passwords", SqlDbType.VarChar);
            sp[1].Value = Password;

            ds = this.Execute(sp);
            return ds.Tables[0].Rows[0][0].ToString();

        }
    }
}
