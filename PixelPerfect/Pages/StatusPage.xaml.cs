﻿using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PixelPerfect.Pages
{
    /// <summary>
    /// Логика взаимодействия для StatusPage.xaml
    /// </summary>
    public partial class StatusPage : Page
    {
        public BitmapImage statusGreen, statusYellow, statusRed, statusMessage, statusUpdate;

        public StatusPage()
        {
            InitializeComponent();

            statusGreen = Utils.ImageFromResource("Images/status_green.png");
            statusYellow = Utils.ImageFromResource("Images/status_yellow.png");
            statusRed = Utils.ImageFromResource("Images/status_red.png");
            statusMessage = Utils.ImageFromResource("Images/status_message.png");
            statusUpdate = Utils.ImageFromResource("Images/status_update.png");
        }

        public void setStatus(string status, Image image, Label text)
        {
            switch (status)
            {
                case "green":
                    image.Source = statusGreen;
                    text.Content = "Сервис работает нормально.";
                    break;
                case "yellow":
                    image.Source = statusYellow;
                    text.Content = "Имеются некоторые проблемы.";
                    break;
                case "red":
                    image.Source = statusRed;
                    text.Content = "Сервис временно недоступен.";
                    break;
                case "message":
                    image.Source = statusMessage;
                    text.Content = "Сервис обновляется.";
                    break;
                case "update":
                    image.Source = statusUpdate;
                    text.Content = "Сервис обновляется.";
                    break;
            }
        }

        public async void updateStatuses()
        {
            Dictionary<string, string> status = await Utils.GetMojangStatus();

            if (status == null)
                return;

            setStatus(status["minecraft.net"], minecraftIcon, minecraftText);
            setStatus(status["account.mojang.com"], accountsIcon, accountsText);
            setStatus(status["authserver.mojang.com"], authIcon, authText);
            setStatus(status["sessionserver.mojang.com"], multiplayerIcon, multiplayerText);
            setStatus(status["textures.minecraft.net"], texturesIcon, texturesText);
            setStatus(status["api.mojang.com"], apiIcon, apiText);
        }
    }
}
