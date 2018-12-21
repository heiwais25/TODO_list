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
        public int startDateFull { get; set; }

        public long startDateTimeTick { get; set; }
        public string startDateTime { get; set; }
        public long finishDateTimeTick { get; set; }
        public string finishDateTime { get; set; }

        public int finishDateFull { get; set; }
        public string startDate { get; set; }
        public string finishDate { get; set; }
        public string task { get; set; }
        public bool isFinished { get; set; }

        // fullDate : yyMMdd
        public WorkItem(long startDateTimeTick, string task, bool isFinished, int rowId = 0)
        {
            this.startDateTimeTick = startDateTimeTick;
            DateTime dt = new DateTime(startDateTimeTick);
            this.startDateTime = dt.ToString("MM-dd");

            this.finishDateTimeTick = 0;
            this.finishDateTime = "";

            //string month = Convert.ToString((int)((startDateFull % 10000) / 100));
            //string date = Convert.ToString((int)((startDateFull % 100)));
            //this.startDate = month + "-" + date;
            this.task = task;
            this.isFinished = isFinished;

            // To distinguish each task
            this.rowId = rowId;
        }
    }


}
