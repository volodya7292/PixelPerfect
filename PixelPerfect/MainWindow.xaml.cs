﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PixelPerfect
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < 10; i++)
            {
                ProfileItem item = new ProfileItem();
                item.Width = double.NaN;
                item.Height = 68;
                item.MainText = "Срач версия";
                item.SubText = "1.10.2";
                Thickness margin = item.Margin;
                margin.Top = -1;
                item.Margin = margin;
                item.IconImage = new BitmapImage(new Uri(@"pack://application:,,,/Images/block_granite.png", UriKind.Absolute));

                profilesSP.Children.Add(item);
            }

            bottomG.Height = 41;
            playButtonsSP.Opacity = 0.0;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DoubleAnimation anim0 = new DoubleAnimation(80, TimeSpan.FromMilliseconds(200));


            QuarticEase easingFunction = new QuarticEase();
            easingFunction.EasingMode = EasingMode.EaseInOut;
            anim0.EasingFunction = easingFunction;



            anim0.Completed += new EventHandler((object sender2, EventArgs e2) =>
            {
                DoubleAnimation anim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(300));
                anim.EasingFunction = easingFunction;
                playButtonsSP.BeginAnimation(OpacityProperty, anim);
            });

            bottomG.BeginAnimation(HeightProperty, anim0);
        }

        private void playB_Click(object sender, RoutedEventArgs e)
        {
            profilesSV.Visibility = Visibility.Hidden;
            playButtonsSP.Visibility = Visibility.Hidden;

            DoubleAnimation anim0 = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            basePlayInfoGrid.BeginAnimation(OpacityProperty, anim0);

            DoubleAnimation anim1 = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            downloadInfoGrid.BeginAnimation(OpacityProperty, anim1);
        }

        private void addProfileB_Click(object sender, RoutedEventArgs e)
        {

        }

        private void profilesB_Click(object sender, RoutedEventArgs e)
        {
            if (profilesSV.Visibility == Visibility.Visible)
                profilesSV.Visibility = Visibility.Hidden;
            else
                profilesSV.Visibility = Visibility.Visible;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            profilesSV.Visibility = Visibility.Hidden;
        }

        private void generalTB_Clicked(object sender, MouseButtonEventArgs e)
        {
            updateMainToggleButtons(generalTB);
        }

        private void settingsTB_Clicked(object sender, MouseButtonEventArgs e)
        {
            updateMainToggleButtons(settingsTB);
        }

        private void profilesTB_Clicked(object sender, MouseButtonEventArgs e)
        {
            updateMainToggleButtons(profilesTB);
        }

        private void updateMainToggleButtons(MinecraftToggleButton checkedButton)
        {
            DoubleAnimation anim0 = new DoubleAnimation(generalTB.CheckOpacity, generalTB.Equals(checkedButton) ? 1 : 0, TimeSpan.FromMilliseconds(250));
            DoubleAnimation anim1 = new DoubleAnimation(settingsTB.CheckOpacity, settingsTB.Equals(checkedButton) ? 1 : 0, TimeSpan.FromMilliseconds(250));
            DoubleAnimation anim2 = new DoubleAnimation(profilesTB.CheckOpacity, profilesTB.Equals(checkedButton) ? 1 : 0, TimeSpan.FromMilliseconds(250));

            generalTB.BeginAnimation(MinecraftToggleButton.checkOpacityProperty, anim0);
            settingsTB.BeginAnimation(MinecraftToggleButton.checkOpacityProperty, anim1);
            profilesTB.BeginAnimation(MinecraftToggleButton.checkOpacityProperty, anim2);  
        }
    }
}
