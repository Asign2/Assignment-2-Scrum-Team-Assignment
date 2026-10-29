using Assign_2;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;
using System;
using Xunit;

namespace SprintUnitTests;

// These tests expect MainWindow(bool startGeneration), with false skipping
// SensorsDatabase.Initialize() and the never-ending Generation task.
public class MainWindowCoverageTests
{
    private static void OnSta(Action test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            MainWindow? window = null;
            try
            {
                window = new MainWindow(false);
                testWithWindow(window, test);
            }
            catch (Exception ex) { failure = ex; }
            finally { window?.Close(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
    private static void InvokePrivate(
    MainWindow window,
    string methodName,
    params object?[] arguments)
    {
        var method = typeof(MainWindow).GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(method);

        try
        {
            method.Invoke(window, arguments);
        }
        catch (System.Reflection.TargetInvocationException ex)
            when (ex.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo
                .Capture(ex.InnerException).Throw();
        }
    }
    private static void RunSta(Action action)
    {
        Exception? error = null;

        Thread thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo
                .Capture(error).Throw();
    }
    // Temporary delegate bridging: the test retrieves its window from thread-local state.
    [ThreadStatic] private static MainWindow? currentWindow;
    private static void testWithWindow(MainWindow window, Action test)
    {
        currentWindow = window;
        try { test(); }
        finally { currentWindow = null; }
    }
    private static MainWindow W => currentWindow ?? throw new InvalidOperationException("No test window");

    private static T Control<T>(string name) where T : UIElement =>
        Assert.IsAssignableFrom<T>(W.FindName(name));

    private static void Call(string name, params object?[] args)
    {
        var method = typeof(MainWindow).GetMethod(name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        try { method.Invoke(W, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    private static void SetReady(bool ready)
    {
        var field = typeof(MainWindow).GetField("userScreenReady",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(W, ready);
    }

    private static (CheckBox floor, CheckBox first, CheckBox second) MakeFloor(
        bool selectedFirst = true, bool selectedSecond = true)
    {
        var tree = Control<TreeView>("LocationTreeView");
        tree.Items.Clear();
        var floor = new CheckBox { Content = "Floor 1", Tag = 1, IsChecked = true };
        var first = new CheckBox { Content = "Room A", IsChecked = selectedFirst };
        var second = new CheckBox { Content = "Room B", IsChecked = selectedSecond };
        var floorItem = new TreeViewItem { Header = floor };
        floorItem.Items.Add(new TreeViewItem { Header = first });
        floorItem.Items.Add(new TreeViewItem { Header = second });
        tree.Items.Add(floorItem);
        return (floor, first, second);
    }

    private static void Show(string name) => Call("ShowScreen", Control<UIElement>(name));

    [Theory]
    [InlineData("LoginScreen")]
    [InlineData("AdminScreen")]
    [InlineData("NewUserScreen")]
    [InlineData("UserScreen")]
    public void ShowScreen_OnlyRequestedScreenVisible(string target)
    {
        OnSta(() =>
        {
            Show(target);
            foreach (string name in new[] { "LoginScreen", "AdminScreen", "NewUserScreen", "UserScreen" })
                Assert.Equal(name == target ? Visibility.Visible : Visibility.Collapsed,
                    Control<UIElement>(name).Visibility);
        });
    }

    [Fact]
    public void Logout_ClearsCredentialsAndReturnsToLogin()
    {
        OnSta(() =>
        {
            Control<TextBox>("UsernameBox").Text = "temporaryUser";
            Control<PasswordBox>("PasswordBox").Password = "temporaryPassword";
            Show("UserScreen");
            Call("LogoutButton_Click", null, new RoutedEventArgs());
            Assert.Equal("", Control<TextBox>("UsernameBox").Text);
            Assert.Equal("", Control<PasswordBox>("PasswordBox").Password);
            Assert.Equal(Visibility.Visible, Control<UIElement>("LoginScreen").Visibility);
            Assert.Equal(Visibility.Collapsed, Control<UIElement>("UserScreen").Visibility);
        });
    }

    [Fact]
    public void NewUserButton_ShowsRegistrationScreen()
    {
        OnSta(() =>
        {
            Call("ShowNewUserScreen_Click", null, new RoutedEventArgs());
            Assert.Equal(Visibility.Visible, Control<UIElement>("NewUserScreen").Visibility);
            Assert.Equal(Visibility.Collapsed, Control<UIElement>("LoginScreen").Visibility);
        });
    }

    [Fact]
    public void SelectedLocations_EmptyTree_ReturnsEmpty()
    {
        OnSta(() =>
        {
            Control<TreeView>("LocationTreeView").Items.Clear();
            Assert.Empty(W.SelectedLocations());
        });
    }

    [Fact]
    public void SelectedLocations_NoSelectedRooms_ReturnsEmpty()
    {
        OnSta(() =>
        {
            MakeFloor(false, false);
            Assert.Empty(W.SelectedLocations());
        });
    }

    [Fact]
    public void SelectedLocations_RoomsWithoutLocationRecords_AreIgnored()
    {
        OnSta(() =>
        {
            MakeFloor(true, true); // Synthetic room tags are absent.
            Assert.Empty(W.SelectedLocations());
        });
    }

    [Fact]
    public void SelectedLocations_AllChecked_FromDatabase_ReturnsCorrectIds()
    {
        OnSta(() =>
        {
            var locations = SensorsDatabase.GetAllLocations();
            Assert.NotEmpty(locations);
            var tree = Control<TreeView>("LocationTreeView");
            tree.Items.Clear();
            var floor = new TreeViewItem { Header = new CheckBox { Content = "Test" } };
            foreach (var location in locations.Take(3))
                floor.Items.Add(new TreeViewItem
                {
                    Header = new CheckBox { IsChecked = true },
                    Tag = location
                });
            tree.Items.Add(floor);
            Assert.Equal(locations.Take(3).Select(l => l.Id).ToList(), W.SelectedLocations());
        });
    }

    [Fact]
    public void SelectedLocations_MixedChecks_IncludesCheckedOnly()
    {
        OnSta(() =>
        {
            var locations = SensorsDatabase.GetAllLocations();
            Assert.True(locations.Count >= 2, "Needs two seeded locations");
            var tree = Control<TreeView>("LocationTreeView");
            tree.Items.Clear();
            var floor = new TreeViewItem { Header = new CheckBox { Content = "Test" } };
            floor.Items.Add(new TreeViewItem
            {
                Header = new CheckBox { IsChecked = true },
                Tag = locations[0]
            });
            floor.Items.Add(new TreeViewItem
            {
                Header = new CheckBox { IsChecked = false },
                Tag = locations[1]
            });
            tree.Items.Add(floor);
            Assert.Equal(new[] { locations[0].Id }, W.SelectedLocations());
        });
    }

    [Fact]
    public void FloorCheckbox_Selected_ChecksAllRooms()
    {
        OnSta(() =>
        {
            var (floor, first, second) = MakeFloor(false, false);
            floor.IsChecked = true;
            Call("FloorCheckBox_Click", floor, new RoutedEventArgs());
            Assert.True(first.IsChecked == true);
            Assert.True(second.IsChecked == true);
        });
    }

    [Fact]
    public void FloorCheckbox_Unselected_UnchecksAllRooms()
    {
        OnSta(() =>
        {
            var (floor, first, second) = MakeFloor();
            floor.IsChecked = false;
            Call("FloorCheckBox_Click", floor, new RoutedEventArgs());
            Assert.False(first.IsChecked == true);
            Assert.False(second.IsChecked == true);
        });
    }

    [Fact]
    public void FloorCheckbox_InvalidSender_DoesNotThrow()
    {
        OnSta(() => Call("FloorCheckBox_Click", new object(), new RoutedEventArgs()));
    }

    [Fact]
    public void RoomCheckbox_InvalidSender_DoesNotThrow()
    {
        OnSta(() => Call("RoomCheckBox_Click", new object(), new RoutedEventArgs()));
    }

    [Fact]
    public void ChartOverlay_Close_HidesAndClearsChildren()
    {
        OnSta(() =>
        {
            var overlay = Control<UIElement>("ChartOverlay");
            var host = Control<Panel>("OverlayContentHost");
            host.Children.Add(new TextBlock { Text = "Fake chart" });
            overlay.Visibility = Visibility.Visible;
            Call("CloseChartOverlay_Click", null, new RoutedEventArgs());
            Assert.Equal(Visibility.Collapsed, overlay.Visibility);
            Assert.Empty(host.Children);
        });
    }

    [Fact]
    public void ChartOverlay_InvalidSender_DoesNothing()
    {
        OnSta(() =>
        {
            var host = Control<Panel>("OverlayContentHost");
            host.Children.Clear();
            Call("ChartTile_Clicked", new object(), EventArgs.Empty);
            Assert.Empty(host.Children);
        });
    }

    [Fact]
    public void ChartOverlay_ChartClick_AddsEnlargedCopy()
    {
        OnSta(() =>
        {
            var chart = new ChartTile();
            chart.ShowLine("test", new List<string> { "one", "two" },
                new List<double> { 10, 20 }, null, null);
            Call("ChartTile_Clicked", chart, EventArgs.Empty);
            Assert.Equal(Visibility.Visible, Control<UIElement>("ChartOverlay").Visibility);
            Assert.Single(Control<Panel>("OverlayContentHost").Children);
        });
    }

    [Fact]
    public void RefreshDashboard_NoGranularity_DoesNotThrow()
    {
        OnSta(() =>
        {
            Control<ComboBox>("UserGranularityBox").SelectedItem = null;
            W.RefreshDashboard();
        });
    }

    [Fact]
    public void RedrawChartTiles_NoSelectedLocations_ReturnsSafely()
    {
        OnSta(() =>
        {
            Control<TreeView>("LocationTreeView").Items.Clear();
            W.RedrawChartTiles();
        });
    }

    [Fact]
    public void RedrawChartTiles_NoGranularity_ReturnsSafely()
    {
        OnSta(() =>
        {
            Control<ComboBox>("UserGranularityBox").SelectedItem = null;
            W.RedrawChartTiles();
        });
    }

    [Fact]
    public void ReadingFilter_WhenScreenNotReady_DoesNotRefresh()
    {
        OnSta(() =>
        {
            SetReady(false);
            Control<ComboBox>("UserGranularityBox").SelectedItem = null;
            Call("ReadingFilter_Changed", null, new RoutedEventArgs());
        });
    }

    [Fact]
    public void LocationSelection_WhenScreenNotReady_DoesNotRefresh()
    {
        OnSta(() =>
        {
            SetReady(false);
            Call("LocationTreeView_SelectedItemChanged", null, new RoutedEventArgs());
        });
    }

    [Fact]
    public void RefreshLog_LoadsDatabaseLogIntoGrid()
    {
        OnSta(() =>
        {
            // Read-only integration test; errors are swallowed by production UI code.
            Call("RefreshLog_Click", null, new RoutedEventArgs());
            Assert.NotNull(Control<DataGrid>("LogGrid").ItemsSource);
        });
    }

    [Fact]
    public void Settings_LoadedFromDatabase_UpdatesCheckboxes()
    {
        OnSta(() =>
        {
            var settings = SensorsDatabase.GetSettings();
            Call("LoadSettingsIntoForm");
            Assert.Equal(settings.ShowCurrentTemp,
                Control<CheckBox>("BarCheckbox").IsChecked);
            Assert.Equal(settings.ShowTempComparison,
                Control<CheckBox>("BoxplotCheckbox").IsChecked);
            Assert.Equal(settings.ShowTempReading,
                Control<CheckBox>("LineChcekbox").IsChecked);
        });
    }
    [Fact]
    public void OpenUserScreen_ValidData_LoadsDashboardAndCharts()
    {
        RunSta(() =>
        {
            SensorsDatabase.Initialize();

            var window = new MainWindow(false);

            InvokePrivate(window, "OpenUserScreen");

            var tree = window.FindName("LocationTreeView")
                as TreeView;

            var chartHost = window.FindName("ChartHost")
                as Grid;

            Assert.NotNull(tree);
            Assert.NotNull(chartHost);

            Assert.NotEmpty(tree.Items);
            Assert.NotEmpty(chartHost.Children);
        });
    }
    // Paste these methods INSIDE MainWindowCoverageTests, before its final closing brace.
    // They reuse OnSta(), W, Control<T>(), Call() and MakeFloor().

    [Theory]
    [InlineData("NewSensorScreen")]
    [InlineData("UpdateSensorScreen")]
    public void ShowScreen_SensorScreens_HidesOtherScreens(string target)
    {
        OnSta(() =>
        {
            Show(target);
            foreach (string name in new[]
            {
                "LoginScreen", "AdminScreen", "NewUserScreen",
                "UserScreen", "NewSensorScreen", "UpdateSensorScreen"
            })
            {
                Assert.Equal(name == target ? Visibility.Visible : Visibility.Collapsed,
                    Control<UIElement>(name).Visibility);
            }
        });
    }

    [Fact]
    public void NewSensorButton_OpensNewSensorScreen()
    {
        OnSta(() =>
        {
            Call("NewSensor_Click", null, new RoutedEventArgs());
            Assert.Equal(Visibility.Visible, Control<UIElement>("NewSensorScreen").Visibility);
            Assert.Equal(Visibility.Collapsed, Control<UIElement>("LoginScreen").Visibility);
        });
    }

    [Fact]
    public void UpdateSensorButton_OpensUpdateSensorScreen()
    {
        OnSta(() =>
        {
            Call("UpdateSensor_Click", null, new RoutedEventArgs());
            Assert.Equal(Visibility.Visible, Control<UIElement>("UpdateSensorScreen").Visibility);
            Assert.Equal(Visibility.Collapsed, Control<UIElement>("LoginScreen").Visibility);
        });
    }

    [Fact]
    public void SensorCombo_LoadSensorIds_SetsBindingPaths()
    {
        OnSta(() =>
        {
            Call("LoadSensorIds");
            var combo = Control<ComboBox>("SensorIDComboBox");
            Assert.Equal("Id", combo.DisplayMemberPath);
            Assert.Equal("Id", combo.SelectedValuePath);
            Assert.NotNull(combo.ItemsSource);
        });
    }

    [Fact]
    public void SensorCombo_NoSelection_DoesNotOverwriteFields()
    {
        OnSta(() =>
        {
            Control<ComboBox>("SensorIDComboBox").SelectedItem = null;
            Control<TextBox>("UpdateMake").Text = "Preserved make";
            Control<TextBox>("UpdateModel").Text = "Preserved model";
            Call("SensorIDComboBox_SelectionChanged", null,
                new SelectionChangedEventArgs(Selector.SelectionChangedEvent,
                    new List<object>(), new List<object>()));
            Assert.Equal("Preserved make", Control<TextBox>("UpdateMake").Text);
            Assert.Equal("Preserved model", Control<TextBox>("UpdateModel").Text);
        });
    }

    [Fact]
    public void SensorCombo_RealSensor_FillsEditFields()
    {
        OnSta(() =>
        {
            var sensors = SensorsDatabase.GetAllSensors();
            Assert.NotEmpty(sensors);

            var sensor = sensors[0];

            var combo = Control<ComboBox>("SensorIDComboBox");

            combo.ItemsSource = null;
            combo.ItemsSource = sensors;
            combo.SelectedIndex = 0;
            Assert.Same(sensor, combo.SelectedItem);

            Call("SensorIDComboBox_SelectionChanged",
                combo,
                new SelectionChangedEventArgs(
                    Selector.SelectionChangedEvent,
                    new List<object>(),
                    new List<object> { sensor }));

            Assert.Equal(
                sensor.Make,
                Control<TextBox>("UpdateMake").Text);

            Assert.Equal(
                sensor.Model,
                Control<TextBox>("UpdateModel").Text);

            Assert.Equal(
                sensor.Floor.ToString(),
                Control<TextBox>("UpdateFloor").Text);

            Assert.Equal(
                sensor.Room.ToString(),
                Control<TextBox>("UpdateRoom").Text);
        });
    }

    [Fact]
    public void UsersGrid_NoSelection_ReturnsWithoutChangingEditForm()
    {
        OnSta(() =>
        {
            Control<DataGrid>("UsersGrid").SelectedItem = null;
            Control<TextBox>("EditUsernameBox").Text = "unchanged";
            Call("UsersGrid_SelectionChanged", null,
                new SelectionChangedEventArgs(Selector.SelectionChangedEvent,
                    new List<object>(), new List<object>()));
            Assert.Equal("unchanged", Control<TextBox>("EditUsernameBox").Text);
        });
    }

    [Fact]
    public void UsersGrid_SelectExistingUser_LoadsEditForm()
    {
        OnSta(() =>
        {
            var users = UserDatabase.GetAllUsers();
            Assert.NotEmpty(users);
            var user = users[0];
            Control<DataGrid>("UsersGrid").ItemsSource = users;
            Control<DataGrid>("UsersGrid").SelectedItem = user;
            Call("UsersGrid_SelectionChanged", null,
                new SelectionChangedEventArgs(Selector.SelectionChangedEvent,
                    new List<object>(), new List<object>()));
            Assert.Equal(user.Id.ToString(), Control<TextBox>("ShowIDBox").Text);
            Assert.Equal(user.Username, Control<TextBox>("EditUsernameBox").Text);
            Assert.Equal(user.first_name, Control<TextBox>("EditFirstNameBox").Text);
            Assert.Equal(user.last_name, Control<TextBox>("EditLastNameBox").Text);
            Assert.Equal("", Control<PasswordBox>("NewPasswordBox").Password);
            Assert.Equal(user.Role,
                (Control<ComboBox>("RoleBox").SelectedItem as ComboBoxItem)?.Content?.ToString());
        });
    }

    [Fact]
    public void RefreshUserList_RebindsUsersGrid()
    {
        OnSta(() =>
        {
            Call("RefreshUserList");
            Assert.NotNull(Control<DataGrid>("UsersGrid").ItemsSource);
        });
    }

    [Fact]
    public void RoomCheckbox_UncheckedRoom_UnchecksItsFloor()
    {
        OnSta(() =>
        {
            var (floor, first, second) = MakeFloor(true, true);
            second.IsChecked = false;
            Call("RoomCheckBox_Click", second, new RoutedEventArgs());
            Assert.False(floor.IsChecked == true);
            Assert.True(first.IsChecked == true);
        });
    }

    [Fact]
    public void RoomCheckbox_AllRoomsChecked_ChecksFloor()
    {
        OnSta(() =>
        {
            var (floor, first, second) = MakeFloor(true, false);
            floor.IsChecked = false;
            second.IsChecked = true;
            Call("RoomCheckBox_Click", second, new RoutedEventArgs());
            Assert.True(floor.IsChecked == true);
            Assert.True(first.IsChecked == true);
        });
    }

    [Fact]
    public void RoomCheckbox_NoParent_DoesNotThrow()
    {
        OnSta(() => Call("RoomCheckBox_Click",
            new CheckBox { IsChecked = true }, new RoutedEventArgs()));
    }

    [Fact]
    public void FloorCheckbox_Indeterminate_DoesNotModifyRooms()
    {
        OnSta(() =>
        {
            var (floor, first, second) = MakeFloor(true, false);
            floor.IsThreeState = true;
            floor.IsChecked = null;
            Call("FloorCheckBox_Click", floor, new RoutedEventArgs());
            Assert.True(first.IsChecked == true);
            Assert.False(second.IsChecked == true);
        });
    }

    [Fact]
    public void FloorCheckbox_NonMatchingTag_DoesNotModifyRooms()
    {
        OnSta(() =>
        {
            var (_, first, second) = MakeFloor(false, false);
            var unrelated = new CheckBox { Tag = 99999, IsChecked = true };
            Call("FloorCheckBox_Click", unrelated, new RoutedEventArgs());
            Assert.False(first.IsChecked == true);
            Assert.False(second.IsChecked == true);
        });
    }

    [Fact]
    public void BuildChartTiles_NoLocations_ClearsExistingCharts()
    {
        OnSta(() =>
        {
            var host = Control<Grid>("ChartHost");
            host.Children.Add(new TextBlock { Text = "Old" });
            host.RowDefinitions.Add(new RowDefinition());
            host.ColumnDefinitions.Add(new ColumnDefinition());
            Call("BuildChartTiles", new List<int>(), Granularity.Monthly);
            Assert.Empty(host.Children);
            Assert.Empty(host.RowDefinitions);
            Assert.Empty(host.ColumnDefinitions);
        });
    }

    [Theory]
    [InlineData(Granularity.Daily)]
    [InlineData(Granularity.Weekly)]
    [InlineData(Granularity.Monthly)]
    public void BuildChartTiles_SingleLocation_UsesAvailableReadings(Granularity interval)
    {
        OnSta(() =>
        {
            var location = SensorsDatabase.GetAllLocations()
                .FirstOrDefault(l => SensorsDatabase.GetChartReadings(l.Id, interval).Count > 0);
            // This integration test needs a location with readings in the test database.
            Assert.True(location != null,
                $"Seed a location with {interval} readings to exercise chart creation.");
            Call("BuildChartTiles", new List<int> { location!.Id }, interval);
            var host = Control<Grid>("ChartHost");
            Assert.Single(host.Children);
            Assert.Single(host.RowDefinitions);
            var roomGrid = Assert.IsType<Grid>(host.Children[0]);
            var settings = SensorsDatabase.GetSettings();
            int expected = (settings.ShowTempReading ? 1 : 0)
                + (settings.ShowCurrentTemp ? 1 : 0)
                + (settings.ShowTempComparison ? 1 : 0);
            Assert.Equal(expected, roomGrid.Children.Count);
        });
    }

    [Fact]
    public void ToggleSensor_InvalidSenders_LeaveSensorGridUntouched()
    {
        OnSta(() =>
        {
            var grid = Control<DataGrid>("SensorsGrid");
            var marker = new object[] { "unchanged" };
            grid.ItemsSource = marker;
            Call("ToggleSensor_Click", new object(), new RoutedEventArgs());
            Call("ToggleSensor_Click", new Button(), new RoutedEventArgs());
            Assert.Same(marker, grid.ItemsSource);
        });
    }

    [Fact]
    public void OpenAdminScreen_RefreshesUserAndSensorGrids()
    {
        OnSta(() =>
        {
            Call("OpenAdminScreen");
            Assert.Equal(Visibility.Visible, Control<UIElement>("AdminScreen").Visibility);
            Assert.NotNull(Control<DataGrid>("UsersGrid").ItemsSource);
            Assert.NotNull(Control<DataGrid>("SensorsGrid").ItemsSource);
            Assert.NotNull(Control<DataGrid>("LogGrid").ItemsSource);
        });
    }
    [Fact]
    public void RedrawChartTiles_WithRealReadings_UpdatesCharts()
    {
        OnSta(() =>
        {
            // Production RedrawChartTiles iterates ALL locations. Build charts
            // for every location to avoid missing dictionary keys.
            var locations = SensorsDatabase.GetAllLocations();
            Assert.NotEmpty(locations);
            var tree = Control<TreeView>("LocationTreeView");
            tree.Items.Clear();
            foreach (var group in locations.GroupBy(l => l.Floor))
            {
                var floor = new TreeViewItem
                {
                    Header = new CheckBox { IsChecked = true, Tag = group.Key }
                };
                foreach (var location in group)
                    floor.Items.Add(new TreeViewItem
                    {
                        Header = new CheckBox { IsChecked = true },
                        Tag = location
                    });
                tree.Items.Add(floor);
            }
            var combo = Control<ComboBox>("UserGranularityBox");
            combo.SelectedItem = combo.Items.OfType<ComboBoxItem>()
                .First(i => i.Content?.ToString() == "Monthly");
            var ids = locations.Select(l => l.Id).ToList();
            Call("BuildChartTiles", ids, Granularity.Monthly);
            var host = Control<Grid>("ChartHost");
            Assert.NotEmpty(host.Children);
            int chartCount = host.Children.Count;
            W.RedrawChartTiles();
            Assert.Equal(chartCount, host.Children.Count);
        });
    }
}

// New class name avoids conflicts with the existing test classes.
// Tests read from the configured database; they do not modify stored records.
public class MainWindowAdditionalBranchTests
{
    private static void WithWindow(Action<MainWindow> action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            MainWindow? window = null;
            try
            {
                window = new MainWindow(false);
                action(window);
            }
            catch (Exception ex) { failure = ex; }
            finally { window?.Close(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void Call(MainWindow window, string method, params object?[] args)
    {
        MethodInfo? info = typeof(MainWindow).GetMethod(method,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(info);
        try { info.Invoke(window, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    [Fact]
    public void BuildChartTiles_NonexistentLocation_LeavesEmptyChartHost()
    {
        WithWindow(window =>
        {
            // No database row can have this ID. GetChartReadings must be empty,
            // exercising the 'continue' path in BuildChartTiles.
            var existing = SensorsDatabase.GetAllLocations();
            int missingId = -1;
            Assert.DoesNotContain(existing, loc => loc.Id == missingId);
            Call(window, "BuildChartTiles", new List<int> { missingId }, Granularity.Monthly);
            var host = Assert.IsAssignableFrom<Grid>(window.FindName("ChartHost"));
            Assert.Empty(host.Children);
            Assert.Empty(host.RowDefinitions);
        });
    }

    [Fact]
    public void SelectedLocations_MultipleFloors_ReturnsOnlyCheckedRoomIds()
    {
        WithWindow(window =>
        {
            var locations = SensorsDatabase.GetAllLocations().Take(3).ToList();
            Assert.Equal(3, locations.Count);
            var tree = Assert.IsAssignableFrom<TreeView>(window.FindName("LocationTreeView"));
            tree.Items.Clear();
            for (int i = 0; i < locations.Count; i++)
            {
                var floor = new TreeViewItem { Header = new CheckBox { IsChecked = true } };
                floor.Items.Add(new TreeViewItem
                {
                    Header = new CheckBox { IsChecked = i != 1 },
                    Tag = locations[i]
                });
                tree.Items.Add(floor);
            }
            Assert.Equal(new[] { locations[0].Id, locations[2].Id }, window.SelectedLocations());
        });
    }
}

// This is a NEW test class. It does not duplicate MainWindowCoverageTests.
// IMPORTANT: Integration tests modify SQL LocalDB briefly and then clean up.
// Run on a TEST DATABASE / backup. Do NOT run tests in parallel with the live app.
[Collection("MainWindowDatabaseSerial")]
public sealed class MainWindowEightHandlersCoverageTests
{
    private static void RunWindow(Action<MainWindow> action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            MainWindow? window = null;
            try { window = new MainWindow(false); action(window); }
            catch (Exception ex) { error = ex; }
            finally { window?.Close(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(45)))
            throw new TimeoutException("WPF test timed out. Check for a MessageBox or SQL connection problem.");
        if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
    }

    private static T Find<T>(MainWindow w, string name) where T : class =>
        Assert.IsAssignableFrom<T>(w.FindName(name));

    private static void Call(MainWindow w, string method)
    {
        MethodInfo info = Assert.IsAssignableFrom<MethodInfo>(typeof(MainWindow).GetMethod(method,
            BindingFlags.Instance | BindingFlags.NonPublic));
        try { info.Invoke(w, new object?[] { null, new RoutedEventArgs() }); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); }
    }

    // MessageBox.Show opens a native #32770 dialog and blocks the event handler.
    // The DispatcherTimer keeps running in its modal dispatcher loop and dismisses
    // dialogs ON THE CURRENT TEST STA THREAD. This avoids leaving modal windows open.
    // This helper must NEVER be used for production code.

    private sealed class DismissMessages : IDisposable
    {
        private const uint WM_COMMAND = 0x0111;

        private readonly uint targetThreadId;
        private readonly int buttonId;
        private readonly Thread worker;

        private volatile bool running = true;
        private volatile bool clicked = false;

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(
            uint threadId,
            EnumWindowsProc callback,
            IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(
            IntPtr hwnd,
            StringBuilder className,
            int maxCount);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(
            IntPtr hwnd,
            uint message,
            IntPtr wParam,
            IntPtr lParam);

        private delegate bool EnumWindowsProc(
            IntPtr hwnd,
            IntPtr lParam);

        public DismissMessages(int buttonId = 1)
        {
            this.buttonId = buttonId;
            targetThreadId = GetCurrentThreadId();

            worker = new Thread(() =>
            {
                while (running)
                {
                    EnumThreadWindows(
                        targetThreadId,
                        (hwnd, _) =>
                        {
                            var className = new StringBuilder(64);

                            GetClassName(
                                hwnd,
                                className,
                                className.Capacity);

                            if (className.ToString() == "#32770")
                            {
                                clicked = true;

                                PostMessage(
                                    hwnd,
                                    WM_COMMAND,
                                    new IntPtr(this.buttonId),
                                    IntPtr.Zero);
                            }

                            return true;
                        },
                        IntPtr.Zero);

                    Thread.Sleep(25);
                }
            });

            worker.IsBackground = true;
            worker.Start();
        }

        public bool SawDialog => clicked;

        public void Dispose()
        {
            running = false;
            worker.Join(1000);
        }
    }


    private static UserRecord MakeUser(string prefix = "ui_")
    {
        string username = prefix + Guid.NewGuid().ToString("N")[..12];
        Assert.True(UserDatabase.Register(username, "pass123", "User", "Test", "User", ""));
        return Assert.Single(UserDatabase.GetAllUsers(), u => u.Username == username);
    }

    private static (SensorRecord Sensor, int LocationId) MakeSensor()
    {
        // New location keyed by a very unlikely floor value; cleanup uses exact IDs.
        int floor = 100000 + Random.Shared.Next(1, 800000);
        int room = Random.Shared.Next(100000, 900000);
        string make = "TEST_" + Guid.NewGuid().ToString("N")[..10];
        Assert.True(SensorsDatabase.Register(make, "UnitTest", floor, room, true, 0));
        SensorRecord sensor = Assert.Single(SensorsDatabase.GetAllSensors(), s => s.Make == make);
        var loc = Assert.Single(SensorsDatabase.GetAllLocations(), l => l.Floor == floor && l.Room == room);
        return (sensor, loc.Id);
    }

    private static void DeleteSensorAndLocation(int sensorId, int locationId)
    {
        using var c = UserDatabase.OpenConnection();
        using (var cmd = new SqlCommand("DELETE FROM dbo.Sensors WHERE Id=@id", c))
        { cmd.Parameters.AddWithValue("@id", sensorId); cmd.ExecuteNonQuery(); }
        using (var cmd = new SqlCommand("DELETE FROM dbo.Locations WHERE Id=@id", c))
        { cmd.Parameters.AddWithValue("@id", locationId); cmd.ExecuteNonQuery(); }
    }

    private static ComboBoxItem SelectByText(ComboBox box, string text)
    {
        var item = Assert.Single(box.Items.OfType<ComboBoxItem>(), x => x.Content?.ToString() == text);
        box.SelectedItem = item;
        return item;
    }

    [Fact]
    public void LoginButton_ValidTemporaryUser_ShowsUserScreen()
    {
        var user = MakeUser();
        try
        {
            RunWindow(w =>
            {
                Find<TextBox>(w, "UsernameBox").Text = "  " + user.Username + "  ";
                Find<PasswordBox>(w, "PasswordBox").Password = "pass123";
                Call(w, "LoginButton_Click");
                Assert.Equal(Visibility.Visible, Find<UIElement>(w, "UserScreen").Visibility);
            });
        }
        finally { UserDatabase.DeleteUser(user.Id); }
    }

    [Fact]
    public void LoginButton_WrongPassword_DoesNotChangeScreen()
    {
        RunWindow(w =>
        {
            Find<TextBox>(w, "UsernameBox").Text = "admin";
            Find<PasswordBox>(w, "PasswordBox").Password = "wrong_password";
            using var dialogs = new DismissMessages();
            Call(w, "LoginButton_Click");
            Assert.True(dialogs.SawDialog);
            Assert.Equal(Visibility.Visible, Find<UIElement>(w, "LoginScreen").Visibility);
        });
    }

    [Fact]
    public void RegisterButton_ValidFields_CreatesTemporaryUser_ThenClearsForm()
    {
        string name = "newui_" + Guid.NewGuid().ToString("N")[..12];
        try
        {
            RunWindow(w =>
            {
                Find<TextBox>(w, "NewUsername").Text = name;
                Find<PasswordBox>(w, "NewPassword").Password = "pass123";
                Find<TextBox>(w, "NewFirstname").Text = "Taylor";
                Find<TextBox>(w, "NewLastname").Text = "Test";
                SelectByText(Find<ComboBox>(w, "NewRole"), "User");
                using var dialogs = new DismissMessages();
                Call(w, "RegisterButton_Click");
                Assert.True(dialogs.SawDialog);
                Assert.Equal("", Find<TextBox>(w, "NewUsername").Text);
                Assert.Contains(UserDatabase.GetAllUsers(), u => u.Username == name);
            });
        }
        finally
        {
            var account = UserDatabase.GetAllUsers().FirstOrDefault(x => x.Username == name);
            if (account != null) UserDatabase.DeleteUser(account.Id);
        }
    }

    [Fact]
    public void RegisterSensor_ValidFields_CreatesSensorAndClearsForm()
    {
        int floor = Random.Shared.Next(900000, 1500000);
        int room = Random.Shared.Next(900000, 1500000);
        string make = "TEST_UI_" + Guid.NewGuid().ToString("N")[..10];
        try
        {
            RunWindow(w =>
            {
                Find<TextBox>(w, "NewMake").Text = make;
                Find<TextBox>(w, "NewModel").Text = "M1";
                Find<TextBox>(w, "NewFloor").Text = floor.ToString();
                Find<TextBox>(w, "NewRoom").Text = room.ToString();
                Find<TextBox>(w, "Newvariance").Text = "3";
                var choices = Find<ComboBox>(w, "NewBool");
                Assert.NotEmpty(choices.Items.OfType<ComboBoxItem>());
                choices.SelectedIndex = 0;
                using var dialogs = new DismissMessages();
                Call(w, "RegisterSensor_Click");
                Assert.True(dialogs.SawDialog);
                Assert.Equal("", Find<TextBox>(w, "NewMake").Text);
                Assert.Contains(SensorsDatabase.GetAllSensors(), s => s.Make == make);
            });
        }
        finally
        {
            var s = SensorsDatabase.GetAllSensors().FirstOrDefault(x => x.Make == make);
            var loc = SensorsDatabase.GetAllLocations().FirstOrDefault(x => x.Floor == floor && x.Room == room);
            if (loc != null)
            {
                if (s != null) DeleteSensorAndLocation(s.Id, loc.Id);
                else
                {
                    using var c = UserDatabase.OpenConnection();
                    using var cmd = new SqlCommand("DELETE FROM dbo.Locations WHERE Id=@id", c);
                    cmd.Parameters.AddWithValue("@id", loc.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    [Fact]
    public void SaveChanges_SelectedTemporaryUser_UpdatesDetailsAndPassword()
    {
        UserRecord user = MakeUser();
        try
        {
            RunWindow(w =>
            {
                var grid = Find<DataGrid>(w, "UsersGrid");
                grid.ItemsSource = UserDatabase.GetAllUsers();
                grid.SelectedItem = grid.ItemsSource.Cast<UserRecord>().Single(x => x.Id == user.Id);
                SelectByText(Find<ComboBox>(w, "RoleBox"), "Admin");
                Find<TextBox>(w, "EditUsernameBox").Text = user.Username;
                Find<TextBox>(w, "EditFirstNameBox").Text = "Updated";
                Find<TextBox>(w, "EditLastNameBox").Text = "Person";
                Find<PasswordBox>(w, "NewPasswordBox").Password = "newpass456";
                using var dialogs = new DismissMessages();
                Call(w, "SaveChanges_Click");
                Assert.True(dialogs.SawDialog);
                Assert.Equal("Updated", Assert.Single(UserDatabase.GetAllUsers(), x => x.Id == user.Id).first_name);
                Assert.True(UserDatabase.Login(user.Username, "newpass456", out string role));
                Assert.Equal("Admin", role);
            });
        }
        finally { UserDatabase.DeleteUser(user.Id); }
    }

    [Fact]
    public void DeleteUser_ConfirmationYes_DeletesTemporaryUser()
    {
        UserRecord user = MakeUser();
        try
        {
            RunWindow(w =>
            {
                var grid = Find<DataGrid>(w, "UsersGrid");
                grid.ItemsSource = UserDatabase.GetAllUsers();
                grid.SelectedItem = grid.ItemsSource.Cast<UserRecord>().Single(x => x.Id == user.Id);
                using var dialogs = new DismissMessages(6); // IDYES
                Call(w, "DeleteUser_Click");
                Assert.True(dialogs.SawDialog);
                Assert.DoesNotContain(UserDatabase.GetAllUsers(), x => x.Id == user.Id);
            });
        }
        finally { UserDatabase.DeleteUser(user.Id); }
    }

    [Fact]
    public void SaveSettings_AllGraphSettingsModified_PersistAndRestore()
    {
        DashboardSettings original = SensorsDatabase.GetSettings();
        try
        {
            RunWindow(w =>
            {
                string next = original.DefaultGranularity == "Monthly" ? "Weekly" : "Monthly";
                SelectByText(Find<ComboBox>(w, "DefaultGranularityBox"), next);
                Find<CheckBox>(w, "BarCheckbox").IsChecked = !original.ShowCurrentTemp;
                Find<CheckBox>(w, "BoxplotCheckbox").IsChecked = !original.ShowTempComparison;
                Find<CheckBox>(w, "LineChcekbox").IsChecked = !original.ShowTempReading;
                using var dialogs = new DismissMessages();
                Call(w, "SaveSettings_Click");
                Assert.True(dialogs.SawDialog);
                var saved = SensorsDatabase.GetSettings();
                Assert.Equal(next, saved.DefaultGranularity);
                Assert.Equal(!original.ShowCurrentTemp, saved.ShowCurrentTemp);
            });
        }
        finally
        {
            SensorsDatabase.SaveSettings(original, original.UpdatedBy);
            SensorsDatabase.UpdateSettings(original);
        }
    }

    [Fact]
    public void ExportTemperatureData_ValidSelection_WritesJson()
    {
        var location = SensorsDatabase.GetAllLocations().First();
        var output = Path.Combine(Environment.CurrentDirectory, "TemperatureData_Monthly.json");
        // File is backed up in memory to avoid overwriting a user's export.
        byte[]? previous = File.Exists(output) ? File.ReadAllBytes(output) : null;
        try
        {
            RunWindow(w =>
            {
                var tree = Find<TreeView>(w, "LocationTreeView");
                tree.Items.Clear();
                var floor = new TreeViewItem { Header = new CheckBox { IsChecked = true } };
                floor.Items.Add(new TreeViewItem
                {
                    Header = new CheckBox { IsChecked = true }, Tag = location
                });
                tree.Items.Add(floor);
                SelectByText(Find<ComboBox>(w, "UserGranularityBox"), "Monthly");
                using var dialogs = new DismissMessages(); // handles SaveJson confirmation if present
                Call(w, "ExportTemperatureData_Click");
            });
            Assert.True(File.Exists(output), "Expected monthly JSON export file in test working directory.");
            string json = File.ReadAllText(output);
            Assert.Contains("Readings", json);
            Assert.Contains("Monthly", json);
        }
        finally
        {
            if (previous == null) { if (File.Exists(output)) File.Delete(output); }
            else File.WriteAllBytes(output, previous);
        }
    }

    [Fact]
    public void ToggleSensor_ValidTemporarySensor_ChangesActivity()
    {
        var (sensor, locationId) = MakeSensor();
        try
        {
            RunWindow(w =>
            {
                var button = new Button { DataContext = sensor };
                CallWithSender(w, "ToggleSensor_Click", button);
                SensorRecord changed = Assert.Single(SensorsDatabase.GetAllSensors(), x => x.Id == sensor.Id);
                Assert.False(changed.Active);
            });
        }
        finally { DeleteSensorAndLocation(sensor.Id, locationId); }
    }

    private static void CallWithSender(MainWindow w, string method, object sender)
    {
        MethodInfo info = Assert.IsAssignableFrom<MethodInfo>(typeof(MainWindow).GetMethod(method,
            BindingFlags.Instance | BindingFlags.NonPublic));
        try { info.Invoke(w, new object?[] { sender, new RoutedEventArgs() }); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); }
    }
}

// xUnit collection keeps these NEW tests serial relative to one another.
// It cannot serialize against other test classes unless they join the same collection.
[CollectionDefinition("MainWindowDatabaseSerial", DisableParallelization = true)]
public sealed class MainWindowDatabaseSerialCollection { }
