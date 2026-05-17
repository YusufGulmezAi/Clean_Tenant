using System.CommandLine;
using CleanTenant.MigrationRunner.Commands;
using CleanTenant.MigrationRunner.Infrastructure;

var rootCommand = new RootCommand("CleanTenant Migration ve Seed CLI aracı");

rootCommand.Subcommands.Add(MigrateCommand.Build());
rootCommand.Subcommands.Add(SeedCommand.Build());
rootCommand.Subcommands.Add(InitSystemAdminCommand.Build());

return await rootCommand.Parse(args).InvokeAsync();

/// <summary>
/// MigrationRunner CLI'nin giriş noktası. System.CommandLine ile alt komutlar
/// (migrate / seed / init-system-admin) dispatch edilir.
/// </summary>
public partial class Program;
