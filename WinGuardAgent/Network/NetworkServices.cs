using WinGuardAgent.Storage;
namespace WinGuardAgent.Network;
public sealed class DnsFilterService { public Task ApplyAsync(CancellationToken ct=default)=>Task.CompletedTask; }
public sealed class FriendlyBlockPageService { public string GetHtml(string message)=>$"<!doctype html><html><head><meta charset='utf-8'><title>Blocked by WinGuard</title></head><body><main><h1>😼 {System.Net.WebUtility.HtmlEncode(message)}</h1><p>This site is blocked by the administrator's WinGuard policy.</p></main></body></html>"; }
public sealed class NetworkProtectionManager(DnsFilterService dns,SettingsRepository settings){ public async Task ApplyAsync(CancellationToken ct=default){var cfg=await settings.GetAsync(ct);if(cfg.DnsProtectionEnabled)await dns.ApplyAsync(ct);} }