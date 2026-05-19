using System.Runtime.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ClosedXML.Excel;
using OfficeOpenXml;
using Sylvan.Data;
using Sylvan.Data.Excel;

BenchmarkRunner.Run<Benchmarks>();
//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());

[MemoryDiagnoser]
public class Benchmarks
{
    [Params( "inputs/large.xlsx", "inputs/small.xlsx")]
    public string Filename { get; set; } = string.Empty;
    
    [GlobalSetup]
    public void Setup()
    {
        // Set the EPP license to community
        ExcelPackage.License.SetNonCommercialPersonal("James");
    }
    
    [Benchmark]
    public List<NotQ100Data> ReadExcelFile_EPPlus()
    {
        using var package = new ExcelPackage(new FileInfo(Filename));
        var worksheet = package.Workbook.Worksheets.FirstOrDefault(x => x.Name == "NotQ100");
        if (worksheet == null)
        {
            throw new Exception($"Worksheet 'NotQ100' not found in file {Filename}");
        }
        
        // Read the data from the worksheet and store it in a list of NotQ100Data objects
        var data = new List<NotQ100Data>();
        var columnIndex = new Dictionary<string, int>();
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            if (row == 2)
            {
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    columnIndex[worksheet.Cells[row, col].Text] = col;
                }
                continue;
            }
            
            var notQ100Data = new NotQ100Data
            {
                Authority = worksheet.Cells[row, columnIndex["Authority"]].Text,
                Period = worksheet.Cells[row, columnIndex["Period"]].Text,
                QuestionNumber = worksheet.Cells[row, columnIndex["QuestionNumber"]].Text,
                QuestionText = worksheet.Cells[row, columnIndex["QuText"]].Text,
                CollateText = worksheet.Cells[row, columnIndex["CollateText"]].Text,
                RowText = worksheet.Cells[row, columnIndex["RowText"]].Text,
                ColText = worksheet.Cells[row, columnIndex["ColText"]].Text,
                Data = worksheet.Cells[row, columnIndex["Data"]].Text,
                MaterialGroup = worksheet.Cells[row, columnIndex["MaterialGroup"]].Text
            };
            data.Add(notQ100Data);
        }

        return data;
    }


    [Benchmark]
    public List<NotQ100Data> ReadExcelFile_ClosedXML()
    {
        using var workbook = new XLWorkbook(Filename);
        var worksheet = workbook.Worksheet("NotQ100");
        
        // Read the data from the worksheet and store it in a list of NotQ100Data objects
        var data = new List<NotQ100Data>();
        var columnIndex = new Dictionary<string, int>();
        for (int row = 2; row <= worksheet.LastRowUsed().RowNumber(); row++)
        {
            if (row == 2)
            {
                for (int col = 1; col <= worksheet.LastColumnUsed().ColumnNumber(); col++)
                {
                    columnIndex[worksheet.Cell(row, col).GetString()] = col;
                }
                continue;
            }
            
            var notQ100Data = new NotQ100Data
            {
                Authority = worksheet.Cell(row, columnIndex["Authority"]).GetString(),
                Period = worksheet.Cell(row, columnIndex["Period"]).GetString(),
                QuestionNumber = worksheet.Cell(row, columnIndex["QuestionNumber"]).GetString(),
                QuestionText = worksheet.Cell(row, columnIndex["QuText"]).GetString(),
                CollateText = worksheet.Cell(row, columnIndex["CollateText"]).GetString(),
                RowText = worksheet.Cell(row, columnIndex["RowText"]).GetString(),
                ColText = worksheet.Cell(row, columnIndex["ColText"]).GetString(),
                Data = worksheet.Cell(row, columnIndex["Data"]).GetString(),
                MaterialGroup = worksheet.Cell(row, columnIndex["MaterialGroup"]).GetString()
            };
            data.Add(notQ100Data);
        }

        return data;
    }

    [Benchmark]
    public List<NotQ100Data> ReadExcelFile_Sylvan()
    {
        using ExcelDataReader edr = ExcelDataReader.Create(Filename);
        var ret = new List<NotQ100Data>();

        do
        {
            if(string.IsNullOrEmpty(edr.WorksheetName))
                continue;
            
            edr.ReadAsync(); // Skip the first row since it's always a comment :'(
            edr.Initialize();
                            
            if (edr.WorksheetName.Equals("NotQ100", StringComparison.OrdinalIgnoreCase))
            {
                var data = edr.GetRecords<NotQ100Data>(new DataBinderOptions()
                {
                    BindingMode = DataBindingMode.AllProperties
                });

                ret.AddRange(data);

            }
        } while (edr.NextResult());
        
        return ret;
    }
}

public class NotQ100Data
{
    public string Authority { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string QuestionNumber { get; set; } = string.Empty;
    [DataMember(Name = "QuText")]
    public string QuestionText { get; set; } = string.Empty;
    public string? CollateText { get; set; }
    public string? RowText { get; set; }
    public string? ColText { get; set; }   
    public string Data { get; set; } = string.Empty;   
    public string? MaterialGroup { get; set; }
}