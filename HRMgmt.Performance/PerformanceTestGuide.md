# Performance Testing Guide

## How to Run the Tests

To execute the performance tests, use the **dotnet CLI** from the `HRMgmt.Performance` directory.

### 1. Run All Benchmarks
This will run all benchmarks in the project. Note that this may take a significant amount of time (20+ minutes) due to the number of tests and iterations.

```powershell
cd HRMgmt.Performance
dotnet run -c Release --filter *Benchmark*
```

### 2. Run Specific Benchmarks
You can filter to run only specific benchmarks using the `--filter` argument.

**Example: Run only AccountController benchmarks**
```powershell
dotnet run -c Release --filter *AccountControllerBenchmark*
```

**Example: Run only Payroll benchmarks**
```powershell
dotnet run -c Release --filter *PayrollBenchmark*
```

## Benchmark Results Summary

The following are the average execution times (Mean) from the initial test run.

### AccountControllerBenchmark
| Method | Average Time | Notes |
| :--- | :--- | :--- |
| **Login** | **148.4 ms** | Includes password hashing cost |
| **Index** | **4.7 μs** | Very fast (simple view return) |
| **CreateAccount** | **148.9 ms** | Heavy due to password hashing |
| **EditAccount** | **14.6 μs** | Efficient EF Core update |
| **DeleteAccount** | **17.9 μs** | Efficient EF Core delete |

### PayrollBenchmark (Partial Results)
| Method | Average Time | Notes |
| :--- | :--- | :--- |
| **AdminCalculate (N=10)** | **197.4 μs** | Payroll calculation logic |
| **Index (N=10)** | **5.5 μs** | Simple list retrieval |
| **CreatePayroll (N=10)** | *~30 ms* | (Currently executing) |

*> **Note:** "μs" = Microseconds (0.000001 sec), "ms" = Milliseconds (0.001 sec)*

## Recommended Performance Thresholds (SLOs)

To maintain a high-quality user experience, we recommend the following "Performance Budgets." If a benchmark consistently exceeds these values, it should be investigated for regressions.

| Category | Budget | Description |
| :--- | :--- | :--- |
| **Identity & Security** | **< 250 ms** | Actions involving BCrypt password hashing (Login, Register). |
| **Complex Logic** | **< 50 ms** | Background processing like Payroll generation or Auto-Scheduling. |
| **Data Access (CRUD)** | **< 1 ms** | Standard database operations (Create, Edit, Delete) using EF Core. |
| **UI Light-Weight** | **< 100 μs** | Actions that simply load a static view or index. |

> [!TIP]
> **Why 250ms?** Password hashing is intentionally slow to prevent brute-force attacks. Anything under 300ms feels "instant" to a human, but anything over 500ms starts to feel sluggish.

## Detailed Reports
After the benchmarks complete, detailed HTML, CSV, and Markdown reports are generated in the artifacts folder:

`HRMgmt.Performance/BenchmarkDotNet.Artifacts/results/`

Open the **.html** files in your browser for rich graphs and detailed memory allocation statistics.
