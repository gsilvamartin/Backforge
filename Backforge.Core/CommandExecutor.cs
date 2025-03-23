using Backforge.Core.Interfaces;
using Backforge.Core.Models;

namespace Backforge.Core;

using System.Diagnostics;
using System.Text;

/// <summary>
/// Implementação do executor de comandos do sistema.
/// </summary>
public class CommandExecutor : ICommandExecutor, IDisposable
{
    private readonly ILogger _logger;
    private readonly bool _isEnabled;
    private bool _disposed;

    // Lista de comandos potencialmente perigosos
    private static readonly string[] _forbiddenCommands = new[]
    {
        "rm -rf", "rmdir /s", "del /f", "format", "mkfs",
        "dd if=", "> /dev/sda", "chmod -R 777", ":(){ :|:& };:",
        "> /dev/null", "mv /* /dev/null", "> /dev/zero"
    };

    /// <summary>
    /// Inicializa uma nova instância da classe CommandExecutor.
    /// </summary>
    /// <param name="logger">Logger para registrar operações.</param>
    /// <param name="isEnabled">Indica se a execução de comandos está habilitada.</param>
    public CommandExecutor(ILogger logger, bool isEnabled = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isEnabled = isEnabled;
        _logger.Log(
            $"CommandExecutor inicializado. Execução de comandos: {(_isEnabled ? "habilitada" : "desabilitada")}");
    }

    /// <summary>
    /// Valida a segurança de um comando antes da execução.
    /// </summary>
    /// <param name="command">Comando a ser validado.</param>
    /// <returns>Resultado da validação de segurança.</returns>
    public Task<CommandSafetyResult> ValidateCommandSafetyAsync(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return Task.FromResult(new CommandSafetyResult
            {
                IsSafe = false,
                Reason = "Comando vazio."
            });
        }

        // Verificar se o comando contém termos proibidos
        var loweredCommand = command.ToLowerInvariant();
        foreach (var forbidden in _forbiddenCommands)
        {
            if (loweredCommand.Contains(forbidden.ToLowerInvariant()))
            {
                return Task.FromResult(new CommandSafetyResult
                {
                    IsSafe = false,
                    Reason = $"Comando contém operação potencialmente perigosa: {forbidden}"
                });
            }
        }

        // Verificar se o comando tenta acessar áreas sensíveis do sistema
        if (loweredCommand.Contains("/etc/") ||
            loweredCommand.Contains("system32") ||
            loweredCommand.Contains("windows\\system") ||
            loweredCommand.Contains("/root/") ||
            loweredCommand.Contains("$env:windir"))
        {
            return Task.FromResult(new CommandSafetyResult
            {
                IsSafe = false,
                Reason = "Comando tenta acessar diretórios de sistema restritos."
            });
        }

        return Task.FromResult(new CommandSafetyResult
        {
            IsSafe = true,
            Reason = "Comando validado com sucesso."
        });
    }

    /// <summary>
    /// Executa um comando no sistema.
    /// </summary>
    /// <param name="command">Comando a ser executado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da execução do comando.</returns>
    public async Task<CommandResult> ExecuteCommandAsync(string command,
        CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
        {
            return new CommandResult
            {
                Command = command,
                Success = false,
                Output = "Execução de comandos não está habilitada.",
                ExitCode = -1
            };
        }

        var safetyResult = await ValidateCommandSafetyAsync(command);
        if (!safetyResult.IsSafe)
        {
            return new CommandResult
            {
                Command = command,
                Success = false,
                Output = $"Comando não executado por segurança: {safetyResult.Reason}",
                ExitCode = -1
            };
        }

        var result = new CommandResult
        {
            Command = command,
            StartTime = DateTime.Now
        };

        try
        {
            _logger.Log($"Executando comando: {command}");

            // Configurar processo
            var processStartInfo = GetProcessStartInfo(command);
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        outputBuilder.AppendLine(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        errorBuilder.AppendLine(args.Data);
                    }
                };

                // Inicia o processo e captura saída
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Configura timeout ou cancelamento
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                var processTask = process.WaitForExitAsync(cancellationToken);

                // Aguardar conclusão ou cancelamento
                if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true);
                            _logger.Log("Processo terminado por timeout.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Erro ao terminar processo", ex);
                    }

                    result.Output = "Execução do comando atingiu o tempo limite.";
                    result.ExitCode = -1;
                    result.Success = false;
                }
                else
                {
                    // Processo concluído normalmente
                    result.ExitCode = process.ExitCode;
                    result.Success = process.ExitCode == 0;

                    var output = outputBuilder.ToString().Trim();
                    var error = errorBuilder.ToString().Trim();

                    if (!string.IsNullOrEmpty(error))
                    {
                        result.Output = string.IsNullOrEmpty(output)
                            ? error
                            : $"{output}\n\nErros:\n{error}";
                    }
                    else
                    {
                        result.Output = output;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            result.Output = "Operação cancelada pelo usuário.";
            result.ExitCode = -1;
            result.Success = false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro ao executar comando '{command}'", ex);
            result.Output = $"Erro: {ex.Message}";
            result.ExitCode = -1;
            result.Success = false;
        }
        finally
        {
            result.EndTime = DateTime.Now;
            result.ExecutionTimeMs = (long)(result.EndTime - result.StartTime).TotalMilliseconds;
            _logger.Log(
                $"Comando concluído. Código de saída: {result.ExitCode}, Tempo: {result.ExecutionTimeMs}ms");
        }

        return result;
    }

    private ProcessStartInfo GetProcessStartInfo(string command)
    {
        // Determinar shell com base no sistema operacional
        var isWindows = OperatingSystem.IsWindows();

        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "/bin/bash",
            Arguments = isWindows ? $"/c {command}" : $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Environment.CurrentDirectory
        };

        return psi;
    }

    /// <summary>
    /// Libera os recursos utilizados pela classe.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Libera os recursos utilizados pela classe.
    /// </summary>
    /// <param name="disposing">Indica se está liberando recursos gerenciados.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
    }
}