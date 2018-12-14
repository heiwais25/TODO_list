using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;


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
        string _dbFullPath = System.Environment.CurrentDirectory;
        private SQLiteConnection _connDB = null;

        public UserDB(string dbFullPath)
        {
            this._dbFullPath = dbFullPath;
        }

        public void Close()
        {
            this._connDB.Close();
        }

        public void Create()
        {
            SQLiteConnection.CreateFile(_dbFullPath);
        }

        public void Connect()
        {
            // DB에 연결
            this._connDB = new SQLiteConnection(String.Format("Data Source={0};Version=3;", this._dbFullPath));
            this._connDB.Open();
        }

        public int ExecuteNonQuery(string sql)
        {
            SQLiteCommand command = new SQLiteCommand(sql, this._connDB);
            return command.ExecuteNonQuery();
        }

        public UserDBReader ExecuteReader(string sql)
        {
            SQLiteCommand command = new SQLiteCommand(sql, this._connDB);
            SQLiteDataReader rdr = command.ExecuteReader();
            UserDBReader dbReader = new UserDBReader(rdr);
            return dbReader;
        }

        public long ExecuteScalar(string sql)
        {
            SQLiteCommand command = new SQLiteCommand(sql, this._connDB);
            return (long)command.ExecuteScalar();
        }
    }
}
