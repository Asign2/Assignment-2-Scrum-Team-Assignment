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

            // Populate the form fields with selected user data
            EditUsernameBox.Text = selectedUser.Username;
            EditFirstNameBox.Text = selectedUser.first_name;
            EditLastNameBox.Text = selectedUser.last_name;
            NewPasswordBox.Password = "";

            // Handle Role ComboBox selection
            foreach (ComboBoxItem item in RoleBox.Items)
            {
                if (item.Content.ToString() == selectedUser.Role)
                {
                    RoleBox.SelectedItem = item;
                    break; // Optional: break once found for efficiency
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
                MessageBox.Show("Cannot delete user. No user currently selected.");
                return;
            }
            else if (selectedUser.Username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Cannot delete current user. You cannot delete the account you are currently logged into.");
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

                GraphCountBox.Text = settings.GraphCount.ToString();

                foreach (ComboBoxItem item in DefaultGranularityBox.Items)
                {
                    if (item.Content.ToString() == settings.DefaultGranularity)
                    {
                        DefaultGranularityBox.SelectedItem = item;
                        break;
                    }
                }

                BarCheckbox.IsChecked = settings.ShowCurrentTemp;
                BoxplotCheckbox.IsChecked = settings.ShowTempComparison;
                LineChcekbox.IsChecked = settings.ShowTempReading;

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
            if (!int.TryParse(GraphCountBox.Text, out int graphCount) ||
                graphCount < 1 || graphCount > 8)
            {
                MessageBox.Show("Graph count must be a whole number between 1 and 8.");
                return;
            }

            ComboBoxItem granularityItem =
                DefaultGranularityBox.SelectedItem as ComboBoxItem;

            if (granularityItem == null)
            {
                MessageBox.Show("Choose a default interval.");
                return;
            }

            DashboardSettings settings = new DashboardSettings
            {
                ShowCurrentTemp = BarCheckbox.IsChecked == true,
                ShowTempComparison = BoxplotCheckbox.IsChecked == true,
                ShowTempReading = LineChcekbox.IsChecked == true,
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

                //TVMSCB Logic

                locations.Sort((a, b) =>
                {
                    int floorComparison = a.Floor.CompareTo(b.Floor);
                    if (floorComparison != 0)
                        return floorComparison;
                    return a.Room.CompareTo(b.Room);
                });

                LocationTreeView.Items.Clear();
                foreach (var floorGroup in locations.GroupBy(l => l.Floor))
                {
                    CheckBox floorCheckBox = new CheckBox
                    {
                        Content = "Floor " + floorGroup.Key, Tag = floorGroup.Key
                    };
                    TreeViewItem floorItem = new TreeViewItem { Header = floorCheckBox };
              
                    foreach (var location in floorGroup)
                    {
                        CheckBox roomCheckBox = new CheckBox
                        {
                            Content = "Room " + location.Room, Tag = location.Id
                        };
                        TreeViewItem locationItem = new TreeViewItem { Header = roomCheckBox, Tag = location };
                        
                        floorItem.Items.Add(locationItem);
                    }
                    LocationTreeView.Items.Add(floorItem);
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
                        GraphCount = 3,
                        DefaultGranularity = "Monthly",
                        ShowCurrentTemp = true,
                        ShowTempComparison = true,
                        ShowTempReading = true
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

        //Exports a simplified version of the temperature data for a specific location and granularity to a JSON file.
        private void ExportTemperatureData_Click(object sender, RoutedEventArgs e)
        {
            var location = LocationTreeView.SelectedItem as LocationRecord;
            var granularityItem = UserGranularityBox.SelectedItem as ComboBoxItem;
            if (location == null || granularityItem == null)
            {
                MessageBox.Show("Please select a location and granularity.");
                return;
            }

            var granularity = Enum.Parse<Granularity>(granularityItem.Content.ToString());
            var series = SensorsDatabase.GetAggregates(location.Id, granularity);

            JsonExport.SaveJson($"TemperatureData_{granularity}.json", new
            {
                Location = location.Display,
                Granularity = granularity.ToString(),
                ExportedAt = DateTime.UtcNow,
                Readings = series
            });
        }
        //Exports all Temprature Data from the database to a JSON file.
        private void ExportAllDataJson_Click(object sender, RoutedEventArgs e)
        {
            JsonExport.SaveJson("AllSensorData.json", new
            {
                Locations = JsonExport.Query("SELECT * FROM dbo.Locations"),
                Sensors = JsonExport.Query("SELECT * FROM dbo.Sensors"),
                Data = JsonExport.Query("SELECT * FROM dbo.Data")
            });
        }
        //Uses Existing Table to export the dashboard log to a JSON file.
        private void ExportUserLog(object sender, RoutedEventArgs e)
        {
            JsonExport.SaveJson("DashboardLog.json", JsonExport.Query("SELECT * FROM dbo.DashboardLog"));
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
            LocationRecord location = LocationTreeView.SelectedItem as LocationRecord;
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
                settings = new DashboardSettings
                {
                    GraphCount = 2,
                    DefaultGranularity = "Monthly",
                    ShowCurrentTemp = true,
                    ShowTempComparison = true,
                    ShowTempReading = true
                };
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
            BuildChartTiles(location.Id, granularity);

            try
            {
                SensorsDatabase.RecordDashboard(new DashboardSnapshot
                {
                    ViewedBy = currentUsername,
                    Location = location.Display,
                    Granularity = granularity.ToString(),
                    GraphCount = settings.GraphCount,
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
        private void BuildChartTiles(int locationId, Granularity granularity)
        {
            List<(DateTime Period, double Temperature)> readings;

            try
            {
                readings = SensorsDatabase.GetChartReadings(locationId, granularity);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            string granularityName = granularity.ToString();

            List<string> labels = readings.Select(reading => reading.Period.ToString(
                granularityName == "Daily" ? "HH:mm" :
                granularityName == "Weekly" ? "dd MMM" :
                granularityName == "Monthly" ? "MMM yyyy" :
                "yyyy")).ToList();

            List<double> temperatures = readings
                .Select(reading => reading.Temperature)
                .ToList();

            ChartHost.Children.Clear();

            DashboardSettings settings = SensorsDatabase.GetSettings();

            // Line - Temperature Reading
            if (settings.ShowTempReading)
            {
                ChartTile topTile = new ChartTile();

                topTile.ShowLine(
                    "Temperature",
                    labels,
                    temperatures,
                    null,
                    null
                );

                topTile.Clicked += ChartTile_Clicked;

                Grid.SetRow(topTile, 0);
                Grid.SetColumnSpan(topTile, 2);

                ChartHost.Children.Add(topTile);
            }

            // Bar - Current Temperature
            if (settings.ShowCurrentTemp)
            {
                ChartTile2 tile2 = new ChartTile2();

                if (temperatures.Count > 0)
                {
                    double latestTemp = temperatures.Last();

                    tile2.UpdateValue(latestTemp);
                }

                Grid.SetRow(tile2, 1);
                Grid.SetColumn(tile2, 0);

                ChartHost.Children.Add(tile2);
            }

            // Boxplot - Temperature Comparison
            if (settings.ShowTempComparison)
            {
                // Boxplot generation goes here
            }
        }
        private void LocationTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (userScreenReady)
            {
                RefreshDashboard();
            }
        }
    }
}




