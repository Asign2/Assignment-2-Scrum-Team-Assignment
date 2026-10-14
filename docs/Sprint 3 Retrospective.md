Date: 14/10/2026
Present: Dan, Gene and Nick

## What went well?
- JSON export - easy implementation - selection logic already implemented prior.
- Gene and Nick pair-programming 3 times in holidays was productive
	- Nick - was very helpful for my understanding of the application's code
- Implementation of 'Conventional Commits' naming conventions.
- Adding co-authors for shared work while pair-programming so contributions are noted equitably
- Nick - felt that communication improved immensely after sprint 2;
	- Dan concurred.
	- Gene - switching to Discord made communication much easier as its our platform of choice.
- Treeview - really difficult to get working, but we pushed through and got it working.
- Refactoring was successful - removed SQL and other backend logic from our UI classes.

## What didn't go well?
- Treeview multiselect has stalled development as integrating it requires radically overhauling our location selection and graph display.
- Changing from hardcoded prototypes to actual data input and validation was complex. Implementation kept being far more complex than the prototype, e.g., for building graphs.
- Maintaining a separation of concerns — keeping backend logic out of MainWindow and other UI classes.
	- JSON export logic added by Daniel was very welcome, but required refactoring to remove the backend logic from MainWindow.


## What should we change next sprint?

- Checking the marking rubric:
	- **1. Sprint Planning, Progression and Reviews** — 13 marks
		- Comprehensive and well-maintained documentation of **all sprint ceremonies** (planning, reviews, and retrospectives)
		- Retrospectives are conducted using **online tools**, with actionable insights clearly recorded and applied in subsequent sprints.
		- [i] **ISSUE: Need to use 'online tools'**(?)
	- **2. Daily StandUps** — 5 marks
		- Highly organized meetings with thorough documentation.
		- [i] **ISSUE: Need more robust meeting documentation**
	- **3. Product Backlog and Updates** — 2 marks
		- Detailed and regularly updated Product Backlog.
		- Dynamic prioritization with clear justification
		- [i] **ISSUE: Our backlog is neglected - need to centralise it**
	- **5. Quality Assurance Processes** — 10 marks
		- Robust and well-documented QA processes.
		- Extensive testing (high coverage and edge case handling) and thorough reviews, with clear evidence of continuous improvement.
		- [i] **ISSUE: QA processes aren't documented**
			- Use 'conventional commits'
			- Document our SCRUM process
			- Comment your code
			- MVVM and Separation of Concerns
			- Pair Programming
			- no PR review process... But not required.
		- [?] **ISSUE: More documentation for unit tests?**
			- Can we output unit test results to a file?
			- Where are tests documented?
	- **6. Software Design Documentation**
		- Exceptional and comprehensive documentation.
		- High level of detail and clarity in all design elements.
		- *(User Stories, Use Case Model, High level design, ERD.)*
			- *What is meant by 'high level design' docs?*
		- [x] We think we've got all the required design documentation
