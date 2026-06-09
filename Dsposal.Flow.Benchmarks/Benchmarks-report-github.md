```

BenchmarkDotNet v0.15.8, Linux EndeavourOS
AMD Ryzen 9 5900X 1.73GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.104
  [Host]     : .NET 10.0.4 (10.0.4, 42.42.42.42424), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.4 (10.0.4, 42.42.42.42424), X64 RyuJIT x86-64-v3


```
| Method                  | Filename          | Mean         | Error      | StdDev     | Ratio | RatioSD | Gen0        | Gen1       | Gen2       | Allocated  | Alloc Ratio |
|------------------------ |------------------ |-------------:|-----------:|-----------:|------:|--------:|------------:|-----------:|-----------:|-----------:|------------:|
| **ReadExcelFile_EPPlus**    | **inputs/large.xlsx** |  **6,811.04 ms** | **116.144 ms** | **108.641 ms** |  **0.56** |    **0.02** | **168000.0000** | **53000.0000** | **11000.0000** | **3992.24 MB** |       **0.759** |
| ReadExcelFile_ClosedXML | inputs/large.xlsx | 12,137.22 ms | 233.337 ms | 286.559 ms |  1.00 |    0.03 | 332000.0000 | 89000.0000 |  5000.0000 | 5261.16 MB |       1.000 |
| ReadExcelFile_Sylvan    | inputs/large.xlsx |    311.33 ms |   2.943 ms |   2.609 ms |  0.03 |    0.00 |   3000.0000 |  2000.0000 |  1000.0000 |   36.32 MB |       0.007 |
|                         |                   |              |            |            |       |         |             |            |            |            |             |
| **ReadExcelFile_EPPlus**    | **inputs/small.xlsx** |  **1,331.45 ms** |  **13.071 ms** |  **12.227 ms** | **0.488** |    **0.00** |  **47000.0000** | **22000.0000** | **12000.0000** |  **853.34 MB** |       **0.666** |
| ReadExcelFile_ClosedXML | inputs/small.xlsx |  2,725.77 ms |  12.948 ms |  11.478 ms | 1.000 |    0.01 |  82000.0000 | 23000.0000 |  3000.0000 | 1281.16 MB |       1.000 |
| ReadExcelFile_Sylvan    | inputs/small.xlsx |     19.80 ms |   0.219 ms |   0.194 ms | 0.007 |    0.00 |    468.7500 |   218.7500 |   218.7500 |    6.05 MB |       0.005 |


**inputs/large.xlsx** is a 35.8 MiB excel file with just over 269,000 rows in the Q100 sheet.
**inputs/small.xlsx** is a 3.7 MiB excel file with just over 82,000 rows in the Q100 sheet.
