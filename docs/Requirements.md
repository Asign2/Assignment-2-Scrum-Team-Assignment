# Requirements

%% ported from original [word doc](https://ucol-my.sharepoint.com/:w:/r/personal/24374442_studentmail_ucol_ac_nz/Documents/D202/Requirements.docx?d=w80f88514993d455191316c8156fa6528&csf=1&web=1&e=a5cyt1) in OneDrive %%

## Functional Requirements

1. The system shall support login and role-based access with two main roles — administrator ("admin") and regular users ("user")[^1] — with a minimum of 5 regular users.[^2]
2. The system shall enable the admin and regular users to view the dashboard, monitor sensor readings, view sensor and sensor location information, and access relevant historical data.[^2]
3. The system shall enable the admin to manage access permissions for users and roles (including controlling which temperature readings are visible),[^3] and manage sensors by adding, editing or deactivating sensors.[^2]
4. The system shall generate temperature readings based off virtual sensors,[^1] with a minimum of 3 virtual sensors.[^2]
5. The system shall display sensor readings both in writing and as data visualisations in a dashboard,[^1] including current temperature, sensor/location, status and timestamp.[^2]
6. The system shall store data received from simulated sensors,[^1] users, and sensors in a database.[^2]

## Non-Functional Requirements

1. The system shall support secure login with proper encryption of user credentials.[^1]
2. The system shall ensure sensor readings are received and displayed in real-time.[^1]
3. The system shall generate temperature readings periodically to simulate IoT data.[^2]

[^1]: Assignment Brief
[^2]: [Project Requirement Clarifications](https://moodle.ucol.ac.nz/mod/forum/discuss.php?d=82763#p157305) 26/08/2026 (Moodle)
[^3]: [Scrum Standup 2026-08-26](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/blob/main/docs/Stand%20Ups/2026-08-26%20Sprint%201.md#product-owner-check-in)
