//Copyright 2011
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using DataBase;
using System.IO;

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

    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point startPoint = new Point();
        private ObservableCollection<WorkItem> _items = new ObservableCollection<WorkItem>();
        private int startIndex = -1;
        private UserDB _db = null;
        private string defaultDBDir = System.Environment.CurrentDirectory + "/db/";
        private string defaultDBName = "record.sqlite";

        

        public MainWindow()
        {
            InitializeComponent();

            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            InitializeDB();
            InitializeListView();
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void AddButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.AddItem();
        }

        private void AddItem()
        {
            if (String.IsNullOrEmpty(newTaskBox.Text))
            {
                MessageBox.Show("You need to input what you will do", "Caution");
                return;
            }
            int nowDate = Convert.ToInt32(DateTime.Now.ToString("yyMMdd"));
            _items.Insert(0, new WorkItem(nowDate, newTaskBox.Text, false));
            this.TotalTaskNumberLabel.Content = _items.Count;

            // Clean the text box
            this.newTaskBox.Clear();
        }


        // =====================================================================================================
        // ListView
        // =====================================================================================================
        private void ListView_Drop(object sender, DragEventArgs e)
        {
            int index = -1;

            if(e.Data.GetDataPresent("WorkItem") && sender == e.Source)
            {
                //Get the drop listViewItem destination
                ListView listView = sender as ListView;
                ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
                if(listViewItem == null)
                {
                    // abort
                    e.Effects = DragDropEffects.None;
                    return;
                }

                // Find the data behind the listViewItem
                WorkItem item = (WorkItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

                // Move item into observable collection
                // (this will be automatically reflected to listView.ItemSource
                e.Effects = DragDropEffects.Move;
                index = _items.IndexOf(item);
                if (startIndex >= 0 && index >= 0)
                {
                    _items.Move(startIndex, index);
                }
                startIndex = -1;
            }
        }

        private void ListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get current mouse position
            this.startPoint = e.GetPosition(null);
        }

        private static T FindAncestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private void ListView_MouseMove(object sender, MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                // Get the dragged ListViewItem
                ListView listView = sender as ListView;
                ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
                if (listViewItem == null) return;

                // Find the data behind the ListViewItem
                WorkItem item = (WorkItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
                if (item == null) return;

                // Initialize the drag & drop operation
                startIndex = this.listView.SelectedIndex;
                DataObject dragData = new DataObject("WorkItem", item);
                DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Copy | DragDropEffects.Move);
            }
        }

        private void ListView_DragEnter(object sender, DragEventArgs e)
        {
            if(!e.Data.GetDataPresent("WorkItem") || sender != e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        // =====================================================================================================
        // removeButton
        // =====================================================================================================
        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the listViewItem which contains current button
            ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            WorkItem item = (WorkItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

            // Remove the item from the container
            _items.Remove(item);

            // Decrease total number of tasks
            this.TotalTaskNumberLabel.Content = _items.Count;
        }

        // =====================================================================================================
        // completeButton
        // =====================================================================================================
        private void completeButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the listViewItem which contains current button
            ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            WorkItem item = (WorkItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

            // Remove the item from the container
            _items.Remove(item);

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

            this.TotalTaskNumberLabel.Content = _items.Count;
        }


        // Move Window
        private Point windowStartPoint;
        private void System_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (this.WindowState == WindowState.Maximized && Math.Abs(windowStartPoint.Y - e.GetPosition(null).Y) > 2)
                {
                    var point = PointToScreen(e.GetPosition(null));

                    this.WindowState = WindowState.Normal;

                    this.Left = point.X - this.ActualWidth / 2;
                    //this.Top = point.Y - border.ActualHeight / 2;
                }
                DragMove();
            }
        }

        // =====================================================================================================
        // MinimizeButton
        // =====================================================================================================
        private void MinimizeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MinimizeButton_MouseEnter(object sender, MouseEventArgs e)
        {
            MinimizeButtonBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD6, 0xD6, 0xD6));
        }

        private void MinimizeButton_MouseLeave(object sender, MouseEventArgs e)
        {
            MinimizeButtonBorder.Background = Brushes.Transparent;
        }

        // =====================================================================================================
        // ExitButton
        // =====================================================================================================
        private void ExitButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Clear the unfinished_rowid table
            string sql = "DELETE FROM unfinished_rowid";
            int result = this._db.ExecuteNonQuery(sql);

            // Add current remained item information to the DB
            foreach (WorkItem item in this._items.Reverse<WorkItem>())
            {
                int currentId = item.rowId;
                if (currentId == 0)
                {
                    sql = System.String.Format(
                    "INSERT INTO all_task (date, task, isFinished) VALUES ({0}, '{1}', {2})",
                    item.fullDate, item.task, item.isFinished
                    );
                    this._db.ExecuteNonQuery(sql);

                    // Get the last input row id
                    sql = "SELECT IFNULL(MAX(rowid), 1) AS Id FROM all_task";
                    currentId = Convert.ToInt32(this._db.ExecuteScalar(sql));
                }
                
                // 현재 순서에 맞춰서 저장
                sql = System.String.Format(
                    "INSERT INTO unfinished_rowid (id) VALUES ({0})", currentId
                );
                this._db.ExecuteNonQuery(sql);
            }
            this._db.Close();

            // Shut down the item
            Application.Current.Shutdown();
        }

        private void ExitButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ExitButtonBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD6, 0xD6, 0xD6));
        }

        private void ExitButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ExitButtonBorder.Background = Brushes.Transparent;
        }

        // =====================================================================================================
        // NewTaskBox
        // =====================================================================================================
        private void NewTaskBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    this.AddItem();
                }
            }
        }

        

        // ==================================================================================
        // Initialization
        // ==================================================================================
        private void InitializeDB()
        {
            UserDBReader rdr = null;
            string defaultDBFullPath = this.defaultDBDir + this.defaultDBName;
            string sql;
            ref UserDB db = ref this._db;

            // Prepare the directory
            DirectoryInfo di = new DirectoryInfo(this.defaultDBDir);
            if (!di.Exists)
            {
                di.Create();
            }

            // Check already exist DB, otherwise create new DB
            db = new UserDB(defaultDBFullPath);
            FileInfo fi = new System.IO.FileInfo(defaultDBFullPath);
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

        public void InitializeListView()
        {
            // Clear the data
            this.listView.Items.Clear();
            _items.Clear();
            this.listView.ItemsSource = _items;

            // Bring the stored record from DB
            // Need to be changed
            //string sql = "SELECT rowid, * FROM all_task WHERE isFinished=0";
            string sql = "SELECT * FROM unfinished_rowid";
            UserDBReader rowIdRdr = this._db.ExecuteReader(sql);
            while (rowIdRdr.Read())
            {
                int currentId = Convert.ToInt32(rowIdRdr.Get("id"));
                sql = String.Format("SELECT rowid, * FROM all_task WHERE rowid={0}", currentId);
                UserDBReader rdr = this._db.ExecuteReader(sql);
                while (rdr.Read())
                {
                    _items.Insert(0, new WorkItem(
                    Convert.ToInt32(rdr.Get("date")),
                    Convert.ToString(rdr.Get("task")),
                    Convert.ToBoolean(rdr.Get("isFinished")),
                    Convert.ToInt32(rdr.Get("rowid"))
                ));
                }
                rdr.Close();    
            }
            this.TotalTaskNumberLabel.Content = _items.Count;
            rowIdRdr.Close();

            

            // Set the today label
            this.todayDate.Content = DateTime.Now.ToString("MM-dd");

            // Set the top-side number label
            this.TotalTaskNumberLabel.Content = _items.Count;
        }

        
    }
}
