# katalon -> azure devops test results integration
 console app that reads test results from Katalon's Reports file and uploads test case results that are properly tagged to Azure Devops Pipeline

Currently there is no way to link Test Cases in Azure DevOps with Katalon test results.  This simple console app will upload a simple passed/failed for a test in Azure Devops based on the relevant Katalon Test Case result.

## How to build
This app was built with .NET Core and Visual Studio Code on Windows.  
It depends on [Microsoft.TeamFoundationServer.Client](https://www.nuget.org/packages/Microsoft.TeamFoundationServer.Client/), get the package via the attached link and install it via terminal within VS Code.

You can build packages that will work on any of MacOS/Windows/Linux via the commands [described on the dotnet core deployment page](https://docs.microsoft.com/en-us/dotnet/core/deploying/).

## Prerequisites
- Katalon runs must generate 
- [User Personal Access Token](https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=preview-page) - this must be created and used to authenticate.  
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
- suffix Test Case names with the Test Point ID for that Test Case/Test Plan combo in Azure DevOps as per the prerequisites.
  - if there is no ID, the Test Case will be skipped

## What it does
