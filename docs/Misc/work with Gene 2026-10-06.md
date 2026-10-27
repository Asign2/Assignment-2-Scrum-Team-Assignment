
%% Pair Programming conducted by Gene and Nick during Sprint 3 %%

What to work on today:
- refactoring
- periodic temp generation

## Refactoring

**MainWindow.Xaml.cs**
- [x] `BuildChartTiles`
	- sql logic - move into its own class
- [-] `SaveSettings_Click`
	- ~~logic for checking data should be separated~~
	- this is fine - it's all just UI logic
- [?] Updating users
	- `SaveChanges_Click` (updating user details)
	- `DeleteUser`
	- `RegisterButton_Click` - registers new user
	- Password Reset
	- **This is kind of okay**
- RefreshDashboard
	- checks SensorsDatabase.DashboardSettings and if unable to, populates database with default settings
- *try to replace logic with bool conditions which are handled elsewhere*

- [x] Refactoring 'Buckets' to 'TimeGroups'
	- rename `@buckets` parameter for SQL params.

---
