using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Xceed.Wpf.Toolkit;

namespace Jarloo.Calendar
{
    using IEvent = MPLite.Event.IEvent;
    using RecurringFrequencies = MPLite.Event.RecurringFrequencies;
    using CustomEvent = MPLite.Event.CustomEvent;
    using MultiTriggerEvent = MPLite.Event.MultiTriggerEvent;
    using SchedulerEventArgs = MPLite.Event.SchedulerEventArgs;
    using Playlist = MPLite.Core.Playlist;
    using PlaylistCollection = MPLite.Core.PlaylistCollection;
    using PlaybackCommands = MPLite.Event.PlaybackCommands;
    using PlaybackMode = MPLite.Core.PlaybackMode;

    public partial class WindowEventSetting : Window
    {
        private SolidColorBrush focusedGridBackground = new SolidColorBrush(Color.FromArgb(0xFF, 0x82, 0x65, 0x3F));
        private SolidColorBrush focusedPlayerSettingGridBackground = new SolidColorBrush(Color.FromArgb(0xFF, 0x2A, 0x4F, 0x75));

        private PlaylistCollection plc;

        private Regex reEscChars = new Regex("[\\\\/:*?\"<>|]");
        private Regex reDigitOnly = new Regex("^[0-9]+$");

        #region Events
        public delegate void NewEventIsCreatedEventHandler(CustomEvent evnt);
        public event NewEventIsCreatedEventHandler NewEventIsCreatedEvent;
        #endregion

        public DateTime InitialBeginningTime { get; set; }

        public WindowEventSetting()
        {
            InitializeComponent();
            InitializeControls();
        }

        public WindowEventSetting(DateTime selectedDateTime)
        {
            InitializeComponent();
            InitialBeginningTime = selectedDateTime;
            InitializeControls();
        }

        private void InitializeControls()
        {
            // txtEventName


            // txtRnak
            txtRank.Text = "1";

            // dateTimePicker
            dateTimePicker.Value = (InitialBeginningTime == DateTime.MinValue) ? DateTime.Now : InitialBeginningTime;

            // timeSpanUpDown
            timeSpanUpDown.Value = TimeSpan.FromMinutes(1);

            // chkSetDuration
            chkSetDuration.Checked += (sender, args) =>
            {
                timeSpanUpDown.IsEnabled = true;
            };
            chkSetDuration.Unchecked += (sender, args) =>
            {
                timeSpanUpDown.IsEnabled = false;
            };
            chkSetDuration.IsChecked = false;

            // gridRecurringFreq
            gridRecurringFreq.Height = 50;
            dpRecurringDate.Visibility = Visibility.Hidden;

            // cmbPlaylist & cmbTrackIndex
            plc = PlaylistCollection.GetDatabase();
            if (plc != null)
            {
                List<string> plNames = new List<string>(plc.TrackLists.Count);
                foreach (Playlist pl in plc.TrackLists)
                {
                    plNames.Add(pl.ListName);
                }
                cmbPlaylistName.ItemsSource = plNames;
            }

            // cmbPlaybackMode
            List<PlaybackMode> playbackModes = new List<MPLite.Core.PlaybackMode>();
            foreach (PlaybackMode mode in Enum.GetValues(typeof(PlaybackMode)))
            {
                if (mode == PlaybackMode.None) continue;
                playbackModes.Add(mode);
            }
            cmbPlaybackMode.ItemsSource = playbackModes;
            cmbPlaybackMode.SelectedIndex = 0;
        }

        #region Window control
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            NewEventIsCreatedEvent = null;
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        #endregion

        private void CreateEvent()
        {
            CustomEvent ce = null;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParseEventSetting();
                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void ParseEventSetting()
        {
            string eventName;
            int rank;
            DateTime beginningTime = DateTime.MinValue;
            Queue<DateTime> beginningTimeQueue = new Queue<DateTime>();
            TimeSpan duration = TimeSpan.Zero;
            bool autoDelete = (chkAutoDelete.IsChecked == true) ? true : false;
            bool thisDayForwardOnly = (chkThisDayForwardOnly.IsChecked == true) ? true : false;

            // EventName
            if (reEscChars.IsMatch(txtEventName.Text))
                throw new Exception("Event name shoud not contain the following charaters: \\/:*?\"<>|");
            eventName = txtEventName.Text.TrimEnd(' ');

            // Rank
            if (!reDigitOnly.IsMatch(txtRank.Text))
                throw new Exception("Given value of \"Rank\" is invalid, it should contains digits only.");
            rank = int.Parse(txtRank.Text);

            // Beginning time
            if (spTimePickerList.Children.Count == 0)
            {
                if (dateTimePicker.Value == null)
                    throw new Exception("Invalid value of DateTimePicker");
                else beginningTime = dateTimePicker.Value.Value;
            }
            else
            {
                List<DateTime> dtList = new List<DateTime>();

                if (dateTimePicker.Value == null)
                    throw new Exception("Invalid value of DateTimePicker");
                else dtList.Add(dateTimePicker.Value.Value);
                
                foreach (Grid gd in spTimePickerList.Children)
                {
                    TimePicker tp = gd.FindChild<TimePicker>();
                    if (tp == null)
                        continue;

                    // Get time component only (although formate of timePicker has been set as `DateTimeFormat.LongTime`,
                    // we still have to prevent illegal input)
                    DateTime dt = dateTimePicker.Value.Value.Date;  // get date only
                    dtList.Add(dt.Add(tp.Value.Value.TimeOfDay));
                }

                dtList.Sort();
                foreach (DateTime dt in dtList)
                {
                    beginningTimeQueue.Enqueue(dt);
                }
            }

            // Duration
            if (chkSetDuration.IsChecked == true)
                duration = timeSpanUpDown.Value.Value;

            // Set recurring frequency
            int recurringFreq = 0;
            if (cmbRecurringFreq.SelectedItem.ToString() == RecurringFrequencies.Custom.ToString())
            {
                // Do sth
                int temp = 1;
                foreach (CheckBox chkbox in gridRecurringDate.Children)
                {
                    recurringFreq += (chkbox.IsChecked == true) ? temp : 0;
                    temp *= 2;
                }
            }
            else
            {
                recurringFreq = (int)cmbRecurringFreq.SelectedItem;
            }

            Console.WriteLine(((RecurringFrequencies)recurringFreq).ToString());

            //CustomEvent evnt = new CustomEvent
            //{
            //    EventText = eventName,
            //    Rank = rank,
            //    BeginningTime = beginningTime,
            //    Duration = duration,
            //    AutoDelete = autoDelete,
            //    ThisDayForwardOnly = thisDayForwardOnly,
            //    Enabled = true,  // let user set this while creating event?
            //    RecurringFrequency = (RecurringFrequencies)recurringFreq,
            //    IgnoreTimeComponent = true,
            //    ReadOnlyEvent = false
            //};

            MultiTriggerEvent evnt = new MultiTriggerEvent(beginningTimeQueue)
            {
                EventText = eventName,
                Rank = rank,
                Duration = duration,
                AutoDelete = autoDelete,
                ThisDayForwardOnly = thisDayForwardOnly,
                Enabled = true,  // let user set this while creating event?
                RecurringFrequency = (RecurringFrequencies)recurringFreq,
                IgnoreTimeComponent = true,
                ReadOnlyEvent = false
            };

            // TODO: create event args
            evnt.ActionStartsEventArgs = new SchedulerEventArgs
            {
                Playlist = cmbPlaylistName.SelectedItem.ToString(),
                Command = PlaybackCommands.Play,
                Mode = (PlaybackMode)cmbPlaybackMode.SelectedItem,
                TrackIndex = cmbTrackIndex.SelectedIndex
            };

            evnt.ActionEndsEventArgs = new SchedulerEventArgs
            {
                Command = PlaybackCommands.Stop
            };

            NewEventIsCreatedEvent(evnt);
        }

        #region Grid background controls
        private void BasicGrid_MouseEnter(object sender, RoutedEventArgs e)
        {
            Grid grid = sender as Grid;
            if (grid == null) return;
            grid.Background = focusedGridBackground;
        }

        private void BasicGrid_MouseLeave(object sender, RoutedEventArgs e)
        {
            Grid grid = sender as Grid;
            if (grid == null) return;
            grid.Background = Brushes.Transparent;
        }

        private void PlayerSettingGrid_MouseEnter(object sender, RoutedEventArgs e)
        {
            Grid grid = sender as Grid;
            if (grid == null) return;
            grid.Background = focusedPlayerSettingGridBackground;
        }

        private void PlayerSettingGrid_MouseLeave(object sender, RoutedEventArgs e)
        {
            Grid grid = sender as Grid;
            if (grid == null) return;
            grid.Background = Brushes.Transparent;
        }
        #endregion

        private void cmbRecurringFreq_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpRecurringDate == null) return;
            bool showing = (cmbRecurringFreq.SelectedItem.ToString() == "Custom");
            gridRecurringFreq.Height = showing ? 90 : 50;
            dpRecurringDate.Visibility = showing ? Visibility.Visible : Visibility.Hidden;
        }

        private void cmbPlaylistName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string plName = cmbPlaylistName.SelectedItem.ToString();
            int trackAmount = plc.TrackLists.Find(x => x.ListName == plName).TrackAmount;
            int[] trackIndices = new int[trackAmount];
            for (int i = 0; i < trackAmount; i++)
            {
                trackIndices[i] = i;
            }
            cmbTrackIndex.ItemsSource = trackIndices;
        }

        private void btnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void txtEventName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (reEscChars.IsMatch(((TextBox)sender).Text))
            {
                SetToolTip((TextBox)sender, "Text shoud not contain the following charaters: \\/:*?\"<>|");
            }
            else
            {
                RemoveToolTip((TextBox)sender);
            }
        }

        private void txtEventName_LostFocus(object sender, RoutedEventArgs e)
        {
            RemoveToolTip((TextBox)sender);
        }

        private void txtRank_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!reDigitOnly.IsMatch(((TextBox)sender).Text))
            {
                SetToolTip((TextBox)sender, "Text shoud contain digits only.");
            }
            else
            {
                RemoveToolTip((TextBox)sender);
            }
        }

        private void txtRank_LostFocus(object sender, RoutedEventArgs e)
        {
            RemoveToolTip((TextBox)sender);
        }

        private void SetToolTip<T>(T owner, string msg) where T : System.Windows.Controls.Control
        {
            if ((owner).ToolTip == null)
            {
                System.Windows.Controls.ToolTip tp = new System.Windows.Controls.ToolTip();
                tp.Content = msg;
                tp.PlacementTarget = owner;
                tp.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
                tp.HorizontalOffset = owner.Width + 5;
                owner.ToolTip = tp;
                tp.IsOpen = true;
            }
            else
            {
                (owner.ToolTip as System.Windows.Controls.ToolTip).IsOpen = true;
            }
        }

        private void RemoveToolTip<T>(T owner) where T : System.Windows.Controls.Control
        {
            if (owner.ToolTip != null)
            {
                System.Windows.Controls.ToolTip tp = owner.ToolTip as System.Windows.Controls.ToolTip;
                tp.Content = "";
                tp.IsOpen = false;
                owner.ToolTip = null;
                tp = null;
            }
        }

        private void btnAddMoreTimePicker_Click(object sender, RoutedEventArgs e)
        {
            ChangeMarginOfTimePickerContainer(true);

            Button btnRemoveTimePicker = new Button
            {
                Name = "btnRemoveTimePicker",
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 0, 2),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left,
                Style = this.FindResource("FlatButton_Opacity") as Style,
            };
            btnRemoveTimePicker.SetResourceReference(Button.ContentProperty, "ImagRemoveTimePickerContainer");
            btnRemoveTimePicker.Click += btnRemoveTimePicker_Click;

            TimePicker timePicker = new TimePicker()
            {
                Name = "timePicker",
                Height = 20,
                Margin = new Thickness(20, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Format = DateTimeFormat.LongTime,
            };
            timePicker.Value = this.InitialBeginningTime;

            Grid gdTimePicker = new Grid()
            {
                Name = "gdTimePicker",
                Height = 20,
                Margin = new Thickness(0, 0, 0, 10),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            gdTimePicker.Children.Add(btnRemoveTimePicker);
            gdTimePicker.Children.Add(timePicker);

            spTimePickerList.Children.Add(gdTimePicker);
        }

        private void btnRemoveTimePicker_Click(object sender, RoutedEventArgs e)
        {
            Grid gd = ((Button)sender).Parent as Grid;
            spTimePickerList.Children.Remove(gd);
            ChangeMarginOfTimePickerContainer(false);
        }

        private void ChangeMarginOfTimePickerContainer(bool extend)
        {
            gdDateTimePickerContainer.Height += extend ? 30 : -30;
            spTimePickerList.Height += extend ? 30 : -30;

            Thickness temp = spTimePickerList.Margin;
            spTimePickerList.Margin = new Thickness(temp.Left, 0, temp.Right, 0);
        }
    }

    public static class ControlExtension
    {
        public static T FindChild<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            T child = null;
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                child = VisualTreeHelper.GetChild(parent, i) as T;
                if (child != null)
                    break;
            }
            return child;
        }
    }
}
