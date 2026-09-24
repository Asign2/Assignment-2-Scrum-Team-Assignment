using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

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

            
                SensorsDatabase.Initialize();
            
        }
        /// <summary>
        /// Compares users account input to the database account credentials.
        /// If matches then sends user to their account role's screen.
        /// </summary>
        /// <param name="sender">The login button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password;
            string role;

            try
            {
                bool success = UserDatabase.Login(username, password, out role);

                if (success == false)
                {
                    MessageBox.Show("Invalid username or password.");
                    return;
                }

                currentUsername = username;

                if (role == "Admin")
                {
                    OpenAdminScreen();
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
        /// Redundant method
        /// change all  OpenAdminScreen(); to just ShowScreen(AdminScreen);
        /// </summary>
        private void OpenAdminScreen()
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

        /// <summary>
        /// Refreshes the user grid when it is updated/changed.
        /// </summary>
        private void RefreshUserList()
        {
            UsersGrid.ItemsSource = UserDatabase.GetAllUsers();
        }

        /// <summary>
        /// Grabs the values from the box clicked to input it into the edit account input boxes.
        /// </summary>
        /// <param name="sender">The box on the grid that was Clicked</param>
        /// <param name="e">The event data.</param>
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
                }
            }
        }

        /// <summary>
        /// Commit the changes that were in the edit/update account input boxes.
        /// </summary>
        /// <param name="sender">The save changes button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            string newUsername = EditUsernameBox.Text.Trim();
            ComboBoxItem chosenRole = RoleBox.SelectedItem as ComboBoxItem;

            if (newUsername == "" || chosenRole == null)
            {
                MessageBox.Show("Username and role are required.");
                return;
            }

            string newRole = chosenRole.Content.ToString();

            // Pull remaining values directly from form controls or keep original values from selectedUser
            string newFirstName = EditFirstNameBox.Text.Trim(); // Replace with your actual TextBox name
            string newLastName = EditLastNameBox.Text.Trim();   // Replace with your actual TextBox name
            DateTime newDateCreated = selectedUser.date_created; // Preserves the original creation date

            try
            {
                UserDatabase.UpdateUser(
                    selectedUser.Id,
                    newUsername,
                    newRole,
                    newFirstName,
                    newLastName,
                    newDateCreated
                );

                RefreshUserList();
                MessageBox.Show("User updated.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// Updates the database with the password from the input box.
        /// </summary>
        /// <param name="sender">The reset password button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            if (NewPasswordBox.Password == "")
            {
                MessageBox.Show("Enter a new password.");
                return;
            }

            UserDatabase.UpdatePassword(selectedUser.Id, NewPasswordBox.Password);
            NewPasswordBox.Password = "";
            MessageBox.Show("Password updated.");
        }

        /// <summary>
        /// Deletes the selected user from the database. 
        /// </summary>
        /// <param name="sender">The delete user button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Select a user first.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Delete user '" + selectedUser.Username + "'?",
                "Confirm Delete",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                UserDatabase.DeleteUser(selectedUser.Id);
                RefreshUserList();
            }
        }

        /// <summary>
        /// Swaps the Display to the new user registration screen. 
        /// </summary>
        /// <param name="sender">The show new user button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void ShowNewUserScreen_Click(object sender, RoutedEventArgs e)
        {
            ShowScreen(NewUserScreen);
        }
        /// <summary>
        /// Registers a new user to the database with the inputted values from the new user registration screen.
        /// </summary>
        /// <param name="sender">The register new user button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = NewUsername.Text.Trim();
            string password = NewPassword.Password;
            string firstname = NewFirstname.Text.Trim();
            string lastname = NewLastname.Text.Trim();
            string datecreated = DateTime.Now.Date.ToShortDateString();
            ComboBoxItem chosenRole = NewRole.SelectedItem as ComboBoxItem;

            if (username == "" || password == "" || chosenRole == null || firstname == "" || lastname == "" || datecreated == null)
            {
                MessageBox.Show("All fields are required.");
                return;
            }

            string role = chosenRole.Content.ToString();

            try
            {
                UserDatabase.Register(username, password, role, firstname, lastname, datecreated);
                MessageBox.Show("User registered.");
                NewUsername.Clear();
                NewPassword.Clear();
                NewFirstname.Clear();
                NewLastname.Clear();
                NewRole.SelectedItem = null;
                OpenAdminScreen();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// Returns the display to the admin screen from the new user registration screen.
        /// </summary>
        /// <param name="sender">The retunr button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            OpenAdminScreen();
        }
        /// <summary>
        /// Returns the display to the login screen from any other screen and clears the input boxes.
        /// </summary>
        /// <param name="sender">The logout button that was Clicked</param>
        /// <param name="e">The event data.</param>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            UsernameBox.Text = "";
            PasswordBox.Password = "";
            ShowScreen(LoginScreen);
        }

        /// <summary>
        /// Handles the visibility of the screens, hiding all other screens and showing the one passed in.
        /// </summary>
        /// <param name="screenToShow">The screen that will be shown.</param>
        private void ShowScreen(UIElement screenToShow)
        {
            LoginScreen.Visibility = Visibility.Collapsed;
            AdminScreen.Visibility = Visibility.Collapsed;
            NewUserScreen.Visibility = Visibility.Collapsed;
            UserScreen.Visibility = Visibility.Collapsed;

            screenToShow.Visibility = Visibility.Visible;
        }

        // ---------------- Sensor Settings (Admin) ----------------

        /// <summary>Loads the saved dashboard settings into the admin form fields.</summary>
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
                    }
                }

                SettingsUpdatedText.Text = string.IsNullOrEmpty(settings.UpdatedBy)
                    ? ""
                    : "Last updated by " + settings.UpdatedBy + " at " + settings.UpdatedAt;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Validates and saves the admin's temperature range, graph count and
        /// default interval to dbo.DashboardSettings.
        /// </summary>
        /// <param name="sender">The save settings button that was Clicked</param>
        /// <param name="e">The event data.</param>
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

        /// <summary>
        /// Reloads the dashboard view log grid.
        /// </summary>
        /// <param name="sender">The refresh log button that was Clicked</param>
        /// <param name="e">The event data.</param>
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

        /// <summary>
        /// Shows the user screen, loads locations once, and applies admin defaults.
        /// </summary>
        private void OpenUserScreen()
        {
            ShowScreen(UserScreen);

            if (!userScreenReady)
            {
                userScreenReady = true;

                List<LocationRecord> locations = new List<LocationRecord>();
             
                try
                {
                    locations.AddRange(SensorsDatabase.GetAllLocations());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                LocationFilterBox.ItemsSource = locations;
                if (locations.Count > 0)
                {
                    LocationFilterBox.SelectedIndex = 0;
                }

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
                    }
                }

                if (UserGranularityBox.SelectedItem == null)
                {
                    UserGranularityBox.SelectedIndex = 2; // Monthly
                }
            }

            RefreshDashboard();
        }


        private void ChartTile_Clicked(object sender, EventArgs e)
        {
            ChartTile source = sender as ChartTile;
            if (source == null) return;

            OverlayContentHost.Children.Clear();
            OverlayContentHost.Children.Add(source.CreateEnlargedCopy());
            ChartOverlay.Visibility = Visibility.Visible;
        }

        private void CloseChartOverlay_Click(object sender, RoutedEventArgs e)
        {
            ChartOverlay.Visibility = Visibility.Collapsed;
        }




        /// <summary>
        /// Re-runs the dashboard whenever the location or interval filter changes.
        /// </summary>
        /// <param name="sender">The filter control that changed.</param>
        /// <param name="e">The event data.</param>
        private void ReadingFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (userScreenReady)
            {
                RefreshDashboard();
            }
        }

        /// <summary>
        /// Loads the aggregated readings for the selected location/interval and
        /// fills the chart tiles, sized by the admin's graph count setting.
        /// </summary>
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

            using var connection = UserDatabase.OpenConnection();
            BuildChartTiles(connection);    

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
                // Logging failure should not block the dashboard from showing.
            }
        }

        /// <summary>
        /// Rebuilds the ChartHost panel with as many tiles as the admin's
        /// graph count allows. The first two slots are the average/min/max
        /// band and the sample-count bars; extra slots are placeholders
        /// ready for future visualisations.
        /// </summary>
        private void BuildChartTiles(SqlConnection connection)
        {
            List<string> labels = new List<string>();
            List<double> temperatures = new List<double>();
            string granularity = (UserGranularityBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string query = granularity switch {
                "Hourly" => @"
                    SELECT 
                        DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0) AS Period,
                        CAST(AVG(Temperature) AS DECIMAL(5,2)) AS Temperature
                    FROM dbo.Data
                    WHERE Sensor_Id = 1
                    GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0)
                    ORDER BY Period",

                "Daily" => @"
                    SELECT 
                        DATEADD(DAY, DATEDIFF(DAY, 0, Timestamp), 0) AS Period,
                        CAST(AVG(Temperature) AS DECIMAL(5,2)) AS Temperature
                    FROM dbo.Data
                    WHERE Sensor_Id = 1
                    GROUP BY DATEADD(DAY, DATEDIFF(DAY, 0, Timestamp), 0)
                    ORDER BY Period",

                "Monthly" => @"
                    SELECT 
                        DATEADD(MONTH, DATEDIFF(MONTH, 0, Timestamp), 0) AS Period,
                        CAST(AVG(Temperature) AS DECIMAL(5,2)) AS Temperature
                    FROM dbo.Data
                    WHERE Sensor_Id = 1
                    GROUP BY DATEADD(MONTH, DATEDIFF(MONTH, 0, Timestamp), 0)
                    ORDER BY Period",

                "Yearly" => @"
                    SELECT 
                        DATEADD(YEAR, DATEDIFF(YEAR, 0, Timestamp), 0) AS Period,
                        CAST(AVG(Temperature) AS DECIMAL(5,2)) AS Temperature
                    FROM dbo.Data
                    WHERE Sensor_Id = 1
                    GROUP BY DATEADD(YEAR, DATEDIFF(YEAR, 0, Timestamp), 0)
                    ORDER BY Period",
                null => throw new InvalidOperationException("Unknown granularity: " + granularity) 
            };


            using SqlCommand command = new SqlCommand(query, connection);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                DateTime period = reader.GetDateTime(0);
                labels.Add(period.ToString(
                    granularity == "Hourly" ? "HH:mm" :
                    granularity == "Daily" ? "dd MMM" :
                    granularity == "Monthly" ? "MMM yyyy" :
                    "yyyy")
                );

                labels.Add(reader.GetDateTime(0).ToString("HH:mm"));
                temperatures.Add((double)reader.GetDecimal(1));
            }

            ChartHost.Children.Clear();

            ChartHost.Children.Clear();

            // Top Row
            ChartTile topTile = new ChartTile();
            Grid.SetRow(topTile, 0);
            Grid.SetColumnSpan(topTile, 2);
            ChartHost.Children.Add(topTile);

            // Bottom Left
            ChartTile2 tile2 = new ChartTile2();
            Grid.SetRow(tile2, 1);
            Grid.SetColumn(tile2, 0);
            ChartHost.Children.Add(tile2);

            // Bottom Right
            ChartTile barTile = new ChartTile();
            Grid.SetRow(barTile, 1);
            Grid.SetColumn(barTile, 1);
            ChartHost.Children.Add(barTile);
        }
    }
}


