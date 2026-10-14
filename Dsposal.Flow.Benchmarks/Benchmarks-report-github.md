```
BenchmarkDotNet v0.15.8, Linux EndeavourOS
AMD Ryzen 9 5900X 1.73GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.112
  [Host]     : .NET 10.0.12 (10.0.12, 42.42.42.42424), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 42.42.42.42424), X64 RyuJIT x86-64-v3
```

| Method                  | Filename          | Mean         | Error     | StdDev    | Ratio | Gen0        | Gen1       | Gen2       | Allocated  | Alloc Ratio |
|------------------------ |------------------ |-------------:|----------:|----------:|------:|------------:|-----------:|-----------:|-----------:|------------:|
| **ReadExcelFile_EPPlus**    | **inputs/large.xlsx** |  **6,816.39 ms** | **64.405 ms** | **57.093 ms** |  **0.56** | **168000.0000** | **54000.0000** | **11000.0000** | **3995.99 MB** |        **0.76** |
| ReadExcelFile_ClosedXML | inputs/large.xlsx | 12,205.49 ms | 66.589 ms | 62.287 ms |  1.00 | 332000.0000 | 89000.0000 |  5000.0000 | 5264.91 MB |        1.00 |
| ReadExcelFile_Sylvan    | inputs/large.xlsx |    383.67 ms |  3.539 ms |  3.310 ms |  0.03 |   4000.0000 |  3000.0000 |  1000.0000 |   53.72 MB |        0.01 |
|                         |                   |              |           |           |       |             |            |            |            |             |
| **ReadExcelFile_EPPlus**    | **inputs/small.xlsx** |  **1,342.96 ms** | **10.113 ms** |  **8.965 ms** | **0.473** |  **47000.0000** | **22000.0000** | **12000.0000** |   **853.5 MB** |       **0.666** |
| ReadExcelFile_ClosedXML | inputs/small.xlsx |  2,839.82 ms | 31.600 ms | 29.558 ms | 1.000 |  82000.0000 | 23000.0000 |  3000.0000 | 1281.32 MB |       1.000 |
| ReadExcelFile_Sylvan    | inputs/small.xlsx |     22.20 ms |  0.086 ms |  0.072 ms | 0.008 |    468.7500 |   218.7500 |   218.7500 |    6.76 MB |       0.005 |


- **inputs/large.xlsx** is a 35.8 MiB excel file with just over 269,000 rows in the Q100 sheet.

- **inputs/small.xlsx** is a 3.7 MiB excel file with just over 82,000 rows in the Q100 sheet.
