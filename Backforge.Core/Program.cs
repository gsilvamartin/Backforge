using Backforge.Core.Services;
using Backforge.Core.Services.ArchitectureCore;
using Backforge.Core.Services.ArchitectureCore.Interfaces;
using Backforge.Core.Services.LLamaCore;
using Backforge.Core.Services.ProjectCodeGenerationService;
using Backforge.Core.Services.ProjectCodeGenerationService.Interfaces;
using Backforge.Core.Services.ProjectInitializerCore;
using Backforge.Core.Services.ProjectInitializerCore.Interfaces;
using Backforge.Core.Services.RequirementAnalyzerCore;
using Backforge.Core.Services.RequirementAnalyzerCore.Interfaces;
using Backforge.Core.Services.StructureGeneratorCore;
using Backforge.Core.Services.StructureGeneratorCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Backforge.Core;

public class Program
{
    // ASCII art for the logo
    private static readonly string[] BackforgeAsciiArt = new[]
    {
        @"██████╗  █████╗  ██████╗██╗  ██╗███████╗ ██████╗ ██████╗  ██████╗ ███████╗",
        @"██╔══██╗██╔══██║██╔════╝██║ ██╔╝██╔════╝██╔═══██╗██╔══██╗██╔════╝ ██╔════╝",
        @"██████╔╝███████║██║     █████╔╝ █████╗  ██║   ██║██████╔╝██║  ███╗█████╗  ",
        @"██╔══██╗██╔══██║██║     ██╔═██╗ ██╔══╝  ██║   ██║██╔══██╗██║   ██║██╔══╝  ",
        @"██████╔╝██║  ██║╚██████╗██║  ██╗██║     ╚██████╔╝██║  ██║╚██████╔╝███████╗",
        @"╚═════╝ ╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝╚═╝      ╚═════╝ ╚═╝  ╚═╝ ╚═════╝ ╚══════╝",
    };

    // Application state
    private static readonly ConcurrentQueue<LogEntry> _logMessages = new();
    private static string _currentPhase = "Initializing";
    private static string _currentActivity = "Starting up";
    private static double _currentProgress = 0.0;
    private static ProgressTask _progressTask;
    private static IHost _host;
    private static bool _isDebugMode = false;
    private static readonly CancellationTokenSource _cancellationTokenSource = new();

    // Define consistent colors
    private static readonly Style _primaryColor = new Style(Color.Cyan1);
    private static readonly Style _secondaryColor = new Style(Color.Yellow);
    private static readonly Style _accentColor = new Style(Color.Magenta1);
    private static readonly Style _successColor = new Style(Color.Green);
    private static readonly Style _errorColor = new Style(Color.Red);
    private static readonly Style _warningColor = new Style(Color.Yellow);
    private static readonly Style _infoColor = new Style(Color.Grey);

    public static async Task Main(string[] args)
    {
        try
        {
            // Process command line arguments
            ProcessArguments(args);

            // Setup console
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Backforge - AI-Powered Code Generation";

            // Handle cancellation (Ctrl+C)
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // Prevent the process from terminating immediately
                _cancellationTokenSource.Cancel();
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Operation cancelled by user.[/]");
            };

            // Display splash screen
            DisplayIntro();

            // Initialize host with services
            _host = CreateHostBuilder(args).Build();

            // Get project details
            var (requirement, outputPath) = await GetProjectDetailsAsync();

            // Exit if cancelled
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            // Create and execute the project
            await RunProjectGenerationAsync(requirement, outputPath);
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
        }
        catch (Exception ex)
        {
            DisplayException(ex);
        }
        finally
        {
            // Clean up resources
            _cancellationTokenSource.Dispose();
        }

        // Wait for user to exit
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[yellow]Press any key to exit[/]").RuleStyle("grey").Centered());
        Console.ReadKey(true);
    }

    private static void ProcessArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLower();
            switch (arg)
            {
                case "--debug":
                case "-d":
                    _isDebugMode = true;
                    break;

                case "--help":
                case "-h":
                    DisplayHelp();
                    Environment.Exit(0);
                    break;

                case "--version":
                case "-v":
                    DisplayVersion();
                    Environment.Exit(0);
                    break;
            }
        }
    }

    private static void DisplayHelp()
    {
        // Create a panel with consistent styling for help info
        var helpPanel = new Panel(@"
[cyan]Backforge - AI-Powered Code Generation[/]
[grey]Usage: backforge [options][/]

[cyan]Options:[/]
  -h, --help      Display this help message
  -v, --version   Display version information
  -d, --debug     Run in debug mode with verbose logging"
            )
            .Border(BoxBorder.Rounded)
            .BorderStyle(_primaryColor)
            .Header("[cyan]HELP[/]")
            .HeaderAlignment(Justify.Center)
            .Expand();

        AnsiConsole.Clear();
        DisplayLogo(standalone: false);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(helpPanel);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Markup("[bold magenta]MADE BY GUILHERME MARTIN[/]").Centered());
    }

    private static void DisplayVersion()
    {
        // Create a panel with consistent styling for version info
        var versionPanel = new Panel(@"
[cyan]Backforge v1.0.0[/]
[grey]AI-Powered Code Generation Framework[/]
[grey]© 2024 Guilherme Martin[/]"
            )
            .Border(BoxBorder.Rounded)
            .BorderStyle(_primaryColor)
            .Header("[cyan]VERSION[/]")
            .HeaderAlignment(Justify.Center)
            .Expand();

        AnsiConsole.Clear();
        DisplayLogo(standalone: false);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(versionPanel);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Markup("[bold magenta]MADE BY GUILHERME MARTIN[/]").Centered());
    }

    private static void DisplayIntro()
    {
        AnsiConsole.Clear();

        // Display the boxed logo with custom styling
        DisplayLogo(standalone: true);

        // Loading animation
        AnsiConsole.WriteLine();
        AnsiConsole.Status()
            .Start("Initializing system...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(_primaryColor);

                // Simulate loading
                Thread.Sleep(1000);

                // Update status
                ctx.Status("Loading services...");
                Thread.Sleep(300);

                // Update status again
                ctx.Status("Preparing environment...");
                Thread.Sleep(300);
            });
    }

    private static void DisplayLogo(bool standalone = false)
    {
        // Calculate logo width
        int logoWidth = BackforgeAsciiArt[0].Length;

        // Create a grid for the logo with proper centering
        var grid = new Grid();

        // Add a single centered column
        grid.AddColumn(new GridColumn().Centered());

        // Add each line of the ASCII art logo with proper styling
        foreach (var line in BackforgeAsciiArt)
        {
            // Use markup centering
            grid.AddRow(new Text(line, _primaryColor).Centered());
        }

        // Add subtitle with centering
        grid.AddRow(new Text("AI-Powered Code Generation Framework", new Style(Color.Cyan1, null, Decoration.Bold))
            .Centered());

        // Add credits if standalone
        if (standalone)
        {
            grid.AddRow(new Text("MADE BY GUILHERME MARTIN", new Style(Color.Magenta1, null, Decoration.Bold))
                .Centered());
        }

        // Create outer panel with border for the entire logo
        var logoPanel = new Panel(grid)
            .Border(BoxBorder.Double)
            .BorderStyle(_primaryColor)
            .Header("[cyan]BACKFORGE[/]")
            .HeaderAlignment(Justify.Center);

        // Use panel centering and expansion for responsive display
        logoPanel.Expand();

        // Render the panel
        AnsiConsole.Write(logoPanel);
    }

    private static async Task<(string, string)> GetProjectDetailsAsync()
    {
        AnsiConsole.Clear();
        DisplayLogo();
        AnsiConsole.WriteLine();

        // Get project type
        var projectSelection = new SelectionPrompt<string>()
            .Title("[cyan]Select project type:[/]")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
            .HighlightStyle(_secondaryColor)
            .AddChoices(new[]
            {
                "REST API with Authentication",
                "Web Application (MVC)",
                "Microservice Architecture",
                "Console Application",
                "Blazor WASM Application",
                "Desktop Application (WPF)",
                "Class Library",
                "Custom Project",
                "Load Project from File"
            });

        string projectType = await Task.Run(() => AnsiConsole.Prompt(projectSelection));

        string requirement;

        if (projectType == "Load Project from File")
        {
            // Get file path
            var filePath = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Enter path to requirements file:[/]")
                    .PromptStyle("green")
                    .ValidationErrorMessage("[red]Please enter a valid file path[/]")
                    .Validate(path =>
                    {
                        if (string.IsNullOrWhiteSpace(path))
                            return ValidationResult.Error("[red]File path cannot be empty[/]");

                        if (!File.Exists(path))
                            return ValidationResult.Error("[red]File does not exist[/]");

                        return ValidationResult.Success();
                    }));

            // Load requirement from file
            requirement = await File.ReadAllTextAsync(filePath, _cancellationTokenSource.Token);

            // Show requirement preview
            if (requirement.Length > 500)
            {
                var shortPreview = requirement.Substring(0, 500) + "...";
                AnsiConsole.Write(new Panel(shortPreview)
                    .Header("[cyan]Project Requirements (Preview)[/]")
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(_primaryColor));
            }
            else
            {
                AnsiConsole.Write(new Panel(requirement)
                    .Header("[cyan]Project Requirements[/]")
                    .HeaderAlignment(Justify.Center)
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(_primaryColor));
            }
        }
        else if (projectType == "Custom Project")
        {
            // Get custom requirement
            var promptOptions = new List<string>
            {
                "Enter requirements manually",
                "Use interactive requirement builder"
            };

            var inputMethod = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]How would you like to specify your requirements?[/]")
                    .PageSize(10)
                    .HighlightStyle(_secondaryColor)
                    .AddChoices(promptOptions));

            if (inputMethod == "Enter requirements manually")
            {
                // Get custom requirement with multi-line support
                requirement = AnsiConsole.Prompt(
                    new TextPrompt<string>(
                            "[cyan]Enter your project requirements (multi-line, press Esc+Enter when done):[/]")
                        .PromptStyle("green")
                        .ValidationErrorMessage("[red]Please enter valid requirements[/]")
                        .Validate(req =>
                        {
                            return !string.IsNullOrWhiteSpace(req)
                                ? ValidationResult.Success()
                                : ValidationResult.Error("[red]Requirements cannot be empty[/]");
                        }));
            }
            else // Interactive builder
            {
                requirement = await BuildRequirementsInteractivelyAsync();
            }
        }
        else
        {
            // Use template requirement based on selection
            requirement = projectType switch
            {
                "REST API with Authentication" =>
                    "Create a RESTful API using ASP.NET Core with CRUD operations, JWT authentication, role-based authorization, and Swagger documentation.",
                "Web Application (MVC)" =>
                    "Create an ASP.NET Core MVC web application with user authentication, Bootstrap UI, and a SQL Server database.",
                "Microservice Architecture" =>
                    "Create a microservice architecture using ASP.NET Core with separate services, API gateway, service discovery, and message broker for communication.",
                "Console Application" =>
                    "Create a .NET console application with command-line argument parsing, configuration support, and logging capabilities.",
                "Blazor WASM Application" =>
                    "Create a Blazor WebAssembly application with authentication, component library, and API communication.",
                "Desktop Application (WPF)" =>
                    "Create a WPF desktop application with MVVM architecture, modern UI styling, and data persistence.",
                "Class Library" =>
                    "Create a .NET class library with well-structured API, XML documentation, and unit tests.",
                _ => "Create a custom .NET project following best practices and clean architecture principles."
            };

            // Allow customization of template
            var customize = AnsiConsole.Prompt(
                new ConfirmationPrompt("[cyan]Would you like to customize the template requirements?[/]"));

            if (customize)
            {
                // Edit the template requirement
                requirement = AnsiConsole.Prompt(
                    new TextPrompt<string>("[cyan]Edit the requirements:[/]")
                        .DefaultValue(requirement)
                        .ShowDefaultValue(true)
                        .PromptStyle("green")
                        .ValidationErrorMessage("[red]Please enter valid requirements[/]")
                        .Validate(req =>
                        {
                            return !string.IsNullOrWhiteSpace(req)
                                ? ValidationResult.Success()
                                : ValidationResult.Error("[red]Requirements cannot be empty[/]");
                        }));
            }
        }

        // Show the chosen requirement
        AnsiConsole.Write(new Panel(requirement)
            .Header("[cyan]Project Requirements[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderStyle(_primaryColor));

        // Get output path
        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Backforge",
            $"Project_{DateTime.Now:yyyyMMdd_HHmmss}");

        var outputPath = AnsiConsole.Prompt(
            new TextPrompt<string>($"[cyan]Enter output directory[/] [grey](default: {defaultPath})[/]")
                .AllowEmpty()
                .PromptStyle("green"));

        // Use default path if empty
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = defaultPath;
        }

        // Ensure directory exists
        if (!Directory.Exists(outputPath))
        {
            AnsiConsole.Status()
                .Start($"Creating directory: {outputPath}", _ => { Directory.CreateDirectory(outputPath); });
        }

        return (requirement, outputPath);
    }

    private static async Task<string> BuildRequirementsInteractivelyAsync()
    {
        // This method provides a structured way to build requirements through guided questions
        var requirementBuilder = new StringBuilder();

        // Project domain
        var domain = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]What is the domain of your project?[/]")
                .PageSize(10)
                .HighlightStyle(_secondaryColor)
                .AddChoices(new[]
                {
                    "Business/Enterprise",
                    "E-commerce",
                    "Education",
                    "Finance",
                    "Healthcare",
                    "Social Media",
                    "Data Processing",
                    "Entertainment",
                    "Other"
                }));

        requirementBuilder.AppendLine($"Domain: {domain}");

        // If other, ask for details
        if (domain == "Other")
        {
            var customDomain = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Please specify the domain:[/]")
                    .PromptStyle("green"));

            requirementBuilder.AppendLine($"Custom Domain: {customDomain}");
        }

        // Project type
        var typeSelection = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[cyan]What type of components will your project include?[/]")
                .PageSize(10)
                .HighlightStyle(_secondaryColor)
                .InstructionsText("[grey](Press space to select, enter to confirm)[/]")
                .AddChoices(new[]
                {
                    "Web API",
                    "Web UI",
                    "Database",
                    "Authentication",
                    "File Processing",
                    "Reporting",
                    "Integration with External Systems",
                    "Background Services",
                    "Mobile Support"
                }));

        requirementBuilder.AppendLine("\nComponents:");
        foreach (var type in typeSelection)
        {
            requirementBuilder.AppendLine($"- {type}");
        }

        // Project scale
        var scale = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]What is the scale of your project?[/]")
                .PageSize(10)
                .HighlightStyle(_secondaryColor)
                .AddChoices(new[]
                {
                    "Small (Prototype/POC)",
                    "Medium (Department-level application)",
                    "Large (Enterprise application)"
                }));

        requirementBuilder.AppendLine($"\nScale: {scale}");

        // Get additional requirements
        var additionalReqs = AnsiConsole.Prompt(
            new TextPrompt<string>("[cyan]Any specific requirements or constraints? (Optional)[/]")
                .AllowEmpty());

        if (!string.IsNullOrWhiteSpace(additionalReqs))
        {
            requirementBuilder.AppendLine("\nAdditional Requirements:");
            requirementBuilder.AppendLine(additionalReqs);
        }

        // Show the generated requirements
        var generatedRequirements = requirementBuilder.ToString();

        AnsiConsole.Write(new Panel(generatedRequirements)
            .Header("[cyan]Generated Requirements[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderStyle(_primaryColor));

        // Ask if they want to edit the requirements
        var editReqs = AnsiConsole.Prompt(
            new ConfirmationPrompt("[cyan]Would you like to edit these requirements?[/]"));

        if (editReqs)
        {
            // Edit the requirements
            generatedRequirements = AnsiConsole.Prompt(
                new TextPrompt<string>("[cyan]Edit the requirements:[/]")
                    .DefaultValue(generatedRequirements)
                    .ShowDefaultValue(true)
                    .PromptStyle("green"));
        }

        await Task.Delay(100, _cancellationTokenSource.Token); // Small delay to allow UI to catch up
        return generatedRequirements;
    }

    private static async Task RunProjectGenerationAsync(string requirement, string outputPath)
    {
        AnsiConsole.Clear();

        // Display the fixed logo at the top
        DisplayLogo();
        AnsiConsole.WriteLine();

        // Get BackforgeService
        var backforgeService = _host.Services.GetRequiredService<BackforgeService>();

        // Create the progress reporter
        var progress = new Progress<BackforgeProgressUpdate>(update =>
        {
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            // Update progress data
            _currentPhase = update.Phase;
            _currentActivity = update.Activity;
            _currentProgress = update.Progress;

            // Update progress display if it exists
            if (_progressTask != null)
            {
                _progressTask.Value(Math.Min(100, (int)(_currentProgress * 100)));
                _progressTask.Description(_currentActivity);
            }

            // Log message with timestamp
            _logMessages.Enqueue(new LogEntry(
                DateTime.Now,
                _currentPhase,
                _currentActivity,
                LogLevel.Information
            ));
        });

        // Show project information
        AnsiConsole.Write(
            new Panel(
                    new Markup($"[bold cyan]Generating project based on your requirements[/]\n[grey]{requirement}[/]"))
                .Header("[cyan]PROJECT DETAILS[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderStyle(_primaryColor)
                .Expand());

        // Add initial log message
        _logMessages.Enqueue(new LogEntry(
            DateTime.Now,
            "Initialization",
            $"Starting project generation for output: {outputPath}",
            LogLevel.Information
        ));

        // Variable to store the result
        BackforgeResult result = null;

        // Create a single panel for console output that will be used throughout
        var consolePanel = new Panel(new LogDisplay(_logMessages))
            .Header("[cyan]CONSOLE OUTPUT[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderStyle(_primaryColor)
            .Expand();

        // First display of the console panel
        AnsiConsole.Write(consolePanel);

        try
        {
            // Check for cancellation
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            // Create progress display
            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn(),
                    new ElapsedTimeColumn()
                })
                .StartAsync(async progressContext =>
                {
                    // Create a single task
                    _progressTask = progressContext.AddTask($"[green]{_currentPhase}[/]", maxValue: 100);

                    // Execute the service and store the result
                    result = await backforgeService.RunAsync(
                        requirement,
                        outputPath,
                        _cancellationTokenSource.Token,
                        progress);

                    // Force progress to 100% when done
                    _progressTask.Value = 100;
                });

            // Update and display the console panel again with any new log messages
            AnsiConsole.Clear();
            DisplayLogo();
            AnsiConsole.WriteLine();
            AnsiConsole.Write(consolePanel);

            // Wait a moment before proceeding
            await Task.Delay(500, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            _logMessages.Enqueue(new LogEntry(
                DateTime.Now,
                "Cancellation",
                "Operation was cancelled by user",
                LogLevel.Warning
            ));

            // Display the updated console panel
            AnsiConsole.Clear();
            DisplayLogo();
            AnsiConsole.WriteLine();
            AnsiConsole.Write(consolePanel);
        }
        catch (Exception ex)
        {
            _logMessages.Enqueue(new LogEntry(
                DateTime.Now,
                "Error",
                $"ERROR: {ex.Message}",
                LogLevel.Error
            ));

            // Display the updated console panel
            AnsiConsole.Clear();
            DisplayLogo();
            AnsiConsole.WriteLine();
            AnsiConsole.Write(consolePanel);

            // Display error after a short delay
            await Task.Delay(500);
            DisplayError(ex.Message);
            return;
        }

        // Display the result if it's available
        if (result != null)
        {
            DisplayResult(result, outputPath);
        }
    }

    private static void DisplayResult(BackforgeResult result, string outputPath)
    {
        AnsiConsole.Clear();
        DisplayLogo();
        AnsiConsole.WriteLine();

        if (result.Success)
        {
            // Success panel
            AnsiConsole.Write(new Panel(
                    new Markup($"[green bold]✓ Project successfully generated![/]\n[grey]Location: {outputPath}[/]"))
                .Header("[green]SUCCESS[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderStyle(_successColor)
                .Expand()
            );

            // Extraction details panel
            var extractionPanel = new Panel(
                    new Markup($"[cyan bold]EXTRACTION DETAILS[/]\n\n" +
                               $"[white]Entities Extracted:[/] [green]{result.ExtractedEntities}[/]\n" +
                               $"[white]Inferred Requirements:[/] [green]{result.InferredRequirements}[/]" +
                               (result.RelationshipsIdentified > 0
                                   ? $"\n[white]Relationships Identified:[/] [green]{result.RelationshipsIdentified}[/]"
                                   : "")))
                .Header("[cyan]EXTRACTION SUMMARY[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderStyle(_primaryColor)
                .Expand();

            AnsiConsole.Write(extractionPanel);

            // Main entities table if available
            if (result.PrimaryEntityNames != null && result.PrimaryEntityNames.Count > 0)
            {
                var entityTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderStyle(_primaryColor)
                    .Title("[cyan]MAIN ENTITIES[/]")
                    .Expand();

                entityTable.AddColumn(new TableColumn("Entity").Centered());
                entityTable.AddColumn(new TableColumn("Type").Centered());

                foreach (var entityName in result.PrimaryEntityNames)
                {
                    string entityType = "Entity";
                    if (result.EntityTypes != null && result.EntityTypes.TryGetValue(entityName, out var type))
                        entityType = type;

                    entityTable.AddRow($"[cyan]{entityName}[/]", entityType);
                }

                AnsiConsole.Write(entityTable);
            }

            // Summary table
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderStyle(_primaryColor)
                .Title("[cyan]GENERATION SUMMARY[/]")
                .Expand();

            table.AddColumn("Category");
            table.AddColumn(new TableColumn("Count").Centered());

            table.AddRow("[cyan]Entities Extracted[/]", result.ExtractedEntities.ToString());
            table.AddRow("[cyan]Components Created[/]", result.Components.ToString());
            table.AddRow("[cyan]Files Generated[/]", result.GeneratedFiles.ToString());

            // Add architectural patterns if available
            if (result.ArchitecturePatterns > 0)
            {
                table.AddRow(
                    "[cyan]Architecture Patterns[/]",
                    result.ArchitecturePatterns.ToString());
            }

            // Add quality score if available
            if (result.CodeQualityScore > 0)
            {
                table.AddRow(
                    "[cyan]Code Quality Score[/]",
                    $"{result.CodeQualityScore:F1}/100");
            }

            AnsiConsole.Write(table);

            // File distribution table if available
            if (result.FileTypeDistribution != null && result.FileTypeDistribution.Count > 0)
            {
                var fileTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderStyle(_primaryColor)
                    .Title("[cyan]FILE DISTRIBUTION[/]")
                    .Expand();

                fileTable.AddColumn(new TableColumn("File Type").Centered());
                fileTable.AddColumn(new TableColumn("Count").Centered());

                foreach (var fileType in result.FileTypeDistribution)
                {
                    fileTable.AddRow($"[cyan]{fileType.Key}[/]", fileType.Value.ToString());
                }

                AnsiConsole.Write(fileTable);
            }

            // Next steps
            AnsiConsole.Write(new Panel(
                    new Markup("[cyan]NEXT STEPS:[/]\n" +
                               "1. Open the project in your IDE\n" +
                               "2. Review the generated code\n" +
                               "3. Run the project to test functionality\n" +
                               "4. Customize and extend as needed"))
                .Border(BoxBorder.Rounded)
                .BorderStyle(_successColor)
                .Expand()
            );

            // Project commands
            var cmdTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderStyle(_successColor)
                .Title("[green]USEFUL COMMANDS[/]")
                .Expand();

            cmdTable.AddColumn("Purpose");
            cmdTable.AddColumn("Command");

            cmdTable.AddRow("[green]Open in Visual Studio Code[/]", $"code \"{outputPath}\"");
            cmdTable.AddRow("[green]Build Project[/]", $"dotnet build \"{outputPath}\"");
            cmdTable.AddRow("[green]Run Project[/]", $"dotnet run --project \"{outputPath}\"");

            AnsiConsole.Write(cmdTable);
        }
        else
        {
            // Error panel
            AnsiConsole.Write(new Panel(
                    new Markup($"[red bold]✗ Project generation failed![/]\n[grey]Error: {result.ErrorMessage}[/]"))
                .Header("[red]ERROR[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderStyle(_errorColor)
                .Expand()
            );

            // Partial progress (if any)
            if (result.ExtractedEntities > 0 || result.Components > 0 || result.GeneratedFiles > 0)
            {
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderStyle(_warningColor)
                    .Title("[yellow]PARTIAL PROGRESS[/]")
                    .Expand();

                table.AddColumn("Step");
                table.AddColumn(new TableColumn("Progress").Centered());

                if (result.ExtractedEntities > 0)
                    table.AddRow("[yellow]Entities Extracted[/]", result.ExtractedEntities.ToString());

                if (result.InferredRequirements > 0)
                    table.AddRow("[yellow]Requirements Inferred[/]", result.InferredRequirements.ToString());

                if (result.RelationshipsIdentified > 0)
                    table.AddRow("[yellow]Relationships Identified[/]", result.RelationshipsIdentified.ToString());

                if (result.Components > 0)
                    table.AddRow("[yellow]Components Created[/]", result.Components.ToString());

                if (result.GeneratedFiles > 0)
                    table.AddRow("[yellow]Files Generated[/]", result.GeneratedFiles.ToString());

                AnsiConsole.Write(table);
            }

            // Troubleshooting tips
            AnsiConsole.Write(new Panel(
                new Panel("[yellow]TROUBLESHOOTING TIPS:[/]\n" +
                          "• Check if the requirements are clear and specific\n" +
                          "• Verify output directory permissions\n" +
                          "• Try again with a simpler project scope\n" +
                          "• Check log files for more detailed information\n" +
                          "• Run with --debug flag for verbose logging")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(_warningColor)
                    .Expand()
            ));
        }

        // Always display credits at the bottom
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Markup("[bold magenta]MADE BY GUILHERME MARTIN[/]").Centered());
    }

    private static void DisplayError(string errorMessage)
    {
        AnsiConsole.Clear();
        DisplayLogo();
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Panel(
                new Markup($"[red bold]✗ An error occurred during generation![/]\n[grey]{errorMessage}[/]"))
            .Header("[red]ERROR[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderStyle(_errorColor)
            .Expand()
        );

        // Always display credits at the bottom
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Markup("[bold magenta]MADE BY GUILHERME MARTIN[/]").Centered());
    }

    private static void DisplayException(Exception ex)
    {
        AnsiConsole.Clear();
        DisplayLogo();
        AnsiConsole.WriteLine();

        var errorPanel = new Panel(new Markup($"[red bold]✗ Unexpected Error![/]\n[grey]{ex.Message}[/]"))
            .Header("[red]SYSTEM ERROR[/]")
            .HeaderAlignment(Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderStyle(_errorColor)
            .Expand();

        // In debug mode, add stack trace
        if (_isDebugMode && ex.StackTrace != null)
        {
            var firstStackTraceLine = ex.StackTrace.Split('\n')[0].Trim();
            errorPanel = new Panel(
                    new Markup(
                        $"[red bold]✗ Unexpected Error![/]\n[grey]{ex.Message}[/]\n\n[dim]{firstStackTraceLine}[/]"))
                .Header("[red]SYSTEM ERROR[/]")
                .HeaderAlignment(Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderStyle(_errorColor)
                .Expand();
        }

        AnsiConsole.Write(errorPanel);

        // Always display credits at the bottom
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Markup("[bold magenta]MADE BY GUILHERME MARTIN[/]").Centered());
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(new BackforgeConsoleLoggerProvider(message =>
                {
                    _logMessages.Enqueue(new LogEntry(
                        DateTime.Now,
                        "System",
                        message,
                        LogLevel.Information
                    ));

                    // Prevent unbounded growth
                    while (_logMessages.Count > 200)
                    {
                        _logMessages.TryDequeue(out _);
                    }
                }));

                // Set minimum log level based on debug mode
                logging.SetMinimumLevel(_isDebugMode ? LogLevel.Debug : LogLevel.Information);
            })
            .ConfigureServices((_, services) =>
            {
                // Register core services
                services.AddSingleton<ILlamaService, LlamaService>();
                services.AddSingleton<ITextProcessingService, TextProcessingService>();

                // Register RequirementAnalyzer services
                services.AddScoped<IEntityRelationshipExtractor, EntityRelationshipExtractor>();
                services.AddScoped<IImplicitRequirementsAnalyzer, ImplicitRequirementsAnalyzer>();
                services.AddScoped<IArchitecturalDecisionService, ArchitecturalDecisionService>();
                services.AddScoped<IAnalysisValidationService, AnalysisValidationService>();
                services.AddScoped<IRequirementAnalyzer, RequirementAnalyzer>();

                // Register Architecture services
                services.AddScoped<IArchitecturePatternResolver, ArchitecturePatternResolver>();
                services.AddScoped<IComponentRecommender, ComponentRecommenderService>();
                services.AddScoped<IIntegrationDesigner, IntegrationDesignerService>();
                services.AddScoped<IScalabilityPlanner, ScalabilityPlannerService>();
                services.AddScoped<ISecurityDesigner, SecurityDesignerService>();
                services.AddScoped<IPerformanceOptimizer, PerformanceOptimizerService>();
                services.AddScoped<IResilienceDesigner, ResilienceDesignerService>();
                services.AddScoped<IMonitoringDesigner, MonitoringDesignerService>();
                services.AddScoped<IArchitectureDocumenter, ArchitectureDocumentService>();
                services.AddScoped<IArchitectureGenerator, ArchitectureGeneratorService>();

                // Register Project Initializer
                services.AddScoped<IProjectInitializerService, ProjectInitializerService>();
                services.AddScoped<IProjectInitializerPromptBuilder, ProjectInitializerPromptBuilder>();
                services.AddScoped<IDirectoryService, DirectoryService>();
                services.AddScoped<ICommandExecutor, CommandExecutor>();

                // Project Structure Generator
                services.AddScoped<IProjectStructureGeneratorService, ProjectStructureGeneratorService>();

                // Code Generation
                services.AddSingleton<IBuildService, BuildService>();
                services.AddSingleton<ITestRunnerService, TestRunnerService>();
                services.AddSingleton<ICodeAnalysisService, CodeAnalysisService>();
                services.AddSingleton<IFileGenerationTrackerService, FileGenerationTrackerService>();
                services.AddSingleton<IProjectCodeGenerationService, ProjectCodeGenerationService>();

                // Register the main application service
                services.AddScoped<BackforgeService>();
            });
}

/// <summary>
/// Represents a log entry with timestamp, phase, message, level and detail
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; }
    public string Phase { get; }
    public string Message { get; }
    public LogLevel Level { get; }
    public string Detail { get; }

    public LogEntry(DateTime timestamp, string phase, string message, LogLevel level, string detail = null)
    {
        Timestamp = timestamp;
        Phase = phase;
        Message = message;
        Level = level;
        Detail = detail;
    }
}

/// <summary>
/// Custom renderable for displaying log messages with enhanced formatting
/// </summary>
public class LogDisplay : IRenderable
{
    private readonly ConcurrentQueue<LogEntry> _messages;
    private readonly int _maxMessages = 15; // Número de mensagens a mostrar

    public LogDisplay(ConcurrentQueue<LogEntry> messages)
    {
        _messages = messages;
    }

    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        return new Measurement(maxWidth, _maxMessages + 2);
    }

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var lines = new List<Segment>();

        // Get the most recent messages
        var messages = _messages.TakeLast(_maxMessages).ToArray();

        // Add each message with enhanced formatting
        foreach (var entry in messages)
        {
            // Determine color based on log level and phase
            Color phaseColor = Color.Cyan1;
            Color messageColor = entry.Level switch
            {
                LogLevel.Error => Color.Red,
                LogLevel.Warning => Color.Yellow,
                LogLevel.Information => Color.White,
                LogLevel.Debug => Color.Grey,
                _ => Color.White
            };

            // Special highlights for extraction phases
            if (entry.Phase.Contains("Analyzing") || entry.Phase.Contains("Entity") ||
                entry.Phase.Contains("Extracting") || entry.Phase.Contains("Requirement"))
            {
                phaseColor = Color.Green;

                // Highlight entity names in the message with different color
                if (entry.Message.Contains("Entity") || entry.Message.Contains("Requirement") ||
                    entry.Message.Contains("Relationship"))
                {
                    messageColor = Color.Cyan1;
                }
            }

            // Format timestamp
            string timestamp = entry.Timestamp.ToString("HH:mm:ss");

            // Format phase with fixed width and highlighting
            string phase = entry.Phase;
            if (phase.Length > 12)
                phase = phase.Substring(0, 12);
            else
                phase = phase.PadRight(12);

            // Add timestamp in grey
            lines.Add(new Segment($"[{timestamp}] ", new Style(Color.Grey)));

            // Add phase in its color
            lines.Add(new Segment($"[{phase}] ", new Style(phaseColor)));

            // Add message with appropriate color
            lines.Add(new Segment(entry.Message, new Style(messageColor)));

            // Add detail if present
            if (!string.IsNullOrEmpty(entry.Detail))
            {
                lines.Add(Segment.LineBreak);
                lines.Add(new Segment("          ", Style.Plain));
                lines.Add(new Segment($"└─ {entry.Detail}", new Style(Color.Grey)));
            }

            // Add line break
            lines.Add(Segment.LineBreak);
        }

        // If no messages, add a placeholder
        if (messages.Length == 0)
        {
            lines.Add(new Segment("No log messages yet...", new Style(Color.Grey)));
            lines.Add(Segment.LineBreak);
        }

        return lines;
    }
}

/// <summary>
/// Custom console logger provider that works with our UI
/// </summary>
public class BackforgeConsoleLoggerProvider : ILoggerProvider
{
    private readonly Action<string> _logAction;

    public BackforgeConsoleLoggerProvider(Action<string> logAction)
    {
        _logAction = logAction;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new BackforgeConsoleLogger(categoryName, _logAction);
    }

    public void Dispose()
    {
        // No resources to dispose
    }

    private class BackforgeConsoleLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly Action<string> _logAction;

        public BackforgeConsoleLogger(string categoryName, Action<string> logAction)
        {
            _categoryName = categoryName;
            _logAction = logAction;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true; // We'll filter in the log handler
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            var shortCategory = _categoryName.Split('.').LastOrDefault() ?? _categoryName;

            _logAction($"[{shortCategory}] {message}");

            // If exception is provided, log it as well
            if (exception != null)
            {
                _logAction($"[{shortCategory}] Exception: {exception.Message}");
            }
        }
    }
}