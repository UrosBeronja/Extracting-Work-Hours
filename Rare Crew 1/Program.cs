using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class EmployeeTimeEntry
{
    public string EmployeeName { get; set; }
    public DateTime StarTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
}

public class EmployeeTotalTime
{
    public string Name { get; set; }
    public double TotalTime { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        string url = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";
        string json = await new HttpClient().GetStringAsync(url);
        var timeEntries = JsonConvert.DeserializeObject<List<EmployeeTimeEntry>>(json);

        var totalTimePerEmployee = timeEntries
            .GroupBy(entry => entry.EmployeeName)
            .Select(group => new EmployeeTotalTime
            {
                Name = group.Key,
                TotalTime = group.Sum(entry => (entry.EndTimeUtc - entry.StarTimeUtc).TotalHours)
            })
            .OrderByDescending(e => e.TotalTime)
            .ToList();

        string html = GenerateHtmlTable(totalTimePerEmployee);
        System.IO.File.WriteAllText("EmployeeTimeReport.html", html);
        Console.WriteLine("HTML report generated.");

    }

    static string GenerateHtmlTable(List<EmployeeTotalTime> employeeTotalTimes)
    {
        var html = new System.Text.StringBuilder("<table border='1'>");
        html.AppendLine("<tr><th>Name</th><th>Total Time Worked</th></tr>");

        foreach (var employee in employeeTotalTimes)
        {
            string rowColor = employee.TotalTime < 100 ? " style='background-color: red;'" : "";
            html.AppendLine($"<tr{rowColor}><td>{employee.Name}</td><td>{employee.TotalTime:N2} hours</td></tr>");
        }

        html.AppendLine("</table>");
        return html.ToString();
    }

}
