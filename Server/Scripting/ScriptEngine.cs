using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Items;
using RealmOfReality.Shared.Skills;

namespace RealmOfReality.Server.Scripting;

/// <summary>
/// Base interface for all scripts
/// </summary>
public interface IScript
{
    string Name { get; }
    void Initialize(ScriptContext context);
}

/// <summary>
/// Context passed to scripts with access to game systems
/// </summary>
public class ScriptContext
{
    public required ILogger Logger { get; init; }
    public required EntityManager Entities { get; init; }
    public required ItemManager Items { get; init; }
    public required SkillManager Skills { get; init; }
    public required SpellManager Spells { get; init; }
    public required WorldManager World { get; init; }
    
    // Event publishing
    public Action<string, object?>? PublishEvent { get; init; }
}

/// <summary>
/// Interface for item scripts
/// </summary>
public interface IItemScript : IScript
{
    /// <summary>
    /// Called when item is used
    /// </summary>
    bool OnUse(Item item, Mobile user, object? target);
    
    /// <summary>
    /// Called when item is equipped
    /// </summary>
    void OnEquip(Item item, Mobile wearer);
    
    /// <summary>
    /// Called when item is unequipped
    /// </summary>
    void OnUnequip(Item item, Mobile wearer);
    
    /// <summary>
    /// Called when item deals damage
    /// </summary>
    int OnHit(Item weapon, Mobile attacker, Mobile defender, int baseDamage);
    
    /// <summary>
    /// Called when item is dropped
    /// </summary>
    void OnDrop(Item item, Mobile dropper, WorldPosition location);
    
    /// <summary>
    /// Called when item is picked up
    /// </summary>
    void OnPickup(Item item, Mobile picker);
}

/// <summary>
/// Interface for skill scripts
/// </summary>
public interface ISkillScript : IScript
{
    /// <summary>
    /// Called when skill is used
    /// </summary>
    bool OnUse(Mobile user, object? target);
    
    /// <summary>
    /// Called to check if skill use is valid
    /// </summary>
    bool CanUse(Mobile user, object? target, out string? failReason);
    
    /// <summary>
    /// Called when skill gains a point
    /// </summary>
    void OnGain(Mobile user, int newValue);
    
    /// <summary>
    /// Calculate skill check difficulty
    /// </summary>
    float GetDifficulty(Mobile user, object? target);
}

/// <summary>
/// Interface for spell scripts
/// </summary>
public interface ISpellScript : IScript
{
    /// <summary>
    /// Called when spell is cast
    /// </summary>
    bool OnCast(Mobile caster, object? target, WorldPosition? targetLocation);
    
    /// <summary>
    /// Called to check if spell can be cast
    /// </summary>
    bool CanCast(Mobile caster, object? target, out string? failReason);
    
    /// <summary>
    /// Called when spell effect is applied
    /// </summary>
    void OnEffect(Mobile caster, Mobile target, int damage);
    
    /// <summary>
    /// Called when spell duration ends
    /// </summary>
    void OnExpire(Mobile caster, Mobile target);
}

/// <summary>
/// Interface for NPC AI scripts
/// </summary>
public interface INpcScript : IScript
{
    /// <summary>
    /// Called every tick for AI update
    /// </summary>
    void OnThink(NpcEntity npc, GameTime time);
    
    /// <summary>
    /// Called when NPC is attacked
    /// </summary>
    void OnAttacked(NpcEntity npc, Mobile attacker, int damage);
    
    /// <summary>
    /// Called when NPC dies
    /// </summary>
    void OnDeath(NpcEntity npc, Mobile? killer);
    
    /// <summary>
    /// Called when NPC spawns
    /// </summary>
    void OnSpawn(NpcEntity npc);
    
    /// <summary>
    /// Called when player interacts (double-click)
    /// </summary>
    void OnInteract(NpcEntity npc, PlayerEntity player);
    
    /// <summary>
    /// Called when player speaks near NPC
    /// </summary>
    void OnSpeech(NpcEntity npc, PlayerEntity speaker, string text);
}

/// <summary>
/// Base class for item scripts with default implementations
/// </summary>
public abstract class ItemScriptBase : IItemScript
{
    public abstract string Name { get; }
    protected ScriptContext Context { get; private set; } = null!;
    
    public virtual void Initialize(ScriptContext context) => Context = context;
    public virtual bool OnUse(Item item, Mobile user, object? target) => false;
    public virtual void OnEquip(Item item, Mobile wearer) { }
    public virtual void OnUnequip(Item item, Mobile wearer) { }
    public virtual int OnHit(Item weapon, Mobile attacker, Mobile defender, int baseDamage) => baseDamage;
    public virtual void OnDrop(Item item, Mobile dropper, WorldPosition location) { }
    public virtual void OnPickup(Item item, Mobile picker) { }
}

/// <summary>
/// Base class for skill scripts with default implementations
/// </summary>
public abstract class SkillScriptBase : ISkillScript
{
    public abstract string Name { get; }
    protected ScriptContext Context { get; private set; } = null!;
    
    public virtual void Initialize(ScriptContext context) => Context = context;
    public virtual bool OnUse(Mobile user, object? target) => false;
    public virtual bool CanUse(Mobile user, object? target, out string? failReason) { failReason = null; return true; }
    public virtual void OnGain(Mobile user, int newValue) { }
    public virtual float GetDifficulty(Mobile user, object? target) => 1.0f;
}

/// <summary>
/// Base class for spell scripts with default implementations
/// </summary>
public abstract class SpellScriptBase : ISpellScript
{
    public abstract string Name { get; }
    protected ScriptContext Context { get; private set; } = null!;
    
    public virtual void Initialize(ScriptContext context) => Context = context;
    public virtual bool OnCast(Mobile caster, object? target, WorldPosition? targetLocation) => false;
    public virtual bool CanCast(Mobile caster, object? target, out string? failReason) { failReason = null; return true; }
    public virtual void OnEffect(Mobile caster, Mobile target, int damage) { }
    public virtual void OnExpire(Mobile caster, Mobile target) { }
}

/// <summary>
/// Base class for NPC AI scripts with default implementations
/// </summary>
public abstract class NpcScriptBase : INpcScript
{
    public abstract string Name { get; }
    protected ScriptContext Context { get; private set; } = null!;
    
    public virtual void Initialize(ScriptContext context) => Context = context;
    public virtual void OnThink(NpcEntity npc, GameTime time) { }
    public virtual void OnAttacked(NpcEntity npc, Mobile attacker, int damage) { }
    public virtual void OnDeath(NpcEntity npc, Mobile? killer) { }
    public virtual void OnSpawn(NpcEntity npc) { }
    public virtual void OnInteract(NpcEntity npc, PlayerEntity player) { }
    public virtual void OnSpeech(NpcEntity npc, PlayerEntity speaker, string text) { }
}

/// <summary>
/// Compiled script assembly
/// </summary>
public class CompiledScript
{
    public string SourcePath { get; init; } = "";
    public Assembly Assembly { get; init; } = null!;
    public DateTime CompiledAt { get; init; }
    public List<Type> ScriptTypes { get; init; } = new();
    public AssemblyLoadContext LoadContext { get; init; } = null!;
}

/// <summary>
/// Script compilation result
/// </summary>
public class CompilationResult
{
    public bool Success { get; init; }
    public CompiledScript? Script { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Script engine - compiles and manages C# scripts at runtime
/// </summary>
public class ScriptEngine : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _scriptsPath;
    private readonly ConcurrentDictionary<string, CompiledScript> _compiledScripts = new();
    private readonly ConcurrentDictionary<string, IScript> _scriptInstances = new();
    private readonly List<MetadataReference> _references;
    private FileSystemWatcher? _watcher;
    private ScriptContext? _context;
    
    // Events
    public event Action<string>? ScriptCompiled;
    public event Action<string, Exception>? ScriptError;
    public event Action<string>? ScriptReloaded;
    
    public ScriptEngine(ILogger logger, string scriptsPath)
    {
        _logger = logger;
        _scriptsPath = scriptsPath;
        
        // Prepare metadata references for compilation
        _references = GetMetadataReferences();
        
        // Ensure scripts directory exists
        Directory.CreateDirectory(scriptsPath);
        Directory.CreateDirectory(Path.Combine(scriptsPath, "Items"));
        Directory.CreateDirectory(Path.Combine(scriptsPath, "Skills"));
        Directory.CreateDirectory(Path.Combine(scriptsPath, "Spells"));
        Directory.CreateDirectory(Path.Combine(scriptsPath, "Npcs"));
    }
    
    /// <summary>
    /// Initialize with game context
    /// </summary>
    public void Initialize(ScriptContext context)
    {
        _context = context;
        
        // Compile all scripts
        CompileAllScripts();
        
        // Start watching for changes
        StartWatching();
    }
    
    /// <summary>
    /// Get metadata references for Roslyn compilation
    /// </summary>
    private List<MetadataReference> GetMetadataReferences()
    {
        var references = new List<MetadataReference>();
        
        // Core .NET assemblies
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")));
        
        // Game assemblies
        references.Add(MetadataReference.CreateFromFile(typeof(Entity).Assembly.Location));       // Shared
        references.Add(MetadataReference.CreateFromFile(typeof(ScriptEngine).Assembly.Location)); // Server
        
        return references;
    }
    
    /// <summary>
    /// Compile all scripts in the scripts directory
    /// </summary>
    public void CompileAllScripts()
    {
        _logger.LogInformation("Compiling all scripts from {Path}", _scriptsPath);
        
        var files = Directory.GetFiles(_scriptsPath, "*.cs", SearchOption.AllDirectories);
        var compiled = 0;
        var failed = 0;
        
        foreach (var file in files)
        {
            var result = CompileScript(file);
            if (result.Success)
            {
                compiled++;
                RegisterScripts(result.Script!);
            }
            else
            {
                failed++;
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Script compilation error in {File}: {Error}", file, error);
                }
            }
        }
        
        _logger.LogInformation("Script compilation complete: {Compiled} compiled, {Failed} failed", compiled, failed);
    }
    
    /// <summary>
    /// Compile a single script file
    /// </summary>
    public CompilationResult CompileScript(string filePath)
    {
        var result = new CompilationResult();
        
        try
        {
            var sourceCode = File.ReadAllText(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // Parse the code
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, 
                new CSharpParseOptions(LanguageVersion.Latest),
                filePath);
            
            // Create compilation
            var compilation = CSharpCompilation.Create(
                $"Script_{fileName}_{DateTime.UtcNow.Ticks}",
                new[] { syntaxTree },
                _references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(OptimizationLevel.Release)
                    .WithAllowUnsafe(false)
            );
            
            // Emit to memory
            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);
            
            // Collect diagnostics
            foreach (var diagnostic in emitResult.Diagnostics)
            {
                var message = $"{diagnostic.Location}: {diagnostic.GetMessage()}";
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                    result.Errors.Add(message);
                else if (diagnostic.Severity == DiagnosticSeverity.Warning)
                    result.Warnings.Add(message);
            }
            
            if (!emitResult.Success)
            {
                return result;
            }
            
            // Load assembly
            ms.Seek(0, SeekOrigin.Begin);
            var loadContext = new CollectibleAssemblyLoadContext();
            var assembly = loadContext.LoadFromStream(ms);
            
            // Find script types
            var scriptTypes = assembly.GetTypes()
                .Where(t => typeof(IScript).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToList();
            
            var compiled = new CompiledScript
            {
                SourcePath = filePath,
                Assembly = assembly,
                CompiledAt = DateTime.UtcNow,
                ScriptTypes = scriptTypes,
                LoadContext = loadContext
            };
            
            // Store and return
            _compiledScripts[filePath] = compiled;
            
            result = new CompilationResult
            {
                Success = true,
                Script = compiled,
                Warnings = result.Warnings
            };
            
            ScriptCompiled?.Invoke(filePath);
            _logger.LogInformation("Compiled script: {File} ({Count} types)", filePath, scriptTypes.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Exception: {ex.Message}");
            ScriptError?.Invoke(filePath, ex);
            return result;
        }
    }
    
    /// <summary>
    /// Register scripts from a compiled assembly
    /// </summary>
    private void RegisterScripts(CompiledScript compiled)
    {
        foreach (var type in compiled.ScriptTypes)
        {
            try
            {
                var instance = (IScript)Activator.CreateInstance(type)!;
                
                if (_context != null)
                {
                    instance.Initialize(_context);
                }
                
                _scriptInstances[instance.Name] = instance;
                _logger.LogDebug("Registered script: {Name} ({Type})", instance.Name, type.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to instantiate script type {Type}", type.Name);
            }
        }
    }
    
    /// <summary>
    /// Get a script instance by name
    /// </summary>
    public T? GetScript<T>(string name) where T : class, IScript
    {
        return _scriptInstances.GetValueOrDefault(name) as T;
    }
    
    /// <summary>
    /// Get all scripts of a type
    /// </summary>
    public IEnumerable<T> GetScripts<T>() where T : class, IScript
    {
        return _scriptInstances.Values.OfType<T>();
    }
    
    /// <summary>
    /// Start watching for file changes
    /// </summary>
    public void StartWatching()
    {
        if (_watcher != null) return;
        
        _watcher = new FileSystemWatcher(_scriptsPath)
        {
            Filter = "*.cs",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
        };
        
        _watcher.Changed += OnScriptChanged;
        _watcher.Created += OnScriptChanged;
        _watcher.Deleted += OnScriptDeleted;
        _watcher.Renamed += OnScriptRenamed;
        
        _watcher.EnableRaisingEvents = true;
        
        _logger.LogInformation("Script hot-reload watching enabled");
    }
    
    /// <summary>
    /// Stop watching for file changes
    /// </summary>
    public void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
    
    private DateTime _lastChange = DateTime.MinValue;
    
    private void OnScriptChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce rapid changes
        if ((DateTime.UtcNow - _lastChange).TotalMilliseconds < 500)
            return;
        _lastChange = DateTime.UtcNow;
        
        Task.Run(async () =>
        {
            // Wait a bit for file to be fully written
            await Task.Delay(100);
            
            _logger.LogInformation("Script changed, recompiling: {File}", e.FullPath);
            
            // Unload old version
            if (_compiledScripts.TryRemove(e.FullPath, out var old))
            {
                // Remove old script instances
                foreach (var type in old.ScriptTypes)
                {
                    var instance = _scriptInstances.Values.FirstOrDefault(s => s.GetType() == type);
                    if (instance != null)
                    {
                        _scriptInstances.TryRemove(instance.Name, out _);
                    }
                }
                
                // Unload assembly
                old.LoadContext.Unload();
            }
            
            // Compile new version
            var result = CompileScript(e.FullPath);
            if (result.Success)
            {
                RegisterScripts(result.Script!);
                ScriptReloaded?.Invoke(e.FullPath);
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Hot-reload compilation error: {Error}", error);
                }
            }
        });
    }
    
    private void OnScriptDeleted(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Script deleted: {File}", e.FullPath);
        
        if (_compiledScripts.TryRemove(e.FullPath, out var old))
        {
            foreach (var type in old.ScriptTypes)
            {
                var instance = _scriptInstances.Values.FirstOrDefault(s => s.GetType() == type);
                if (instance != null)
                {
                    _scriptInstances.TryRemove(instance.Name, out _);
                }
            }
            old.LoadContext.Unload();
        }
    }
    
    private void OnScriptRenamed(object sender, RenamedEventArgs e)
    {
        OnScriptDeleted(sender, new FileSystemEventArgs(WatcherChangeTypes.Deleted, 
            Path.GetDirectoryName(e.OldFullPath)!, Path.GetFileName(e.OldFullPath)));
        OnScriptChanged(sender, e);
    }
    
    /// <summary>
    /// Reload all scripts
    /// </summary>
    public void ReloadAll()
    {
        _logger.LogInformation("Reloading all scripts...");
        
        // Unload all
        foreach (var kvp in _compiledScripts)
        {
            kvp.Value.LoadContext.Unload();
        }
        _compiledScripts.Clear();
        _scriptInstances.Clear();
        
        // Recompile
        CompileAllScripts();
    }
    
    public void Dispose()
    {
        StopWatching();
        
        foreach (var kvp in _compiledScripts)
        {
            kvp.Value.LoadContext.Unload();
        }
        _compiledScripts.Clear();
        _scriptInstances.Clear();
    }
}

/// <summary>
/// Assembly load context that can be unloaded (for hot-reload)
/// </summary>
public class CollectibleAssemblyLoadContext : AssemblyLoadContext
{
    public CollectibleAssemblyLoadContext() : base(isCollectible: true)
    {
    }
    
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Return null to fall back to default context
        return null;
    }
}

// Placeholder interfaces for script context - these would be implemented elsewhere
public interface ItemManager { }
public interface SkillManager { }
public interface SpellManager { }
public interface WorldManager { }
