using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinGuardAgent.Agent;
using WinGuardAgent.Apps;
using WinGuardAgent.Chrome;
using WinGuardAgent.Cli;
using WinGuardAgent.Network;
using WinGuardAgent.Security;
using WinGuardAgent.Storage;
namespace WinGuardAgent;
public static class Program { public static async Task<int> Main(string[] args) { if (!OperatingSystem.IsWindows()) { Console.Error.WriteLine("WinGuard Agent requires Windows 11/Windows 10."); return 2; } if (args.Length > 0) return await AdminCli.RunAsync(args); var builder=Host.CreateApplicationBuilder(args); builder.Services.AddWindowsService(o=>o.ServiceName=Constants.ServiceName); builder.Logging.ClearProviders(); builder.Logging.AddEventLog(s=>s.SourceName=Constants.ServiceName); builder.Logging.AddProvider(new RollingFileLoggerProvider(Paths.LogDirectory)); builder.Services.AddSingleton<Database>(); builder.Services.AddSingleton<PolicyRepository>(); builder.Services.AddSingleton<EventRepository>(); builder.Services.AddSingleton<SettingsRepository>(); builder.Services.AddSingleton<AgentStatusRepository>(); builder.Services.AddSingleton<UpdateManager>(); builder.Services.AddSingleton<PolicyEngine>(); builder.Services.AddSingleton<ChromePolicyManager>(); builder.Services.AddSingleton<ProcessBlocker>(); builder.Services.AddSingleton<InstalledAppDetector>(); builder.Services.AddSingleton<DnsFilterService>(); builder.Services.AddSingleton<FriendlyBlockPageService>(); builder.Services.AddSingleton<NetworkProtectionManager>(); builder.Services.AddSingleton<SecurityHardening>(); builder.Services.AddSingleton<TamperProtectionService>(); builder.Services.AddHostedService<AgentWorker>(); try { await builder.Build().RunAsync(); return 0; } catch(Exception ex) { try { Directory.CreateDirectory(Paths.LogDirectory); await File.AppendAllTextAsync(Paths.FatalLog,$"{DateTimeOffset.UtcNow:o} {ex}\n"); } catch {} return 1; } } }