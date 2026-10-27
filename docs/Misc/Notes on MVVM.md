
- Model is like [[Database]]
- View is UI
- ViewModel is the go-between.

No connection between View and model

Instead of a Code-Behind as data context, we create a 'ViewModel'... what about the Code-Behind?
- sometimes code-behind is more efficient and even needed.
- generally not needed though.
- No business logic in your code-behind. If you do, you've coupled your view with your model.
- can be used for view-specific logic though.

Is MVVM right for this project? Not necessarily. But we should follow [[Object-Oriented|OOP]] Principles.

See [[05 D201 Lecture]] for guidelines on [[Interface (C-Sharp)|Interfaces]], [[Orthogonality]], [[Separation of Concerns]] and [[SOLID Principles]].

---

SensorDatabase - is responsibe for updating to the database (logging, initialising, seeding etc)
- New class for periodically generating the temp.

Charts - where are they being built from

MainWindow.Xaml.cs
- [x] `BuildChartTiles`
	- sql logic - move into its own class
- `SaveSettings_Click`
	- logic for checking data should be separated
- `SaveChanges_Click` (updating user details)
- Password Reset
- RefreshDashboard
	- checks SensorsDatabase.DashboardSettings and if unable to, populates database with default settings
- *try to replace logic with bool conditions which are handled elsewhere*
