# Use Case Descriptions

%% migrated to Github from [original Word document](https://ucol-my.sharepoint.com/:w:/r/personal/24374442_studentmail_ucol_ac_nz/Documents/D202/Use%20Case%20Descriptions.docx?d=wb12286786c0f42a996d9f11626de4eeb&csf=1&web=1&e=JLvErE). This markdown document is the current version %%

Supplemental descriptions for our two Use Case Diagrams ([Link to Draw.io file](https://ucol-my.sharepoint.com/:u:/r/personal/24374442_studentmail_ucol_ac_nz/Documents/D202/D202%20Use%20Case%20Diagram.drawio?d=w9b4e5361165c4d10a7f38e77c13ff5a3&csf=1&web=1&e=khQc8q)).

## Diagram 1: User Management

![User Management Use Case Diagram](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/blob/main/docs/Use%20Case%20Diagram-User%20Management.jpg)

| **Use Case**       | ~~Update Personal Details~~ (dropped)                                                                        |
| ------------------ | ------------------------------------------------------------------------------------------------------------ |
| **Description**    | Update non-credential details of user                                                                        |
| **Actors**         | User                                                                                                         |
| **Preconditions**  | User signed in                                                                                               |
| **Steps**          | <ol><li>Click ‘update personal information’</li><li>Enter details</li><li>Click ‘save’ or ‘cancel’</li></ol> |
| **Extensions**     |                                                                                                              |
| **Non-Functional** |                                                                                                              |

| **Use Case**       | ~~Change Password~~ (dropped)                                                                                                                    |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Description**    | A user changing their own password                                                                                                               |
| **Actors**         | User                                                                                                                                             |
| **Preconditions**  | User signed in                                                                                                                                   |
| **Steps**          | <ol><li>Click ‘reset password’</li><li>Enter original password, and new password in respective fields</li><li>Click ‘save’ or ‘cancel’</li></ol> |
| **Extensions**     |                                                                                                                                                  |
| **Non-Functional** | Pop-up should ask user if they are sure they want to update their password upon clicking ‘save’                                                  |

| **Use Case**       | **Issue Password Reset**                                                                                              |
| ------------------ | --------------------------------------------------------------------------------------------------------------------- |
| **Description**    | Issuing a default password to a user who has forgotten their password                                                 |
| **Actors**         | Admin                                                                                                                 |
| **Preconditions**  | Admin signed in                                                                                                       |
| **Steps**          | <ol><li>Select user whose password is to be reset</li><li>Enter password</li><li>Click ‘reset password’</li></ol> |
| **Extensions**     |                                                                                                                       |
| **Non-Functional** | Pop-up should ask admin if they are sure they want to reset the user’s password                                       |

| **Use Case**       | **Delete User**                                                       |
| ------------------ | --------------------------------------------------------------------- |
| **Description**    | Deleting a user from the app, removing their access.                  |
| **Actors**         | Admin                                                                 |
| **Preconditions**  | Admin signed in                                                       |
| **Steps**          | <ol><li>Select user to delete</li><li>Click ‘Delete User’</li></ol>      |
| **Extensions**     |                                                                       |
| **Non-Functional** | Pop-up should ask admin if they are sure they want to delete the user |

| **Use Case**       | **Create User**                                                                                                                                                      |
| ------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Description**    | Creating a new user                                                                                                                                                  |
| **Actors**         | Admin                                                                                                                                                                |
| **Preconditions**  | Admin signed in                                                                                                                                                      |
| **Steps**          | <ol><li>View Users dashboard</li><li>Click ‘Add New User’</li><li>Enter details</li><li>Click ‘Register New User’ to save, or ‘Return’ to abort</li></ol> |
| **Extensions**     |                                                                                                                                                                      |
| **Non-Functional** |                                                                                                                                                                      |

| **Use Case**       | **Change User Permissions**                                                                                                                                                                                |
| ------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Description**    | Updating the permissions of a single user                                                                                                                                                                  |
| **Actors**         | Admin                                                                                                                                                                                                      |
| **Preconditions**  | Admin signed in                                                                                                                                                                                            |
| **Steps**          | <ol><li>View Users dashboard</li><li>Select a user</li><li>Click ‘Update Permissions’</li><li>Make changes to permissions</li><li>Click ‘Confirm’ to save or ‘Cancel’ to abort</li></ol> |
| **Extensions**     |                                                                                                                                                                                                            |
| **Non-Functional** | Pop-up should ask admin if they are sure they want to update the permissions                                                                                                                               |

| **Use Case**       | **Change Role Permissions**                                                                                                                                                              |
| ------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Description**    | Updating the permissions of an entire Role                                                                                                                                               |
| **Actors**         | Admin                                                                                                                                                                                    |
| **Preconditions**  | Admin signed in                                                                                                                                                                          |
| **Steps**          | <ol><li>View ‘Users’ tab in dashboard</li><li>Click ‘Manage Roles’</li><li>Select Role, and update permissions</li><li>Click ‘Confirm’ to save or ‘Cancel’ to abort</li></ol> |
| **Extensions**     |                                                                                                                                                                                          |
| **Non-Functional** | Pop-up should ask admin if they are sure they want to update the permissions                                                                                                             |

## Diagram 2: Manage Sensors

![Manage Sensors Use Case Diagram](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/blob/main/docs/Use%20Case%20Diagram-Manage%20Sensors.jpg)

| **Use Case**       | **Monitor Sensor Readings**                                                                   |
| ------------------ | --------------------------------------------------------------------------------------------- |
| **Description**    | Viewing sensor readings                                                                       |
| **Actors**         | User, Admin                                                                                   |
| **Preconditions**  | User/Admin signed in                                                                          |
| **Steps**          | <ol><li>Go to Temperature Dashboard</li><li>View all sensors or select individual ones</li></ol> |
| **Extensions**     | Export Sensor Data                                                                            |
| **Non-Functional** |                                                                                               |

| **Use Case**       | **Export Sensor Data**                                                                                                           |
| ------------------ | -------------------------------------------------------------------------------------------------------------------------------- |
| **Description**    | Exporting sensor readings as a text-based file                                                                                   |
| **Actors**         | User, Admin                                                                                                                      |
| **Preconditions**  | Monitor Sensor Readings                                                                                                          |
| **Steps**          | <ol><li>Go to Temperature Dashboard</li><li>View all sensors or select individual ones</li><li>Click ‘Export Data’</li></ol> |
| **Extensions**     |                                                                                                                                  |
| **Non-Functional** |                                                                                                                                  |

| **Use Case**       | **View Sensor Information**                                                      |
| ------------------ | -------------------------------------------------------------------------------- |
| **Description**    | View information/attributes of individual sensors                                |
| **Actors**         | User, Admin                                                                      |
| **Preconditions**  | Signed in                                                                        |
| **Steps**          | <ol><li>Go to ‘Sensor Details’ tab</li><li>Select sensor and view details</li></ol> |
| **Extensions**     |                                                                                  |
| **Non-Functional** |                                                                                  |

| **Use Case**       | **Export User Activity Data**                                                |
| ------------------ | ---------------------------------------------------------------------------- |
| **Description**    | Export logs of user activity as a text-based file                            |
| **Actors**         | Admin                                                                        |
| **Preconditions**  | Signed in                                                                    |
| **Steps**          | <ol><li>In ‘Users’ tab of admin dashboard</li><li>Click ‘Export Data’</li></ol> |
| **Extensions**     |                                                                              |
| **Non-Functional** |                                                                              |

| **Use Case**       | **Manage Sensors**                                                                                         |
| ------------------ | ---------------------------------------------------------------------------------------------------------- |
| **Description**    | Update information/attributes of individual sensors                                                        |
| **Actors**         | User, Admin                                                                                                |
| **Preconditions**  | Signed in                                                                                                  |
| **Steps**          | <ol><li>Go to ‘Sensor Details’ tab</li><li>Select sensor and view details</li><li>Click ‘Update’</li></ol> |
| **Extensions**     |                                                                                                            |
| **Non-Functional** |                                                                                                            |

| **Use Case**       | **Generate Sensor Readings**                                                                                                  |
| ------------------ | ----------------------------------------------------------------------------------------------------------------------------- |
| **Description**    | Temperature Sensor generates temperature readings                                                                             |
| **Actors**         | `<<Sensor>>`                                                                                                                  |
| **Preconditions**  | Database connection                                                                                                           |
| **Steps**          | <ol><li>Check temperature and send reading to database</li><li>Stop</li><li>Check temp again in 60 seconds, repeat.</li></ol> |
| **Extensions**     |                                                                                                                               |
| **Non-Functional** |                                                                                                                               |
