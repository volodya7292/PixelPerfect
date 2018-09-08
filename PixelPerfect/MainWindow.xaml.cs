﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PixelPerfect.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace PixelPerfect
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GeneralPage generalPage;
        private SettingsPage settingsPage;
        private StatusPage statusPage;
        private AddProfilePage addProfilePage;
        private EditProfilePage editProfilePage;

        private JObject settings;
        private string ppPath, configPath;

        private VersionManifest versionManifest;
        private FileDownloader fileDownloader;

        private BitmapImage grassIcon, craftingTableIcon;
        private string grassIconData, craftingTableIconData;

        private bool isPlaying = false;

        public static string RELEASE_VERSION_NAME = "Последний выпуск";
        public static string SNAPSHOT_VERSION_NAME = "Предварительная версия";

        public MainWindow()
        {
            InitializeComponent();

            generalPage = new GeneralPage();
            settingsPage = new SettingsPage();
            statusPage = new StatusPage();
            addProfilePage = new AddProfilePage();
            editProfilePage = new EditProfilePage();

            // Set values for startup animations
            bottomG.Height = 41;
            playButtonsSP.Opacity = 0.0;

            // ----------------------------------------------------
            grassIcon = Utils.ImageFromResource("Images\\Blocks\\Grass.png");
            craftingTableIcon = Utils.ImageFromResource("Images\\Blocks\\Crafting_Table.png");

            grassIconData = Convert.ToBase64String(Utils.ImageToBytes(grassIcon));
            craftingTableIconData = Convert.ToBase64String(Utils.ImageToBytes(craftingTableIcon));

            ppPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\AppData\\Roaming\\PixelPerfect\\";
            configPath = ppPath + "\\config.json";
            Directory.CreateDirectory(ppPath);

            loadConfig();

            versionManifest = Utils.GetMCVersions();

            if (versionManifest == null)
            {
                versionManifest = new VersionManifest(new Dictionary<string, MCVersion>(), "", "");
            }
            else
            {
                addProfilePage.loadVersionManifest(versionManifest);
                updateGamePath();
            } 

            updateProfileItems();

            string responce = Utils.ValidateAccessData((string)settings["accessToken"], (string)settings["clientToken"]);

            if (responce == "403")
            {
                responce = Utils.RefreshAccessData((string)settings["accessToken"], (string)settings["clientToken"]);

                try
                {
                    JObject data = JObject.Parse(responce);
                    settings["accessToken"] = (string)data["accessToken"];
                    saveConfig();
                }
                catch { responce = "403"; }
            }

            if ((string)settings["accessToken"] == "0" || (string)settings["uuid"] == "0" || (string)settings["clientToken"] == "0" || responce == "403")
                loadLogin();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (!isPlaying && loginG.Visibility == Visibility.Hidden)
                loadSelectedPage();
        }

        private void minecraftImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://minecraft.net");
        }

        private void addProfileB_Click(object sender, RoutedEventArgs e)
        {
            profilesSV.Visibility = Visibility.Hidden;
            hidePlayBar();
            navigatePage(addProfilePage, false, false);
        }

        private void editProfileB_Click(object sender, RoutedEventArgs e)
        {
            string selectedProfile = settings["selectedProfile"].ToString();
            editProfilePage.load(selectedProfile, getProfile(selectedProfile));

            profilesSV.Visibility = Visibility.Hidden;
            hidePlayBar();
            navigatePage(editProfilePage, false, false);
        }

        private void playB_Click(object sender, RoutedEventArgs e)
        {
            profilesSV.Visibility = Visibility.Hidden;
            playButtonsSP.Visibility = Visibility.Hidden;

            DoubleAnimation anim0 = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            basePlayInfoGrid.BeginAnimation(OpacityProperty, anim0);

            DoubleAnimation anim1 = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            downloadInfoGrid.BeginAnimation(OpacityProperty, anim1);

            downloadPB.Value = 0;
            downloadInfoL.Content = "Подготовка ...";
            downloadLeftL.Content = string.Empty;

            JObject selectedProfile = getProfile((string)settings["selectedProfile"]);
            string name = (string)selectedProfile["name"];
            string version = (string)selectedProfile["version"];
            string javaArgs = (string)selectedProfile["javaArgs"];
            string gamePath = (string)settings["gamePath"];
            string profilePath = (bool)selectedProfile["custom"] ? gamePath + "\\" + name : gamePath;

            startGame(version, javaArgs, gamePath, profilePath);
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
            loadGeneralPage();
        }

        private void settingsTB_Clicked(object sender, MouseButtonEventArgs e)
        {
            updateMainToggleButtons(settingsTB);
            loadSettingsPage();
        }

        private void statusTB_Clicked(object sender, MouseButtonEventArgs e)
        {
            updateMainToggleButtons(statusTB);
            loadStatusPage();
        }

        private async void loginB_Click(object sender, RoutedEventArgs e)
        {
            await loadLogin();

            string username = emailTB.Text;
            string password = passwordTB.Password;

            if ((string)settings["clientToken"] == "0")
                settings["clientToken"] = Utils.GenerateClientToken();

            incorrectLoginL.Visibility = Visibility.Collapsed;
            unknownLoginErrorL.Visibility = Visibility.Collapsed;

            string accessData = await Utils.GetAccessData((string)settings["clientToken"], username, password);

            try
            {
                JObject data = JObject.Parse(accessData);

                string playerName = (string)data["selectedProfile"]["name"];
                settings["uuid"] = await Utils.GetPlayerUUID(playerName);
                settings["accessToken"] = (string)data["accessToken"];
                settings["playerName"] = playerName;
                settings["loginUsername"] = username;
                closeLogin();
                loadSelectedPage();
            }
            catch
            {
                if (accessData == "403")
                {
                    passwordTB.Password = string.Empty;
                    incorrectLoginL.Visibility = Visibility.Visible;
                }
                else if (!InternetAvailability.IsInternetAvailable())
                {
                    unknownLoginErrorL.Visibility = Visibility.Visible;
                    unknownLoginErrorL.Content = "Нет подключения к интернету.";
                }
                else
                {
                    unknownLoginErrorL.Visibility = Visibility.Visible;
                    unknownLoginErrorL.Content = accessData;
                }
            }

            saveConfig();
        }

        private void emailTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            loginB.IsEnabled = !string.IsNullOrWhiteSpace(emailTB.Text) && !string.IsNullOrWhiteSpace(passwordTB.Password);
        }

        private void passwordTB_PasswordChanged(object sender, RoutedEventArgs e)
        {
            loginB.IsEnabled = !string.IsNullOrWhiteSpace(emailTB.Text) && !string.IsNullOrWhiteSpace(passwordTB.Password);
        }

        private void showPlayBar()
        {
            if (isPlaying)
                return;

            int h = 80;

            playButtonsSP.Visibility = Visibility.Visible;

            DoubleAnimation anim0 = new DoubleAnimation(h, TimeSpan.FromMilliseconds(200));

            QuarticEase easingFunction = new QuarticEase();
            easingFunction.EasingMode = EasingMode.EaseInOut;

            anim0.EasingFunction = easingFunction;
            anim0.Completed += new EventHandler((object sender2, EventArgs e2) =>
            {
                DoubleAnimation anim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(300));
                anim.EasingFunction = easingFunction;
                playButtonsSP.BeginAnimation(OpacityProperty, anim);
            });

            
            Thickness start = frameSV.Margin;
            start.Bottom = bottomG.Height;
            Thickness end = frameSV.Margin;
            end.Bottom = 80;

            ThicknessAnimation mAnim = new ThicknessAnimation(start, end, TimeSpan.FromMilliseconds(200));
            mAnim.EasingFunction = easingFunction;


            bottomG.BeginAnimation(HeightProperty, anim0);
            frameSV.BeginAnimation(MarginProperty, mAnim);
        }

        private void hidePlayBar()
        {
            int h = 41;

            DoubleAnimation anim0 = new DoubleAnimation(h, TimeSpan.FromMilliseconds(200));

            QuarticEase easingFunction = new QuarticEase();
            easingFunction.EasingMode = EasingMode.EaseInOut;

            bottomG.BeginAnimation(HeightProperty, anim0);

            DoubleAnimation anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(300));
            anim.EasingFunction = easingFunction;
            anim.Completed += new EventHandler((object sender, EventArgs e) =>
            {
                playButtonsSP.Visibility = Visibility.Hidden;
            });

            Thickness margin = frameSV.Margin;
            margin.Bottom = h;
            frameSV.Margin = margin;

            playButtonsSP.BeginAnimation(OpacityProperty, anim);
        }

        private void updateMainToggleButtons(MinecraftToggleButton checkedButton)
        {
            DoubleAnimation anim0 = new DoubleAnimation(generalTB.CheckOpacity, generalTB.Equals(checkedButton) ? 1 : 0, TimeSpan.FromMilliseconds(220));
            DoubleAnimation anim1 = new DoubleAnimation(settingsTB.CheckOpacity, settingsTB.Equals(checkedButton) ? 1 : 0, TimeSpan.FromMilliseconds(220));
            DoubleAnimation anim2 = new DoubleAnimation(statusTB.CheckOpacity, statusTB.Equals(checkedButton) ? 1 : 0, TimeSpan.FromMilliseconds(220));

            generalTB.BeginAnimation(MinecraftToggleButton.checkOpacityProperty, anim0);
            settingsTB.BeginAnimation(MinecraftToggleButton.checkOpacityProperty, anim1);
            statusTB.BeginAnimation(MinecraftToggleButton.checkOpacityProperty, anim2);
        }

        public void loadGeneralPage()
        {
            generalPage.updateNews();
            navigatePage(generalPage, false, true);
            showPlayBar();
        }

        public void loadSettingsPage()
        {
            settingsPage.loadSettings(settings);
            navigatePage(settingsPage, false, false);
            showPlayBar();
        }

        public void loadStatusPage()
        {
            statusPage.updateStatuses();
            navigatePage(statusPage, false, false);
            showPlayBar();
        }

        public void loadSelectedPage()
        {
            if (generalTB.CheckOpacity == 1)
                loadGeneralPage();
            else if (settingsTB.CheckOpacity == 1)
                loadSettingsPage();
            else if (statusTB.CheckOpacity == 1)
                loadStatusPage();

            showPlayBar();
        }

        public void navigatePage(Page page, bool disableScroll, bool stretch)
        {
            Frame frame = new Frame();

            if (stretch)
            {
                frame.HorizontalAlignment = HorizontalAlignment.Stretch;
                frame.VerticalAlignment = VerticalAlignment.Stretch;
            }
            else
            {
                frame.HorizontalAlignment = HorizontalAlignment.Center;
                frame.VerticalAlignment = VerticalAlignment.Center;

                Thickness margin = frame.Margin;
                margin.Top = 20;
                margin.Bottom = 30;
                frame.Margin = margin;
            }

            frame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            frame.Content = page;

            frameSV.Content = null;
            frameG.Children.Clear();

            if (disableScroll)
                frameG.Children.Add(frame);
            else
                frameSV.Content = frame;
        }

        public async Task loadLogin()
        {
            loginG.Visibility = Visibility.Visible;

            if (!string.IsNullOrWhiteSpace((string)settings["loginUsername"]))
                emailTB.Text = (string)settings["loginUsername"];

            Dictionary<string, string> status = await Utils.GetMojangStatus();

            if (status == null)
                return;

            switch (status["authserver.mojang.com"])
            {
                case "green":
                    authIcon.Source = statusPage.statusGreen;
                    authText.Content = "Сервис работает нормально.";
                    break;
                case "yellow":
                    authIcon.Source = statusPage.statusYellow;
                    authText.Content = "Имеются некоторые проблемы.";
                    break;
                case "red":
                    authIcon.Source = statusPage.statusRed;
                    authText.Content = "Сервис временно недоступен.";
                    break;
                case "message":
                    authIcon.Source = statusPage.statusMessage;
                    authText.Content = "Сервис обновляется.";
                    break;
                case "update":
                    authIcon.Source = statusPage.statusUpdate;
                    authText.Content = "Сервис обновляется.";
                    break;
            }
        }

        public void closeLogin()
        {
            loginG.Visibility = Visibility.Hidden;
        }

        public void updateGamePath()
        {
            editProfilePage.updateVersions(versionManifest, (string)settings["gamePath"] + "\\versions\\", (bool)settings["showSnapshots"]);
            updateProfileItems();
        }

        public void clearProfileItems()
        {
            profilesSP.Children.Clear();
        }

        bool selectedProfileExistsInList = false;
        public void addProfileItem(string mainText, string subText, BitmapImage icon)
        {
            ProfileItem item = new ProfileItem();
            item.Width = double.NaN;
            item.Height = 68;
            item.MainText = mainText;
            item.SubText = subText;
            Thickness margin = item.Margin;
            margin.Top = profilesSP.Children.Count == 0 ? 0 : -1;
            item.Margin = margin;
            item.IconImage = icon;
            item.MouseUp += new MouseButtonEventHandler((object sender, MouseButtonEventArgs e) =>
            {
                ProfileItem i = (ProfileItem)sender;
                settings["selectedProfile"] = i.MainText;
                saveConfig();

                DoubleAnimation anim0 = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(110));
                anim0.Completed += new EventHandler((object sender2, EventArgs e2) =>
                {
                    selectedProfileNameL.Content = i.MainText + " - " + i.SubText;

                    DoubleAnimation anim1 = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(110));
                    selectedProfileNameL.BeginAnimation(OpacityProperty, anim1);
                });

                selectedProfileNameL.BeginAnimation(OpacityProperty, anim0);
            });

            profilesSP.Children.Add(item);

            // Check for selected
            string selectedProfile = settings["selectedProfile"].ToString();
            if (selectedProfile == mainText)
            {
                selectedProfileNameL.Content = item.MainText + " - " + item.SubText;
                selectedProfileExistsInList = true;
            }
        }

        public void updateProfileItems()
        {
            clearProfileItems();
            selectedProfileExistsInList = false;

            if (versionManifest != null)
            {
                addProfileItem(RELEASE_VERSION_NAME, versionManifest.latestVersion, grassIcon);

                if ((bool)settings["showSnapshots"])
                    addProfileItem(SNAPSHOT_VERSION_NAME, versionManifest.latestSnapshot, craftingTableIcon);
            }

            JObject profiles = (JObject)settings["profiles"];

            foreach (JProperty property in profiles.Properties())
            { 
                JObject profile = (JObject)property.Value;
                string name = property.Name;

                if (name != RELEASE_VERSION_NAME && name != SNAPSHOT_VERSION_NAME)
                {
                    string version = (string)profiles[name]["version"];
                    string versionDir = (string)settings["gamePath"] + "\\versions\\" + version + "\\";
                    string versionJsonPath = versionDir + "\\" + version + ".json";

                    if (File.Exists(versionJsonPath))
                    {
                        try
                        {
                            JObject obj = JObject.Parse(File.ReadAllText(versionJsonPath));
                            string type = (string)obj["type"];

                            if (type == "snapshot" && (bool)settings["showSnapshots"] || type != "snapshot")
                            {
                                BitmapImage icon = Utils.BytesToImage(Convert.FromBase64String(profiles[name]["icon"].ToString()));
                                addProfileItem(name, version, icon);
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        if (versionManifest.versions.ContainsKey(version))
                        {
                            string type = versionManifest.versions[version].type;
                            if (type == "snapshot" && (bool)settings["showSnapshots"] || type != "snapshot")
                            {
                                BitmapImage icon = Utils.BytesToImage(Convert.FromBase64String(profiles[name]["icon"].ToString()));
                                addProfileItem(name, version, icon);
                            }
                        }
                        else
                        {
                            BitmapImage icon = Utils.BytesToImage(Convert.FromBase64String(profiles[name]["icon"].ToString()));
                            addProfileItem(name, version, icon);
                        }
                    }
                }
            }

            // Check for selected
            if (!selectedProfileExistsInList)
            {
                settings["selectedProfile"] = RELEASE_VERSION_NAME;
                selectedProfileNameL.Content = RELEASE_VERSION_NAME + " - " + versionManifest.latestVersion;
            }
        }

        public void loadConfig()
        {
            if (File.Exists(configPath))
                try
                {
                    settings = JObject.Parse(File.ReadAllText(configPath));

                    if (!settings.ContainsKey("gamePath"))
                        settings["gamePath"] = ppPath + "Minecraft";
                    if (!settings.ContainsKey("profiles"))
                        settings["profiles"] = new JObject();
                    if (!settings.ContainsKey("selectedProfile"))
                        settings["selectedProfile"] = RELEASE_VERSION_NAME;
                    if (!settings.ContainsKey("showSnapshots"))
                        settings["showSnapshots"] = false;
                    if (!settings.ContainsKey("width"))
                        settings["width"] = 854;
                    if (!settings.ContainsKey("height"))
                        settings["height"] = 480;
                    if (!settings.ContainsKey("uuid"))
                        settings["uuid"] = "0";
                    if (!settings.ContainsKey("accessToken"))
                        settings["accessToken"] = "0";
                    if (!settings.ContainsKey("clientToken"))
                        settings["clientToken"] = "0";
                    if (!settings.ContainsKey("loginUsername"))
                        settings["loginUsername"] = string.Empty;

                    saveConfig();
                }
                catch { createNewConfig(); }
            else createNewConfig();
        }

        public void saveConfig()
        {
            if (File.Exists(configPath))
                File.Delete(configPath);

            File.WriteAllText(configPath, JToken.Parse(JsonConvert.SerializeObject(settings)).ToString(Formatting.Indented));
        }

        public JObject getConfig()
        {
            return settings;
        }

        public void setConfig(JObject config)
        {
            settings = config;
            saveConfig();
        }

        public void createNewConfig()
        {
            settings = new JObject();
            settings["gamePath"] = ppPath + "Minecraft";
            settings["profiles"] = new JObject();
            settings["selectedProfile"] = RELEASE_VERSION_NAME;
            settings["showSnapshots"] = false;
            settings["width"] = 854;
            settings["height"] = 480;
            settings["uuid"] = "0";
            settings["accessToken"] = "0";
            settings["clientToken"] = "0";
            settings["loginUsername"] = string.Empty;

            saveConfig();
        }

        public JObject getProfile(string name)
        {
            JObject o;

            if (name == RELEASE_VERSION_NAME)
            {
                o = new JObject();
                o["name"] = name;
                o["icon"] = grassIconData;
                o["version"] = versionManifest.latestVersion;
                o["javaArgs"] = "-Xmx1G -XX:+UnlockExperimentalVMOptions -XX:+UseG1GC -XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=16M";
                o["custom"] = false;
            }
            else if (name == SNAPSHOT_VERSION_NAME)
            {
                o = new JObject();
                o["name"] = name;
                o["icon"] = craftingTableIconData;
                o["version"] = versionManifest.latestSnapshot;
                o["javaArgs"] = "-Xmx1G -XX:+UnlockExperimentalVMOptions -XX:+UseG1GC -XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=16M";
                o["custom"] = false;
            }
            else
            {
                o = (JObject)settings["profiles"][name];
                o["name"] = name;
            }

            return o;
        }

        public void setProfile(string oldName, string newName, JObject profile)
        {
            if (oldName != newName)
            {
                if (oldName == (string)settings["selectedProfile"])
                    settings["selectedProfile"] = newName;

                settings["profiles"][oldName].Parent.Remove();
            }

            settings["profiles"][newName] = profile;
            saveConfig();
        }

        public void deleteProfile(string name)
        {
            settings["profiles"][name].Parent.Remove();

            if (name == (string)settings["selectedProfile"])
                settings["selectedProfile"] = RELEASE_VERSION_NAME;

            saveConfig();
        }

        public bool isProfileExists(string name)
        {
            return settings["profiles"][name] != null || name == RELEASE_VERSION_NAME || name == SNAPSHOT_VERSION_NAME;
        }

        public async void startGame(string version, string javaArgs, string gamePath, string profilePath)
        {
            try
            {
                isPlaying = true;

                List<FileToDownload> files = await Utils.GetFilesForDownload(version, (string)settings["gamePath"], versionManifest);

                fileDownloader = new FileDownloader(files, this);
                fileDownloader.OnProgressChanged += new FileDownloader.OnProgressChangedEventHandler((object sender, ProgressChangedEventArgs e) =>
                {
                    long downloadLeftMB = (e.TotalSize - e.Downloaded) / 1024 / 1024;

                    downloadPB.Value = e.Downloaded;
                    downloadPB.Maximum = e.TotalSize;
                    downloadInfoL.Content = e.CurrentFileName;
                    downloadLeftL.Content = downloadLeftMB + "МБ осталось";
                });
                fileDownloader.OnCompleted += new FileDownloader.OnCompletedEventHandler(async (object sender) =>
                {
                    string args = await Utils.CreateMinecraftStartArgs(version, javaArgs, gamePath, profilePath, (string)settings["playerName"], (string)settings["uuid"],
                        (string)settings["accessToken"], (int)settings["width"], (int)settings["height"]);
                    ProcessStartInfo procStartInfo = new ProcessStartInfo("javaw", args);
                    procStartInfo.WorkingDirectory = profilePath;

                    Process proc = new Process();
                    proc.Exited += minecraftProcess_Exited;
                    proc.EnableRaisingEvents = true;
                    proc.StartInfo = procStartInfo;

                    proc.Start();
                    Hide();
                });

                fileDownloader.Start();
            }
            catch
            {
                prepareGameClose();
            }
        }

        private void prepareGameClose()
        {
            DoubleAnimation anim0 = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(0));
            basePlayInfoGrid.BeginAnimation(OpacityProperty, anim0);

            DoubleAnimation anim1 = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(0));
            downloadInfoGrid.BeginAnimation(OpacityProperty, anim1);

            playButtonsSP.Visibility = Visibility.Visible;

            isPlaying = false;
        }

        private void minecraftProcess_Exited(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                prepareGameClose();
                Show();
            });
        }
    }
}
