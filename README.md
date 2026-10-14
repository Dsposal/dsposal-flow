<p align="center">
  <img src="./.docs/dsposal.jpg" alt="Dsposal.Flow" width="430"/>
</p>

# Dsposal.Flow

> **Beta** - .NET 10 CLI tool for processing UK Waste Data Flow (WDF) Excel files into clean, normalized CSV datasets.

Waste Data Flow (WDF) is the UK-wide system used by waste operators to submit quarterly returns to environmental regulators (EA, NRW, SEPA, NIEA). Dsposal.Flow reads these `.xlsx` exports, extracts structured data from the Q100 and NotQ100 worksheets, and produces relational CSV files suitable for injesting into a relational database for analysis, reporting, and further processing.

Built for performance - powered by [Sylvan.Data.Excel](https://github.com/MarkPflug/Sylvan.Data.Excel) for fast, low-memory Excel reading.

## Features

- **Process WDF worksheets** - handles both Q100 (waste flow) and NotQ100 (questionnaire) sheet types
- **Interactive or CLI-driven** - no arguments launches a Spectre.Console file picker; otherwise fully scriptable
- **Concurrent file processing** - processes up to 4 files in parallel with progress feedback
- **Normalized CSV output** - produces a relational schema (facilities, waste streams, materials, periods, authorities, etc.)
- **UK permit/registration extraction** - regex-based identification of EA, NRW, SEPA, and NIEA permit numbers from text fields
- **Cross-platform** - runs on Linux, macOS, and Windows (.NET 10)

## TODO

- Full denormalization of NotQ100 data alongside Q100 output (currently prioritised Q100 for internal use).
- Tests
- Downloadable executable for users who don't want to build the tool themselves
- Re-write into Rust for MOOOORE speed??

## Obtaining WDF Data

[WasteDataFlow (WDF)](https://www.wastedataflow.org/home.aspx) is the web-based system for municipal waste data reporting by UK local authorities to government, serving as the data collection system for the Defra Municipal Waste Management Survey in England and similar surveys in Wales, Northern Ireland and Scotland.

You must first have an account on the [official WasteDataFlow website](https://www.wastedataflow.org/home.aspx) in order to download the raw data that this tool uses to produce it's outputs.
This tool has been specifically designed for the "Raw Data" report type generated from the WDF website (which can be generated at https://www.wastedataflow.org/reports/default.aspx) and has been tested with the following parameters to generate the reports:

- National Region: **England**
- Period Type: **Quarterly**
- Report Type: **Raw Data**
- Report: **Raw Data Download**
- Authority List: **England**
- Available Periods: **Various** - we have tested with the latest dataset from 2024, downloading each quarter seperatly to speed up data aquisition.
- Selected Questions: **ALL**

> Dsposal.Flow is not affiliated with the Environment Agency, Defra, or the WasteDataFlow system. The tool processes standard WDF export files - you will need appropriate access to the WDF portal to obtain input data.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Quick Start

```bash
git clone https://github.com/Dsposal/dsposal-flow.git
cd dsposal-flow
dotnet run --project Dsposal.Flow -i /path/to/wdf/files -o ./output
```

Run without arguments to launch the interactive file picker:

```bash
dotnet run --project Dsposal.Flow
```

## CLI Usage

| Option | Short | Description | Default |
|---|---|---|---|
| `--input` | `-i` | One or more file paths or directories containing `.xlsx` files | Interactive picker |
| `--output` | `-o` | Output directory for CSV files | `./output` |
| `--log-level` | `-l` | Log level: `Trace`, `Debug`, `Information`, `Warning`, `Error` | `Information` |
| `--recursive-dir-search` | `-r` | Search subdirectories when input is a directory | `true` |

### Example

```bash
dotnet run --project Dsposal.Flow \
  -i "~/wdf/Q1-2026.xlsx" "~/wdf/Q2-2026" \
  -o "./processed" \
  -l Debug
```

## Output

Files are written to the specified output directory (default `./output`):

```
output/
├── Q100.csv                           # Denormalized waste flow data
└── NormalizedTables/
    ├── FacilityTypes.csv               # Reference: facility type lookups
    ├── WasteStreamTypes.csv            # Reference: waste stream categories
    ├── NationalFacilities.csv          # Facility names, addresses, licences
    ├── OutputProcessTypes.csv          # Reference: output process types
    ├── Materials.csv                   # Reference: material descriptions
    ├── Usages.csv                      # Reference: usage descriptions
    ├── Periods.csv                     # Unique reporting periods
    ├── Questions.csv                   # NotQ100 questions
    └── Authorities.csv                 # Local authorities
```

## Performance

Dsposal.Flow uses [Sylvan.Data.Excel](https://github.com/MarkPflug/Sylvan.Data.Excel) for Excel reading, chosen for its exceptional performance:

| Library | Time (large file) | vs. Sylvan |
|---|---|---|
| **Sylvan.Data.Excel** | **~311 ms** | **1×** |
| EPPlus | ~6,811 ms | ~22× slower |
| ClosedXML | ~12,137 ms | ~39× slower |

See [`Dsposal.Flow.Benchmarks/Benchmarks-report-github.md`](Dsposal.Flow.Benchmarks/Benchmarks-report-github.md) for full benchmark details.

Don't believe us on the speed?
We have tested this tool with 2020 to 2024 data and it's extrodinarily fast. Seriously!
Just over 1,400,000 records read in 10 seconds then it's processed and written in less than 20!

```bash
11:22:09 [Info ] Dsposal.Flow:         
                 Starting processing...
                                                                                                                 
Processed England 2023 and JanMar 2024.xlsx ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   0% 00:00:06               
   Processed England - Oct 24 - Dec 24.xlsx ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   0% 00:00:03               
                Processed England 2020.xlsx ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   0% 00:00:05               
   Processed England - Apr 24 - Jun 24.xlsx ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   0% 00:00:03               
                Processed England 2022.xlsx ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   0% 00:00:05               
                Processed England 2021.xlsx ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   0% 00:00:05               
   Processed England - Jul 24 - Sep 24.xlsx ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   0% 00:00:03               
                                                                                                                 
11:22:19 [Info ] Dsposal.Flow.Services.DataProcessor:
                 Finished processing all files...    
11:22:19 [Info ] Dsposal.Flow.Services.DataProcessor:
                 ┌─────────────┬─────────┐           
                 │ Data type   │ Count   │           
                 ├─────────────┼─────────┤           
                 │ NotQ100Data │ 434810  │           
                 │ Q100Data    │ 1404633 │           
                 └─────────────┴─────────┘           
11:22:19 [Info ] Dsposal.Flow.Services.CsvFileService:                       
                 Facility types CSV file written to ./output/NormalizedTables
11:22:19 [Info ] Dsposal.Flow.Services.CsvFileService:                           
                 Waste stream types CSV file written to ./output/NormalizedTables
11:22:19 [Info ] Dsposal.Flow.Services.CsvFileService:                                
                 National facility names CSV file written to ./output/NormalizedTables
11:22:19 [Info ] Dsposal.Flow.Services.CsvFileService:                             
                 Output process types CSV file written to ./output/NormalizedTables
11:22:19 [Info ] Dsposal.Flow.Services.CsvFileService:                  
                 Materials CSV file written to ./output/NormalizedTables
11:22:19 [Info ] Dsposal.Flow.Services.CsvFileService:               
                 Usages CSV file written to ./output/NormalizedTables
11:22:19 [Info ] Dsposal.Flow.Services.CsvFileService:                  
                 Questions CSV file written to ./output/NormalizedTables
11:22:19 [Info ] Dsposal.Flow.Services.CsvFileService:                
                 Periods CSV file written to ./output/NormalizedTables
11:22:20 [Info ] Dsposal.Flow.Services.CsvFileService:                    
                 Authorities CSV file written to ./output/NormalizedTables
11:22:24 [Info ] Dsposal.Flow.Services.CsvFileService:
                 Q100 CSV file written to ./output
```

## Built With

- [.NET 10](https://dotnet.microsoft.com/) - runtime and SDK
- [CommandLineParser](https://github.com/commandlineparser/commandline) - CLI argument parsing
- [Spectre.Console](https://spectreconsole.net/) - console UI, progress bars, tables
- [Sylvan.Data.Excel](https://github.com/MarkPflug/Sylvan.Data.Excel) - high-performance Excel reader
- [Sylvan.Data.Csv](https://github.com/MarkPflug/Sylvan.Data.Csv) - high-performance CSV writer
- [NUnit](https://nunit.org/) - unit testing
- [BenchmarkDotNet](https://benchmarkdotnet.org/) - performance benchmarking

## License

[GNU General Public License v3.0](LICENSE.md)
