using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;


namespace TODO_list
{
    public class DB
    {
        private string _defaultDBpath = System.Environment.CurrentDirectory + "/db/record.sqlite";
        private SQLiteConnection connDB = null;
        private SQLiteCommand command = null;
        private SQLiteDataReader rdr = null;

        public void connectToDB(string dbPath = "")
        {
            // 경로 확인
            if(dbPath == String.Empty)
            {
                dbPath = this._defaultDBpath;
            }

            // 기존의 폴더가 없다면 폴더 생성
            string dirPath = dbPath.Substring(0, dbPath.LastIndexOf('/'));
            DirectoryInfo di = new DirectoryInfo(dirPath);
            if (!di.Exists)
            {
                di.Create();
            }

            // 기존 DB가 없다면 생성
            System.IO.FileInfo fi = new System.IO.FileInfo(dbPath);
            if (!fi.Exists)
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            // DB에 연결
            this.connDB = new SQLiteConnection(String.Format("Data Source={0};Version=3;", dbPath));
            this.connDB.Open();
        }

        public void execute(string sql)
        {
            command = new SQLiteCommand(sql, this.connDB);
            this.rdr = command.ExecuteReader();
        }

        public void readOutput()
        {
            this.rdr.Read();
        }



        public int executeNonQuery(string sql)
        {
            this.command = new SQLiteCommand(sql, this.connDB);
            int result = command.ExecuteNonQuery();
            return result;
        }
    }
}
