//Copyright 2011
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace TODO_list
{
    public partial class MainWindow : Window
    {
        enum ListActionMode {
            Add,
            Remove,
            Complete
        }


        public MainWindow()
        {
            InitializeComponent();
            InitializeCommonKeysHandler();
            InitializeAppDB();
            InitializeListView();
        }


        private void AddNewTaskButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AddNewTask();
        }


        private void HandleCommonKeys(object sender, KeyEventArgs e)
        {
            if (this.AddNewTaskTextArea.IsVisible)
            {
                if (e.Key.Equals(Key.Escape))
                {
                    BackToStartUpScreen();
                }
            }
            else if (!this.AddNewTaskTextArea.IsVisible)
            {
                if (e.Key.Equals(Key.Escape))
                {
                    ShutDownProcess();
                }
                else if (e.Key.Equals(Key.Enter))
                {
                    ShowNewTaskTextArea();
                }
            }
        }


        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent("WorkItem") && sender.Equals(e.Source))
            {
                var listViewItem = GetSelectedListViewItem(e);
                if(listViewItem != null)
                {
                    var listView = sender as ListView;
                    var item = GetItemFromListViewItem(listView, listViewItem);
                    ChangeDragAndDropItemPosition(item);
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            return;
        }


        private void ListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SaveCurrentMousePosition(e);
        }


        private void ListView_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseDraggingEvent(e))
            {
                var listViewItem = GetSelectedListViewItem(e);
                if(listViewItem != null)
                {
                    var listView = sender as ListView;
                    var item = GetItemFromListViewItem(listView, listViewItem);
                    if(item != null)
                    {
                        StartDragDropOperation(listView, listViewItem);
                    }
                }
            }
        }


        private void ListView_DragEnter(object sender, DragEventArgs e)
        {
            if(!IsCorrectDragEvent(sender, e))
            {
                e.Effects = DragDropEffects.None;
            }
        }


        private void System_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton.Equals(MouseButtonState.Pressed))
            {
                DragMove();
            }
        }


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


        private void ExitButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShutDownProcess();
        }


        private void ExitButton_MouseEnter(object sender, MouseEventArgs e)
        {
            ExitButtonBorder.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xD6, 0xD6, 0xD6));
        }


        private void ExitButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ExitButtonBorder.Background = Brushes.Transparent;
        }


        private void NewTaskBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    this.AddNewTask();
                }
            }
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


        private void NewTaskBoxArea_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.newTaskBox.Focus();
        }


        private void CancelButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BackToStartUpScreen();
        }


        private void RemoveIconArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RemoveSelectedItem(sender, e, ListActionMode.Remove);
        }


        private void CompleteIconArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RemoveSelectedItem(sender, e, ListActionMode.Complete);
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ShutDownProcess();
        }
    }
}
