using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class DataAccess<T> where T : class
    {
        private SqlConnection Conn;
        private readonly Func<T> CreateInstanceT;
        private SqlCommand comm;
        private int count;
        private SqlDataReader dr;
        private string constr;

        public DataAccess(string connString)
        {
            constr = connString;
            Conn = new SqlConnection(connString);
            NewExpression NewExp = Expression.New(typeof (T));
            Expression<Func<T>> Lamda = Expression.Lambda<Func<T>>(NewExp);
            CreateInstanceT = Lamda.Compile();
        }

        public int TotalRecord { get; set; }

        public List<T> LoadData(T Obj, string strQueryCondition, int intCurrPage, string SPName, int PageSize,
                                Dictionary<string, object> Params)
        {
            var Cols = new List<T>();


            try
            {
                Conn.Open();
                comm = new SqlCommand(SPName, Conn);
                comm.CommandTimeout = 2000;
                comm.CommandType = CommandType.StoredProcedure;
                comm.Parameters.Add("@CURRENTPAGE", SqlDbType.BigInt).Value = intCurrPage;
                comm.Parameters.Add("@PAGESIZE", SqlDbType.BigInt).Value = PageSize;
                comm.Parameters.Add("@STRQUERY", SqlDbType.VarChar, 4000).Value = strQueryCondition;

                if (Params != null)
                {
                    foreach (string Key in Params.Keys)
                    {
                        comm.Parameters.Add(Key, SqlDbType.DateTime).Value = Params[Key];
                    }
                }

                dr = comm.ExecuteReader();
                DynamicBuilder<T> DynamicBuilder = null;
                DynamicBuilder = DynamicBuilder<T>.CreateBuilder(dr);

                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        T col = DynamicBuilder.Build(dr); // CreateInstanceT();
                        Cols.Add(col);
                    }

                    dr.NextResult();
                    //Record Count
                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            TotalRecord = Convert.ToInt32(dr[0]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            return Cols;
        }


        public List<T> LoadData(T Obj, string strQueryCondition, int intCurrPage, string SPName, int PageSize,
                                string SortBy, string Sort)
        {
            var Cols = new List<T>();

            try
            {
                Conn.Open();
                comm = new SqlCommand(SPName, Conn);
                comm.CommandTimeout = 2000;
                comm.CommandType = CommandType.StoredProcedure;
                comm.Parameters.Add("@CURRENTPAGE", SqlDbType.BigInt).Value = intCurrPage;
                comm.Parameters.Add("@PAGESIZE", SqlDbType.BigInt).Value = PageSize;
                comm.Parameters.Add("@STRQUERY", SqlDbType.VarChar, 4000).Value = strQueryCondition;
                comm.Parameters.Add("@SORT_BY", SqlDbType.VarChar, 50).Value = SortBy;
                comm.Parameters.Add("@SORT", SqlDbType.VarChar, 50).Value = Sort;

                dr = comm.ExecuteReader();
                DynamicBuilder<T> DynamicBuilder = null;
                DynamicBuilder = DynamicBuilder<T>.CreateBuilder(dr);

                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        T col = DynamicBuilder.Build(dr); // CreateInstanceT();
                    }

                    dr.NextResult();
                    //Record Count
                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            TotalRecord = dr.GetInt32(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            return Cols;
        }


        public List<T> GetData(string SQL)
        {
            var Cols = new List<T>();
            var conn = new SqlConnection(constr);
            try
            {
                
                conn.Open();

                comm = new SqlCommand(SQL, conn);
                comm.CommandTimeout = 30000;
                comm.CommandType = CommandType.Text;
                dr = comm.ExecuteReader();
                DynamicBuilder<T> DynamicBuilder = null;
                DynamicBuilder = DynamicBuilder<T>.CreateBuilder(dr);

                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        T col = DynamicBuilder.Build(dr); // CreateInstanceT();
                        Cols.Add(col);
                    }
                    dr.NextResult();
                    //Record Count
                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            TotalRecord = dr.GetInt32(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
            }

            return Cols;
        }


        public IEnumerable GetDataList(string SQL)
        {
            var Cols = new List<ArrayList>();
            try
            {
                Conn.Open();
                comm = new SqlCommand(SQL, Conn);
                comm.CommandType = CommandType.Text;
                dr = comm.ExecuteReader();
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        var Col = new ArrayList();
                        for (int i = 0; i <= dr.FieldCount - 1; i++)
                        {
                            Col.Add(dr[i]);
                        }
                        Cols.Add(Col);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            return Cols;
        }

        public DataSet GetDataListDataSet(string SQL)
        {
            var DS = new DataSet();
           
            try
            {
                Conn.Open();
                comm = new SqlCommand(SQL, Conn);
                comm.CommandType = CommandType.Text;
                comm.CommandTimeout = 3000;
                var da = new SqlDataAdapter(comm);
                da.Fill(DS);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            return DS;
        }

        public DataTable GetDataListDataTable(string SQL)
        {
            var DT = new DataTable();
            try
            {
                Conn.Open();
                comm = new SqlCommand(SQL, Conn);
                comm.CommandType = CommandType.Text;
                dr = comm.ExecuteReader();
                DT.Load(dr);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            return DT;
        }

        public DataTable GetDataListDataTableBySP(string SPName, Dictionary<string, string> Params)
        {
            var DT = new DataTable();
            try
            {
                Conn.Open();
                comm = new SqlCommand(SPName, Conn);
                comm.CommandTimeout = 2000;
                comm.CommandType = CommandType.StoredProcedure;
                if (Params != null)
                {
                    foreach (string Key in Params.Keys)
                    {
                        comm.Parameters.Add(Key, SqlDbType.Char, 36).Value = Params[Key];
                    }
                }
                dr = comm.ExecuteReader();
                DT.Load(dr);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            return DT;
        }

        public DataSet GetDataListDataSet(string SPName, Dictionary<string, object> Params)
        {
            var DT = new DataTable();
            var DS = new DataSet();

            try
            {
                Conn.Open();
                comm = new SqlCommand(SPName, Conn);
                comm.CommandTimeout = 2000;
                comm.CommandType = CommandType.StoredProcedure;
                if (Params != null)
                {
                    foreach (string Key in Params.Keys)
                    {
                        comm.Parameters.Add(Key, SqlDbType.Char, 36).Value = Params[Key];
                    }
                }
                dr = comm.ExecuteReader();
                DT.Load(dr);
                DS.Tables.Add(DT);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            return DS;
        }

        public void BulkInsert<T>(string connection, string tableName, IList<T> list)
        {
            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.BatchSize = list.Count;
                bulkCopy.DestinationTableName = tableName;
                bulkCopy.BulkCopyTimeout = 1000;

                var table = new DataTable();
                var props = TypeDescriptor.GetProperties(typeof(T))
                    //Dirty hack to make sure we only have system data types 
                    //i.e. filter out the relationships/collections
                                           .Cast<PropertyDescriptor>()
                                           .Where(propertyInfo => propertyInfo.PropertyType.Namespace.Equals("System"))
                                           .ToArray();

                foreach (var propertyInfo in props)
                {
                    bulkCopy.ColumnMappings.Add(propertyInfo.Name, propertyInfo.Name);
                    table.Columns.Add(propertyInfo.Name, Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType);
                }

                var values = new object[props.Length];
                foreach (var item in list)
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        values[i] = props[i].GetValue(item);
                    }

                    table.Rows.Add(values);
                }

                bulkCopy.WriteToServerAsync(table);
            }
        }


        public bool Insert(T Obj)
        {
            var strSQL = new StringBuilder();

            strSQL.AppendFormat("INSERT INTO {0} (", Obj.GetType().Name);
            foreach (PropertyInfo P in Obj.GetType().GetProperties())
            {
                strSQL.AppendFormat("{0}", P.Name);
                strSQL.Append(",");
            }
            strSQL.Replace(",", ")", strSQL.ToString().Length - 1, 1);

            strSQL.Append(" VALUES (");
            foreach (PropertyInfo P in Obj.GetType().GetProperties())
            {
                if (P.GetValue(Obj, null) == null)
                {
                    strSQL.Append("null");
                }
                else if (P.GetValue(Obj, null).GetType().ToString() == "System.String")
                {
                    strSQL.AppendFormat("'{0}'", P.GetValue(Obj, null));
                }
                else if (P.GetValue(Obj, null).GetType().ToString() == "System.DateTime")
                {
                    strSQL.AppendFormat("'{0}'",
                                        Convert.ToDateTime(P.GetValue(Obj, null)).ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else if (P.GetValue(Obj, null).GetType().ToString() == "System.Boolean")
                {
                    strSQL.AppendFormat("{0}", Convert.ToInt16(P.GetValue(Obj, null)));
                }
                else if (P.GetValue(Obj, null).GetType().ToString() == "System.Double")
                {
                    strSQL.AppendFormat("{0}", (Convert.ToDecimal(P.GetValue(Obj, null))));
                }
                else if (P.GetValue(Obj, null).GetType().ToString() == "System.Decimal")
                {
                    strSQL.AppendFormat("{0}", (Convert.ToDecimal(P.GetValue(Obj, null))));
                }
                else
                {
                    strSQL.AppendFormat("{0}", P.GetValue(Obj, null));
                }
                strSQL.Append(",");
            }
            strSQL.Replace(",", ")", strSQL.ToString().Length - 1, 1);

            int i = 0;

            try
            {
                Conn.Open();
                comm = new SqlCommand(strSQL.ToString(), Conn);
                comm.CommandType = CommandType.Text;
                i = comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            if (i > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<T> LoadData(T Obj, string strQueryCondition, int intCurrPage, string SPName, int PageSize)
        {
            var Cols = new List<T>();

            try
            {
                Conn.Open();
                comm = new SqlCommand(SPName, Conn);
                comm.CommandTimeout = 2000;
                comm.CommandType = CommandType.StoredProcedure;
                comm.Parameters.Add("@CURRENTPAGE", SqlDbType.BigInt).Value = intCurrPage;
                comm.Parameters.Add("@PAGESIZE", SqlDbType.BigInt).Value = PageSize;
                comm.Parameters.Add("@STRQUERY", SqlDbType.VarChar, 4000).Value = strQueryCondition;

                dr = comm.ExecuteReader();
                DynamicBuilder<T> DynamicBuilder = null;
                DynamicBuilder = DynamicBuilder<T>.CreateBuilder(dr);

                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        T col = DynamicBuilder.Build(dr); // CreateInstanceT();
                        Cols.Add(col);
                    }

                    dr.NextResult();
                    //Record Count
                    if (dr.HasRows)
                    {
                        while (dr.Read())
                        {
                            TotalRecord = dr.GetInt32(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            return Cols;
        }

        public bool Insert(T Obj, string[] ExceptProperties)
        {
            var strSQL = new StringBuilder();

            strSQL.AppendFormat("INSERT INTO {0} (", Obj.GetType().Name);
            foreach (PropertyInfo P in Obj.GetType().GetProperties())
            {
                if (!String.IsNullOrEmpty(ExceptProperties.FirstOrDefault(p => p.ToLower() == P.Name.ToLower())))
                    continue;

                strSQL.AppendFormat("{0}", P.Name);
                strSQL.Append(",");
            }
            strSQL.Replace(",", ")", strSQL.ToString().Length - 1, 1);

            strSQL.Append(" VALUES (");
            foreach (PropertyInfo P in Obj.GetType().GetProperties())
            {
                if (! String.IsNullOrEmpty(ExceptProperties.FirstOrDefault(p => p.ToLower() == P.Name.ToLower())))
                    continue;

                if (P.GetValue(Obj, null) == null)
                {
                    strSQL.Append("null");
                }
                else if (P.GetValue(Obj, null).GetType().ToString() == "System.String")
                {
                    strSQL.AppendFormat("'{0}'", P.GetValue(Obj, null));
                }
                else if (P.GetValue(Obj, null).GetType().ToString() == "System.DateTime")
                {
                    strSQL.AppendFormat("Cast('{0}'as DateTime)",
                                        Convert.ToDateTime(P.GetValue(Obj, null)).ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else if (P.GetValue(Obj, null).GetType().ToString() == "System.Boolean")
                {
                    strSQL.AppendFormat("{0}", Convert.ToInt16(P.GetValue(Obj, null)));
                }
                else if (P.GetValue(Obj, null).GetType().ToString() == "System.Double")
                {
                    strSQL.AppendFormat("{0}", (Convert.ToDecimal(P.GetValue(Obj, null))));
                }
                else if (P.GetValue(Obj, null).GetType().ToString() == "System.Decimal")
                {
                    strSQL.AppendFormat("{0}", (Convert.ToDecimal(P.GetValue(Obj, null))));
                }
                else
                {
                    strSQL.AppendFormat("{0}", P.GetValue(Obj, null));
                }
                strSQL.Append(",");
            }
            strSQL.Replace(",", ")", strSQL.ToString().Length - 1, 1);

            int i = 0;
            try
            {
                Conn.Open();
                comm = new SqlCommand(strSQL.ToString(), Conn);
                comm.CommandType = CommandType.Text;
                i = comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            if (i > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Delete(T Obj, string[] arrColumn, object[] arrValue)
        {
            string strTableName = Obj.GetType().Name;
            var strSQL = new StringBuilder();

            strSQL.AppendFormat("DELETE {0} WHERE ", strTableName);
            for (int x = 0; x <= arrColumn.GetUpperBound(0); x++)
            {
                if (arrValue[x].GetType().ToString() == "System.String")
                {
                    strSQL.AppendFormat("{0}='{1}'", arrColumn[x], arrValue[x]);
                }
                else if (arrValue[x].GetType().ToString() == "System.DateTime")
                {
                    strSQL.AppendFormat("{0}='{1}'", arrColumn[x],
                                        Convert.ToDateTime(arrValue[x]).ToString("yyyy-MM-dd"));
                }
                else if (arrValue[x].GetType().ToString() == "System.Boolean")
                {
                    strSQL.AppendFormat("{0}={1}", arrColumn[x], Convert.ToInt16(arrValue[x]));
                }
                else if (arrValue[x].GetType().ToString() == "System.Decimal")
                {
                    strSQL.AppendFormat("{0}={1}", arrColumn[x], (Convert.ToDecimal(arrValue[x])));
                }
                else if (arrValue[x].GetType().ToString() == "System.Double")
                {
                    strSQL.AppendFormat("{0}={1}", arrColumn[x], (Convert.ToDecimal(arrValue[x])));
                }
                else
                {
                    strSQL.AppendFormat("{0}={1}", arrColumn[x], arrValue[x]);
                }
                if (x < arrColumn.GetUpperBound(0))
                {
                    strSQL.Append(" AND ");
                }
            }

            int i = 0;
            try
            {
                Conn.Open();
                comm = new SqlCommand(strSQL.ToString(), Conn);
                comm.CommandType = CommandType.Text;
                i = comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            if (i > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public bool Update(T OBJ, string[] arrWhereCol, object[] arrWhereVal)
        {
            var strSQL = new StringBuilder();
            string strTableName = OBJ.GetType().Name;
            strSQL.AppendFormat("UPDATE {0}", strTableName);
            strSQL.Append(" Set ");
            int i = 0;
            foreach (PropertyInfo P in OBJ.GetType().GetProperties())
            {
                if (P.GetValue(OBJ, null) == null)
                {
                    strSQL.AppendFormat("{0}=null", P.Name);
                }
                else if (P.GetValue(OBJ, null).GetType().ToString() == "System.String")
                {
                    strSQL.AppendFormat("{0}='{1}'", P.Name, P.GetValue(OBJ, null));
                }
                else if (P.GetValue(OBJ, null).GetType().ToString() == "System.DateTime")
                {
                    strSQL.AppendFormat("{0}='{1}'", P.Name,
                                        Convert.ToDateTime(P.GetValue(OBJ, null)).ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else if (P.GetValue(OBJ, null).GetType().ToString() == "System.Boolean")
                {
                    strSQL.AppendFormat("{0}={1}", P.Name, Convert.ToInt16(P.GetValue(OBJ, null)));
                }
                else if (P.GetValue(OBJ, null).GetType().ToString() == "System.Decimal")
                {
                    strSQL.AppendFormat("{0}={1}", P.Name, (Convert.ToDecimal(P.GetValue(OBJ, null))));
                }
                else if (P.GetValue(OBJ, null).GetType().ToString() == "System.Double")
                {
                    strSQL.AppendFormat("{0}={1}", P.Name, (Convert.ToDecimal(P.GetValue(OBJ, null))));
                }
                else
                {
                    strSQL.AppendFormat("{0}={1}", P.Name, P.GetValue(OBJ, null));
                }
                if (i < (OBJ.GetType().GetProperties().GetLength(0) - 1)) strSQL.Append(", ");
                i += 1;
            }

            //strSQL.Replace(",", "", strSQL.ToString.Length - 1, 1)

            strSQL.Append(" WHERE ");
            for (int x = 0; x <= arrWhereCol.GetUpperBound(0); x++)
            {
                if (arrWhereVal[x].GetType().ToString() == "System.String")
                {
                    strSQL.AppendFormat("{0}='{1}'", arrWhereCol[x], arrWhereVal[x]);
                }
                else if (arrWhereVal[x].GetType().ToString() == "System.DateTime")
                {
                    strSQL.AppendFormat("{0}='{1}'", arrWhereCol[x],
                                        Convert.ToDateTime(arrWhereVal[x]).ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else if (arrWhereVal[x].GetType().ToString() == "System.Boolean")
                {
                    strSQL.AppendFormat("{0}={1}", arrWhereCol[x], Convert.ToInt16(arrWhereVal[x]));
                }
                else if (arrWhereVal[x].GetType().ToString() == "System.Decimal")
                {
                    strSQL.AppendFormat("{0}={1}", arrWhereCol[x], (Convert.ToDecimal(arrWhereVal[x])));
                }
                else if (arrWhereVal[x].GetType().ToString() == "System.Double")
                {
                    strSQL.AppendFormat("{0}={1}", arrWhereCol[x], (Convert.ToDecimal(arrWhereVal[x])));
                }
                else
                {
                    strSQL.AppendFormat("{0}={1}", arrWhereCol[x], arrWhereVal[x]);
                }
                if (x < arrWhereCol.GetUpperBound(0))
                {
                    strSQL.Append(" AND ");
                }
            }


            i = 0;
            try
            {
                Conn.Open();
                comm = new SqlCommand(strSQL.ToString(), Conn);
                comm.CommandType = CommandType.Text;
                i = comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            if (i > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Update(T OBJ)
        {
            var strSQL = new StringBuilder();
            string strTableName = OBJ.GetType().Name;
            strSQL.AppendFormat("UPDATE {0}", OBJ.GetType().Name);
            strSQL.Append(" Set ");
            int i = 0;
            foreach (PropertyInfo P in OBJ.GetType().GetProperties())
            {
                if (P.GetValue(OBJ, null) == null)
                {
                    strSQL.AppendFormat("{0}=null", P.Name);
                }
                else if (P.GetValue(OBJ, null).GetType().ToString() == "System.String")
                {
                    strSQL.AppendFormat("{0}='{1}'", P.Name, P.GetValue(OBJ, null));
                }
                else if (P.GetValue(OBJ, null).GetType().ToString() == "System.DateTime")
                {
                    strSQL.AppendFormat("{0}='{1}'", P.Name,
                                        Convert.ToDateTime(P.GetValue(OBJ, null)).ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else if (P.GetValue(OBJ, null).GetType().ToString() == "System.Boolean")
                {
                    strSQL.AppendFormat("{0}={1}", P.Name, Convert.ToInt16(P.GetValue(OBJ, null)));
                }
                else if (P.GetValue(OBJ, null).GetType().ToString() == "System.Decimal")
                {
                    strSQL.AppendFormat("{0}={1}", P.Name, (Convert.ToDecimal(P.GetValue(OBJ, null))));
                }
                else if (P.GetValue(OBJ, null).GetType().ToString() == "System.Double")
                {
                    strSQL.AppendFormat("{0}={1}", P.Name, (Convert.ToDecimal(P.GetValue(OBJ, null))));
                }
                else
                {
                    strSQL.AppendFormat("{0}={1}", P.Name, P.GetValue(OBJ, null));
                }
                if (i < (OBJ.GetType().GetProperties().GetLength(0) - 1)) strSQL.Append(", ");
                i += 1;
            }

            //strSQL.Replace(",", "", strSQL.ToString.Length - 1, 1)

            strSQL.Append(" WHERE ");
            string WhereCol =
                OBJ.GetType().GetProperties().Where(p => p.Name.ToLower().Contains("id")).FirstOrDefault().Name;
            object WhereVal =
                OBJ.GetType().GetProperties().Where(p => p.Name.ToLower().Contains("id")).FirstOrDefault().GetValue(
                    OBJ, null);
            strSQL.AppendFormat("{0}='{1}'", WhereCol, WhereVal);

            i = 0;
            try
            {
                Conn.Open();
                comm = new SqlCommand(strSQL.ToString(), Conn);
                comm.CommandType = CommandType.Text;
                i = comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            if (i > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public bool ExecuteCommand(string SQL)
        {
            int i = 0;
            try
            {
                Conn.Open();
                comm = new SqlCommand(SQL, Conn);
                comm.CommandType = CommandType.Text;
                comm.CommandTimeout = 30000;
                i = comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            if (i > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public async Task<bool> ExecuteCommandAsync(string SQL)
        {
            int i = 0;
            try
            {
                Conn.Open();
                comm = new SqlCommand(SQL, Conn);
                comm.CommandType = CommandType.Text;
                comm.CommandTimeout = 30000;
                i = await comm.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            if (i > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public bool ExecuteCommand(string SQL,out int recAffected)
        {
            recAffected = 0;
            try
            {
                Conn.Open();
                comm = new SqlCommand(SQL, Conn);
                comm.CommandType = CommandType.Text;
                comm.CommandTimeout = 0;
                recAffected = comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Conn.Close();
            }

            if (recAffected > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public List<T> GetDataAsyncs(string[] SQL, T Obj)
        {
            var Alls = new BlockingCollection<T>();
            var actions = new Action<IAsyncResult>[SQL.Length];

            var _resets = new ManualResetEvent[SQL.Length];

            var conns = new SqlConnection[SQL.Length];
            var comms = new SqlCommand[SQL.Length];
            for (int x = 0; x < SQL.Length; x++)
            {
                conns[x] = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConn"].ConnectionString);
                comms[x] = new SqlCommand(SQL[x], conns[x]);
                comms[x].CommandTimeout = 3000;
                comms[x].CommandType = CommandType.Text;
                _resets[x] = new ManualResetEvent(false);
            }

            for (int y = 0; y < SQL.Length; y++)
            {
                actions[y] = delegate(IAsyncResult result)
                                 {
                                     var command = (SqlCommand) result.AsyncState;


                                     try
                                     {
                                         SqlDataReader dr1 = command.EndExecuteReader(result);
                                         if (dr1.HasRows)
                                         {
                                             while (dr1.Read())
                                             {
                                                 T col = CreateInstanceT();
                                                 foreach (PropertyInfo P in Obj.GetType().GetProperties())
                                                 {
                                                     for (int i = 0; i <= dr1.FieldCount - 1; i++)
                                                     {
                                                         if (dr1.GetName(i).ToLower() == P.Name.ToLower())
                                                         {
                                                             if (!Convert.IsDBNull(dr1[P.Name]))
                                                                 P.SetValue(col, dr1[P.Name], null);
                                                         }
                                                     }
                                                 }

                                                 Alls.Add(col);
                                             }
                                         }
                                     }
                                     catch (Exception ex)
                                     {
                                         throw ex;
                                     }
                                     finally
                                     {
                                         command.Connection.Close();
                                         _resets[count].Set();
                                     }
                                 };
            }


            try
            {
                var callbacks = new AsyncCallback[SQL.Length];
                var results = new IAsyncResult[SQL.Length];

                for (int i = 0; i < SQL.Length; i++)
                {
                    conns[i].Open();
                    callbacks[i] = new AsyncCallback(actions[i]);
                    results[i] = comms[i].BeginExecuteReader(callbacks[i], comms[i]);
                }

                foreach (ManualResetEvent reset in _resets)
                {
                    reset.WaitOne();
                    count++;
                }

                return Alls.ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                for (int i = 0; i < SQL.Length; i++)
                {
                    if (conns[i].State != ConnectionState.Closed) conns[i].Close();
                }

                count = 0;
            }
        }


        private static Action<T> Setter<T>(string PropertyName, object value)
        {
            ParameterExpression parameter = Expression.Parameter(typeof (T), "params");
            ConstantExpression val = Expression.Constant(value);
            PropertyInfo pi = typeof (T).GetProperty(PropertyName);
            MethodCallExpression set = Expression.Call(parameter, pi.GetSetMethod(), val);
            LambdaExpression lamda = Expression.Lambda(set, parameter);
            return (Action<T>) lamda.Compile();
        }


        private DataTable ConvertListToDataTable(IList<T> Source)
        {
            var Obj = (T) Activator.CreateInstance(typeof (T));
            var dt = new DataTable(Obj.GetType().ToString());

            foreach (PropertyInfo Prop in Obj.GetType().GetProperties())
            {
                if (Prop.PropertyType.Name.Contains("Null"))
                {
                    dt.Columns.Add(new DataColumn(Prop.Name, Nullable.GetUnderlyingType(Prop.PropertyType)));
                }
                else
                {
                    dt.Columns.Add(new DataColumn(Prop.Name, Prop.PropertyType));
                }
            }

            foreach (T obj in Source)
            {
                DataRow row = dt.NewRow();
                foreach (PropertyInfo Prop in obj.GetType().GetProperties())
                {
                    //value = Prop.GetValue(Obj, Nothing)
                    //If value IsNot Nothing Then row(Prop.Name) = value
                    if (Prop.GetValue(obj, null) == null)
                    {
                        row[Prop.Name] = DBNull.Value;
                    }
                    else
                    {
                        row[Prop.Name] = Prop.GetValue(obj, null);
                    }
                }
                dt.Rows.Add(row);
            }
            return dt;
        }


        private dynamic SqlDataReaderToExpando(SqlDataReader reader)
        {
            var expandoObject = new ExpandoObject() as IDictionary<string, object>;

            for (var i = 0; i < reader.FieldCount; i++)
            {
                try
                {
                    expandoObject.Add(reader.GetName(i), reader[i]);
                }
                catch (Exception ex)
                {
                    expandoObject.Add(reader.GetName(i), 0);
                    
                }
                
            }

            return expandoObject;
        }

        public List<dynamic> GetDataList(string sql, string connectionStringName)
        {

            var result = new List<dynamic>();
            var conn = new SqlConnection(connectionStringName);

            try
            {
                conn.Open();

                // declare command
                var readCommand =
                new SqlCommand(sql, conn);

                readCommand.CommandTimeout = 0;
                using (var reader = readCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var data = SqlDataReaderToExpando(reader);
                        result.Add(data);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                conn.Close();
            }

            return result;
        }


        public async Task<IList<dynamic>> GetDataListAsync(string sql, string connectionStringName)
        {

            var result = new List<dynamic>();
            var conn = new SqlConnection(connectionStringName);

            try
            {
                 conn.Open();
                // declare command
                var readCommand =
                new SqlCommand(sql, conn);

                readCommand.CommandTimeout = 0;
                using (var reader = await readCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var data = SqlDataReaderToExpando(reader);
                        result.Add(data);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                conn.Close();
            }

            return result;
        }


        //public IList<dynamic> GetDataList(string sql, string connectionStringName)
        //{

        //    var result = new List<dynamic>();
        //    var conn = new SqlConnection(connectionStringName);

        //    try
        //    {
        //        conn.Open();
        //        // declare command
        //        var readCommand =
        //        new SqlCommand(sql, conn);

        //        readCommand.CommandTimeout = 0;
        //        using (var reader = readCommand.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                var data = SqlDataReaderToExpando(reader);
        //                result.Add(data);
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }

        //    return result;
        //}

    }


    public class DynamicBuilder<T>
    {
        private static readonly MethodInfo getValueMethod = typeof (IDataRecord).GetMethod("get_Item",
                                                                                           new[] {typeof (int)});

        private static readonly MethodInfo isDBNullMethod = typeof (IDataRecord).GetMethod("IsDBNull",
                                                                                           new[] {typeof (int)});

        private Load handler;

        private DynamicBuilder()
        {
        }

        public T Build(IDataRecord dataRecord)
        {
            return handler(dataRecord);
        }


        public static DynamicBuilder<T> CreateBuilder(IDataRecord dataRecord)
        {
            var dynamicBuilder = new DynamicBuilder<T>();
            int i = 0;

            var method = new DynamicMethod("DynamicCreate", typeof (T), new[] {typeof (IDataRecord)}, typeof (T), true);
            ILGenerator generator = method.GetILGenerator();

            LocalBuilder result = generator.DeclareLocal(typeof (T));
            generator.Emit(OpCodes.Newobj, typeof (T).GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Stloc, result);

            for (i = 0; i <= dataRecord.FieldCount - 1; i++)
            {
                PropertyInfo propertyInfo =
                    typeof (T).GetProperties().FirstOrDefault(x => x.Name.Contains(dataRecord.GetName(i)));
                //if (propertyInfo == null ) propertyInfo = typeof(T).GetProperty(dataRecord.GetName(i).ToUpper());
                //if (propertyInfo == null) propertyInfo = typeof(T).GetProperty(dataRecord.GetName(i).ToLower());
                Label endIfLabel = generator.DefineLabel();

                if ((propertyInfo != null))
                {
                    if ((propertyInfo.GetSetMethod() != null))
                    {
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldc_I4, i);
                        generator.Emit(OpCodes.Callvirt, isDBNullMethod);
                        generator.Emit(OpCodes.Brtrue, endIfLabel);
                        generator.Emit(OpCodes.Ldloc, result);
                        generator.Emit(OpCodes.Ldarg_0);
                        generator.Emit(OpCodes.Ldc_I4, i);
                        generator.Emit(OpCodes.Callvirt, getValueMethod);
                        Type dataType = dataRecord.GetFieldType(i);
                        bool isNullable = false;

                        if (propertyInfo.PropertyType.Name.ToLower().Contains("nullable")) isNullable = true;
                        if (isNullable) generator.Emit(OpCodes.Unbox_Any, getNullableType(dataType));
                        else
                            generator.Emit(OpCodes.Unbox_Any, dataType);

                        generator.Emit(OpCodes.Callvirt, propertyInfo.GetSetMethod());
                        generator.MarkLabel(endIfLabel);
                    }
                }
            }

            generator.Emit(OpCodes.Ldloc, result);
            generator.Emit(OpCodes.Ret);

            dynamicBuilder.handler = (Load) method.CreateDelegate(typeof (Load));
            return dynamicBuilder;
        }


        private static Type getNullableType(Type type)
        {
            Type result = null;
            if (type == typeof (bool))
                result = typeof (bool?);
            if (type == typeof (byte))
                result = typeof (byte?);
            if (type == typeof (DateTime))
                result = typeof (DateTime?);
            if (type == typeof (decimal))
                result = typeof (decimal?);
            if (type == typeof (double))
                result = typeof (double?);
            if (type == typeof (float))
                result = typeof (float?);
            if (type == typeof (Guid))
                result = typeof (Guid?);
            if (type == typeof (Int16))
                result = typeof (Int16?);
            if (type == typeof (Int32))
                result = typeof (Int32?);
            if (type == typeof (Int64))
                result = typeof (Int64?);
            return result;
        }

        #region Nested type: Load

        private delegate T Load(IDataRecord dataRecord);

        #endregion
    }
}