using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System;
using System.IO;
using System.Linq;

namespace HRMgmt.Performance
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Exporters explicitly
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddExporter(HtmlExporter.Default)
                .AddExporter(CsvExporter.Default)
                .AddExporter(MarkdownExporter.GitHub)
                .AddLogger(ConsoleLogger.Default);

            var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

            // Print the location of the reports
            Console.WriteLine();
            Console.WriteLine("---------------------------------------------------------");
            Console.WriteLine("Performance Test Reports Generated:");
            Console.WriteLine("---------------------------------------------------------");

            foreach (var report in summary)
            {
                var reportDir = report.ResultsDirectoryPath;
                if (Directory.Exists(reportDir))
                {
                    var files = Directory.GetFiles(reportDir, "*report*.*", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                         Console.WriteLine($" - {file}");
                    }
                }
            }
            Console.WriteLine("---------------------------------------------------------");
        }
    }
}
