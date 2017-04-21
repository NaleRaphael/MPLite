using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace Jarloo.Calendar
{
    using Playlist = MPLite.Core.Playlist;
    using PlaylistCollection = MPLite.Core.PlaylistCollection;

    public partial class WindowEventSetting : Window
    {
        private SolidColorBrush focusedGridBackground = new SolidColorBrush(Color.FromArgb(0xFF, 0x82, 0x65, 0x3F));
        private SolidColorBrush focusedPlayerSettingGridBackground = new SolidColorBrush(Color.FromArgb(0xFF, 0x2A, 0x4F, 0x75));

        private PlaylistCollection plc;

        public DateTime InitialBeginningTime;

        public delegate IEvent NewlyAddedEventHandler();
        public event NewlyAddedEventHandler NewlyAddedEvent;

        public delegate void PassingDataEventHandler(string data);
        public event PassingDataEventHandler PassingDataEvent;

        public delegate void NewEventIsCreatedEventHandler(CustomEvent evnt);
        public event NewEventIsCreatedEventHandler NewEventIsCreatedEvent;

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
        }

        #region Window control
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.PassingDataEvent = null;
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
            PassingDataEvent(timeSpanUpDown.Value.ToString());

            string eventName;
            int rank;
            DateTime beginningTime;
            TimeSpan duration = TimeSpan.Zero;
            bool autoDelete = (chkAutoDelete.IsChecked == true) ? true : false;
            bool thisDayForwardOnly = (chkThisDayForwardOnly.IsChecked == true) ? true : false;

            // Check parameters
            eventName = txtEventName.Text;

            if (!int.TryParse(txtRank.Text, out rank))
                throw new Exception("Given value of \"Rank\" is invalid, it should contains digits only.");

            if (dateTimePicker.Value == null)
                throw new Exception("Invalid value of DateTimePicker");
            else beginningTime = dateTimePicker.Value.Value;

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

            //MessageBox.Show(((RecurringFrequencies)recurringFreq).ToString());
            Console.WriteLine(((RecurringFrequencies)recurringFreq).ToString());

            CustomEvent ce = new CustomEvent
            {
                EventText = eventName,
                Rank = rank,
                BeginningTime = beginningTime,
                Duration = duration,
                AutoDelete = autoDelete,
                ThisDayForwardOnly = thisDayForwardOnly,
                Enabled = true,  // let user set this while creating event?
                RecurringFrequency = (RecurringFrequencies)recurringFreq,
                IgnoreTimeComponent = true,
                ReadOnlyEvent = false
            };

            //ce.ActionStartsEventArgs;

            // TODO: create event args

            NewEventIsCreatedEvent(ce);
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
            /*if (cmbRecurringFreq.SelectedItem.ToString() == "None")
                cmbRecurringFreq.Height = 70;*/
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
    }
}
