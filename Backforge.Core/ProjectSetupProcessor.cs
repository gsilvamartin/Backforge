using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backforge.Core.Enum;
using Backforge.Core.Interfaces;
using Backforge.Core.Models;

namespace Backforge.Core;

/// <summary>
/// Implementação do processador para configuração de projetos.
/// Responsável por gerenciar a criação, configuração e inicialização de projetos.
/// </summary>
public class ProjectSetupProcessor : IProjectSetupProcessor
{
    private readonly ILlamaExecutor _executor;
    private readonly IFileManager _fileManager;
    private readonly ICommandExecutor _commandExecutor;
    private readonly IDependencyManager _dependencyManager;
    private readonly ICodeGenerationProcessor _codeGenerationProcessor;
    private readonly IDocumentationGenerator _docGenerator;
    private readonly ILogger _logger;

    // Constantes para evitar strings repetidas
    private const string DefaultProjectPrefix = "DefaultProject_";
    private const string ReadmeMd = "README.md";

    /// <summary>
    /// Inicializa uma nova instância da classe ProjectSetupProcessor.
    /// </summary>
    public ProjectSetupProcessor(
        ILlamaExecutor executor,
        IFileManager fileManager,
        ICommandExecutor commandExecutor,
        IDependencyManager dependencyManager,
        ICodeGenerationProcessor codeGenerationProcessor,
        IDocumentationGenerator docGenerator,
        ILogger logger)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        _commandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
        _dependencyManager = dependencyManager ?? throw new ArgumentNullException(nameof(dependencyManager));
        _codeGenerationProcessor =
            codeGenerationProcessor ?? throw new ArgumentNullException(nameof(codeGenerationProcessor));
        _docGenerator = docGenerator ?? throw new ArgumentNullException(nameof(docGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processa uma tarefa de configuração de projeto.
    /// </summary>
    public async Task<ExecutionResult> ProcessAsync(
        ExecutionResult result,
        string userRequest,
        string language,
        bool executeCommands,
        bool installDependencies,
        CancellationToken cancellationToken)
    {
        _logger.Log("🏗️ Iniciando configuração de projeto...");

        try
        {
            // Passo 1: Analisar a solicitação para extrair detalhes do projeto
            var projectDetails = await ExtractProjectDetailsAsync(userRequest, language, cancellationToken);
            result.Files.Add(CreateProjectDetailsFile(projectDetails));

            // Passo 2: Criar estrutura de diretórios
            var projectPath = await CreateProjectStructureAsync(projectDetails, cancellationToken);
            LogAndAddToOutput(result, $"📁 Estrutura de diretórios criada em: {projectPath}");

            // Passo 3: Inicializar projeto (se comandos estiverem habilitados)
            await HandleProjectInitialization(result, executeCommands, projectDetails, projectPath, cancellationToken);

            // Passo 4: Instalar dependências (se habilitado)
            await HandleDependencyInstallation(result, installDependencies, executeCommands, projectDetails,
                projectPath, cancellationToken);

            // Passo 5: Gerar arquivos de código básicos
            await GenerateBaseCodeFilesAsync(projectDetails, projectPath, language, cancellationToken);
            LogAndAddToOutput(result, "📝 Arquivos base do projeto gerados");

            // Passo 6: Gerar documentação do projeto
            await GenerateProjectDocumentation(result, projectDetails, projectPath, language, cancellationToken);

            result.Success = true;
            result.Message = $"Projeto '{projectDetails.ProjectName}' configurado com sucesso.";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro durante configuração do projeto", ex);
            result.Success = false;
            result.Message = $"Erro na configuração do projeto: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Cria um arquivo com os detalhes do projeto.
    /// </summary>
    private GeneratedFile CreateProjectDetailsFile(ProjectDetails projectDetails)
    {
        return new GeneratedFile
        {
            FileName = "project_details.json",
            Content = JsonSerializer.Serialize(projectDetails, new JsonSerializerOptions { WriteIndented = true }),
            Language = "json"
        };
    }

    /// <summary>
    /// Lida com a inicialização do projeto com base nas permissões.
    /// </summary>
    private async Task HandleProjectInitialization(
        ExecutionResult result,
        bool executeCommands,
        ProjectDetails projectDetails,
        string projectPath,
        CancellationToken cancellationToken)
    {
        if (executeCommands)
        {
            await InitializeProjectAsync(projectDetails, projectPath, cancellationToken);
            LogAndAddToOutput(result, "🚀 Projeto inicializado com sucesso");
        }
        else
        {
            LogAndAddToOutput(result, "⚠️ Execução de comandos não está habilitada. Pulando inicialização do projeto.");
        }
    }

    /// <summary>
    /// Lida com a instalação de dependências com base nas permissões.
    /// </summary>
    private async Task HandleDependencyInstallation(
        ExecutionResult result,
        bool installDependencies,
        bool executeCommands,
        ProjectDetails projectDetails,
        string projectPath,
        CancellationToken cancellationToken)
    {
        if (installDependencies && executeCommands)
        {
            var dependencies = projectDetails.Dependencies.ToList();
            await InstallDependenciesAsync(dependencies, projectDetails.ProjectType, projectPath, cancellationToken);
            LogAndAddToOutput(result, "📦 Dependências instaladas com sucesso");
        }
        else if (!installDependencies)
        {
            LogAndAddToOutput(result, "⚠️ Instalação de dependências não está habilitada. Pulando instalação.");
        }
    }

    /// <summary>
    /// Gera a documentação do projeto e a adiciona ao resultado.
    /// </summary>
    private async Task GenerateProjectDocumentation(
        ExecutionResult result,
        ProjectDetails projectDetails,
        string projectPath,
        string language,
        CancellationToken cancellationToken)
    {
        var readmeContent = await GenerateProjectDocumentationAsync(projectDetails, cancellationToken);
        var readmePath = Path.Combine(projectPath, ReadmeMd);

        _fileManager.SaveToFile(readmePath, readmeContent, language);
        result.GeneratedFiles.Add(ReadmeMd);
        LogAndAddToOutput(result, "📘 Documentação do projeto gerada");
    }

    /// <summary>
    /// Extrai detalhes do projeto a partir da solicitação do usuário.
    /// </summary>
    private async Task<ProjectDetails> ExtractProjectDetailsAsync(
        string userRequest,
        string language,
        CancellationToken cancellationToken)
    {
        _logger.Log("Extraindo detalhes do projeto da solicitação...");

        string prompt = CreateProjectDetailsPrompt(userRequest, language);
        var jsonResponse = await _executor.CollectFullResponseAsync(prompt);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var cleanedJson = CleanJsonResponse(jsonResponse);
            var projectDetails = JsonSerializer.Deserialize<ProjectDetails>(cleanedJson);

            if (string.IsNullOrEmpty(projectDetails.ProjectName))
            {
                projectDetails.ProjectName = GenerateDefaultProjectName();
            }

            _logger.Log($"Detalhes extraídos: Projeto={projectDetails.ProjectName}, Tipo={projectDetails.ProjectType}");
            return projectDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao processar JSON dos detalhes do projeto", ex);
            return CreateDefaultProjectDetails(language);
        }
    }

    /// <summary>
    /// Cria o prompt para extrair detalhes do projeto.
    /// </summary>
    private string CreateProjectDetailsPrompt(string userRequest, string language)
    {
        return $@"
Extraia os detalhes do projeto a partir da seguinte solicitação do usuário. 
Forneça o resultado em formato JSON seguindo estritamente esta estrutura:
{{
  ""projectName"": ""Nome do projeto (com CamelCase, sem espaços)"",
  ""projectType"": ""Tipo do projeto (Console, Web, Library, API, etc.)"",
  ""description"": ""Descrição breve do projeto"",
  ""language"": ""{language}"",
  ""dependencies"": [""dep1"", ""dep2"", ...],
  ""features"": [""feature1"", ""feature2"", ...],
  ""directories"": [""dir1"", ""dir2"", ...],
  ""mainFiles"": [""arquivo1"", ""arquivo2"", ...]
}}

Solicitação do usuário: {userRequest}

Responda apenas com o JSON, sem texto adicional.
";
    }

    /// <summary>
    /// Gera um nome de projeto padrão com timestamp.
    /// </summary>
    private string GenerateDefaultProjectName()
    {
        return DefaultProjectPrefix + DateTime.Now.ToString("yyyyMMddHHmmss");
    }

    /// <summary>
    /// Cria detalhes de projeto padrão em caso de falha.
    /// </summary>
    private ProjectDetails CreateDefaultProjectDetails(string language)
    {
        return new ProjectDetails
        {
            ProjectName = GenerateDefaultProjectName(),
            ProjectType = DetermineProjectType(language),
            Description = "Projeto gerado automaticamente a partir da solicitação do usuário",
            Language = language,
            Dependencies = [],
            Features = [],
            Directories = DetermineDefaultDirectories(language),
            MainFiles = DetermineDefaultMainFiles(language)
        };
    }

    /// <summary>
    /// Adiciona uma mensagem ao log e ao output do resultado.
    /// </summary>
    private void LogAndAddToOutput(ExecutionResult result, string message)
    {
        _logger.Log(message);
        result.ExecutionOutput.Add(message);
    }

    /// <summary>
    /// Limpa a resposta JSON para remover textos extras que possam causar erro na desserialização.
    /// </summary>
    private string CleanJsonResponse(string jsonResponse)
    {
        int startIndex = jsonResponse.IndexOf('{');
        int endIndex = jsonResponse.LastIndexOf('}');

        if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
        {
            return jsonResponse.Substring(startIndex, endIndex - startIndex + 1);
        }

        return jsonResponse;
    }

    /// <summary>
    /// Determina o tipo de projeto com base na linguagem.
    /// </summary>
    private string DetermineProjectType(string language)
    {
        return language.ToLower() switch
        {
            "c#" or "csharp" => "Console",
            "javascript" or "js" => "Node",
            "typescript" or "ts" => "Node",
            "python" or "py" => "Script",
            "java" => "Maven",
            _ => "Generic"
        };
    }

    /// <summary>
    /// Determina diretórios padrão com base na linguagem.
    /// </summary>
    private List<string> DetermineDefaultDirectories(string language)
    {
        return language.ToLower() switch
        {
            "c#" or "csharp" => new List<string> { "src", "tests", "docs" },
            "javascript" or "js" or "typescript" or "ts" => new List<string> { "src", "tests", "public" },
            "python" or "py" => new List<string> { "src", "tests", "docs" },
            "java" => new List<string> { "src/main/java", "src/main/resources", "src/test/java" },
            _ => new List<string> { "src", "tests", "docs" }
        };
    }

    /// <summary>
    /// Determina arquivos principais padrão com base na linguagem.
    /// </summary>
    private List<string> DetermineDefaultMainFiles(string language)
    {
        return language.ToLower() switch
        {
            "c#" or "csharp" => new List<string> { "Program.cs", "project.csproj" },
            "javascript" or "js" => new List<string> { "index.js", "package.json" },
            "typescript" or "ts" => new List<string> { "index.ts", "package.json", "tsconfig.json" },
            "python" or "py" => new List<string> { "main.py", "requirements.txt" },
            "java" => new List<string> { "Main.java", "pom.xml" },
            _ => new List<string> { "main", ReadmeMd }
        };
    }

    /// <summary>
    /// Cria a estrutura de diretórios para o projeto.
    /// </summary>
    private async Task<string> CreateProjectStructureAsync(
        ProjectDetails projectDetails,
        CancellationToken cancellationToken)
    {
        _logger.Log("Criando estrutura de diretórios do projeto...");

        var projectPath = Path.Combine(projectDetails.OutputDirectory, projectDetails.ProjectName);

        // Criar diretório principal do projeto
        _fileManager.CreateDirectory(projectPath);

        // Criar diretórios da estrutura do projeto
        foreach (var directory in projectDetails.Directories)
        {
            var dirPath = Path.Combine(projectPath, directory);
            _fileManager.CreateDirectory(dirPath);
            _logger.Log($"Diretório criado: {directory}");
        }

        return projectPath;
    }

    /// <summary>
    /// Inicializa o projeto usando comandos específicos para o tipo de projeto.
    /// </summary>
    private async Task InitializeProjectAsync(
        ProjectDetails projectDetails,
        string projectPath,
        CancellationToken cancellationToken)
    {
        _logger.Log($"Inicializando projeto {projectDetails.ProjectType} em {projectPath}...");

        string initCommand = GetInitializationCommand(projectDetails, projectPath);

        if (!string.IsNullOrEmpty(initCommand))
        {
            var result = await _commandExecutor.ExecuteCommandAsync(initCommand, cancellationToken);
            _logger.Log($"Resultado da inicialização: {(result.Success ? "Sucesso" : "Falha")}");

            if (!result.Success)
            {
                _logger.LogError("Erro na inicialização do projeto", new Exception(result.Output));
            }
        }
        else
        {
            _logger.Log("Nenhum comando de inicialização encontrado para o tipo de projeto.");
        }
    }

    /// <summary>
    /// Obtém o comando de inicialização com base no tipo de projeto e linguagem.
    /// </summary>
    private string GetInitializationCommand(ProjectDetails projectDetails, string projectPath)
    {
        string language = projectDetails.Language.ToLower();
        string projectType = projectDetails.ProjectType.ToLower();

        // C# projects
        if ((language == "c#" || language == "csharp") &&
            (projectType == "console" || projectType == "web" || projectType == "api"))
        {
            return $"dotnet new {projectType} -o {projectPath}";
        }

        // JavaScript projects
        if (projectType == "node" && (language == "javascript" || language == "js"))
        {
            return "npm init -y";
        }

        // TypeScript projects
        if (projectType == "node" && (language == "typescript" || language == "ts"))
        {
            return "npm init -y && npm install --save-dev typescript @types/node";
        }

        // Python projects
        if ((projectType == "python" || projectType == "script") &&
            (language == "python" || language == "py"))
        {
            return $"echo \"# {projectDetails.ProjectName}\" > \"{Path.Combine(projectPath, ReadmeMd)}\"";
        }

        // Java projects
        if (projectType == "maven" && language == "java")
        {
            return
                $"mvn archetype:generate -DgroupId=com.example -DartifactId={projectDetails.ProjectName} -DarchetypeArtifactId=maven-archetype-quickstart -DinteractiveMode=false";
        }

        return null;
    }

    /// <summary>
    /// Instala as dependências necessárias para o projeto.
    /// </summary>
    private async Task InstallDependenciesAsync(
        List<string> dependencies,
        string projectType,
        string projectPath,
        CancellationToken cancellationToken)
    {
        if (dependencies == null || dependencies.Count == 0)
        {
            _logger.Log("Nenhuma dependência especificada para instalação.");
            return;
        }

        _logger.Log($"Instalando {dependencies.Count} dependências...");

        foreach (var dependency in dependencies)
        {
            _logger.Log($"Instalando dependência: {dependency}");

            var result = await _dependencyManager.InstallDependencyAsync(
                projectType, dependency, cancellationToken);

            if (result.Success)
            {
                _logger.Log($"✅ Dependência {dependency} instalada com sucesso");
            }
            else
            {
                _logger.LogError($"❌ Falha ao instalar dependência {dependency}",
                    new Exception(result.CommandOutput));
            }
        }
    }

    /// <summary>
    /// Gera os arquivos de código base do projeto.
    /// </summary>
    private async Task GenerateBaseCodeFilesAsync(
        ProjectDetails projectDetails,
        string projectPath,
        string language,
        CancellationToken cancellationToken)
    {
        _logger.Log("Gerando arquivos de código base...");

        foreach (var file in projectDetails.MainFiles)
        {
            if (File.Exists(Path.Combine(projectPath, file)))
            {
                _logger.Log($"Arquivo {file} já existe, pulando geração.");
                continue;
            }

            string filePrompt = CreateFileGenerationPrompt(file, projectDetails, language);
            var fileContent = await _executor.CollectFullResponseAsync(filePrompt);
            cancellationToken.ThrowIfCancellationRequested();

            // Limpar o conteúdo do arquivo para remover possíveis marcações de código
            fileContent = CleanCodeContent(fileContent);

            // Determinar diretório apropriado para o arquivo
            string filePath = DetermineFilePath(file, projectDetails, projectPath);

            // Salvar o arquivo
            _fileManager.SaveToFile(filePath, fileContent, language);
            _logger.Log($"Arquivo base gerado: {filePath}");
        }
    }

    /// <summary>
    /// Cria o prompt para geração de arquivo.
    /// </summary>
    private string CreateFileGenerationPrompt(string file, ProjectDetails projectDetails, string language)
    {
        return $@"
Crie um arquivo {file} para um projeto {projectDetails.ProjectType} em {language}.
O projeto se chama '{projectDetails.ProjectName}' e tem a seguinte descrição:
{projectDetails.Description}

Recursos do projeto: {string.Join(", ", projectDetails.Features)}

Gere o conteúdo completo do arquivo {file} que funcionaria como um arquivo base para iniciar o desenvolvimento.
Não inclua comentários como 'Aqui você pode adicionar X' ou 'Implemente Y aqui'.
O código deve ser funcional e seguir as melhores práticas da linguagem {language}.

Responda apenas com o código, sem explicações ou marcações adicionais.
";
    }

    /// <summary>
    /// Limpa o conteúdo de código removendo possíveis marcações extras.
    /// </summary>
    private string CleanCodeContent(string content)
    {
        // Remover blocos de código markdown se existirem
        if (content.StartsWith("```") && content.EndsWith("```"))
        {
            var lines = content.Split('\n');
            if (lines.Length > 2)
            {
                // Remover primeira e última linha (as marcações ```)
                var cleanedLines = lines.Skip(1).Take(lines.Length - 2);
                return string.Join('\n', cleanedLines);
            }
        }

        return content;
    }

    /// <summary>
    /// Determina o caminho apropriado para um arquivo com base no tipo e nome.
    /// </summary>
    private string DetermineFilePath(string fileName, ProjectDetails projectDetails, string projectPath)
    {
        // Dicionário com mapeamentos de extensão para diretório
        var extensionDirectoryMap = new Dictionary<string, string>
        {
            { ".cs", "src" },
            { ".js", "src" },
            { ".ts", "src" },
            { ".py", "src" },
            { ".java", "src/main/java" }
        };

        // Verificar se há um diretório específico para esta extensão
        foreach (var mapping in extensionDirectoryMap)
        {
            if (fileName.EndsWith(mapping.Key) && projectDetails.Directories.Contains(mapping.Value))
            {
                return Path.Combine(projectPath, mapping.Value, fileName);
            }
        }

        // Se não houver mapeamento específico, usar o diretório raiz
        return Path.Combine(projectPath, fileName);
    }

    /// <summary>
    /// Gera a documentação básica do projeto (README.md).
    /// </summary>
    private async Task<string> GenerateProjectDocumentationAsync(
        ProjectDetails projectDetails,
        CancellationToken cancellationToken)
    {
        _logger.Log("Gerando documentação do projeto (README.md)...");

        string prompt = CreateDocumentationPrompt(projectDetails);

        // TODO: Implementar geração de documentação
        // var readmeContent = await _docGenerator.GenerateDocumentationAsync(prompt, cancellationToken);
        return ""; // Placeholder até implementação completa
    }

    /// <summary>
    /// Cria o prompt para geração de documentação.
    /// </summary>
    private string CreateDocumentationPrompt(ProjectDetails projectDetails)
    {
        return $@"
Crie um README.md completo para um projeto {projectDetails.ProjectType} em {projectDetails.Language}.
O projeto se chama '{projectDetails.ProjectName}' e tem a seguinte descrição:
{projectDetails.Description}

Recursos do projeto:
{string.Join("\n", projectDetails.Features.Select(f => "- " + f))}

Dependências:
{string.Join("\n", projectDetails.Dependencies.Select(d => "- " + d))}

A documentação deve incluir as seguintes seções:
1. Título e descrição
2. Requisitos do sistema
3. Instalação
4. Como usar
5. Estrutura do projeto
6. Recursos/Funcionalidades
7. Contribuição (instruções básicas)
8. Licença (MIT)

Use Markdown adequado com formatação, títulos, listas, código quando apropriado, etc.
";
    }
}