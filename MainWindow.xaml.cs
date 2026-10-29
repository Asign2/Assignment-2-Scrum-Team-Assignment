using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Assign_2
{
    /// <summary>
    /// 
    /// </summary>
    public partial class MainWindow : Window
    {
        private UserRecord selectedUser;
        private string currentUsername;
        private bool adminScreenReady;
        private bool userScreenReady;
        /// <summary>
        /// 
        /// </summary>
        public MainWindow() : this(true)
        {
        }

        /// <summary>
        /// Allows tests to create the window without starting
        /// the database initialization and generation loop.
        ///Means that Tests will still run fast and not require the database to be initialized.
        /// </summary>
        /// <param name="startGeneration"></param>
        public MainWindow(bool startGeneration)
        {
            InitializeComponent();

            if (startGeneration)
            {
                SensorsDatabase.Initialize();

                Generation generator = new Generation();
                generator.DataGenerated += () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        RedrawChartTiles();
                    });
                };
                //Discards the first generated temperature to avoid wrong readings for future use.
                _ = generator.generatePeriodicTemperature();
            }
        }
        //Stores existing graphs, for updating their temp.
        private Dictionary<int, (ChartTile Line, ChartTile2 Bar, BoxPlot Box)> chartTiles = new();

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
            ShowIDBox.Text = selectedUser.Id.ToString();
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

            try {
                bool success = UserDatabase.UpdateUser(
                      selectedUser.Id,
                      newUsername,
                      newRole,
                      newFirstName,
                      newLastName,
                      newDateCreated
                  );


                if (!success)
                {
                    MessageBox.Show("Failed to update user");
                    return;
                }
                //Reuses Old Reset Password Code now embedded inside SaveChanges 
                if (!string.IsNullOrEmpty(NewPasswordBox.Password)) {
                    UserDatabase.UpdatePassword(selectedUser.Id, NewPasswordBox.Password);
                    NewPasswordBox.Password = "";
                }
                RefreshUserList();
                MessageBox.Show("User Updated Successfully");
            }
            catch (Exception ex) 
                {
                    MessageBox.Show(ex.Message);
                }
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
        /// <param name="sender">The return button that was Clicked</param>
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

        /// <summary>
        /// Loads the saved dashboard settings into the admin form fields.
        /// </summary>
        private void LoadSettingsIntoForm()
        {
            try
            {
                DashboardSettings settings = SensorsDatabase.GetSettings();

               

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

        /// <summary>
        /// Shows the user screen, loads locations once, and applies admin defaults.
        /// </summary>
        private void OpenUserScreen()
        {
            ShowScreen(UserScreen);
            userScreenReady = false; 
            if (!userScreenReady)
            {

                List<LocationRecord> locations = new List<LocationRecord>();

                try
                {
                    locations.AddRange(SensorsDatabase.GetAllLocations());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                //Tree View MultiSelect Combo Box logic.
                
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
                        Content = "Floor " + floorGroup.Key,
                        Tag = floorGroup.Key,
                        IsChecked = true,

                    };
                    floorCheckBox.Click += FloorCheckBox_Click;
                    floorCheckBox.Click += LocationTreeView_SelectedItemChanged;

                    TreeViewItem floorItem = new TreeViewItem { Header = floorCheckBox };

                    foreach (var location in floorGroup)
                    {
                        CheckBox roomCheckBox = new CheckBox
                        {
                            Content = "Room " + location.Room,
                            Tag = location.Id,
                            IsChecked = true

                        };
                        TreeViewItem locationItem = new TreeViewItem { Header = roomCheckBox, Tag = location };
                        roomCheckBox.Click += RoomCheckBox_Click;
                        roomCheckBox.Click += LocationTreeView_SelectedItemChanged;

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
            userScreenReady = true;
            RefreshDashboard();
        }

        /// <summary>
        /// Exports a simplified version of the temperature data for a specific location and granularity to a JSON file.
        /// </summary>
        /// <param name="sender">Export Temperature Data button</param>
        /// <param name="e">The button was clicked.</param>
        private void ExportTemperatureData_Click(object sender, RoutedEventArgs e)
        {
            List<int> locations = SelectedLocations();
            var granularityItem = UserGranularityBox.SelectedItem as ComboBoxItem;
            if (locations.Count == 0 || granularityItem == null)
            {
                MessageBox.Show("Please select a location and granularity.");
                return;
            }

            var granularity = Enum.Parse<Granularity>(granularityItem.Content.ToString());

            // Project and explicitly round each value
            var series = SensorsDatabase.GetAggregates(locations, granularity)
                .Select(r => new
                {
                    r.Location,
                    r.Period,
                    AvgTemp = Math.Round(r.AvgTemp, 2),
                    MinTemp = Math.Round(r.MinTemp, 2),
                    MaxTemp = Math.Round(r.MaxTemp, 2),
                    r.Samples
                }).ToList();

            JsonExport.SaveJson($"TemperatureData_{granularity}.json", new
            {

                Location = string.Join(", ", locations),
                Granularity = granularity.ToString(),
                ExportedAt = DateTime.UtcNow,
                Readings = series
            });
        }

        /// <summary>
        /// Exports all Temperature Data from the database to a JSON file.
        /// </summary>
        /// <param name="sender">Export all data json</param>
        /// <param name="e">The button was clicked.</param>
        private void ExportAllDataJson_Click(object sender, RoutedEventArgs e)
        {
            JsonExport.SaveJson("AllSensorData.json", new
            {
                Locations = JsonExport.Query("SELECT * FROM dbo.Locations"),
                Sensors = JsonExport.Query("SELECT * FROM dbo.Sensors"),
                Data = JsonExport.Query("SELECT * FROM dbo.Data")
            });
        }

        /// <summary>
        /// Uses Existing Table to export the dashboard log to a JSON file.
        /// </summary>
        /// <param name="sender">Export user log was exported</param>
        /// <param name="e">The button was clicked</param>
        private void ExportUserLog(object sender, RoutedEventArgs e)
        {
            JsonExport.SaveJson("DashboardLog.json", JsonExport.Query("SELECT * FROM dbo.DashboardLog"));
        }

        /// <summary>
        /// The chart gets enlarged in an overlay.
        /// </summary>
        /// <param name="sender">Chart Clicked</param>
        /// <param name="e">Event data.</param>
        private void ChartTile_Clicked(object sender, EventArgs e)
        {
            ChartTile source = sender as ChartTile;
            if (source == null) return;

            OverlayContentHost.Children.Clear();
            
            OverlayContentHost.Children.Add(source.CreateEnlargedCopy());

            ChartOverlay.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Closes the enlarged chart overlay.
        /// </summary>
        /// <param name="sender">Exit button clicked.</param>
        /// <param name="e">Event data</param>
        private void CloseChartOverlay_Click(object sender, RoutedEventArgs e)
        {
            ChartOverlay.Visibility = Visibility.Collapsed;
            OverlayContentHost.Children.Clear();
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
        public void RefreshDashboard()
        {
            List<int> locations = SelectedLocations();
            ComboBoxItem granularityItem = UserGranularityBox.SelectedItem as ComboBoxItem;

            if ( granularityItem == null)
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
                    DefaultGranularity = "Monthly",
                    ShowCurrentTemp = true,
                    ShowTempComparison = true,
                    ShowTempReading = true
                };
            }

            List<ReadingAggregate> series;

            try
            {
                series = SensorsDatabase.GetAggregates(locations, granularity);
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
            BuildChartTiles(locations, granularity);

            try
            {
                SensorsDatabase.RecordDashboard(new DashboardSnapshot
                {
                    ViewedBy = currentUsername,
                    Location = string.Join(", ", locations),
                    Granularity = granularity.ToString(),
                    AvgTemp = series.Count == 0 ? 0 : Math.Round(avgs.Average(), 2)
                });
            }
            catch
            {
                // Logging failure should not block the dashboard from showing.
            }
        }

        /// <summary>
        /// Rebuilds the ChartHost panel with as many tiles as the admin's.
        /// graph count allows. The first two slots are the average/min/max.
        /// band and the sample-count bars; extra slots are placeholders.
        /// ready for future visualisations.
        /// </summary>
        /// <param name="locations">list of locations that the charts will represent</param>
        /// <param name="granularity">the granularity of the data (e.g., Daily, Weekly, Monthly)</param>
        private void BuildChartTiles(List<int> locations, Granularity granularity)
        {
            ChartHost.Children.Clear();
            ChartHost.RowDefinitions.Clear();
            ChartHost.ColumnDefinitions.Clear();

            chartTiles.Clear();

            List<LocationRecord> allLocations = SensorsDatabase.GetAllLocations();

            int rowIndex = 0;
            foreach (int location in locations)
            {
                List<(DateTime Period, double Temperature)> readings;

                bool isActive = SensorsDatabase.IsLocationActive(location);
                try
                {
                    readings = SensorsDatabase.GetChartReadings(location, granularity);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

                if (readings.Count == 0) continue;


                string granularityName = granularity.ToString();
                

                List<string> labels = readings.Select(reading => reading.Period.ToString(
                    granularityName == "Daily" ? "HH:mm" :
                    granularityName == "Weekly" ? "dd MMM" :
                    granularityName == "Monthly" ? "MMM yyyy" :
                    "yyyy")).ToList();

                List<double> temperatures = readings.Select(reading => reading.Temperature).ToList();
                DashboardSettings settings = SensorsDatabase.GetSettings();

                // 1. Add row to main host
                ChartHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // 2. Create sub-grid with your 5* (Line), 1.5* (Bar), 1.5* (Box Plot) proportions
                Grid roomGrid = new Grid();
                roomGrid.Margin = new Thickness(0, 0, 0, 10);
                roomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Star) });   // Line (Wide)
                roomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // Bar
                roomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // Box Plot

                ChartTile topTile = null;
                ChartTile2 tile2 = null;
                BoxPlot boxPlotTile = null;
                // --- Column 0: Line Graph ---
                if (settings.ShowTempReading)
                {
                    Debug.WriteLine(location);
                    topTile = new ChartTile();
                    topTile.UpdateStatus(isActive);
                    topTile.ShowLine($"Floor {allLocations.First(l => l.Id == location).Floor}. Room {allLocations.First(l => l.Id == location).Room}", labels, temperatures, null, null);
                    topTile.Clicked += ChartTile_Clicked;

                    Grid.SetColumn(topTile, 0);
                    roomGrid.Children.Add(topTile);
                }

                // --- Column 1: Bar / Current Temperature ---
                if (settings.ShowCurrentTemp)
                {
                    tile2 = new ChartTile2();
                    tile2.UpdateStatus(isActive);
                    if (temperatures.Count > 0)
                    {
                        tile2.UpdateValue(temperatures.Last());
                    }
                    Grid.SetColumn(tile2, 1);
                    roomGrid.Children.Add(tile2);
                }

                // --- Column 2: Box Plot ---
                if (settings.ShowTempComparison)
                {
                    boxPlotTile = new BoxPlot();
                    boxPlotTile.UpdateStatus(isActive);
                    boxPlotTile.ShowBoxPlot(temperatures);
                    Grid.SetColumn(boxPlotTile, 2);
                    roomGrid.Children.Add(boxPlotTile);
                }
                
                
                chartTiles[location] = (topTile, tile2, boxPlotTile);

                Grid.SetRow(roomGrid, rowIndex);
                ChartHost.Children.Add(roomGrid);

                rowIndex++;
            }
        }

        /// <summary>
        /// When the selected items in the tree view change it gets called to refresh the dashboard.
        /// </summary>
        /// <param name="sender">Changes in the tree-view multi-select.</param>
        /// <param name="e">The event data.</param>
        private void LocationTreeView_SelectedItemChanged(object sender, RoutedEventArgs e)
        {
            if (userScreenReady)
            {
                RefreshDashboard();
            }
        }

        /// <summary>
        /// Changes the room checkbox automatically when its floor is ticked or not ticked.
        /// e.g. if you select or deselect a floor it will change all rooms under it appropriately.
        /// </summary>
        /// <param name="sender">if a floor checkbox is ticked.</param>
        /// <param name="e">The data for floor checkbox ticked.</param>
        private void FloorCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var floorKey = checkBox.Tag;
                if (checkBox.IsChecked == true)
                {
                    foreach (TreeViewItem floorItem in LocationTreeView.Items)
                    {
                        if (floorItem.Header is CheckBox floorCheckBox && floorCheckBox.Tag.Equals(floorKey))
                        {
                            foreach (TreeViewItem roomItem in floorItem.Items)
                            {
                                if (roomItem.Header is CheckBox roomCheckBox)
                                {
                                    roomCheckBox.IsChecked = true;
                                    roomCheckBox.Click += RoomCheckBox_Click;

                                }
                            }
                            break;
                        }
                    }

                }
                else if (checkBox.IsChecked == false)
                {
                    foreach (TreeViewItem floorItem in LocationTreeView.Items)
                    {
                        if (floorItem.Header is CheckBox floorCheckBox && floorCheckBox.Tag.Equals(floorKey))
                        {
                            foreach (TreeViewItem roomItem in floorItem.Items)
                            {
                                if (roomItem.Header is CheckBox roomCheckBox)
                                {
                                    roomCheckBox.IsChecked = false;
                                    roomCheckBox.Click -= RoomCheckBox_Click;

                                }
                            }
                            break;
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Changes the floor checkbox automatically when its rooms are changed
        /// e.g. if a you deselect a room its floor will also be deselected with other rooms selected.
        /// e.g. if you select all rooms under a floor it will then be selected.
        /// </summary>
        /// <param name="sender">if a room checkbox is ticked.</param>
        /// <param name="e">The data for room checkbox ticked.</param>
        private void RoomCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox roomCheckBox)
            {
                var roomItem = roomCheckBox.Parent as TreeViewItem;
                var floorItem = roomItem?.Parent as TreeViewItem;
                if (floorItem != null)
                {
                    bool allRoomsChecked = true;
                    foreach (TreeViewItem item in floorItem.Items)
                    {
                        if (item.Header is CheckBox cb && cb.IsChecked == false)
                        {
                            allRoomsChecked = false;
                            break;
                        }
                    }
                    if (floorItem.Header is CheckBox floorCheckBox)
                    {
                        floorCheckBox.IsChecked = allRoomsChecked;
                    }
                }
            }

        }

        /// <summary>
        /// This will return a list for all the selected room location ints.
        /// </summary>
        /// <returns> room int locations.</returns>
        public List<int> SelectedLocations()
        {
            var selectedLocations = new List<int>();
            foreach (TreeViewItem floorItem in LocationTreeView.Items)
            {
                foreach (TreeViewItem roomItem in floorItem.Items)
                {
                    if (roomItem.Header is CheckBox roomCheckBox && roomCheckBox.IsChecked == true)
                    {
                        if (roomItem.Tag is LocationRecord location)
                        {
                            selectedLocations.Add(location.Id);
                        }
                    }
                }
            }
            return selectedLocations;
        }   

        private void ToggleSensor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is SensorRecord sensor) 
            {
                bool newStatus;
                if (sensor.Active)
                {
                    newStatus = false;
                }
                else
                {
                    newStatus = true;
                }
                SensorsDatabase.UpdateActivity(newStatus, sensor.Id);

                SensorsGrid.ItemsSource = SensorsDatabase.GetAllSensors();
                
                //updates the activty indicator on the graphs,
                //this is to catch if all sensors are deactivated as then redraw wont be caleld.
                RedrawChartTiles();
            }
        }
        
        /// <summary>
        /// Redraws the graphs that are already generated.
        /// </summary>
        public void RedrawChartTiles()
        {
            List<int> locations = SelectedLocations();
            ComboBoxItem granularityItem = UserGranularityBox.SelectedItem as ComboBoxItem;
            if (locations.Count == 0 || granularityItem == null)
            {
                return;
            }
            Granularity granularity = (Granularity)Enum.Parse(
                typeof(Granularity), granularityItem.Content.ToString());

            List<LocationRecord> allLocations = SensorsDatabase.GetAllLocations();
            DashboardSettings settings = SensorsDatabase.GetSettings();
            foreach (LocationRecord location in allLocations) {

                List<(DateTime Period, double Temperature)> readings = SensorsDatabase.GetChartReadings(location.Id, granularity);
                //Checks if a location has readings but no graph.
                //if true then calls BuildChartTiles




                if (!chartTiles.ContainsKey(location.Id) && readings.Count > 0) 
                {
                    BuildChartTiles(locations, granularity);
                    return;
                }
                //Checks if a Location has 0 readings, continues if not.
                if (readings.Count == 0) { 
                    continue;
                }
    
                bool isActive = SensorsDatabase.IsLocationActive(location.Id);

                var tiles = chartTiles[location.Id];

                tiles.Line?.UpdateStatus(isActive);
                tiles.Bar?.UpdateStatus(isActive);
                tiles.Box?.UpdateStatus(isActive);

                List<string> labels = readings.Select(reading => reading.Period.ToString(
                    granularity.ToString() == "Daily" ? "HH:mm" :
                    granularity.ToString() == "Weekly" ? "dd MMM" :
                    granularity.ToString() == "Monthly" ? "MMM yyyy" :
                    "yyyy")).ToList();

                List<double> temperatures = readings.Select(reading => reading.Temperature).ToList();

                if (tiles.Line != null && settings.ShowTempReading) { 
                    string Title = $"Floor {allLocations.First(l => l.Id == location.Id).Floor}." +
                                   $"Room {allLocations.First(l => l.Id == location.Id).Room}";
                    

                    tiles.Line.ShowLine(Title, labels, temperatures, null, null);
                }

                if(tiles.Bar != null && settings.ShowCurrentTemp && temperatures.Count > 0)
                {
                    tiles.Bar.UpdateValue(temperatures.Last());
                }

                if(tiles.Box != null && settings.ShowTempComparison)
                {
                    tiles.Box.ShowBoxPlot(temperatures);
                }
            }
        }
    }
}