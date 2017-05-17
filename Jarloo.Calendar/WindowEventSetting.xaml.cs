using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        private List<Control> controlList = new List<Control>();

        private IEvent evntToBeUpdated;

        #region Events
        public delegate void NewEventIsCreatedEventHandler(CustomEvent evnt);
        public event NewEventIsCreatedEventHandler NewEventIsCreatedEvent;
        public delegate void UpdateEventHandler(IEvent evnt);
        public event UpdateEventHandler UpdateEvent;
        #endregion

        public DateTime InitialBeginningTime { get; set; }
        public bool IsReadOnly { get; private set; }
        public DisplayMode WindowMode { get; private set; }

        public WindowEventSetting()
        {
            InitializeComponent();
            InitializeControls();
            IsReadOnly = false;
        }

        public WindowEventSetting(DateTime selectedDateTime)
        {
            InitializeComponent();
            InitialBeginningTime = selectedDateTime;
            InitializeControls();
            WindowMode = DisplayMode.Create;
            IsReadOnly = false;
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
            cmbPlaylist.ItemsSource = (plc == null) ? null : plc.TrackLists;

            // cmbPlaybackMode
            List<PlaybackMode> playbackModes = new List<MPLite.Core.PlaybackMode>();
            foreach (PlaybackMode mode in Enum.GetValues(typeof(PlaybackMode)))
            {
                if (mode == PlaybackMode.None) continue;
                playbackModes.Add(mode);
            }
            cmbPlaybackMode.ItemsSource = playbackModes;
            cmbPlaybackMode.SelectedIndex = 0;

            // Build control list
            controlList.Add(txtEventName);
            controlList.Add(txtRank);
            controlList.Add(dateTimePicker);
            controlList.Add(btnAddMoreTimePicker);
            controlList.Add(chkSetDuration);
            controlList.Add(timeSpanUpDown);
            controlList.Add(cmbRecurringFreq);
            controlList.Add(chkAutoDelete);
            controlList.Add(chkThisDayForwardOnly);

            controlList.Add(cmbPlaylist);
            controlList.Add(cmbTrackIndex);
            controlList.Add(cmbPlaybackMode);
        }

        public void ShowEventSetting<T>(T evnt, DisplayMode mode) where T : class, IEvent
        {
            WindowMode = mode;
            IsReadOnly = (mode == DisplayMode.ShowInfo) ? true : false;
            evntToBeUpdated = (mode == DisplayMode.Edit) ? evnt as T : null;
            txtEventName.Text = evnt.EventText;
            txtRank.Text = evnt.Rank.ToString();

            // beginning time
            if (evnt.GetType().Equals(typeof(CustomEvent)))
            {
                dateTimePicker.Value = evnt.BeginningTime;
            }
            else if (evnt.GetType().Equals(typeof(MultiTriggerEvent)))
            {
                MultiTriggerEvent mte = evnt as MultiTriggerEvent;
                dateTimePicker.Value = mte.BeginningTimeQueue.Peek();
                for (int i = 1; i < mte.BeginningTimeQueue.Count; i++)
                {
                    Button btn;
                    TimePicker tp;
                    ChangeMarginOfTimePickerContainer(true);
                    CreateTimePickerGroup(mte.BeginningTimeQueue.ElementAt(i), out btn, out tp);
                    btn.IsEnabled = !IsReadOnly;
                    tp.IsEnabled = !IsReadOnly;
                }
            }

            timeSpanUpDown.Value = evnt.Duration;
            chkSetDuration.IsChecked = (evnt.Duration == TimeSpan.Zero) ? false : true;

            // Recurring frequency
            if (Enum.GetValues(typeof(RecurringFrequencies)).Contains<RecurringFrequencies>(evnt.RecurringFrequency))
                cmbRecurringFreq.SelectedItem = evnt.RecurringFrequency;
            else
            {
                cmbRecurringFreq.SelectedItem = RecurringFrequencies.Custom;
                ConvertRecurringFreqToBlocks(evnt.RecurringFrequency);
            }

            gridRecurringDate.IsEnabled = false;
            chkAutoDelete.IsChecked = evnt.AutoDelete;
            chkThisDayForwardOnly.IsChecked = evnt.ThisDayForwardOnly;

            // Settings of playback
            SchedulerEventArgs actionArgs = evnt.ActionStartsEventArgs as SchedulerEventArgs;
            cmbPlaylist.SelectedItem = cmbPlaylist.Items.OfType<Playlist>().ToList().Find(x => x.GUID == actionArgs.PlaylistGUID);
            cmbTrackIndex.SelectedIndex = actionArgs.TrackIndex;
            cmbPlaybackMode.SelectedItem = actionArgs.Mode;

            if (IsReadOnly)
            {
                foreach (Control ctrl in controlList)
                {
                    ctrl.IsEnabled = false;
                }
            }
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
                if (!IsReadOnly)
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
            if (eventName.Length == 0)
                throw new Exception("Event name should not be space.");

            // Rank
            if (!reDigitOnly.IsMatch(txtRank.Text))
                throw new Exception("Given value of \"Rank\" is invalid, it should contains digits only.");
            rank = int.Parse(txtRank.Text);

            // Beginning time
            List<DateTime> dtList = new List<DateTime>();

            dtList.Add(dateTimePicker.Value.Value);
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

            // Duration
            if (chkSetDuration.IsChecked == true)
                duration = timeSpanUpDown.Value.Value;

            // Set recurring frequency
            RecurringFrequencies recurringFreq = 0;
            if (cmbRecurringFreq.SelectedItem.ToString() == RecurringFrequencies.Custom.ToString())
            {
                recurringFreq = ConvertBlocksToRecurringFreq();
            }
            else
            {
                recurringFreq = (RecurringFrequencies)cmbRecurringFreq.SelectedItem;
            }

            if (cmbTrackIndex.SelectedIndex == -1)
            {
                throw new Exception("No track is avalible in this playlist");
            }

            MultiTriggerEvent evnt = new MultiTriggerEvent(beginningTimeQueue)
            {
                EventText = eventName,
                Rank = rank,
                Duration = duration,
                AutoDelete = autoDelete,
                ThisDayForwardOnly = thisDayForwardOnly,
                Enabled = true,  // let user set this while creating event?
                RecurringFrequency = recurringFreq,
                IgnoreTimeComponent = true,
                ReadOnlyEvent = false
            };

            evnt.ActionStartsEventArgs = new SchedulerEventArgs
            {
                PlaylistGUID = (cmbPlaylist.SelectedItem as Playlist).GUID,
                Command = PlaybackCommands.Play,
                Mode = (PlaybackMode)cmbPlaybackMode.SelectedItem,
                TrackIndex = cmbTrackIndex.SelectedIndex
            };

            evnt.ActionEndsEventArgs = new SchedulerEventArgs
            {
                Command = PlaybackCommands.Stop
            };

            if (WindowMode == DisplayMode.Create)
                NewEventIsCreatedEvent(evnt);
            else if (WindowMode == DisplayMode.Edit)
            {
                evnt.CloneTo(evntToBeUpdated);
                UpdateEvent(evntToBeUpdated);
            }
        }

        #region Converter of RecurringFreqeuncy and date blocks
        private void ConvertRecurringFreqToBlocks(RecurringFrequencies rf)
        {
            byte temp = 0x1;
            byte brf = (byte)rf;
            foreach (CheckBox chkbox in gridRecurringDate.Children)
            {  
                if ((temp & brf) == 1)
                    chkbox.IsChecked = true;
                brf >>= 1;
            }
        }

        private RecurringFrequencies ConvertBlocksToRecurringFreq()
        {
            int recurringFreq = 0;
            int temp = 1;
            foreach (CheckBox chkbox in gridRecurringDate.Children)
            {
                recurringFreq += (chkbox.IsChecked == true) ? temp : 0;
                temp <<= 1;
            }
            return (RecurringFrequencies)recurringFreq;
        }
        #endregion

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

        private void cmbPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Playlist pl = cmbPlaylist.SelectedItem as Playlist;
            if (pl == null) return;

            int trackAmount = plc.TrackLists.Find(x => x.GUID == pl.GUID).TrackAmount;
            if (trackAmount == 0)
            {
                System.Windows.MessageBox.Show("No track is avalible in this playlist.");
                cmbTrackIndex.ItemsSource = null;
                return;
            }

            int[] trackIndices = new int[trackAmount];
            for (int i = 0; i < trackAmount; i++)
            {
                trackIndices[i] = i;
            }
            cmbTrackIndex.ItemsSource = trackIndices;
            cmbTrackIndex.SelectedIndex = 0;
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
            Button btn;
            TimePicker tp;
            CreateTimePickerGroup(this.InitialBeginningTime, out btn, out tp);
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

        private void CreateTimePickerGroup(DateTime initialValue, out Button btnRemoveTimePicker, out TimePicker timePicker)
        {
            btnRemoveTimePicker = new Button
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

            timePicker = new TimePicker()
            {
                Name = "timePicker",
                Height = 20,
                Margin = new Thickness(20, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Format = DateTimeFormat.LongTime,
            };
            timePicker.Value = initialValue;

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
    }

    public enum DisplayMode
    {
        Create = 0,
        ShowInfo,
        Edit
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

    public static class EnumExtension
    {
        public static bool Contains<T>(this Array ary, T obj)
        {
            if (ary.Length == 0)
                throw new Exception("Given array is empty");
            if (ary.GetValue(0).GetType() != typeof(T))
                throw new Exception("Given object does not equal to the type of array elements.");

            bool result = false;
            for (int i = 0; i < ary.Length; i++)
            {
                if (ary.GetValue(i).Equals(obj))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }
    }
}
