## Quality Assurance Processes

### Version Control
- Use git and Github to track changes and rollback if needed.
- Prefer use of Github Desktop for pushing commits and managing branches, instead of the terminal or Visual Studio's built-in git. Desktop's integration with Github enables:
	- Easily add co-authors to commits
	- WIP changes can be easily stashed when switching branches
	- Github Issues can be easily referenced by hashtag.
- Use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification for consistency of commit messages
	- Introduced by Nick from Sprint 3 onward (16/09/2026)
- Encourage creation of major features into separate branches
	- Introduced by Nick from Sprint 3 onwards, encouraging Gene to move their work on graphs to a separate branch, ['Feat-RT-Graph-Sensors'](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/tree/Feat-RT-Graph-Sensors)

### Software Testing

Our testing practices evolved throughout development. Testing was originally conducted on an ad-hoc basis, with no defined timeframe or procedure for when to conduct it. In early sprints, breaking commits were even pushed to Main without testing the app. By Sprint 4, we were running unit tests throughout development before committing as well as running the app to manually test changes every time, and these tests were often done by other team members as code reviews.

#### Unit Testing
- Use [xUnit](https://xunit.net/?tabs=cs) for unit testing
- Unit test files were originally divided by their sprint (e.g., `Sprint 1.cs`, `Sprint 2.cs` etc). From Sprint 4, new unit test files were instead divided functionally (e.g., `ChartDataTests.cs`, `MainWindowTests.cs` etc) for better clarity of purpose.
- Logging of unit test results introduced in Sprint 4 (commit [9453d77](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/commit/9453d778c14fffb91b60bad1d4869c38353492df))
- [Fine Code Coverage](https://github.com/FortuneN/FineCodeCoverage) to calculate unit test code coverage (introduced in Sprint 4)

#### Manual Testing
- Manual testing focused on UI, leaving unit tests for the majority of our testing
- Manual tests cases were created in Sprint 4 with our [Manual Test Cases](https://ucol-my.sharepoint.com/:w:/r/personal/24374442_studentmail_ucol_ac_nz/Documents/D202/Manual%20Test%20Cases.docx?d=w41ccc5d6713d4eb78a7ec2a34e19e22d&csf=1&web=1&e=bqthZx) document.
	- issues [#78](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/78) and [#82](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/issues/82)
- Manual tests were conducted by Gene and Daniel.
- Only 1 test was completed for each manual test case, as it all occurred during Sprint 4 (our final sprint), except where a test case was a **fail** (in which case an issue was lodged, a fix attempted, and the test conducted again).

The following template was used for each test case:

| Field                  | Details |
| ---------------------- | ------- |
| **Test Case ID**       |         |
| **Test Case Scenario** |         |
| **Test Steps**         |         |
| **Test Data**          |         |
| **Expected Output**    |         |
| **Actual Output**      |         |
| **Pass/Fail**          |         |

### Pair Programming

Pair programming was utilised during each sprint between the Driver and Navigator.

### Documentation

- Encouraged adding comments to code to improve readability and provide context to design decisions.
- xml `<summary>` added to most classes and methods to improve code readability
- Scrum Ceremonies documented:
	- A Sprint Plan was documented at the beginning of each sprint
	- Stand-Up meetings halfway between each sprint were documented (except [Sprint 2's](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/blob/main/docs/Stand%20Ups/2026-09-09%20Sprint%202.md) stand-up, which was written on paper but misplaced)
	- Sprint Reviews and Retrospectives were documented for each sprint.
		- Until Sprint 3, we did not understand the functional difference between a Sprint 'Review' and a 'Retrospective', so our Sprint 1 & 2 documents are a combination of review and retrospective. Sprint 3 onward differentiates between Reviews and Retrospectives.
	- User Stories were documented during Sprint 0-1, with a couple of updates, but we did not define further User Stories each sprint. Their usefulness was hindered by a lack of acceptance criteria
	- Acceptance Criteria was not defined due to our own misunderstanding of how to utilise User Stories. This oversight was discovered too late during our final sprint. Our task breakdowns in each sprint plan act somewhat as de-facto acceptance criteria, but only a limited fashion.
	- Planning Poker was conducted intermittently. We did it once during Sprint one but did not realise we should be doing this every sprint. There was also confusion over whether the points we assigned were a measure of *effort* or of *priority*. The second time we conducted Planning Poker was in Sprint 4.
- [Requirements](https://github.com/Asign2/Assignment-2-Scrum-Team-Assignment/blob/main/docs/Requirements.md) were documented and evolved based off updates and feedback from the Product Owner (Harshvardhan). We used footnotes to cite and track these.
