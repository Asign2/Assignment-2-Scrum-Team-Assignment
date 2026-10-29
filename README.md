# Temperature Sensor App

This Temperature Sensor App is an [IoT](https://en.wikipedia.org/wiki/Internet_of_things) application which manages the collection, monitoring and analysis of temperature sensor data. For the purposes of this [UCOL](https://www.ucol.ac.nz/) assignment, temperature sensors will be simulations, not real sensors.

## Features

This app provides the following features:

- Simulated real-time temperature readings from up to 9 different simulated sensors.
- Real-time data visualisation
- Role-based access control
- Login authentication and password encryption
- Data storage
- Data exporting for admin and users
- User management

## Usage

The app has two types of user roles: the **Admin** and the **User**. Each has different functionality.

In practice, the administrator would have a separate user account with only user-level permissions to do day-to-day sensor monitoring, keeping their admin account only for admin duties.

### Admin capabilities

Admins have access to three screens:

1. **Users** screen (User management)
	- Update user details
	- Reset passwords
	- Create new users
	- Delete users
2. **Sensor Settings** screen (Sensor management)
	- View sensor details
	- Update sensor details
	- Add new sensors
	- Deactivate or reactivate sensors
	- Change default interval of temperature reading graphs
	- Toggle which graphs are viewable to users
3. **Dashboard Log** screen (view user activity)

And present at the bottom of all admin screens are the following options:
- <kbd>Add New user</kbd>
- <kbd>Export UserLog</kbd>
- Export all temperature readings: <kbd>Export Full Data</kbd>

### User capabilities

Users can access a single screen: the **Temperature Dashboard**. They can perform the following tasks:

- View visualisations of temperature trends and current temperatures for selected sensors
- Filter by location: <kbd>Select Locations</kbd>
- Change the time period of graphs: `Interval`
- Export temperature readings of the selected sensors and selected interval: <kbd>Export simplified data</kbd>
- Export all temperature readings: <kbd>Export Full Data</kbd>
- <kbd>Logout</kbd>

## Development

### Software Requirements

- Visual Studio 2022 or higher

### Dependencies

| Dependency                        | Version |
| --------------------------------- | ------- |
| `BCrypt.Net-Next`                 | 4.2.0   |
| `DaFoVectSharp.Raster.ImageSharp` | 2.3.0   |
| `Microsoft.Data.SqlClient`        | 7.0.3   |
| `Microsoft.Data.Sqlite`           | 10.0.11 |
| `Microsoft.NET.Test.Sdk`          | 17.11.1 |
| `Newtonsoft.Json`                 | 13.0.3  |
| `VectSharp`                       | 2.6.1   |
| `VectSharp.Plots`                 | 1.1.3   |
| `VectSharp.Raster.ImageSharp`     | 2.1.0   |
| `xunit`                           | 2.9.2   |
| `xunit.assert`                    | 2.9.2   |
| `xunit.extensibility.core`        | 2.9.2   |
| `xunit.runner.visualstudio`       | 2.8.2   |

### Opening/Building the Project

Open `Assign2.sln`, then run **▶️ Assign 2**

### Commands

- `dotnet test "./Assign2.sln" --settings "./test.runsettings" --logger "html;LogFileName=test-results.md"` (run tests and exports results through html as a .md)
- `mkdir ""` (create new folder)
- `cd ""` (go to new folder)
- `dotnet restore .\Assign2.sln --verbosity diagnostic` (Run this if api.nuget.org is unable to load)
- `dotnet new xunit3` (create xnit 3 template)

### Test Login Details
- `admin` \| `admin123`
- `alex` \| `user123`
