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
        enum ListActionMode {
            Add,
            Remove,
            Complete
        }


        private Point startPoint = new Point();
        private ObservableCollection<WorkItem> _items = new ObservableCollection<WorkItem>();
        private int startIndex = -1;
        private string defaultDBDir = System.Environment.CurrentDirectory + "/db/";
        private string defaultDBName = "record.sqlite";
        private ApplicationDB _appDB = null;

        public MainWindow()
        {
            InitializeComponent();

            this.KeyDown += new KeyEventHandler(HandleEsc);
            this._appDB = new ApplicationDB(this.defaultDBDir + this.defaultDBName);

            InitializeListView();
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                if(this.AddNewTaskTextArea.Visibility is Visibility.Visible)
                {
                    BackToStartUpScreen();
                }
                else
                {
                    ShutDownProcess();
                }
            }
            if (this.AddNewTaskTextArea.Visibility is Visibility.Hidden)
            {
                if (e.Key == Key.Enter)
                {
                    ShowNewTaskTextArea();
                }
            }


            //if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) && e.OriginalSource is ListViewItem)
            //{
            //    if(e.Key == Key.F4)
            //    {
            //        Console.Write("1");
            //    }
            //    e.Handled = true;
            //}
        }

        private void AddButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.AddItem();
        }

        
        private void AddItem()
        {
            if (IsNewTaskBoxEmpty())
            {
                MessageBox.Show("You need to input what you will do", "Caution");
                return;
            }

            _items.Insert(0, new WorkItem(GetTodayDate(), newTaskBox.Text, false));
            this.newTaskBox.Clear();
            UpdateStatusLabel(ListActionMode.Add);
            this.listView.ScrollIntoView(this.listView.Items[0]);
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
            ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            WorkItem item = (WorkItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

            _items.Remove(item);
            this._appDB.RemoveUnfinishedTask(item);
            UpdateStatusLabel(ListActionMode.Remove);
        }

        // =====================================================================================================
        // completeButton
        // =====================================================================================================
        private void completeButton_Click(object sender, RoutedEventArgs e)
        {
            ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            WorkItem item = (WorkItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

            _items.Remove(item);
            this._appDB.UpdateFinishedTask(item);
            UpdateStatusLabel(ListActionMode.Complete);
        }


        // Move Window
        private void System_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
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
            this._appDB.ClearUnfinishedTaskTable();
            this._appDB.SaveUnfinishedTask(this._items);
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

        public void BackToStartUpScreen()
        {
            this.newTaskBox.Clear();
            this.AddNewTaskTextArea.Visibility = Visibility.Hidden;
            this.AddNewTaskButtonBorder.Visibility = Visibility.Visible;
        }


        public void InitializeListView()
        {
            this.listView.Items.Clear();
            _items = this._appDB.GetUnfinishedTask();
            this.listView.ItemsSource = _items;
            
            this.todayDate.Content = DateTime.Now.ToString("MM-dd");

            InitializeStatusLabel();
        }


        private void UpdateStatusLabel(ListActionMode mode)
        {
            int todoTaskNumber = this._items.Count;
            int doneTaskNumber = Convert.ToInt32(this.DoneTaskNumberLabel.Content);
            this.TodoTaskNumberLabel.Content = todoTaskNumber;
            if(mode == ListActionMode.Add || mode == ListActionMode.Remove)
            {
                this.TotalTaskNumberLabel.Content = todoTaskNumber + doneTaskNumber;
            }
            else if(mode == ListActionMode.Complete)
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

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    this.AddNewTaskButton.Visibility = Visibility.Hidden;
        //    this.AddNewTaskTextArea.Visibility = Visibility.Visible;

        //}

        private void AddNewTaskButton_MouseEnter(object sender, MouseEventArgs e)
        {
            BrushConverter bc = new BrushConverter();
        }

        private void AddNewTaskButton_MouseLeave(object sender, MouseEventArgs e)
        {
            BrushConverter bc = new BrushConverter();
        }


        private void ShowNewTaskTextArea()
        {
            this.AddNewTaskButtonBorder.Visibility = Visibility.Hidden;
            this.AddNewTaskTextArea.Visibility = Visibility.Visible;
            this.newTaskBox.Focus();
        }


        private void AddNewTaskButtonBorder_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowNewTaskTextArea();
        }

        private void AddNewTaskButtonBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            this.AddNewTaskButtonBorder.Background = Brushes.White;
            BrushConverter bc = new BrushConverter();
            this.AddNewTaskButtonBorder.Background = (Brush)bc.ConvertFrom("#FF8181A5");
        }

        private void AddNewTaskButtonBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            BrushConverter bc = new BrushConverter();
            this.AddNewTaskButtonBorder.Background = (Brush)bc.ConvertFrom("#FF393957");
        }

        private void AddNewTaskTextArea_MouseEnter(object sender, MouseEventArgs e)
        {
            BrushConverter bc = new BrushConverter();
        }


        private void NewTaskBoxArea_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.newTaskBox.Focus();
        }


        private void CancelButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.newTaskBox.Clear();
            this.AddNewTaskTextArea.Visibility = Visibility.Hidden;
            this.AddNewTaskButtonBorder.Visibility = Visibility.Visible;
        }


        private void RemoveIconArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            WorkItem item = (WorkItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

            _items.Remove(item);
            this._appDB.RemoveUnfinishedTask(item);
            UpdateStatusLabel(ListActionMode.Remove);
        }

        private void CompleteIconArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ListViewItem listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
            WorkItem item = (WorkItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);

            _items.Remove(item);
            this._appDB.UpdateFinishedTask(item);
            UpdateStatusLabel(ListActionMode.Complete);
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ShutDownProcess();
        }
    }
}
