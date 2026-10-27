This Temperature Sensor App is an [IoT](https://en.wikipedia.org/wiki/Internet_of_things) application which manages the collection, monitoring and analysis of temperature sensor data. For the purposes of this [UCOL](https://www.ucol.ac.nz/) assignment, temperature sensors will be simulations, not real sensors.

## Features

This app provides the following features:
	Role Based Access Control,
	Login Authentication,
	Data Storage,
	User report,
	User management.

### Login and Role-Based Access

The app supports Administrator and User roles, password login authentication and encryption of user credentials.

### Data

The app simulation for temperature activates every 20 seconds, and that activation will generate the temperature readings for an hour. This means that data will get tracked into the future.

### Database Storage

The app stores data in a database, including:
- User credentials

## Commands

- `dotnet test "./Assign2.sln" --logger "html;LogFileName=test-results.html"` (run tests and exports results through html as a .md)
- `mkdir ""` (create new folder)
- `cd ""` (go to new folder)
- `dotnet restore .\Assign2.sln --verbosity diagnostic` (Run this if api.nuget.org is unable to load)
- `dotnet new xunit3` (create xnit 3 template)
