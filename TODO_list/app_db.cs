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
            UserDBReader rdr = null;
            string sql;
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

            // Check the all_task TABLE exist already Create the table
            try
            {
                sql = "SELECT * FROM all_task";
                rdr = db.ExecuteReader(sql);
            }
            catch
            {
                sql = "create table all_task (date int, task varchar(100), isFinished int)";
                int result = db.ExecuteNonQuery(sql);
            }
            if (rdr != null)
            {
                rdr.Close();
            }

            // Check the unfinished_task TABLE exist already Create the table
            try
            {
                sql = "SELECT * FROM unfinished_rowid";
                rdr = db.ExecuteReader(sql);
            }
            catch
            {
                sql = "create table unfinished_rowid (id int)";
                int result = db.ExecuteNonQuery(sql);
            }
            if (rdr != null)
            {
                rdr.Close();
            }
        }

        public void UpdateFinishedTask(WorkItem item)
        {
            string sql;
            item.isFinished = true;
            // In the case of it is new one
            if (item.rowId == 0)
            {
                // Add the item information to DB that it is finished
                sql = System.String.Format(
                    "INSERT INTO all_task (date, task, isFinished) VALUES ({0}, '{1}', {2})",
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
            string sql = "SELECT * FROM unfinished_rowid";
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
            string sql = "DELETE FROM unfinished_rowid";
            int result = this._db.ExecuteNonQuery(sql);
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
                    sql = System.String.Format(
                        "INSERT INTO all_task (date, task, isFinished) VALUES ({0}, '{1}', {2})",
                        item.fullDate, item.task, item.isFinished
                    );
                    this._db.ExecuteNonQuery(sql);

                    currentId = GetAllTaskCount();
                }

                // 현재 순서에 맞춰서 저장
                sql = System.String.Format(
                    "INSERT INTO unfinished_rowid (id) VALUES ({0})", currentId
                );
                this._db.ExecuteNonQuery(sql);
            }
        }

        public int GetAllTaskCount()
        {
            // Get the last input row id
            string sql = "SELECT IFNULL(MAX(rowid), 1) AS Id FROM all_task";
            int count = Convert.ToInt32(this._db.ExecuteScalar(sql));
            return count;
        }


        public void CloseDB()
        {
            this._db.Close();
        }
    }
}
