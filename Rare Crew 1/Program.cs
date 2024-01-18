using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
            .Where(entry => entry.EndTimeUtc > entry.StarTimeUtc)
            .GroupBy(entry => entry.EmployeeName)
            .Select(group => new EmployeeTotalTime
            {
                Name = group.Key,
                TotalTime = group.Sum(entry => (entry.EndTimeUtc - entry.StarTimeUtc).TotalHours)
            })
            .OrderByDescending(e => e.TotalTime)
            .ToList();

        string pieChartFileName = "EmployeeTimePieChart.png";
        GeneratePieChartImage(totalTimePerEmployee, pieChartFileName);

        string htmlContent = GenerateHtmlPage(totalTimePerEmployee, pieChartFileName);
        string filePath = "EmployeeTimeReport.html";
        System.IO.File.WriteAllText(filePath, htmlContent);

        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }

    static void GeneratePieChartImage(List<EmployeeTotalTime> employeeTotalTimes, string fileName)
    {
        const int width = 600;
        const int height = 600;
        const int margin = 10;
        Font font = new Font("Arial", 10);
        using (Bitmap bitmap = new Bitmap(width, height))
        {
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                Brush[] brushes = new Brush[] {
                    Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow,
                    Brushes.Purple, Brushes.Orange, Brushes.Brown
                };

                double totalHours = employeeTotalTimes.Sum(et => et.TotalTime);
                float startAngle = 0;
                for (int i = 0; i < employeeTotalTimes.Count; i++)
                {
                    var employee = employeeTotalTimes[i];
                    float sweepAngle = (float)((employee.TotalTime / totalHours) * 360);
                    graphics.FillPie(brushes[i % brushes.Length], margin, margin, width - 2 * margin, height - 2 * margin, startAngle, sweepAngle);

                    // Draw labels
                    double angle = (startAngle + sweepAngle / 2.0) * Math.PI / 180;
                    double labelX = width / 2 + (width / 2 - margin) * Math.Cos(angle) / 2;
                    double labelY = height / 2 + (height / 2 - margin) * Math.Sin(angle) / 2;
                    string label = $"{employee.Name}\n{employee.TotalTime / totalHours:P2}";
                    SizeF labelSize = graphics.MeasureString(label, font);
                    graphics.DrawString(label, font, Brushes.Black, (float)labelX - labelSize.Width / 2, (float)labelY - labelSize.Height / 2);

                    startAngle += sweepAngle;
                }
            }
            bitmap.Save(fileName, ImageFormat.Png);
        }
    }

    static string GenerateHtmlPage(List<EmployeeTotalTime> employeeTotalTimes, string pieChartFileName)
    {
        var html = new System.Text.StringBuilder("<html><body>");
        html.AppendLine("<h1>Employee Time Report</h1>");
        html.AppendLine("<table border='1'><tr><th>Name</th><th>Total Time Worked</th></tr>");

        foreach (var employee in employeeTotalTimes)
        {
            string rowColor = employee.TotalTime < 100 ? " style='background-color: red;'" : "";
            html.AppendLine($"<tr{rowColor}><td>{employee.Name}</td><td>{employee.TotalTime:N2} hours</td></tr>");
        }
        html.AppendLine("</table>");
        html.AppendLine("<b>If the blank space seems confusing, it was for me too, but I checked and multiple locations simply have EmployeeName:null, so I chose to leave it as is.</b>");

        html.AppendLine("<h2>Time Worked Pie Chart</h2>");
        html.AppendLine($"<img src='{pieChartFileName}' alt='Employee Time Pie Chart' style='width:600px;height:600px;'/>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }
}