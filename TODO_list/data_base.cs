using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Collections.ObjectModel;



namespace DataBase
{
    public class UserDBReader
    {
        private SQLiteDataReader _rdr = null;
        public UserDBReader(SQLiteDataReader rdr)
        {
            this._rdr = rdr;
        }

        public bool Read()
        {
            return this._rdr.Read();
        }

        public object Get(string key)
        {
            return this._rdr[key];
        }

        public void Close()
        {
            this._rdr.Close();
        }
    }

    public class UserDB
    {
        private string _dbFullPath = System.Environment.CurrentDirectory;
        private string _dbConnectionStatement = "";


        public UserDB(string dbFullPath)
        {
            this._dbFullPath = dbFullPath;
            this._dbConnectionStatement = String.Format("Data Source={0};Version=3;", this._dbFullPath);
        }


        public int ExecuteNonQuery(string sql)
        {
            int ret = 0;
            using(SQLiteConnection connDB = new SQLiteConnection(this._dbConnectionStatement))
            {
                connDB.Open();
                using (SQLiteCommand command = new SQLiteCommand(sql, connDB))
                {
                    ret = command.ExecuteNonQuery();
                }
            }
            return ret;
        }

        public List<Dictionary<String, Object>> ExecuteReader(string sql)
        {
            List<Dictionary<String, Object>> dataTable = new List<Dictionary<String, Object>>();
            using (SQLiteConnection connDB = new SQLiteConnection(this._dbConnectionStatement))
            {
                connDB.Open();
                using(SQLiteCommand command = new SQLiteCommand(sql, connDB))
                {
                    SQLiteDataReader rdr = command.ExecuteReader();
                    if(rdr != null)
                    {
                        var columns = new List<String>();
                        for(int i=0;i<rdr.FieldCount;i++)
                        {
                            columns.Add(rdr.GetName(i));
                        }

                        while (rdr.Read())
                        {
                            Dictionary<String, Object> rowData = new Dictionary<String, Object>();
                            foreach (string columnName in columns)
                            {
                                rowData[columnName] = rdr[columnName];
                            }
                            dataTable.Add(rowData);
                        }
                    }
                }
            }
            return dataTable;
        }


        public void ExecuteTransaction(List<String> sqlList)
        {
            using(var connDB = new SQLiteConnection(this._dbConnectionStatement))
            {
                connDB.Open();
                using(var command = new SQLiteCommand(connDB))
                {
                    using(var transaction = connDB.BeginTransaction())
                    {
                        try
                        {
                            foreach (String sql in sqlList)
                            {
                                command.CommandText = sql;
                                command.ExecuteNonQuery();
                            }
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw ex;
                        }
                    }
                }
            }
        }


        public long ExecuteScalar(string sql)
        {
            long ret = 0;
            using (SQLiteConnection connDB = new SQLiteConnection(this._dbConnectionStatement))
            {
                connDB.Open();
                using (SQLiteCommand command = new SQLiteCommand(sql, connDB))
                {
                    ret = Convert.ToInt64(command.ExecuteScalar());
                }
            }
            return ret;
        }
    }
}