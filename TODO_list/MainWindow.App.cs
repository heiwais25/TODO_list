//Copyright 2011
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;


namespace TODO_list
{
    public partial class MainWindow
    {
        private Point startPoint = new Point();
        private ObservableCollection<WorkItem> _items = new ObservableCollection<WorkItem>();
        private int startIndex = -1;
        private string defaultDBDir = System.Environment.CurrentDirectory + "/db/";
        private string defaultDBName = "record.sqlite";
        private ApplicationDB _appDB = null;


        public void InitializeListView()
        {
            this.listView.Items.Clear();
            _items = this._appDB.GetUnfinishedTask();
            this.listView.ItemsSource = _items;

            this.todayDate.Content = DateTime.Now.ToString("MM-dd");

            InitializeStatusLabel();
        }


        private void InitializeCommonKeysHandler()
        {
            this.KeyDown += new KeyEventHandler(HandleCommonKeys);
        }

        private void InitializeAppDB()
        {
            this._appDB = new ApplicationDB(this.defaultDBDir + this.defaultDBName);
        }

        private void AddNewTask()
        {
            if (IsNewTaskBoxEmpty())
            {
                MessageBox.Show("You need to input what you will do", "Caution");
                return;
            }
            InsertNewTaskToTaskList();
            ClearNewTaskBox();
            UpdateStatusLabel(ListActionMode.Add);
            MoveScrollToTheTop();
        }


        private void RemoveSelectedItem(object sender, MouseButtonEventArgs e, ListActionMode mode)
        {
            ListView listView = FindAncestor<ListView>((DependencyObject)sender);
            ListViewItem listViewItem = GetSelectedListViewItem(e);
            WorkItem item = GetItemFromListViewItem(listView, listViewItem);
            RemoveItemFromItemList(item, mode);
        }


        private void RemoveItemFromItemList(WorkItem item, ListActionMode mode)
        {
            _items.Remove(item);
            if (mode.Equals(ListActionMode.Remove))
            {
                this._appDB.RemoveUnfinishedTask(item);
                UpdateStatusLabel(ListActionMode.Remove);
            }
            else if (mode.Equals(ListActionMode.Complete))
            {
                this._appDB.UpdateFinishedTask(item);
                UpdateStatusLabel(ListActionMode.Complete);
            }
            else
            {
                throw new Exception("Wrong mode is chosen");
            }
        }


        private void MoveScrollToTheTop()
        {
            this.listView.ScrollIntoView(this.listView.Items[0]);
        }

        private void ClearNewTaskBox()
        {
            this.newTaskBox.Clear();
        }

        private void InsertNewTaskToTaskList()
        {
            this._items.Insert(0, new WorkItem(GetTodayDate(), newTaskBox.Text, false));
        }

        

        ListViewItem GetSelectedListViewItem(DragEventArgs e)
        {
            return FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        }


        ListViewItem GetSelectedListViewItem(MouseEventArgs e)
        {
            return FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        }


        ListViewItem GetSelectedListViewItem(RoutedEventArgs e)
        {
            return FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
        }


        WorkItem GetItemFromListViewItem(ListView listView, ListViewItem listViewItem)
        {
            return (WorkItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
        }


        void ChangeDragAndDropItemPosition(WorkItem item)
        {
            int index = index = this._items.IndexOf(item);
            if (startIndex >= 0 && index >= 0)
            {
                _items.Move(startIndex, index);
            }
            startIndex = -1;
        }


        private void SaveCurrentMousePosition(MouseButtonEventArgs e)
        {
            this.startPoint = e.GetPosition(null);
        }


        // Methods for the drag and drop event =====================================================================
        private bool IsMouseDraggingEvent(MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            return e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance);
        }


        private void StartDragDropOperation(ListView listView, ListViewItem listViewItem)
        {
            startIndex = this.listView.SelectedIndex;
            var item = GetItemFromListViewItem(listView, listViewItem);
            DataObject dragData = new DataObject("WorkItem", item);
            DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Copy | DragDropEffects.Move);
        }


        private bool IsCorrectDragEvent(object sender, DragEventArgs e)
        {
            return e.Data.GetDataPresent("WorkItem") && sender.Equals(e.Source);
        }


        private WorkItem GetSelectedItem(object sender, RoutedEventArgs e)
        {
            var listViewItem = GetSelectedListViewItem(e);
            var listView = sender as ListView;
            var item = GetItemFromListViewItem(listView, listViewItem);
            return item;
        }


        private void ShutDownProcess()
        {
            this._appDB.ClearUnfinishedTaskTable();
            this._appDB.SaveUnfinishedTask(this._items);
            Application.Current.Shutdown();
        }


        public void BackToStartUpScreen()
        {
            this.newTaskBox.Clear();
            this.AddNewTaskTextArea.Visibility = Visibility.Hidden;
            this.AddNewTaskButtonBorder.Visibility = Visibility.Visible;
        }


        private void ShowNewTaskTextArea()
        {
            this.AddNewTaskButtonBorder.Visibility = Visibility.Hidden;
            this.AddNewTaskTextArea.Visibility = Visibility.Visible;
            this.newTaskBox.Focus();
        }


        private void UpdateStatusLabel(ListActionMode mode)
        {
            int todoTaskNumber = this._items.Count;
            int doneTaskNumber = Convert.ToInt32(this.DoneTaskNumberLabel.Content);
            this.TodoTaskNumberLabel.Content = todoTaskNumber;
            if (mode == ListActionMode.Add || mode == ListActionMode.Remove)
            {
                this.TotalTaskNumberLabel.Content = todoTaskNumber + doneTaskNumber;
            }
            else if (mode == ListActionMode.Complete)
            {
                this.DoneTaskNumberLabel.Content = doneTaskNumber + 1;
            }
        }


        private void InitializeStatusLabel()
        {
            int todoTaskNumber = this._appDB.GetTableCurrentRowCount(ApplicationDB.TableMode.UnfinishedTaskRowCount);
            int totalTaskCount = this._appDB.GetTableCurrentRowCount(ApplicationDB.TableMode.AllTaskRowCount);

            this.TotalTaskNumberLabel.Content = totalTaskCount;
            this.TodoTaskNumberLabel.Content = todoTaskNumber;
            this.DoneTaskNumberLabel.Content = totalTaskCount - todoTaskNumber;
        }


        private bool IsNewTaskBoxEmpty()
        {
            string currentText = this.newTaskBox.Text;
            currentText = currentText.Replace(" ", "");
            currentText = currentText.Replace("\r\n", "");

            return String.IsNullOrEmpty(currentText);
        }


        private int GetTodayDate()
        {
            int todayDate = Convert.ToInt32(DateTime.Now.ToString("yyMMdd"));
            return todayDate;
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
    }
}
