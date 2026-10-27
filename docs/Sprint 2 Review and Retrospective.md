# Sprint 2 Retrospective

02/09/2026

%% migrated to github from original [word doc](https://ucol-my.sharepoint.com/:w:/r/personal/24374442_studentmail_ucol_ac_nz/Documents/D202/Sprint%202%20Retrospective.docx?d=w10754a77a106455c983ba584e4a4f517&csf=1&web=1&e=ehgM3u) %%

%% Prior to Sprint 3, we were unclear on the difference between a sprint 'review' and a 'retrospective'. Thus, our Sprint 1 & 2 retrospectives are effectively sprint reviews as well. %%

## What went well?

- have 6 users
- Changing from SQLite to SQL Server was smooth and database works
- Table with live management of users is implemented; just needs firstName and lastName fields
- Update and Delete users: Functional and tested (though you can delete yourself, with no effect/without logging you out)
- Selection of sensors: Can filter sensor location (though currently we only have 1 sensor)
- Selection of what graphs can be shown: Not yet implemented
- Automated testing (Unit Tests) complete and functioning
- ERD completed
- Ensure no SQL injection can be done - WIP
- Burndown Chart is made

Tasks:

- working database which matches the ERD and can be viewed in Visual Studio
	- Not fully - sensor locations need to be in a separate table
	- Constraints need adding (Not NULL)
	- need to do an ERD for what the Graph displays
- Finished planning documentation - ERD and Use Case Descriptions
	- ERD mostly
	- Use Case Descriptions - no, but nearly done
- Burndown chart
- Code documentation - comments, and XML summaries for every CS file.
- 6 users (1 admin + 5 regular users) implemented in database.
	- Seeded Users (available on startup, directly in the code) 2 admins + 5 users (7 total). Implemented in the database.
- Admin user-management functionality: CRUD.
- Automated testing script using xUnit.
- _Stretch goal_ - Visualisation of the graph templates.
- Software Inspection Report

## What didn't go well?

- Trouble with viewing SQL Server mdf files for Daniel
- Accidentally committing changes to the MDF files
- Persistent bug where app throws an exception if .mdf and .ldf files aren't present ([issue 16](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/16))
- Communication - Nick requested more regular comms.
- Nick wasn't at the Stand-up; Didn't know what Nick was doing and couldn't get their input
- Gene - doesn't understand the SQL code very well; Nick is the same
- Lack of small comments during coding in sprint 1 in 2 has left a lack of context for documentation and understanding later.

## What new ideas do we have?

- Add more comments during coding
- MVVM

## What actions will we take?

- update gitignore to exclude:
	- .mdf and .ldf files
	- .props and .build files (xUnit template)
- Switched from Teams to Discord for group chat to improve communication.
- Add a logical file-tree for the app (add folders)
- Gene and Nick will add more comments while we are coding in next sprint.
- **Switch to MVVM model:** move the logic from the 'view-model' MainWindow.xaml.cs (Code-Behind) to a separate 'model' class
