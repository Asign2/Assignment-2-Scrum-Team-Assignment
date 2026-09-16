using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Assign_2
{
    public partial class MainWindow : Window
    {
        private UserRecord selectedUser;
        private string currentUsername;
        private bool adminScreenReady;
        private bool userScreenReady;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                SensorsDatabase.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database error:\n\n" + ex.Message);
            }
        }

        /// <summary>
        /// Compares user account input to the database credentials.
        /// Routes the user to the appropriate screen based on their role.
        /// </summary>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;

            try
            {
                bool success = UserDatabase.Login(username, password, out string role);

                if (!success)
                {
                    MessageBox.Show("Invalid username or password.");
                    return;
                }

                currentUsername = username;

                if (role == "Admin")
                {
                    ShowScreen(AdminScreen);
                    RefreshUserList();

                    if (!adminScreenReady)
                    {
                        adminScreenReady = true;
                        LoadSettingsIntoForm();
                    }

                    try
                    {
                        SensorsGrid.ItemsSource = SensorsDatabase.GetAllSensors();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    RefreshLog_Click(null, null);
                }
                else
                {
                    OpenUserScreen();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Refreshes the user data grid.
        /// </summary>
        private void RefreshUserList()
        {
            UsersGrid.ItemsSource = UserDatabase.GetAllUsers();
        }

        /// <summary>
        /// Populates account edit inputs based on the selected grid user.
        /// </summary>
        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = UsersGrid.SelectedItem as UserRecord;

            if (selectedUser == null)
            {
                return;
            }

            EditUsernameBox.Text = selectedUser.Username;
            NewPasswordBox.Password = "";

            foreach (ComboBoxItem item in RoleBox.Items)
            {
                if (item.Content.ToString() == selectedUser.Role)
                {
                    RoleBox.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// Commits updates to the selected user's username and role.
        /// </summary>
        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            string newUsername = EditUsernameBox.Text.Trim();
            ComboBoxItem chosenRole = RoleBox.SelectedItem as ComboBoxItem;

            if (string.IsNullOrEmpty(newUsername) || chosenRole == null)
            {
                MessageBox.Show("Username and role are required.");
                return;
            }

            string newRole = chosenRole.Content.ToString();

            try
            {
                UserDatabase.UpdateUser(selectedUser.Id, newUsername, newRole);
                RefreshUserList();
                MessageBox.Show("User updated.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Updates the database password for the selected user.
        /// </summary>
        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            if (string.IsNullOrEmpty(NewPasswordBox.Password))
            {
                MessageBox.Show("Enter a new password.");
                return;
            }

            UserDatabase.UpdatePassword(selectedUser.Id, NewPasswordBox.Password);
            NewPasswordBox.Password = "";
            MessageBox.Show("Password updated.");
        }

        /// <summary>
        /// Deletes the selected user from the database upon confirmation.
        /// </summary>
        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Delete user '{selectedUser.Username}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                UserDatabase.DeleteUser(selectedUser.Id);
                RefreshUserList();
            }
        }

        /// <summary>
        /// Switches display to the user registration screen.
        /// </summary>
        private void ShowNewUserScreen_Click(object sender, RoutedEventArgs e)
        {
            ShowScreen(NewUserScreen);
        }

        /// <summary>
        /// Registers a new user with provided input values.
        /// </summary>
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = NewUsername.Text.Trim();
            string password = NewPassword.Password;
            ComboBoxItem chosenRole = NewRole.SelectedItem as ComboBoxItem;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || chosenRole == null)
            {
                MessageBox.Show("All fields are required.");
                return;
            }

            string role = chosenRole.Content.ToString();

            try
            {
                UserDatabase.Register(username, password, role);
                MessageBox.Show("User registered.");
                NewUsername.Clear();
                NewPassword.Clear();
                NewRole.SelectedItem = null;

                ShowScreen(AdminScreen);
                RefreshUserList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Returns display to the admin screen from registration.
        /// </summary>
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            ShowScreen(AdminScreen);
            RefreshUserList();
        }

        /// <summary>
        /// Returns display to the login screen and resets inputs.
        /// </summary>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            UsernameBox.Text = "";
            PasswordBox.Password = "";
            ShowScreen(LoginScreen);
        }

        /// <summary>
        /// Manages visibility across all screens.
        /// </summary>
        private void ShowScreen(UIElement screenToShow)
        {
            LoginScreen.Visibility = Visibility.Collapsed;
            AdminScreen.Visibility = Visibility.Collapsed;
            NewUserScreen.Visibility = Visibility.Collapsed;
            UserScreen.Visibility = Visibility.Collapsed;

            screenToShow.Visibility = Visibility.Visible;
        }

        // ---------------- Sensor Settings (Admin) ----------------

        private void LoadSettingsIntoForm()
        {
            try
            {
                DashboardSettings settings = SensorsDatabase.GetSettings();

                MinTempBox.Text = settings.MinTemp.ToString();
                MaxTempBox.Text = settings.MaxTemp.ToString();
                GraphCountBox.Text = settings.GraphCount.ToString();

                foreach (ComboBoxItem item in DefaultGranularityBox.Items)
                {
                    if (item.Content.ToString() == settings.DefaultGranularity)
                    {
                        DefaultGranularityBox.SelectedItem = item;
                        break;
                    }
                }

                SettingsUpdatedText.Text = string.IsNullOrEmpty(settings.UpdatedBy)
                    ? ""
                    : $"Last updated by {settings.UpdatedBy} at {settings.UpdatedAt}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(MinTempBox.Text, out double minTemp) ||
                !double.TryParse(MaxTempBox.Text, out double maxTemp))
            {
                MessageBox.Show("Min and Max temperature must be numbers.");
                return;
            }

            if (minTemp >= maxTemp)
            {
                MessageBox.Show("Min temperature must be less than Max temperature.");
                return;
            }

            if (!int.TryParse(GraphCountBox.Text, out int graphCount) ||
                graphCount < 1 || graphCount > 8)
            {
                MessageBox.Show("Graph count must be a whole number between 1 and 8.");
                return;
            }

            ComboBoxItem granularityItem = DefaultGranularityBox.SelectedItem as ComboBoxItem;

            if (granularityItem == null)
            {
                MessageBox.Show("Choose a default interval.");
                return;
            }

            DashboardSettings settings = new DashboardSettings
            {
                MinTemp = minTemp,
                MaxTemp = maxTemp,
                GraphCount = graphCount,
                DefaultGranularity = granularityItem.Content.ToString()
            };

            try
            {
                SensorsDatabase.SaveSettings(settings, currentUsername);
                LoadSettingsIntoForm();
                MessageBox.Show("Settings saved.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void RefreshLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogGrid.ItemsSource = SensorsDatabase.GetDashboardLog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // ---------------- Dashboard (User) ----------------

        private void OpenUserScreen()
        {
            ShowScreen(UserScreen);

            if (!userScreenReady)
            {
                userScreenReady = true;

                List<LocationRecord> locations = new List<LocationRecord>
                {
                    new LocationRecord { Id = 0, Display = "All locations" }
                };

                try
                {
                    locations.AddRange(SensorsDatabase.GetAllLocations());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                LocationFilterBox.ItemsSource = locations;
                LocationFilterBox.SelectedIndex = 0;

                DashboardSettings settings;

                try
                {
                    settings = SensorsDatabase.GetSettings();
                }
                catch
                {
                    settings = new DashboardSettings
                    {
                        MinTemp = 5,
                        MaxTemp = 30,
                        GraphCount = 2,
                        DefaultGranularity = "Monthly"
                    };
                }

                foreach (ComboBoxItem item in UserGranularityBox.Items)
                {
                    if (item.Content.ToString() == settings.DefaultGranularity)
                    {
                        UserGranularityBox.SelectedItem = item;
                        break;
                    }
                }

                if (UserGranularityBox.SelectedItem == null)
                {
                    UserGranularityBox.SelectedIndex = 2; // Monthly
                }
            }

            RefreshDashboard();
        }

        private void ReadingFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (userScreenReady)
            {
                RefreshDashboard();
            }
        }

        private void RefreshDashboard()
        {
            LocationRecord location = LocationFilterBox.SelectedItem as LocationRecord;
            ComboBoxItem granularityItem = UserGranularityBox.SelectedItem as ComboBoxItem;

            if (location == null || granularityItem == null)
            {
                return;
            }

            Granularity granularity = (Granularity)Enum.Parse(
                typeof(Granularity), granularityItem.Content.ToString());

            DashboardSettings settings;

            try
            {
                settings = SensorsDatabase.GetSettings();
            }
            catch
            {
                settings = new DashboardSettings { MinTemp = 5, MaxTemp = 30, GraphCount = 2 };
            }

            List<ReadingAggregate> series;

            try
            {
                series = SensorsDatabase.GetAggregates(location.Id, granularity);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            List<string> labels = series.Select(r => r.Period).ToList();
            List<double> avgs = series.Select(r => r.AvgTemp).ToList();
            List<double> mins = series.Select(r => r.MinTemp).ToList();
            List<double> maxs = series.Select(r => r.MaxTemp).ToList();
            List<double> samples = series.Select(r => (double)r.Samples).ToList();

            BuildChartTiles(settings.GraphCount, labels, avgs, mins, maxs, samples,
                             settings.MinTemp, settings.MaxTemp);

            try
            {
                SensorsDatabase.RecordDashboard(new DashboardSnapshot
                {
                    ViewedBy = currentUsername,
                    Location = location.Display,
                    Granularity = granularity.ToString(),
                    GraphCount = settings.GraphCount,
                    Buckets = series.Count,
                    AvgTemp = series.Count == 0 ? 0 : Math.Round(avgs.Average(), 2)
                });
            }
            catch
            {
                // Non-blocking log failure
            }
        }

        private void BuildChartTiles(
            int graphCount,
            List<string> labels,
            List<double> avgs,
            List<double> mins,
            List<double> maxs,
            List<double> samples,
            double minTemp,
            double maxTemp)
        {
            ChartHost.Children.Clear();

            for (int i = 0; i < graphCount; i++)
            {
                ChartTile tile = new ChartTile();

                if (i == 0)
                {
                    tile.ShowBand("Avg / Min / Max Temp", labels, mins, maxs, avgs, minTemp, maxTemp);
                }
                else if (i == 1)
                {
                    tile.ShowBars("Sample Count", labels, samples);
                }
                else if (i == 2)
                {
                    tile.ShowPie("Sample Count", labels, samples);
                }
                else
                {
                    tile.ShowPlaceholder("Chart " + (i + 1));
                }

                ChartHost.Children.Add(tile);
            }
        }
    }
}