# katalon -> azure devops test results integration
Console app that reads test results from Katalon's Reports file and uploads test case results that are properly tagged to Azure DevOps as test run results.

Currently, there is no way to link execution results of Test Cases in Katalon with Test Run Reults in Azure DevOps.  This simple console app will upload a simple passed/failed for a test in Azure Devops based on the relevant Katalon Test Case result.

### currently lacking areas
- there is currently no convenient way to view the list of test plans in Azure DevOps and automatically select the test plan number and test point id
  - this must be gotten manually and added to the test suite and test case names, which could be difficult to maintain
  - a plugin in the future could log into Azure DevOps and show these as an option for selection, and automatically get the correct test point id for the test case at the time of reporting
- azure devops test points are also dependent on 'configuration' which cannot be modified per test case with this console app
  - in the future it would be nice to be able to this automatically detected somehow, although I'm not sure how this would be done currently.

## Prerequisites
- Katalon runs must generate reports
- Azure [User Personal Access Token](https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=preview-page) - this must be created and used to authenticate.  
 - test results will be reported as being run by this user.
- all test cases for one Katalon Test Suite must be under the same Test Plan in Azure DevOps
- Important Concept - Test Point ID:
  - Test Case results are reported specifically for the unique Test Case + Test Plan/Test Suite combination. 
  - there is an ID for this combination, it is called the Test Point ID
  - to find the Test Point ID:
    - view the Test Plan you want to run
    - click on the “More…” three vertical dots next to the Test Case title, then click ‘view execution history’
    - click on the link at the bottom to ‘Open execution history for current test point’
    - you will see the Test Point ID in the URL, the end of the URL will show something like this: &contextPointId=13090

## Setup of Test Suite and Test Cases in Katalon
- suffix Test Suite names with the Test Plan ID, in parentheses:  `Test Suite Blah (1234)`
  - if there is no ID, the Test Suite will be skipped
- suffix Test Case names with the Test Point ID for that Test Case/Test Plan combo in Azure DevOps as per the prerequisites. `Test Case Name Blah (5678)`
  - if there is no ID, the Test Case will be skipped

## How to build
This app was built with .NET Core and Visual Studio Code on Windows.  
It depends on [Microsoft.TeamFoundationServer.Client](https://www.nuget.org/packages/Microsoft.TeamFoundationServer.Client/), get the package via the attached link and install it via terminal within VS Code.

You can build packages that will work on any of MacOS/Windows/Linux via the commands [described on the dotnet core deployment page](https://docs.microsoft.com/en-us/dotnet/core/deploying/).



## How to run
 - replace ORGANIZATION_NAME PROJECT_NAME UserPAT KATALON_REPORTS_DIRECTORY with your own information as is appropriate.
 
 - Visual Studio Code terminal:
   - `dotnet run ORGANIZATION_NAME PROJECT_NAME UserPAT KatalonReportsDirectory`
  
 - compiled version from powershell or terminal on mac:
   - `dotnet katalon_azure_integration.dll ORGANIZATION_NAME PROJECT_NAME UserPAT KATALON_REPORTS_DIRECTORY`
   
 - Azure DevOps Pipeline yml file - run this task after all your Katalon tests have completed.
  ```task: PowerShell@2
    inputs:
      targetType: 'inline'
      script: |
        $ReportsDir = Get-Item GET_YOUR_REPORTS_DIRECTORY
        dotnet katalon_azure_integration.dll ORGANIZATION_NAME PROJECT_NAME UserPAT $ReportsDir
      workingDirectory: YOUR_WORKING_DIRECTORY
  ```
      
## What it does
 - iterates through every report file
 - if the test suite was executed less than 12 hours ago (this tool was written originally for use in Azure Devops Pipeline where the reports folder is empty to start every time, if you're running test suites over and over this might not be ideal for you)
   - checks the test suite for a test plan id
   - if a test plan id exists
     - checks each test for a test point id, and if a test point id exists
       - records if the test passed or not (everything except passed will be marked as failed)
 - creates a test run in Azure DevOps
 - uploads the results found to Azure DevOps


## Questions/Comments?
Tweet me @rowr or find me on the Katalon forum as user jeanie.conner
