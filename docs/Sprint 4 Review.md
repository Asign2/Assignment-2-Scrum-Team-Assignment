Date: 28/10/2026
Present: Dan, Gene and Nick

## What was planned

- [x] [US02](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/48)
    - [x] [Review ERD](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/60)
- [x] [US05](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/51)
    - [x] [Treeview Multiselect](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/36)
- [x] [US06](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/52)
    - [x] [periodically generate sensor readings every ~~20~~ seconds](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/61).
	    - Changed to 60 seconds
	- Real-time was implemented in realtime-temperature-data branch; now merged into Main
- [x] [US07](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/53)
    - [x] [Fix graph display for treeview multiselect](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/41)
    - [x] [Implement boxplot graph](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/44)
- [x] [US10](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/56)
    - [x] [implement versioning on the app.](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/64)
    - [x] Maintain git board
- [x] [Centralise product backlog into a single source](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/65).
- [x] [Ensure all sprint ceremonies are documented](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/66)
- [x] [Add quality assurance document outlining our QA practices](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/67).
- [x] [Implement outputting of unit test results to a text file](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/68)
- [x] [Document unit tests](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/69)
- [ ] All Github Issues - **prioritise core functionality and bugfixes** over UI.
	- 4 remaining - only one is code-related and the rest are documentation.
- [ ] [xml \<summary\> for all methods and classes](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/70)

## What was completed

As above

## What wasn't completed

### [Issue #86](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/86) Feat: Ability to add or update sensors (admin)

Updating sensor - requires location. Problem - temps are tied to sensor ID. Updates are destructive. If floor/room is changed, all the temps recorded for the old floor/room get reassigned to the new floor/location as if they were there all along.

To implement, requires redesigning the database and ERD.

A quick-fix would be to only add functionality to *add new sensors*. Since existing sensors can be deactivated, an 'update' could be done by deactivating a sensor and making a new one with changed attributes. Temps recorded at 'previous' location would be retained. The issue with this is that there's no database link between the deactivated sensor and the 'updated' (new) sensor.

We will retain ability to update sensor Make and Model. Other fields need to be read-only.

### Requirements review
- [x] FR1 - missing a user.
	- Added in 95f9431
- [x] FR2 - clarified in the Readme that admin intentionally doesn't have user permissions
	- ba73d0d
- [x] FR3 - "admin to manage access permissions for users and **roles**" — role-based permission management is implicit, not explicit. They cannot make updates explicitly to a type of user role, but they can affect it (i.e., via change Sensor Settings).
- [x] FR4 - yes, 9 sensors (8 active ,1 deactivated)
- [x] FR5
- [x] FR6
- [x] NFR1
- [x] NFR2
	- visualisation (i.e., redrawing charts) only occurs when data generation occurs. Is that 'real-time'? It's close enough — it updates whenever there is new data. Though it's reliant on the delay being every 60 seconds, rather than real-time generation.
	- data is technically not generated in 'real-time', but is generated every minute.
	- Generation only occurs while app is open. After closing and re-opening, it retroactively begins where it left off, putting it out of sync with the current time.
- [x] NFR3

## Action Items

- [Issue 86](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/86) - optional stretch goal due to scope blowout
- [Issue 85](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/85)
- [Issue 82](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/82)
- [Issue 70](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/70)
- bring test coverage back above 70%
