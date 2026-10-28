using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Assign_2;
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
}
