# CodeEditor2VerilogPlugin Parse Instability Analysis

## Overview
Analysis of parse result instability issues in RtlEditor2, particularly when parsing deep hierarchies and clicking through tree nodes.

## Observed Symptoms
1. Module instances not correctly referenced below certain modules
2. Parse results change each time nodes are clicked and reparsed
3. Text color highlighting and error states fluctuate
4. Tree navigation produces inconsistent results

## Root Cause Analysis

### 1. Race Conditions in Parallel Parse Processing

**Location:** `Tool/ParseHierarchy.cs`

The `runParallel` method uses parallel workers with cancellation tokens:

```csharp
private static Task? _currentTask;
private static CancellationTokenSource? _cts;

public static async Task ParseAsync(CodeEditor2.Data.TextFile textFile, ParseMode parseMode)
{
    if (_cts != null)
    {
        textFile.ReparseRequested = true;
        _cts.Cancel(); // Cancels previous parse task
    }
    // ... new task starts immediately
}
```

**Problem:** When nodes are clicked rapidly:
- Parse task A starts
- User clicks another node → cancellation + new parse
- Parse task A may still complete and update UI
- Parse task B also completes and updates UI
- Both tasks may interleave their AcceptParsedDocumentAsync calls

### 2. Shared Root BuildingBlock Dictionary

**Location:** `Verilog/BuildingBlocks/BuildingBlock.cs`

```csharp
public Dictionary<string, BuildingBlock> BuildingBlocks { get; set; } = 
    new Dictionary<string, BuildingBlock>();
```

The `Root.BuildingBlocks` dictionary is shared and modified by multiple parsers when:
- Multiple module instances reference the same source file
- Parameter-overridden instances are parsed

**Problem:** Multiple concurrent parses all try to modify the same shared dictionary without proper synchronization.

### 3. Non-Atomic BuildingBlock Registration

**Location:** `VerilogFile.AcceptParsedDocumentAsync` -> `ProjectProperty.RegisterBuildingBlock`

```csharp
if (vParsedDocument.Root != null)
{
    foreach (BuildingBlock buildingBlock in vParsedDocument.Root.BuildingBlocks.Values)
    {
        ProjectProperty.RegisterBuildingBlock(buildingBlock.Name, buildingBlock, this);
    }
}
```

**Problem:** The registration of building blocks into `buildingBlockTable` is not atomic with the parsing process. Another parser may start before registration completes.

### 4. Race in Updater.UpdateAsync

**Location:** `Data/VerilogCommon/Updater.cs`

```csharp
lock (item.Items)
{
    item.Items.Clear(); // DANGER: Temporary inconsistent state
    foreach (CodeEditor2.Data.Item i in newSubItems.Values)
    {
        item.Items.AddOrUpdate(i.Name, i);
    }
}
```

**Problem:** The Items dictionary is cleared before adding new items, creating a window where:
- UI may read empty Items
- Another parser may modify during clear
- Navigating tree during this time causes inconsistent states

### 5. VerilogModuleInstance ParsedDocument Property Race

**Location:** `Data/VerilogModuleInstance.cs`

```csharp
public override CodeEditor2.CodeEditor.ParsedDocument? ParsedDocument
{
    get
    {
        Data.VerilogFile source = SourceVerilogFile;
        ParsedDocument? parsedDocument = source.GetInstancedParsedDocument(_getKey());
        return parsedDocument;
    }
}
```

**Problem:** The `_getKey()` method reads `_moduleName` and `_parameterOverrides`:
```csharp
private string _getKey()
{
    textFileLock.EnterReadLock();
    try
    {
        string moduleName = _moduleName;
        var parameterOverrides = _parameterOverrides;
        return Verilog.ParsedDocument.KeyGenerator(this, moduleName, parameterOverrides);
    }
    finally
    {
        textFileLock.ExitReadLock();
    }
}
```

While individual reads are locked, the key generation and subsequent dictionary access is not atomic - another thread could modify values between the two reads.

### 6. CodeDocument Shared State with UI

**Location:** `CodeEditor2/CodeEditor/CodeDocument.cs` and `ColorHandler.cs`

When the parser completes, color marks are copied:
```csharp
targetCodeDocument.CopyColorMarkFrom(parser.Document);
```

**Problem:** The UI thread may simultaneously modify the CodeDocument through `ColorHandler.OnTextEdit()`, causing:
- Color information corruption
- Visual flickering
- Inconsistent highlight states

### 7. Parse Mode Dependent Behavior

**Location:** Multiple parse methods in `Verilog/BuildingBlocks/Root.cs`

```csharp
if (parsedDocument.ParseMode == Parser.VerilogParser.ParseModeEnum.LoadParse)
{
    module = await Module.ParseCreate(word, null, parsedDocument.Root, file, true); // prototype=true
    parsedDocument.ReparseRequested = true; // Force reparse
}
else
{
    module = await Module.ParseCreate(word, null, parsedDocument.Root, file, false);
    parsedDocument.ReparseRequested = false;
}
```

**Problem:** LoadParse mode always sets ReparseRequested=true, which causes subsequent parses. If the module lookup happens during the "LoadParse" phase before the actual parse completes, stale data may be used.

### 8. Target BuildingBlock Skip Logic

**Location:** `Verilog/BuildingBlocks/Root.cs` -> `parseModule`

```csharp
if (parsedDocument.TargetBuildingBlockName != null)
{
    if (word.NextText != parsedDocument.TargetBuildingBlockName)
    {
        skipBlock(word, "module", "endmodule"); // Skips ALL other blocks
        return;
    }
}
```

**Problem:** When targeting a specific module, ALL other modules are skipped (colored as inactive). This means if multiple module instances exist, only one is properly parsed at a time.

### 9. Include File Update Race

**Location:** `VerilogFile.updateIncludeFilesAsync`

```csharp
await updateIncludeFilesAsync(vParsedDocument, Items);
```

Include files are updated separately from the main parse. If the Items dictionary is in flux during this update, include file references may be lost.

### 10. ParseWorker Race Condition

**Location:** `CodeEditor2/CodeEditor/Parser/ParseWorker.cs`

```csharp
private async Task runParse(Data.TextFile textFile, System.Threading.CancellationToken token)
{
    await runSingleParse(textFile, token); // Parse root file
    // ...
    foreach (Data.Item item in items)
    {
        if (!subFile.ReparseRequested) continue;
        await runSingleParse(subFile, token); // Parse sub-items
    }
}
```

**Problem:** Sub-file parsing checks `ReparseRequested` after the root parse completes. If multiple EntryParse calls happen, the state of `ReparseRequested` may change between checks.

## Recommended Fixes

### 1. Implement Parse Request Queue
Instead of immediate cancellation, queue parse requests and process sequentially:
```csharp
private static ConcurrentQueue<ParseRequest> _parseQueue = new();
private static async Task ProcessQueueAsync()
{
    while (_parseQueue.TryDequeue(out var request))
    {
        await ParseAsync(request.TextFile, request.Mode);
    }
}
```

### 2. Use Atomic BuildingBlock Updates
Protect the BuildingBlocks dictionary with a lock at the Root level:
```csharp
private readonly object buildingBlocksLock = new();
public void AddBuildingBlock(string name, BuildingBlock block)
{
    lock (buildingBlocksLock)
    {
        if (BuildingBlocks.ContainsKey(name))
            BuildingBlocks[name] = block;
        else
            BuildingBlocks.Add(name, block);
    }
}
```

### 3. Fix Updater.UpdateAsync Atomicity
Use a more atomic update pattern:
```csharp
var tempItems = new Dictionary<string, CodeEditor2.Data.Item>(item.Items);
tempItems.Clear();
foreach (var newItem in newSubItems.Values)
{
    tempItems[newItem.Name] = newItem;
}
// Swap atomically
item.Items = tempItems;
```

### 4. Add Parse Completion Barrier
Wait for all pending parses to complete before accepting new results:
```csharp
public async Task AcceptParsedDocumentAsync(ParsedDocument? newParsedDocument)
{
    // Wait for any in-progress parse to complete
    await _parseSemaphore.WaitAsync();
    try
    {
        // ... accept logic
    }
    finally
    {
        _parseSemaphore.Release();
    }
}
```

### 5. Version-Based Reject Stale Results
Add stronger version checking:
```csharp
if (codeDoc.Version != vParsedDocument.Version)
{
    vParsedDocument.ReparseRequested = true;
    return; // Reject stale result
}
```

### 6. Implement Parse Mode Sequencing
Ensure LoadParse completes before EditParse runs:
```csharp
private SemaphoreSlim _parseModeGate = new SemaphoreSlim(1, 1);
public async Task ParseAsync(ParseModeEnum mode)
{
    if (mode == ParseModeEnum.EditParse)
    {
        // Wait for LoadParse to complete
        await _parseModeGate.WaitAsync();
        try { /* do parse */ }
        finally { _parseModeGate.Release(); }
    }
}
```

## Investigation Status
- [x] ParseHierarchy parallel processing analysis
- [x] BuildingBlock dictionary synchronization analysis
- [x] Updater.UpdateAsync atomicity analysis
- [x] VerilogModuleInstance property race analysis
- [x] CodeDocument shared state analysis
- [x] Parse mode dependency analysis
- [x] Include file update race analysis
- [x] ParseWorker race analysis
- [ ] Detailed fix implementation
- [ ] Test case verification

## Related Files
- `Tool/ParseHierarchy.cs` - Main parallel parse orchestrator
- `Verilog/BuildingBlocks/BuildingBlock.cs` - BuildingBlock base class
- `Verilog/BuildingBlocks/Root.cs` - Root building block
- `Data/VerilogFile.cs` - VerilogFile data class
- `Data/VerilogModuleInstance.cs` - Module instance data
- `Data/VerilogCommon/Updater.cs` - Update logic
- `CodeEditor2/CodeEditor/Parser/ParseWorker.cs` - Background parse worker
