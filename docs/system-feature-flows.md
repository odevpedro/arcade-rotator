# system-feature-flows.md
# Registro de Fluxos de Funcionalidades — ArcadeOrchestrator

> Este arquivo é o mapa histórico e incremental do comportamento do sistema.
> Nunca remova seções anteriores. Sempre adicione novas features ao final.

---

# Feature: Modo Festa — Rotação Automática de Jogos

## Resumo

O **Modo Festa** é a funcionalidade central do ArcadeOrchestrator. Ele orquestra um ciclo contínuo de sessões arcade: lança um jogo no emulador, monitora o fim da partida via estratégias de detecção hierárquicas, encerra o emulador e sorteia automaticamente o próximo jogo com base em pesos e regras de franquia configuráveis. O objetivo é reproduzir a experiência de "mais uma ficha, próximo jogo" de forma totalmente automática e sem intervenção do usuário.

## Fluxo principal

### 1. Ponto de entrada

O fluxo é iniciado pela classe `StartFestaModeUseCase`, chamada pela UI (overlay ou tray icon) ou via hotkey global `Ctrl+Alt+F`. Ela recebe a configuração carregada e o catálogo de jogos como dependências injetadas.

### 2. Validação de entrada

Antes de iniciar, o `ConfigManager` valida:
- Se o executável do emulador existe no caminho configurado.
- Se o catálogo possui ao menos um jogo com ROM acessível no sistema de arquivos.
- Se os pesos somam valores positivos (evitar divisão por zero no sorteio).

Em caso de falha, o `StartFestaModeUseCase` emite um evento de erro para o `OverlayService` exibir ao usuário e aborta sem iniciar a máquina de estados.

### 3. Orquestração da aplicação

A `StateMachine` coordena todo o ciclo:

```
IDLE
  │  StartFestaModeUseCase.Execute()
  ▼
LAUNCHING
  │  EmulatorAdapter.LaunchAsync(game)
  ▼
PLAYING
  │  SessionEndDetector.WaitForSessionEndAsync()
  ▼
DETECTING_END
  │  EmulatorAdapter.StopAsync()
  ▼
COOLDOWN (5s configurável)
  │  RotationEngine.PickNext()
  ▼
IDLE → LAUNCHING → ... (ciclo)
```

### 4. Regras de negócio

**Sorteio ponderado com bias de franquia:**
O `RotationEngine` primeiro decide se mantém a franquia atual (probabilidade = `franchise_bias`, padrão 0.4) ou sorteia uma nova franquia dentre todas disponíveis com peso proporcional. Em seguida, sorteia um jogo dentro da franquia eleita, também com pesos individuais. Jogos presentes na fila de anti-repeat (tamanho = `anti_repeat_count`, padrão 3) são excluídos dos candidatos.

**Hierarquia de detecção:**
O `CompositeDetector` executa todas as estratégias configuradas para o jogo em paralelo via `Task.WhenAny`. A primeira a disparar cancela as demais. A hierarquia de prioridade é implícita pela velocidade de resposta de cada estratégia:
1. `ProcessWatchStrategy` — mais rápida (O(1), event-driven).
2. `LogParserStrategy` — depende do emulador escrever no log antes do timeout.
3. `TimeoutStrategy` — fallback garantido, sempre dispara se as demais falharem.

### 5. Persistência / Integrações

- **`YamlGameCatalogRepository`**: lê `catalog.yaml` na inicialização e mantém o catálogo em memória. Não há banco de dados.
- **`EmulatorAdapter`**: integração com o processo externo do emulador via `System.Diagnostics.Process`.
- **`RotationHistoryWriter`**: ao final de cada sessão, appenda uma linha JSON no arquivo `logs/rotation-history.jsonl` com: timestamp, jogo jogado, estratégia que detectou o fim, e próximo jogo sorteado.
- **`SerilogLogger`**: arquivo rotativo diário em `logs/orchestrator-YYYY-MM-DD.log`.

### 6. Resposta final

Não há resposta direta (o fluxo é contínuo). A cada transição de estado, a `StateMachine` publica eventos para o `OverlayService` atualizar o display: nome do jogo atual, próximo sorteado, tempo de sessão e contador de rotações do dia.

## Fluxos alternativos e erros

**Emulador não inicia (ROM inválida ou emulador corrompido):**
`EmulatorAdapter.LaunchAsync` lança `EmulatorLaunchException`. A `StateMachine` captura, loga o erro, marca o jogo como indisponível na sessão atual e transita para COOLDOWN sorteando o próximo jogo (pulando o problemático).

**Nenhum candidato disponível para sorteio (todos na fila anti-repeat):**
`RotationEngine.PickNext` limpa a fila de anti-repeat e sorteia novamente. Nunca bloqueia o ciclo.

**Timeout da estratégia de processo (emulador travado):**
`StopAsync` tenta `CloseMainWindow` (WM_CLOSE) com timeout de 3 segundos. Se o processo não encerrar, chama `ProcessHelper.KillProcessTree` que usa `TerminateProcess` no processo pai e todos os filhos, evitando processos zumbi.

**Log não encontrado para `LogParserStrategy`:**
A estratégia retorna imediatamente sem disparar, e o `CompositeDetector` continua aguardando as demais. O `TimeoutStrategy` assume como fallback garantido.

**Hotkey de skip acionada pelo usuário:**
`HotkeyManager` recebe o evento Win32, publica `SkipRequestedEvent`. A `StateMachine` força a transição para `DETECTING_END` independentemente do estado atual, executando o fluxo normal de encerramento e sorteio.

## Decisões técnicas importantes

**Por que `Task.WhenAny` em vez de um loop de polling único?**
Cada estratégia tem natureza assíncrona diferente (event-driven vs timer vs FileSystemWatcher). Executar em paralelo com `Task.WhenAny` elimina o overhead de um loop central e permite que a estratégia mais rápida para aquele contexto vença sem penalizar as demais.

**Por que imutabilidade da config durante sessão?**
Um hot-reload de pesos ou estratégias de detecção no meio de uma sessão poderia gerar estados inconsistentes (ex: timeout cancelado mas nova estratégia ainda não iniciada). A config é carregada uma vez por ciclo IDLE→LAUNCHING, garantindo consistência dentro de cada sessão.

**Por que `Channel<T>` entre o Detector e a StateMachine?**
Evita coupling direto via callbacks e permite que o detector publique o evento de fim de sessão sem bloquear sua própria thread, mesmo que a StateMachine esteja ocupada com operações de I/O (KillProcessTree).

**Por que Borderless Window em vez de Fullscreen Exclusive?**
O overlay WPF não consegue se sobrepor a janelas DirectX em modo Exclusive Fullscreen. Forçar Borderless Window via argumento do emulador é o único caminho confiável para exibir o overlay sem patches no driver gráfico.

## Trechos de código relevantes

```csharp
// Início do ciclo na StateMachine (simplificado)
public async Task RunFestaCycleAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        TransitionTo(OrchestratorState.Launching);
        var game = _rotationEngine.PickNext(currentGameId: _currentGame?.Id);
        var emulatorProcess = await _adapter.LaunchAsync(game, ct);
        
        _currentGame = game;
        TransitionTo(OrchestratorState.Playing);
        
        await _detector.WaitForSessionEndAsync(emulatorProcess, ct);
        
        TransitionTo(OrchestratorState.DetectingEnd);
        await _adapter.StopAsync(emulatorProcess, ct);
        _historyWriter.Record(game, _detector.LastTriggeredStrategy);
        
        TransitionTo(OrchestratorState.Cooldown);
        await Task.Delay(_config.CooldownBetweenGamesMs, ct);
    }
}
```

---

# Feature: Sistema de Detecção Híbrida de Fim de Sessão (CompositeDetector)

## Resumo

A detecção do fim de uma sessão arcade é o problema central do sistema, pois emuladores antigos não emitem eventos padronizados de Game Over. O `CompositeDetector` resolve isso executando múltiplas estratégias de detecção em paralelo, onde a primeira a disparar vence e as demais são canceladas. Isso garante robustez: mesmo que o log do emulador não registre o padrão esperado, o timeout garante que o ciclo nunca trave.

## Fluxo principal

### 1. Ponto de entrada

A `StateMachine` chama `SessionEndDetector.WaitForSessionEndAsync(emulatorProcess, ct)` imediatamente após a transição para o estado `PLAYING`.

### 2. Validação de entrada

O `CompositeDetector` valida que ao menos uma estratégia foi configurada. Se a lista estiver vazia, lança `InvalidOperationException` na construção (fail-fast), nunca em runtime.

### 3. Orquestração da aplicação

O `CompositeDetector` cria um `CancellationTokenSource` linked ao token externo e dispara todas as estratégias via `Task.WhenAny`. Um callback `OnSessionEnd()` thread-safe (com flag `triggered`) garante idempotência: mesmo que duas estratégias disparem simultaneamente, apenas o primeiro sinal é processado.

### 4. Regras de negócio

A resolução do conjunto de estratégias para cada jogo segue a ordem:
1. Busca override específico no YAML por `game.RomFileName`.
2. Se não encontrar, usa a estratégia `default` da config.
3. O modo `composite` sempre inclui `ProcessWatchStrategy` como membro obrigatório (o processo pode fechar a qualquer momento).

### 5. Persistência / Integrações

O detector não persiste nada diretamente. Ao disparar, publica `SessionEndedEvent` com metadado `TriggeredBy` (nome da estratégia vencedora) para o `Logger` e `RotationHistoryWriter`.

### 6. Resposta final

`WaitForSessionEndAsync` retorna (Task completa) assim que o primeiro sinal é detectado. A `StateMachine` retoma o controle e executa `StopAsync`.

## Fluxos alternativos e erros

**Todas as estratégias falham silenciosamente:**
Impossível por design: `TimeoutStrategy` sempre dispara após o tempo configurado, servindo como garantia de vida do ciclo.

**Token externo cancelado (usuário fechou o orquestrador):**
`Task.WhenAny` completa com `OperationCanceledException`, que é propagada para a `StateMachine` executar shutdown gracioso.

## Decisões técnicas importantes

**Idempotência do callback `OnSessionEnd`:**
Sem a flag `triggered`, duas estratégias disparando com diferença de milissegundos causariam dupla transição de estado na `StateMachine`. A flag com `Interlocked.CompareExchange` garante que apenas o primeiro vence, independente de concorrência.

**`Task.WhenAll` após `Task.WhenAny`:**
Após o sinal, o código aguarda todas as tasks restantes completarem (com token cancelado) antes de retornar. Isso evita que tasks "zumbi" de detecção continuem consumindo recursos no ciclo seguinte.

---

# Feature: Setup Script — Geração Automatizada da Estrutura do Projeto

## Resumo

O `setup.sh` é um script Bash que provisiona do zero toda a estrutura do projeto ArcadeOrchestrator em uma única execução. Ele cria a árvore de diretórios, inicializa a solução .NET, cria os três projetos (Core, Infrastructure, Overlay) e dois de teste (xUnit), configura todas as referências entre projetos, instala os pacotes NuGet necessários, gera arquivos de código placeholder tipados, cria os YAMLs de configuração iniciais e faz o primeiro commit Git. O objetivo é eliminar o setup manual e garantir que qualquer desenvolvedor chegue a um estado compilável e testável em menos de 2 minutos.

## Fluxo principal

### 1. Ponto de entrada

Execução direta via terminal: `bash setup.sh [diretório-destino]`. Recebe um argumento opcional de diretório base; se omitido, usa o diretório atual. O diretório final criado é sempre `<base>/ArcadeOrchestrator`.

### 2. Validação de entrada

A função `check_prerequisites` valida, na ordem:
- Existência do binário `dotnet` no PATH (`command -v dotnet`).
- Versão mínima do SDK: extrai o major version via `cut` e compara com `8`. Falha com mensagem e URL de download se inferior.
- Presença do `git` (opcional): apenas emite warning se ausente, não aborta.
- Detecção de ambiente: avisa que WPF só compila no Windows se executado em WSL/Linux puro.
- Existência prévia do diretório destino: falha imediatamente para não sobrescrever projetos existentes.

### 3. Orquestração da aplicação

O script executa 10 funções em sequência, cada uma com escopo único e output colorido via variáveis ANSI:

```
check_prerequisites
  └─► setup_root
        └─► create_directory_structure   (mkdir -p em 30+ dirs)
              └─► create_dotnet_solution  (dotnet new sln/classlib/wpf/xunit)
                    └─► add_projects_to_solution  (dotnet sln add)
                          └─► add_project_references  (dotnet add reference)
                                └─► install_nuget_packages  (dotnet add package)
                                      └─► create_placeholder_files  (heredocs)
                                            └─► create_config_files  (YAML)
                                                  └─► create_docs    (README, .gitignore)
                                                        └─► init_git
                                                              └─► final_check (dotnet restore)
```

### 4. Regras de negócio

**Ordem de criação dos projetos importa:** o Core deve existir antes de Infrastructure e Overlay adicionarem referência a ele. O script segue esta ordem explicitamente.

**Pacotes por camada:**
- `Core`: apenas `Microsoft.Extensions.Logging.Abstractions` e `DI.Abstractions` — sem dependências concretas de terceiros (princípio de inversão de dependência).
- `Infrastructure`: `YamlDotNet`, `Serilog` (+ sinks File e Console), `Microsoft.Extensions.Hosting`.
- `Overlay`: `CommunityToolkit.Mvvm` para MVVM sem boilerplate, `Microsoft.Extensions.DependencyInjection`.
- `Tests`: `FluentAssertions` + `NSubstitute` (mock sem código gerado).

**Arquivos placeholder com namespace correto:** cada `.cs` gerado por heredoc já tem o namespace definitivo da camada, evitando refatoração futura.

**Idempotência negativa:** o script falha se o diretório já existir (`set -euo pipefail` + verificação explícita). Não há modo de "update" — deve ser usado apenas para criação inicial.

### 5. Persistência / Integrações

- `dotnet CLI`: toda criação de projetos e adição de pacotes passa pela CLI oficial, garantindo arquivos `.csproj` e `Directory.Build.props` corretos.
- `git CLI` (opcional): cria repositório local com um commit inicial contendo toda a estrutura. Permite `git log` limpo desde o primeiro dia.
- Sistema de arquivos: todos os arquivos são escritos com heredocs Bash, sem dependências de ferramentas externas além de `dotnet` e `bash`.

### 6. Resposta final

O script encerra com um sumário visual no terminal informando o caminho criado e os próximos passos (abrir a solução, configurar emulador e ROMs, rodar e testar). O exit code é `0` em sucesso e não-zero em qualquer falha (garantido por `set -euo pipefail`).

## Fluxos alternativos e erros

**`dotnet` não encontrado:** `error()` imprime mensagem em vermelho com a URL de download e chama `exit 1`. Nenhuma pasta é criada.

**SDK abaixo de 8.0:** mesmo comportamento: mensagem específica de versão + exit 1.

**Diretório destino já existe:** falha antes de criar qualquer arquivo, protegendo projetos existentes.

**`dotnet restore` falha na verificação final:** emite `warn` (amarelo) em vez de `error`, pois pode ser um aviso de compatibilidade de plataforma (ex.: WPF em WSL). O desenvolvedor é instruído a rodar `dotnet restore --verbosity normal` para diagnóstico.

**Git não instalado:** apenas pula a etapa `init_git` com warning, não falha o script.

## Decisões técnicas importantes

**`set -euo pipefail` no topo:** qualquer comando que falhe aborta o script imediatamente. Sem isso, um `dotnet add package` com typo no nome seria ignorado silenciosamente e o projeto compilaria sem o pacote.

**Heredocs para os arquivos `.cs`:** evita dependências de templates externos e garante que o conteúdo gerado seja exatamente o esperado, independente do ambiente.

**`> /dev/null` nos comandos `dotnet new/add`:** suprime o output verboso da CLI do .NET, mantendo o terminal limpo. Erros ainda aparecem via stderr (não redirecionado).

**Separação em funções nomeadas:** cada etapa é uma função isolada com nome descritivo. Permite re-executar etapas individualmente durante desenvolvimento do próprio script (`bash -c "source setup.sh; install_nuget_packages"`).

**Argumento opcional de diretório:** `"${1:-}"` usa o diretório atual como fallback sem erro, tornando o script utilizável tanto com caminho absoluto quanto relativo.

## Trechos de código relevantes

```bash
# Verificação de versão do SDK
sdk_major=$(dotnet --version | cut -d'.' -f1)
if [[ "$sdk_major" -lt 8 ]]; then
    error "Requer .NET SDK 8.0+. Versão atual: $(dotnet --version)"
fi

# Criação do projeto WPF com target framework correto para Windows
dotnet new wpf \
    --name ArcadeOrchestrator.Overlay \
    --output src/ArcadeOrchestrator.Overlay \
    --framework net8.0-windows

# Referência Infrastructure → Core
dotnet add \
    src/ArcadeOrchestrator.Infrastructure/ArcadeOrchestrator.Infrastructure.csproj \
    reference \
    src/ArcadeOrchestrator.Core/ArcadeOrchestrator.Core.csproj
```
