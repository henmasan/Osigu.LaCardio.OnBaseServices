# RCM Upload Functionality Port - Implementation Notes

## Overview
This document describes the implementation of RCM upload functionality in the .NET Framework 4.6.1 WinForms project (`ProcesadorSoportesMinisterio`), ported from the .NET 7 worker service (`Osigu.OnBaseServices.GetSupportSettlement`).

## Changes Made

### 1. Configuration Model Classes (`Model/`)
- **AppsettingConfiguration.cs**: Updated to include `RcmApi`, `Servinte`, `RcmStagingPath`, and `SendSupportToRcmEnabled` properties
- **RcmApiSettings.cs**: New class containing RCM API configuration (BaseUrl, ClientId, ClientSecret, AuthPath, UploadPath, Retries, MaxConcurrentUploads, TokenExpiration)
- **ServinteSettings.cs**: New class containing database configuration (DatabaseOrigin, ConnectionStrings for Oracle/Informix, RcmSendExecutionFrequency, SupportFileCodeMapping)
- **ServinteInvoiceInfo.cs**: Models for invoice information from Servinte (ServinteInvoiceInfo, InvoiceEvent, GroupedServinteInvoiceInfo)
- **SupportTraceRecord.cs**: Model for SQLite trace records tracking support upload status
- **RequestData.cs**: Models for RCM API request/response structure (RequestData, EventInfo, SupportFileMetadata, RcmResponse, RcmAuthToken)

### 2. RCM Service Layer (`Rcm/`)
- **RcmAuthClient.cs**: Handles OAuth2 authentication with RCM API
  - Token caching with 30-second expiration buffer
  - Semaphore-based single-flight for concurrent requests
  - Supports custom token expiration override from config
  
- **RcmSupportClient.cs**: Handles support file uploads to RCM
  - Multipart form data with file content and JSON metadata
  - Supports HTTP 204 (No Content) as success
  - Snake_case JSON serialization for API compatibility

- **IServinteInvoiceRepository.cs**: Interface for Servinte database queries
  - **OracleServinteInvoiceRepository.cs**: Oracle implementation using Oracle.ManagedDataAccess
    - DISTINCT query filtering by ENVFAECUF IS NOT NULL
  - **InformixServinteInvoiceRepository.cs**: Informix implementation using IBM.Data.DB2
    - LEFT JOIN CAENC (no ENVFAECUF filtering)

- **SqliteSupportTraceStore.cs**: SQLite persistence layer
  - Table with status tracking (Pending, Success, Failed)
  - Schema migrations for new columns
  - WAL (Write-Ahead Logging) for reliability
  - Busy timeout to handle concurrent access

- **SendSupportToRcm.cs**: Main queue processor
  - Reads Pending traces from SQLite
  - Batches processing with semaphore-based concurrency limiting
  - Retry logic with attempt counting
  - Moves failed files to `failed/` subfolder
  - Deletes successfully uploaded files from staging

### 3. Capture/Registration Layer
- **SearchSupport.cs**: Updated `MoveFiles()` method to:
  1. Append to OnBase index file `Support{invoice}.txt`
  2. Copy file to OnBase destination path
  3. Move original to RCM staging path
  4. Extract CUV metadata (ProcessId, UniqueVerificationCode) from JSON
  5. Register Pending trace record in SQLite

### 4. UI Integration (Form1.cs)
- Added second `Timer` for RCM queue processing (separate from OnBase processing)
- Initialize RCM services on button start (if `SendSupportToRcmEnabled == true`)
- Timer interval controlled by `Servinte:RcmSendExecutionFrequency`
- Anti-reentry guard (`_rcmProcessing` flag) prevents overlapping queue processes
- Queue processing runs in `Task.Run()` to avoid UI blocking

### 5. Configuration (appsettings.json)
Added two new root sections:
```json
{
  "Configuration": {
    "RcmStagingPath": "\\\\...\\RCM",
    "SendSupportToRcmEnabled": false,
    ...
  },
  "RcmApi": {
    "BaseUrl": "https://api.osigu.com",
    "ClientId": "...",
    "ClientSecret": "...",
    "AuthPath": "/v1/oauth/token",
    "UploadPath": "/rcm/v1/support-files/upload",
    "Retries": 2,
    "MaxConcurrentUploads": 1,
    "TokenExpiration": 86400
  },
  "Servinte": {
    "DatabaseOrigin": "Informix",  // or "Oracle"
    "ConnectionString": "...",
    "InformixConnectionString": "...",
    "RcmSendExecutionFrequency": 300000,
    "SupportFileCodeMapping": [...]
  }
}
```

### 6. Project Files
- **packages.config**: Added System.Data.SQLite.Core, Oracle.ManagedDataAccess, IBM.Data.DB2.NET
- **ProcesadorSoportesMinisterio.csproj**: 
  - Added HintPath references to new NuGet packages
  - Added Compile includes for all new `.cs` files in Model/ and Rcm/ folders

## Key Differences from .NET 7 Implementation

1. **No Dependency Injection**: Uses manual instantiation in Form1.cs instead of DI container
2. **C# 7.3 Constraints**: No nullable reference types, no switch expressions, no default interface members
3. **No Async All The Way**: Use `Task.Run()` for background processing instead of proper async/await throughout
4. **WinForms Timer Instead of BackgroundService**: Two separate timers for OnBase and RCM processing
5. **No Logging Framework**: Errors are logged to database observations and caught locally; consider adding file logging
6. **Manual Package Management**: packages.config vs PackageReference

## Behavior

### When `SendSupportToRcmEnabled` = false (default):
- OnBase capture proceeds normally (copy to destination, write index file)
- Files are **also** copied to `RcmStagingPath`
- **Pending** traces are registered in SQLite
- Queue processor never runs
- Traces accumulate for later processing

### When `SendSupportToRcmEnabled` = true:
- OnBase capture + RCM staging happens as above
- Queue processor runs on separate timer every `RcmSendExecutionFrequency` ms
- For each Pending trace:
  1. Query invoice metadata from Servinte (cached per invoice number + EAD code)
  2. Authenticate to RCM (cached token with 30s expiration buffer)
  3. Upload file with metadata
  4. On success: mark Success, delete staged file
  5. On failure: increment attempt count, stay Pending
  6. After max retries: mark Failed, move to `failed/` subfolder

## Files Modified
- `Model/AppsettingConfiguration.cs` (added RcmApi, Servinte, RcmStagingPath, SendSupportToRcmEnabled)
- `SearchSupport.cs` (updated MoveFiles, added ISupportTraceStore parameter)
- `Form1.cs` (added RCM timer, services initialization, queue processing)
- `packages.config` (added 3 new packages)
- `ProcesadorSoportesMinisterio.csproj` (added references and Compile includes)
- `appsettings.json` (added RcmApi and Servinte sections)

## Files Created
### Model:
- RcmApiSettings.cs
- ServinteSettings.cs
- ServinteInvoiceInfo.cs
- SupportTraceRecord.cs
- RequestData.cs

### Rcm:
- IServinteInvoiceRepository.cs
- OracleServinteInvoiceRepository.cs
- InformixServinteInvoiceRepository.cs
- RcmAuthClient.cs
- RcmSupportClient.cs
- SqliteSupportTraceStore.cs
- SendSupportToRcm.cs

## Testing Checklist
- [ ] Restore NuGet packages (Visual Studio auto-restore or manual)
- [ ] Build solution Debug|AnyCPU
- [ ] Build solution Release|AnyCPU
- [ ] With flag OFF: capture processes same as before (file copy, index write, folder delete)
- [ ] With flag OFF: traces are created in RcmStagingPath/support_trace.db
- [ ] With flag ON: test OAuth token caching and expiration
- [ ] With flag ON: test successful upload -> Success, file deleted
- [ ] With flag ON: test invoice not found -> retry logic
- [ ] With flag ON: test max retries exceeded -> Failed + moved to failed/
- [ ] With flag ON: test CUV JSON parsing (ProcessId, CodigoUnicoValidacion)
- [ ] Verify no changes to OnBase behavior or file naming

## Known Limitations / TODOs
1. No file-based logging (only DB observations); consider adding Serilog or similar
2. No metrics/telemetry on processing performance
3. Oracle driver expects net462+ compatible version; verify in your environment
4. IBM.Data.DB2.NET may require additional setup (connection pooling, client installation)
5. Error messages in observations are English-language only
6. No scheduled retry with exponential backoff (uses fixed poll interval)
