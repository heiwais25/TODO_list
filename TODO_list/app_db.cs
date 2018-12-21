using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DataBase;

namespace TODO_list
{
    public class ApplicationDB
    {
        public enum TableMode
        {
            AllTask,
            UnfinishedTaskRowID,
            AllTaskRowCount,
            UnfinishedTaskRowCount
        }


        public string GetCorrectTableName(TableMode mode)
        {
            string name = "";
            switch (mode)
            {
                case TableMode.AllTask:
                    name = "all_task";
                    break;
                case TableMode.UnfinishedTaskRowID:
                    name = "unfinished_task_rowid";
                    break;
                case TableMode.AllTaskRowCount:
                    name = "all_task_row_count";
                    break;
                case TableMode.UnfinishedTaskRowCount:
                    name = "unfinished_task_row_count";
                    break;
                default:
                    break;
            }
            return name;
        }


        UserDB _db = null;

        public ApplicationDB(string dbFullPath)
        {
            CreateDB(dbFullPath);
            CreateTable(TableMode.AllTask);
            CreateTable(TableMode.AllTaskRowCount);
            CreateTable(TableMode.UnfinishedTaskRowCount);
            CreateTable(TableMode.UnfinishedTaskRowID);
        }


        private void CreateDB(string dbFullPath)
        {
            // Prepare the directory (Recursive하게 만들수도 있긴 할듯)
            DirectoryInfo di = new DirectoryInfo(dbFullPath.Substring(0, dbFullPath.LastIndexOf('/')));
            if (!di.Exists)
            {
                di.Create();
            }
            this._db = new UserDB(dbFullPath);
        }


        private void CreateTable(TableMode mode)
        {
            string sql;
            ref UserDB db = ref this._db;

            sql = GetCreateTableSQL(mode);
            db.ExecuteNonQuery(sql);
            
            if (mode == TableMode.AllTaskRowCount || mode == TableMode.UnfinishedTaskRowCount)
            {
                string name = GetCorrectTableName(mode);
                sql = String.Format("SELECT count(*) FROM {0}", name);
                long currentCount = db.ExecuteScalar(sql);
                if(currentCount == 0)
                {
                    sql = String.Format("INSERT INTO {0} (count) VALUES (0);", name);
                    db.ExecuteNonQuery(sql);
                }
            }
        }


        private string GetCreateTableSQL(TableMode tableMode)
        {
            string sql = "";
            string name = GetCorrectTableName(tableMode);
            switch (tableMode)
            {
                case TableMode.AllTask:
                    sql = String.Format("CREATE TABLE IF NOT EXISTS {0} (date int, task varchar(100), isFinished int)", name);
                    break;
                case TableMode.UnfinishedTaskRowID:
                    sql = String.Format("CREATE TABLE IF NOT EXISTS {0} (id int)", name);
                    break;
                case TableMode.AllTaskRowCount:
                case TableMode.UnfinishedTaskRowCount:
                    sql = String.Format("CREATE TABLE IF NOT EXISTS {0} (count int);", name);
                    break;
                default:
                    break;
            }
            return sql;
        }


        public void ClearUnfinishedTaskTable()
        {
            ClearTable(TableMode.UnfinishedTaskRowID);
            InitializeTableCurrentRowCount(TableMode.UnfinishedTaskRowCount);
        }


        private void ClearTable(TableMode mode)
        {
            string name = GetCorrectTableName(mode);
            string sql = String.Format("DELETE FROM {0}", name);
            int result = this._db.ExecuteNonQuery(sql);
        }


        public ObservableCollection<WorkItem> GetUnfinishedTask()
        {
            ObservableCollection<WorkItem> items = new ObservableCollection<WorkItem>();

            // Bring the stored record from DB
            string sql = "SELECT * FROM unfinished_task_rowid";
            var rowidReader = this._db.ExecuteReader(sql);
            foreach (Dictionary<string, Object> rowidObject in rowidReader)
            {
                int currentId = Convert.ToInt32(rowidObject["id"]);
                sql = String.Format("SELECT rowid, * FROM all_task WHERE rowid={0}", currentId);
                var taskReader = this._db.ExecuteReader(sql);

                foreach (Dictionary<string, Object> taskOjbect in taskReader)
                {
                    items.Insert(0, new WorkItem(
                    Convert.ToInt64(taskOjbect["date"]),
                    Convert.ToString(taskOjbect["task"]),
                    Convert.ToBoolean(taskOjbect["isFinished"]),
                    Convert.ToInt32(taskOjbect["rowid"])
                    ));
                }
            }
            return items;
        }

        
        public void SaveUnfinishedTask(ObservableCollection<WorkItem> items)
        {
            string sql;
            List<string> sqlList = new List<string>();
            List<int> newTaskRowID = new List<int>();
            int newTaskCount = 0;

            int lastInputRowID = GetAllTaskCount();
            int lastAllTaskNumber = GetTableCurrentRowCount(TableMode.AllTaskRowCount);

            // Add current remained item information to the DB
            foreach (WorkItem item in items.Reverse<WorkItem>())
            {
                int currentID = item.rowId;
                if (currentID == 0)
                {
                    // 처음 들어온 item에 대해서만 sql 진행
                    // 1. Insert문 -> all_task 
                    sql = System.String.Format(
                        "INSERT INTO all_task (date, task, isFinished) VALUES ({0}, '{1}', {2})",
                        item.startDateTimeTick, item.task, item.isFinished
                    );
                    currentID = ++lastInputRowID;
                    sqlList.Add(sql);
                    newTaskCount++;
                }

                // 2. Insert문 -> unfinished_task_rowid
                sql = System.String.Format(
                    "INSERT INTO unfinished_task_rowid (id) VALUES ({0})", currentID
                );
                sqlList.Add(sql);   
            }

            // 3. Update문 -> all_task_row_count 
            string name = GetCorrectTableName(TableMode.AllTaskRowCount);
            sql = String.Format("UPDATE {0} SET count={1} WHERE rowid=1;", name, lastAllTaskNumber + newTaskCount);
            sqlList.Add(sql);


            // 4. Update문 -> unfinished_task_row_count
            name = GetCorrectTableName(TableMode.UnfinishedTaskRowCount);
            sql = String.Format("UPDATE {0} SET count={1} WHERE rowid=1;", name, items.Count);
            sqlList.Add(sql);

            // 5. Run the ExecuteTransaction
            this._db.ExecuteTransaction(sqlList);
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
            DecreaseTableCurrentRowCount(TableMode.AllTaskRowCount);
        }


        // Handling Table count
        public int GetAllTaskCount()
        {
            // Get the last input row id
            string sql = "SELECT IFNULL(MAX(rowid), 1) AS Id FROM all_task;";
            int count = Convert.ToInt32(this._db.ExecuteScalar(sql));
            return count;
        }


        public void UpdateFinishedTask(WorkItem item)
        {
            string sql;
            item.isFinished = true;
            // In the case of it is new one
            if (item.rowId == 0)
            {
                IncreaseTableCurrentRowCount(TableMode.AllTaskRowCount);

                sql = System.String.Format(
                    "INSERT INTO all_task (date, task, isFinished) VALUES ({0}, '{1}', {2});",
                    item.startDateTimeTick, item.task, item.isFinished
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


        private void IncreaseTableCurrentRowCount(TableMode mode)
        {
            int currentCount = GetTableCurrentRowCount(mode);
            string name = GetCorrectTableName(mode);
            string sql = String.Format("UPDATE {0} SET count={1} WHERE rowid=1;", name, currentCount + 1);
            this._db.ExecuteReader(sql);
        }


        private void DecreaseTableCurrentRowCount(TableMode mode)
        {
            int currentCount = GetTableCurrentRowCount(mode);
            string name = GetCorrectTableName(mode);
            if (currentCount == 0)
            {
                throw new Exception(String.Format("Currently, there are no row in the {0}", name));
            }
            string sql = String.Format("UPDATE {0} SET count={1} WHERE rowid = 1;", name, currentCount - 1);
            this._db.ExecuteReader(sql);
        }


        public int GetTableCurrentRowCount(TableMode mode)
        {
            if (mode == TableMode.AllTask || mode == TableMode.UnfinishedTaskRowID)
            {
                throw new Exception("Wrong table mode");
            }
            string name = GetCorrectTableName(mode);
            string sql = String.Format("SELECT * FROM {0} WHERE rowid=1;", name);
            var dataReader = this._db.ExecuteReader(sql);
            return Convert.ToInt32(dataReader[0]["count"]);
        }


        private void InitializeTableCurrentRowCount(TableMode mode)
        {
            string name = GetCorrectTableName(mode);
            string sql = String.Format("UPDATE {0} SET count={1} WHERE rowid = 1;", name, 0);
            this._db.ExecuteReader(sql);
        }
    }
}
