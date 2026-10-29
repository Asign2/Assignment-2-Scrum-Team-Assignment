
%% This document was migrated from [Trello](https://trello.com/c/CrSLXQfs) to Github %%

![ERD](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/blob/main/docs/Entity-Relationship-Diagram.jpg)

Our ERD was made using Visual Paradigm ([link to vpp file in OneDrive](https://ucol-my.sharepoint.com/:u:/r/personal/24374442_studentmail_ucol_ac_nz/Documents/D202/ERD.vpp?d=w12f3d477bef64dd0addfa26bb664e642&csf=1&web=1&e=Uq3PdA)).

`ID` is the primary key

The last value is the foreign key, so must match to the linked table

Gene made this in visual paradigm and so they dont have all datatypes in the sensor table. `date_installed` uses _date_ however _datetime_ would be more appropriate. Likewise, *nvarchar* would be more appropriate instead of *varchar*.
