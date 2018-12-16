using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DataBase;
using System.IO;

namespace TODO_list
{
    public class ApplicationDB
    {
        UserDB _db = null;

        public void InitializeDB(string dbFullPath)
        {
            CreateDB(dbFullPath);
            CreateTable("all_task");
            CreateTable("unfinished_task_rowid");
            CreateTable("all_task_row_count");
            CreateTable("unfinished_task_row_count");
        }

        private void CreateDB(string dbFullPath)
        {
            ref UserDB db = ref this._db;

            // Prepare the directory (Recursive하게 만들수도 있긴 할듯)
            DirectoryInfo di = new DirectoryInfo(dbFullPath.Substring(0, dbFullPath.LastIndexOf('/')));
            if (!di.Exists)
            {
                di.Create();
            }

            // Check already exist DB, otherwise create new DB
            db = new UserDB(dbFullPath);
            FileInfo fi = new System.IO.FileInfo(dbFullPath);
            if (!fi.Exists)
            {
                db.Create();
            }
            db.Connect();
        }

        private string GetCreateTableSQL(string mode)
        {
            string sql = "";
            if (mode == "all_task")
            {
                sql = String.Format("create table {0} (date int, task varchar(100), isFinished int)", mode);
            }
            else if(mode == "unfinished_task_rowid")
            {
                sql = String.Format("create table {0} (id int)", mode);
            }
            else if(mode == "all_task_row_count" || mode == "unfinished_task_row_count")
            {
                sql = String.Format("CREATE TABLE {0} (count int);", mode);
            }
            return sql;
        }

        private void CreateTable(string mode)
        {
            string sql;
            ref UserDB db = ref this._db;
            UserDBReader rdr = null;

            try
            {
                sql = String.Format("SELECT * FROM {0}", mode);
                rdr = db.ExecuteReader(sql);
                if (rdr != null)
                {
                    rdr.Close();
                }
            }
            catch
            {
                sql = GetCreateTableSQL(mode);
                int result = db.ExecuteNonQuery(sql);

                if(mode == "all_task_row_count" || mode == "unfinished_task_row_count")
                {
                    sql = String.Format("INSERT INTO {0} (count) VALUES (0);", mode);
                    result = db.ExecuteNonQuery(sql);
                }
            }
        }

        public int GetTableCurrentRowCount(string mode)
        {
            // Get the last input row id
            string sql = String.Format("SELECT * FROM {0} WHERE rowid=1;", mode);
            UserDBReader rdr = this._db.ExecuteReader(sql);
            rdr.Read();
            int ret = Convert.ToInt32(rdr.Get("count"));
            rdr.Close();
            return ret;
        }

        private void IncreaseTableCurrentRowCount(string mode)
        {
            int currentCount = GetTableCurrentRowCount(mode);
            string sql = String.Format("UPDATE {0} SET count={1} WHERE rowid = 1;", mode, currentCount + 1);
            this._db.ExecuteReader(sql);
        }

        private void DecreaseTableCurrentRowCount(string mode)
        {
            int currentCount = GetTableCurrentRowCount(mode);
            if (currentCount == 0)
            {
                throw new Exception(String.Format("Currently, there are no row in the {0}", mode));
            }
            string sql = String.Format("UPDATE {0} SET count={1} WHERE rowid = 1;", mode, currentCount - 1);
            this._db.ExecuteReader(sql);
        }

        private void InitializeTableCurrentRowCount(string mode)
        {
            string sql = String.Format("UPDATE {0} SET count={1} WHERE rowid = 1;", mode, 0);
            this._db.ExecuteReader(sql);
        }

        public void UpdateFinishedTask(WorkItem item)
        {
            string sql;
            item.isFinished = true;
            // In the case of it is new one
            if (item.rowId == 0)
            {
                IncreaseTableCurrentRowCount("all_task_row_count");

                // Add the item information to DB that it is finished
                sql = System.String.Format(
                    "INSERT INTO all_task (date, task, isFinished) VALUES ({0}, '{1}', {2});", 
                    item.fullDate, item.task, item.isFinished
                );
            }
            else
            {
                sql = System.String.Format(
                    "UPDATE all_task SET isFinished={0} WHERE rowid={1}",
                    item.isFinished, item.rowId
                );
            }
            this._db.ExecuteReader(sql);
        }


        public ObservableCollection<WorkItem> GetUnfinishedTask()
        {
            ObservableCollection<WorkItem> items = new ObservableCollection<WorkItem>();

            // Bring the stored record from DB
            string sql = "SELECT * FROM unfinished_task_rowid";
            UserDBReader rowIdRdr = this._db.ExecuteReader(sql);
            while (rowIdRdr.Read())
            {
                int currentId = Convert.ToInt32(rowIdRdr.Get("id"));
                sql = String.Format("SELECT rowid, * FROM all_task WHERE rowid={0}", currentId);
                UserDBReader rdr = this._db.ExecuteReader(sql);
                while (rdr.Read())
                {
                    items.Insert(0, new WorkItem(
                    Convert.ToInt32(rdr.Get("date")),
                    Convert.ToString(rdr.Get("task")),
                    Convert.ToBoolean(rdr.Get("isFinished")),
                    Convert.ToInt32(rdr.Get("rowid"))
                    ));
                }
                rdr.Close();
            }
            return items;
        }

        public void ClearUnfinishedTaskTable()
        {
            string sql = "DELETE FROM unfinished_task_rowid";
            int result = this._db.ExecuteNonQuery(sql);

            InitializeTableCurrentRowCount("unfinished_task_row_count");
        }

        public void SaveUnfinishedTask(ObservableCollection<WorkItem> items)
        {
            string sql;
            // Add current remained item information to the DB
            foreach (WorkItem item in items.Reverse<WorkItem>())
            {
                int currentId = item.rowId;
                if (currentId == 0)
                {
                    IncreaseTableCurrentRowCount("all_task_row_count");

                    sql = System.String.Format(
                        "INSERT INTO all_task (date, task, isFinished) VALUES ({0}, '{1}', {2})",
                        item.fullDate, item.task, item.isFinished
                    );
                    this._db.ExecuteNonQuery(sql);

                    currentId = GetAllTaskCount();
                }

                IncreaseTableCurrentRowCount("unfinished_task_row_count");

                // 현재 순서에 맞춰서 저장
                sql = System.String.Format(
                    "INSERT INTO unfinished_task_rowid (id) VALUES ({0})", currentId
                );

                this._db.ExecuteNonQuery(sql);
            }
        }

        public void RemoveUnfinishedTask(WorkItem item)
        {
            if (item.rowId == 0)
            {
                return;
            }

            string sql;
            item.isFinished = true;

            // In the case of it is new one
            sql = System.String.Format(
                "DELETE FROM all_task WHERE rowid={0};", item.rowId
            );
            this._db.ExecuteReader(sql);

            DecreaseTableCurrentRowCount("all_task_row_count");
        }

        public int GetAllTaskCount()
        {
            // Get the last input row id
            string sql = "SELECT IFNULL(MAX(rowid), 1) AS Id FROM all_task;";
            int count = Convert.ToInt32(this._db.ExecuteScalar(sql));
            return count;
        }


        public void CloseDB()
        {
            this._db.Close();
        }
    }
}
