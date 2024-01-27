
using System.Data;
using System.Data.SqlClient;

namespace EquipmentMonitor
{
    /// <summary>
    /// 連線機台資料庫-專用
    /// </summary>
    class clsDB_Machine
    {
        SqlConnection cn;
        SqlCommand cmd;
        public string m_CONNSTR = "";

        public clsDB_Machine(string conn)
        {
            m_CONNSTR = conn;
        }

        internal DataTable getLine()
        {
            DataTable result;

            using (cn = new SqlConnection(m_CONNSTR))
            using (cmd = new SqlCommand())
            {
                cmd.CommandText = 
                    "SELECT Distinct LEFT(Name,2)+'" + clsCommon.Factory.Substring(clsCommon.Factory.Length -1 , 1) + "' AS LineFullName " +
                    "FROM [LINE] "+
                    "WHERE ACTIVEFAG = '1'";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = cn;
                cn.Open();

                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
                result = ds.Tables[0];

                if (cn.State == ConnectionState.Open) cn.Close();
            }

            return result;
        }
        internal DataTable Get_DB_List()
        {
            DataTable dtResult;

            using (cn = new SqlConnection(m_CONNSTR))
            using (cmd = new SqlCommand())
            {
                cmd.CommandText = "SELECT name FROM master.dbo.sysdatabases where dbid > 4";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = cn;
                cn.Open();

                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
                dtResult = ds.Tables[0];

                if (cn.State == ConnectionState.Open) cn.Close();
            }

            return dtResult;
        }
        internal DataTable QueryMCALARMON(string time)
        {
            DataTable dtResult;
            //廠區最後一碼也是線別最後一碼
            string FactoryCode = clsCommon.Factory.Substring(clsCommon.Factory.Length-1,1);
            using (cn = new SqlConnection(m_CONNSTR))
            using (cmd = new SqlCommand())
            {

                cmd.CommandText =
                    " SELECT LEFT([STARTDATE],19) AS TransDateTime , UPPER([ERRORCODE]) AS ErrCode, " +
                    "      '' AS ErrMsg, " +
                    "      LEFT(RIGHT([LINENAME],2),1)+'"+clsCommon.Station+"'+CAST([MODULENO] AS VARCHAR) AS Station, " +
                    "      LEFT(LINENAME,2)+'" + FactoryCode + "' AS Line, " +
                    "      '"+clsCommon.EQ_SN+"' AS EQ_SN "+
                    " FROM [MCALARMON] "+
                    " WHERE STARTDATE > '"+time+"'"+
                    " ORDER BY STARTDATE ASC";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = cn;
                cn.Open();

                DataSet ds = new DataSet();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
                dtResult = ds.Tables[0];
                if (cn.State == ConnectionState.Open) cn.Close();
            }
            return dtResult;
        }
    }
}
