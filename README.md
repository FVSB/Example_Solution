# PowerPositionCalculator

## Project Description

PowerPositionCalculator is a .NET console application that calculates and generates energy position reports in CSV format. The application runs continuously, extracting energy trade data at regular intervals and generating CSV files with calculated volumes.

## Main Features

- **Continuous Execution**: The application runs in an infinite loop, processing data every X minutes
- **Parallel Processing**: Uses parallel processing to optimize volume calculations
- **Thread-Safe Structure**: Implements a data structure with arrays and semaphores to avoid race conditions
- **Retry System**: Robust error handling with configurable retries
- **Complete Logging**: Detailed logging system with Serilog
- **Flexible Configuration**: Configuration via JSON file and command line parameters

## Architecture and Design

### Thread-Safe Data Structure

The application uses a custom data structure `AsyncTradesVolumeArray<T>` that implements:

- **Array with Semaphores**: Each array index has its own semaphore for concurrency control
- **Asynchronous Operations**: Async methods for reading, writing, and modifying data
- **Race Condition Prevention**: Semaphores guarantee exclusive access to each array element
- **Performance Optimization**: Allows concurrent access to different indices simultaneously

```csharp
// Example of using the thread-safe structure
var calculator = new AsyncTradesVolumeTradesVolumenCalculator(24);
await calculator.AddAsync(index, volume, cancellationToken);
```

### Retry System

The retry system is specifically designed to handle `Axpo.PowerServiceException` exceptions:

- **Configurable Retries**: Configurable number of attempts
- **Delay Between Attempts**: Configurable wait time between retries
- **Cooperative Cancellation**: CancellationToken support
- **Detailed Logging**: Logging of each attempt and error

## Configuration

### appsettings.json Location

The `appsettings.json` file must be located in the project root:

```
PowerPositionCalculator/
├── appsettings.json          # ← Configuration file
├── Program.cs
├── Services/
├── Helpers/
└── ...
```

### appsettings.json Configuration

```json
{
  "options": {
    "CsvFolderPath": "C:\\Users\\dell\\source\\repos\\PowerPositionCalculator\\PowerPositionCalculator\\output",
    "TimeMinutes": 1,
    "DateTime": "2025-07-07T00:00:00",
    "DelayMillisecondsInRetryTimes": 2500,
    "RetryTimes": 5
  }
}
```

#### Configuration Parameters

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `CsvFolderPath` | Path where CSV files will be saved | - |
| `TimeMinutes` | Interval in minutes between executions | 1 |
| `Time` | Start date and time for calculations | London Time |
| `DelayMillisecondsInRetryTimes` | Wait time between retries (ms) | 2500 |
| `RetryTimes` | Number of attempts in case of error | 5 |

## Application Usage

### Basic Command

```bash
dotnet run --project PowerPositionCalculator
```

### Command with Parameters

```bash
dotnet run --project PowerPositionCalculator -- [OPTIONS]
```

### Command Line Parameters

The application uses the `--` pattern to separate project parameters from application parameters:

```bash
dotnet run --project PowerPositionCalculator -- -p "C:\output" -t 30 -d "2025-01-15T10:00:00" -r 10 -m 3000
```

#### Available Options

| Option | Description | Required | Default Value | Example |
|--------|-------------|----------|---------------|---------|
| `-p, --path` | Path where CSV files will be saved | ✅ Yes | - | `-p "C:\output"` |
| `-t, --time` | Interval in minutes for the next iteration | ✅ Yes | 25 | `-t 30` |
| `-d, --date` | Optional date and time to start extraction | ❌ No | London Time | `-d "2025-01-15T10:00:00"` |
| `-r, --retry_times` | Number of attempts for failed operations | ❌ No | 10 | `-r 15` |
| `-m, --delay_milliseconds` | Delay in milliseconds between attempts | ❌ No | 2000 | `-m 3000` |
| `--help` | Display this help screen | ❌ No | - | `--help` |
| `--version` | Display version information | ❌ No | - | `--version` |

### Usage Examples

#### Example 1: Basic Configuration
```bash
dotnet run --project PowerPositionCalculator -- -p "C:\PowerData\Output" -t 15
```
- Saves CSV files in `C:\PowerData\Output`
- Runs every 15 minutes
- Uses default values for retries

#### Example 2: Complete Configuration
```bash
dotnet run --project PowerPositionCalculator -- -p "C:\PowerData\Output" -t 30 -d "2025-01-15T09:00:00" -r 20 -m 5000
```
- Saves CSV files in `C:\PowerData\Output`
- Runs every 30 minutes
- Starts from January 15, 2025 at 9:00 AM
- 20 retry attempts
- 5 seconds wait between attempts

#### Example 3: Production Configuration
```bash
dotnet run --project PowerPositionCalculator -- -p "/var/powerdata/output" -t 60 -r 5 -m 1000
```
- Saves CSV files in `/var/powerdata/output`
- Runs every hour
- 5 retry attempts
- 1 second wait between attempts

## Retry System

### Behavior

The retry system is specifically designed to handle `Axpo.PowerServiceException` exceptions:

1. **Error Detection**: When an `Axpo.PowerServiceException` occurs, the system detects it automatically
2. **Automatic Retries**: The application retries the operation up to the configured maximum number
3. **Delay Between Attempts**: Waits the configured time between each attempt
4. **Detailed Logging**: Logs each attempt and result

### Retry Configuration

- **RetryTimes**: Maximum number of attempts (default: 10)
- **DelayMillisecondsInRetryTimes**: Wait time between attempts in milliseconds (default: 2000)

### Retry Flow Example

```
Attempt 1: Axpo.PowerServiceException error → Wait 2 seconds
Attempt 2: Axpo.PowerServiceException error → Wait 2 seconds
Attempt 3: Axpo.PowerServiceException error → Wait 2 seconds
Attempt 4: Success → Continue with processing
```

## Thread-Safe Data Structure

### AsyncTradesVolumeArray<T>

This data structure implements:

- **Array with Individual Semaphores**: Each index has its own semaphore
- **Atomic Operations**: Guarantees consistency in concurrent operations
- **High Performance**: Allows concurrent access to different indices

```csharp
// Initialization
var calculator = new AsyncTradesVolumeTradesVolumenCalculator(24);

// Thread-safe operations
await calculator.AddAsync(0, 100.5, cancellationToken);
await calculator.AddAsync(1, 200.3, cancellationToken);
await calculator.AddAsync(23, 150.7, cancellationToken);

// Get final result
double[] result = calculator.GetArray();
```

### Implementation Advantages

1. **Safe Concurrency**: Multiple threads can access simultaneously
2. **No Global Locks**: Only the specific index being modified is locked
3. **Scalability**: Performance is maintained with multiple threads
4. **Consistency**: Guarantees that data is not corrupted

## Logging

The application uses Serilog for logging with the following features:

- **Date-based Logs**: Logs are organized by date in separate folders
- **Detailed Logs**: Complete information for each operation
- **Error Logs**: Captures and logs all errors and exceptions
- **Retry Logs**: Logs each retry attempt

### Log Location

```
PowerPositionCalculator/logs/
├── 2025-01-15/
│   ├── logs-09-00-00.txt
│   ├── logs-09-15-00.txt
│   └── logs-09-30-00.txt
└── 2025-01-16/
    └── ...
```

## Application Control

### Start
```bash
dotnet run --project PowerPositionCalculator -- [OPTIONS]
```

### Stop
- **Ctrl+C**: Stops the application safely
- **Logging**: Logs the shutdown

### Monitoring
- Logs provide detailed information about the application status
- Each iteration is logged with timestamp
- Errors are captured and logged completely

## System Requirements

- .NET 8.0 or higher
- Access to PowerService API (Axpo)
- Write permissions in the output folder
- Sufficient memory to process trade data

## Project Structure

```
PowerPositionCalculator/
├── appsettings.json              # Application configuration
├── Program.cs                    # Main entry point
├── Services/
│   ├── OptionsParser.cs          # Command line options parser
│   ├── PowerPositionCsvGenerator.cs  # CSV file generator
│   └── TradeVolumeCalculator.cs  # Trade volume calculator
├── Helpers/
│   ├── AsyncTradesVolumeArray.cs # Thread-safe data structure
│   ├── RetryUtils.cs             # Retry utilities
│   ├── TimeUtils.cs              # Time utilities
│   ├── PathUtils.cs              # Path utilities
│   └── Extenders.cs              # Extensions
├── output/                       # CSV output folder
└── logs/                         # Logs folder
```

## Development Notes

This application was developed as part of a technical test, demonstrating:

- **Solid Architecture**: Clear separation of responsibilities
- **Safe Concurrency**: Proper handling of multi-threaded operations
- **Robustness**: Retry system and error handling
- **Configurability**: Flexible configuration
- **Observability**: Complete and detailed logging 