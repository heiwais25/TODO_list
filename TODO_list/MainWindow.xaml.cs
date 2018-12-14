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
using System.Data.SQLite;


namespace TODO_list
{
    public class WorkItem
    {
        public string date { get; set; }
        public string task { get; set; }
        public bool isFinished { get; set; }

        public WorkItem(string date, string task, bool isFinished)
        {
            this.date = date;
            this.task = task;
            this.isFinished = isFinished;
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
        private SQLiteConnection connDB = null;
        private string defaultDBpath = System.Environment.CurrentDirectory + "/db/record.sqlite";

        public MainWindow()
        {
            InitializeComponent();

            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            initializeListView();

            // Initialize the DB
            // Check already exist DB, otherwise create new DB
            System.IO.FileInfo fi = new System.IO.FileInfo(this.defaultDBpath);
            if (!fi.Exists)
            {
                SQLiteConnection.CreateFile(this.defaultDBpath);
            }
            this.connDB = new SQLiteConnection(String.Format("Data Source={0};Version=3;", defaultDBpath));
            this.connDB.Open();

            // Check the TABLE exist already Create the table
            string sql = "SELECT COUNT(*) cnt FROM sqlite_master WHERE name='member'";
            SQLiteCommand command = new SQLiteCommand(sql, this.connDB);
            SQLiteDataReader rdr = command.ExecuteReader();

            try
            {
                sql = "SELECT * FROM member";
                command = new SQLiteCommand(sql, this.connDB);
                rdr = command.ExecuteReader();
            }
            catch
            {
                //string sql = "create table member (id int PRIMARY KEY, date int, task varchar(100), isFinished int)";
                sql = "create table member (date int, task varchar(100), isFinished int)";
                command = new SQLiteCommand(sql, this.connDB);
                int result = command.ExecuteNonQuery();
            }
            rdr.Close();

            sql = "SELECT * FROM member";
            command = new SQLiteCommand(sql, this.connDB);
            rdr = command.ExecuteReader();
            while (rdr.Read())
            {
                _items.Insert(0, new WorkItem(rdr["date"].ToString(), rdr["task"].ToString(), Convert.ToBoolean(rdr["isFinished"])));
                this.TotalTaskNumberLabel.Content = _items.Count;
            }
            rdr.Close();
            
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        public void initializeListView()
        {
            // Clear the data
            this.listView.Items.Clear();
            _items.Clear();
            this.listView.ItemsSource = _items;

            // Bring the stored record from DB
            /*
             * 
             *
             */ 

            // Set the today label
            this.todayDate.Content = DateTime.Now.ToString("MM-dd");

            // Set the top-side number label
            this.TotalTaskNumberLabel.Content = _items.Count;
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
            var nowDate = DateTime.Now.ToString("MM-dd");
            _items.Insert(0, new WorkItem(nowDate, newTaskBox.Text, false));
            this.TotalTaskNumberLabel.Content = _items.Count;

            // Clean the text box
            this.newTaskBox.Clear();
        }

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

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            WorkItem item = null;
            int index = -1;

            if (this.listView.SelectedItems.Count != 1)
            {
                return;
            }
            item = (WorkItem)this.listView.SelectedItems[0];
            index = _items.IndexOf(item);
            if (index > 0)
            {
                _items.Move(index, index - 1);
            }
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            WorkItem item = null;
            int index = -1;

            if (this.listView.SelectedItems.Count != 1) return;
            item = (WorkItem)this.listView.SelectedItems[0];
            index = _items.IndexOf(item);
            if (index < _items.Count - 1)
            {
                _items.Move(index, index + 1);
            }
        }

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

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Add current remained item information to the DB
            foreach(WorkItem item in this._items)
            {
                int toDate = int.Parse("1215");
                string sql = System.String.Format("INSERT INTO member (date, task, isFinished) VALUES ({0}, '{1}', {2})", toDate, item.task, item.isFinished);
                SQLiteCommand cmd = new SQLiteCommand(sql, this.connDB);
                cmd.ExecuteNonQuery();

            }
            this.connDB.Close();

            // Shut down the item
            Application.Current.Shutdown();
        }

        private void Image_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }


        private void ExitButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ExitButtonBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD6, 0xD6, 0xD6));
        }

        private void ExitButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ExitButtonBorder.Background = Brushes.Transparent;
        }

        private void MinimizeButton_MouseEnter(object sender, MouseEventArgs e)
        {
            MinimizeButtonBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD6, 0xD6, 0xD6));
        }

        private void MinimizeButton_MouseLeave(object sender, MouseEventArgs e)
        {
            MinimizeButtonBorder.Background = Brushes.Transparent;
        }

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

        private void completeButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the listViewItem which contains current button
            ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            WorkItem item = (WorkItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

            // Remove the item from the container
            _items.Remove(item);

            /* Add the item information to DB that it is finished
             * 
             */
            int toDate = int.Parse("1215");
            string sql = System.String.Format("INSERT INTO member (date, task, isFinished) VALUES ({0}, '{1}', {2})" , toDate, item.task, true);
            SQLiteCommand cmd = new SQLiteCommand(sql, this.connDB);
            cmd.ExecuteNonQuery();
            this.TotalTaskNumberLabel.Content = _items.Count;
        }
    }
}
