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
    public partial class MainWindow : Window
    {
        private Point startPoint = new Point();
        private ObservableCollection<WorkItem> _items = new ObservableCollection<WorkItem>();
        private int startIndex = -1;
        private int _totalTaskCount = 0;
        private string defaultDBDir = System.Environment.CurrentDirectory + "/db/";
        private string defaultDBName = "record.sqlite";
        private ApplicationDB _appDB = null;

        public MainWindow()
        {
            InitializeComponent();

            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            this._appDB = new ApplicationDB();
            this._appDB.InitializeDB(this.defaultDBDir + this.defaultDBName);

            InitializeListView();
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                ShutDownProcess();
        }

        private void AddButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.AddItem();
        }

        private void AddItem()
        {
            // 비어있는지 확인
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

            this._appDB.UpdateFinishedTask(item);
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
            ShutDownProcess();
        }

        private void ShutDownProcess()
        {
            // ShutdownWithSaveRecords
            this._appDB.ClearUnfinishedTaskTable();
            this._appDB.SaveUnfinishedTask(this._items);
            this._appDB.CloseDB();

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

        public void InitializeListView()
        {
            // Clear the data
            this.listView.Items.Clear();

            _items = this._appDB.GetUnfinishedTask();
            this.listView.ItemsSource = _items;
            

            // Set the today label
            this.todayDate.Content = DateTime.Now.ToString("MM-dd");


            // Set the top-side number label
            InitializeStatusLabel();
        }

        private void InitializeStatusLabel()
        {
            int todoTaskNumber = _items.Count;
            this._totalTaskCount = this._appDB.GetAllTaskCount();
            this.TotalTaskNumberLabel.Content = this._totalTaskCount;
            this.DoneTaskNumberLabel.Content = this._totalTaskCount - todoTaskNumber;
            this.TodoTaskNumberLabel.Content = todoTaskNumber;
        }

    }
}
