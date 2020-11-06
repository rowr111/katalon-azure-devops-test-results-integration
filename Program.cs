using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Xml;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace katalon_azure_integration
{
    class Program
    {
        static string TFUrl;  
        static TestManagementHttpClient TestManagementClient;
        static string teamProjectName;
        static string UserPAT;

        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Please enter the Organization name, Project name, User PAT, and the Katalon Reports folder location as space-separated parameters.");
                return;
            }

            try{
                TFUrl = @"https://dev.azure.com/" + args[0] + @"/";
                teamProjectName = args[1];
                UserPAT = args[2];
                string reportsFolder = args[3];
                getTestResultsfromReports(reportsFolder);
            }
            catch(Exception e){
                Console.WriteLine(e.Message.ToString());
                Console.WriteLine(e.StackTrace.ToString());
            }
        }
        private static void updateTestResultsInAzure(string testPlanId, List<Tuple<int,string>> resultList){
            try{
                VssConnection connection = new VssConnection(new Uri(TFUrl), new VssBasicCredential(string.Empty, UserPAT));

                DateTime utcDate = DateTime.UtcNow; //getting time for the run report name
                var culture = new CultureInfo("en-US"); 
                string reportName = "Katalon Automated Tests: " + utcDate.ToString(culture) + " " +  utcDate.Kind;

                int[] excPointIds = new int[resultList.Count];
                TestCaseResult[] excTestCases = new TestCaseResult[resultList.Count];
                for(int i=0; i<resultList.Count; i++){
                    excPointIds[i] = resultList[i].Item1;
                    string extrapolatedOutcome = resultList[i].Item2 == "PASSED" ? "Passed" : "Failed"; //we only care if the test passed or not
                    TestCaseResult caseResult = new TestCaseResult() { State = "Completed", Outcome = extrapolatedOutcome, Id = 100000 + i };
                    excTestCases[i] = caseResult;
                }

                TestManagementClient = connection.GetClient<TestManagementHttpClient>();
                RunCreateModel run = new RunCreateModel(
                    name: reportName,
                    plan:  new Microsoft.TeamFoundation.TestManagement.WebApi.ShallowReference(testPlanId),
                    pointIds: excPointIds
                );
                
                TestRun testrun = TestManagementClient.CreateTestRunAsync(run, teamProjectName).Result;
                var testResults = TestManagementClient.UpdateTestResultsAsync(excTestCases, teamProjectName, testrun.Id).Result;
                RunUpdateModel runmodel = new RunUpdateModel(state: "Completed");
                TestRun testRunResult = TestManagementClient.UpdateTestRunAsync(runmodel, teamProjectName, testrun.Id, runmodel).Result;
            }
            catch(Exception e){ //catch exception with writing test case results, don't make this kill the whole process
                Console.WriteLine(e.Message.ToString());
                Console.WriteLine(e.StackTrace.ToString());
            }
        }
    
        private static void getTestResultsfromReports(String reportsFolder){
            string[] filePaths = Directory.GetFiles(reportsFolder, "JUnit_Report.xml", SearchOption.AllDirectories);
            foreach(string filePath in filePaths){
                List<Tuple<int,string>> resultList = new List<Tuple<int, string>>();
                string testPlanId = "";

                using (var reader = XmlReader.Create(filePath))
                {
                    while (reader.Read())
                    {
                        string suiteTime = "";
                        if(reader.IsStartElement() && reader.Name == "testsuite"){
                            suiteTime = reader.GetAttribute("timestamp");
                            string format = "yyyy-MM-dd HH:mm:ss";
                            CultureInfo provider = CultureInfo.InvariantCulture;
                            DateTime testSuiteTime = DateTime.ParseExact(suiteTime, format, provider);
                            Console.WriteLine(filePath + " found, executed at " + suiteTime);
                            if((DateTime.Now - testSuiteTime).TotalHours > 12){
                                Console.WriteLine("Execution time more than 12 hours ago. Skipping.");
                                break;
                            }
                            String testSuiteName = reader.GetAttribute("name");
                            testPlanId = Regex.Match(testSuiteName, @"\((\d+)\)").Groups[1].Value;
                            if(testPlanId.Length == 0){
                                Console.WriteLine("Test Suite has no Test Plan Id. Skipping.");
                                break;
                            }
                        }

                        if (reader.IsStartElement() && reader.Name == "testcase")
                        {
                            string longName = reader.GetAttribute("name");
                            string testCaseNumber = Regex.Match(longName, @"\((\d+)\)").Groups[1].Value;
                            if(testCaseNumber.Length > 0){
                                string status = reader.GetAttribute("status");
                                Console.WriteLine("test case found: " + longName + ", id: " + testCaseNumber + ", status: " + status);
                                resultList.Add(Tuple.Create(Int32.Parse(testCaseNumber), status));
                            }
                        }
                    }
                }
                if(resultList.Count > 0){
                   updateTestResultsInAzure(testPlanId, resultList);
                }
            }
        }
    }
}
