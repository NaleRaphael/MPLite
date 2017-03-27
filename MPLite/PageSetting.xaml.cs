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

namespace MPLite
{
    public partial class PageSetting : Page
    {
        public PageSetting()
        {
            InitializeComponent();
            Init_cmb_PlaybackSetting();
        }

        private void Init_cmb_PlaybackSetting()
        {
            foreach (string mode in Enum.GetNames(typeof(MPLiteConstant.PlaybackMode)))
            {
                cmb_PlaybackSetting.Items.Add(mode);
            }

            cmb_PlaybackSetting.SelectedIndex = Properties.Settings.Default.PlaybackMode;
        }

        private void cmb_PlaybackSetting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedMode = cmb_PlaybackSetting.SelectedItem.ToString();
            MPLiteConstant.PlaybackMode mode;

            if (Enum.TryParse<MPLiteConstant.PlaybackMode>(selectedMode, out mode))
            {
                if ((int)mode != Properties.Settings.Default.PlaybackMode)
                {
                    Properties.Settings.Default.PlaybackMode = (int)mode;
                    Properties.Settings.Default.Save();
                }
            }
        }
    }
}
