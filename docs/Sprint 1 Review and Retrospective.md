# Sprint 1 Retrospective

02/09/2026

%% migrated to github from original [word doc](https://ucol-my.sharepoint.com/:w:/r/personal/24374442_studentmail_ucol_ac_nz/Documents/D202/Sprint%201%20Retrospective.docx?d=wee01316633e14b2ca16e17bf02edda87&csf=1&web=1&e=m2oDK9) %%

%% Prior to Sprint 3, we were unclear on the difference between a sprint 'review' and a 'retrospective'. Thus, our Sprint 1 & 2 retrospectives are effectively sprint reviews as well. %%

## What went well?

- Sign-in – implemented without detectable bugs, and password hashing implemented.
- Role-based access – implemented with a simple if-statement.
- User Creation – able to reuse and easily adapt code from the sign-in functionality to implement user creation using SQL ‘INTO’ statements.
- User Stories – User Stories were straightforward to implement based off clear requirements.
- Use Case Diagram – User interactions with the system are straightforward, so a simple Use Case diagram (without ‘include’ or ‘extend’) was easy to implement.

## What didn’t go well?                

- Using the wrong database – initially used SQLite, and changes to code of the app caused the database to stop updating.
- Absent team member (Nick) on day 1 of sprint (sprint planning) due to illness
- Github setup: initially we were unable to upload XAML files.
- Maintaining documentation in a separate git branch created unnecessary commits each time it pulled from main.

## What new ideas do we have?

- Find a more suitable database replacement for SQLite.
- Keep documentation in Main git branch.

## What actions will we take?

- Swap SQLite to an alternative database.
- Delete ‘documentation’ git branch and rely on ‘documentation’ directory instead.
- Improve readability of the Burndown Chart
