using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TODO_list
{
    public class WorkItem
    {
        public int rowId { get; set; }
        public int fullDate { get; set; }
        public string date { get; set; }
        public string task { get; set; }
        public bool isFinished { get; set; }

        // fullDate : yyMMdd
        public WorkItem(int fullDate, string task, bool isFinished, int rowId = 0)
        {
            this.fullDate = fullDate;
            // Month
            string month = Convert.ToString((int)((fullDate % 10000) / 100));
            string date = Convert.ToString((int)((fullDate % 100)));
            this.date = month + "-" + date;
            this.task = task;
            this.isFinished = isFinished;

            // To distinguish each task
            this.rowId = rowId;
        }
    }


}
