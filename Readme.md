# ArcadeOrchestrator

> **Orquestrador de partidas arcade para Windows.**  
> Alterna automaticamente entre jogos MVS / Neo Geo usando qualquer emulador existente como backend — sem reimplementar emulação.

---

## O que é isso?

Sabe aquela experiência de fliperama onde você coloca uma ficha, joga até o Game Over e o próximo jogo já começa automaticamente? O ArcadeOrchestrator faz exatamente isso no PC.

Você configura uma lista de jogos, define regras de rotação e pesos por franquia, e o sistema cuida do resto: detecta quando a partida acabou, encerra o emulador, sorteia o próximo jogo e o lança — em loop infinito, sem você tocar em nada.

```
KOF '99  →  [Game Over detectado]  →  Garou: MOTW  →  [Game Over detectado]  →  Metal Slug 3  →  ...
```

---

## Funcionalidades

| Funcionalidade | Status |
|---|---|
| Modo Festa — rotação automática infinita | 🔨 Em desenvolvimento |
| Detecção de fim de sessão por timeout configurável | 🔨 Em desenvolvimento |
| Detecção por monitoramento de processo do emulador | 🔨 Em desenvolvimento |
| Detecção por parsing de logs do emulador | 📋 Planejado |
| Sorteio ponderado com anti-repeat e bias de franquia | 🔨 Em desenvolvimento |
| Overlay transparente (click-through, TopMost, DPI-aware) | 🔨 Em desenvolvimento |
| Hotkeys globais (skip, restart, pausar rotação) | 🔨 Em desenvolvimento |
| Catálogo de ROMs configurável por franquia e pesos | 📋 Planejado |
| Histórico de rotações em arquivo | 📋 Planejado |
| Suporte a RetroArch | 🔨 Em desenvolvimento |
| Suporte a FBNeo | 📋 Planejado |
| Leitura de memória por jogo (plugin futuro) | 🗓️ Fase 2 |

---

## Pré-requisitos

### Obrigatório

| Ferramenta | Versão mínima | Download |
|---|---|---|
| **Windows** | 10 ou 11 (64-bit) | — |
| **.NET SDK** | **8.0** | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download/dotnet/8.0) |

> Instale o **SDK** completo, não apenas o Runtime. O SDK inclui o compilador, o CLI e as ferramentas de teste.

### Recomendado

| Ferramenta | Para que serve |
|---|---|
| **Visual Studio 2022 Community** | IDE principal. Workload necessária: *Desenvolvimento para desktop com .NET* |
| **Git** | Controle de versão |
| **VS Code + extensão C# Dev Kit** | Alternativa leve ao Visual Studio |

> No instalador do Visual Studio, dentro da workload "Desenvolvimento para desktop com .NET", você pode **desmarcar com segurança**: Entity Framework 6, ML.NET Model Builder e Ferramentas de criação de perfil do .NET. Mantenha o **Blend for Visual Studio** — é necessário para editar o XAML do Overlay.

---

## Início rápido

### 1. Gerar a estrutura do projeto

```bash
# Git Bash (Windows) — recomendado
bash setup.sh "C:/projetos"

# WSL
bash setup.sh ~/projetos
```

O script cria toda a solução `.NET`, projetos, referências, instala os pacotes NuGet e faz o primeiro commit Git automaticamente. Leva cerca de 90 segundos.

### 2. Configurar o emulador

Edite `config/config.yaml`:

```yaml
emulator:
  type: retroarch
  executable_path: "C:\\RetroArch\\retroarch.exe"
  core_path: "C:\\RetroArch\\cores\\fbneo_libretro.dll"
  window_mode: borderless   # necessário para o overlay funcionar
```

### 3. Configurar as ROMs

Edite `config/catalog.yaml`:

```yaml
franchises:
  - id: kof
    name: "King of Fighters"
    weight: 40
    games:
      - id: kof99
        rom: kof99.zip
        rom_path: "C:\\ROMs\\neogeo\\kof99.zip"
        display_name: "KOF '99"
        weight: 25
```

### 4. Build e execução

```bash
# Restaurar pacotes NuGet
dotnet restore

# Build completo
dotnet build

# Rodar o orquestrador
dotnet run --project src/ArcadeOrchestrator.Overlay

# Rodar os testes
dotnet test
```

---

## Hotkeys globais padrão

| Ação | Hotkey |
|---|---|
| Pular para o próximo jogo | `Ctrl + Alt + N` |
| Reiniciar jogo atual | `Ctrl + Alt + R` |
| Pausar / retomar rotação | `Ctrl + Alt + P` |
| Abrir configurações | `Ctrl + Alt + C` |
| Encerrar o orquestrador | `Ctrl + Alt + Q` |

> Todas as hotkeys são configuráveis em `config/config.yaml` na seção `hotkeys`.

---

## Como funciona a detecção de fim de sessão

Este é o coração do sistema. Jogos arcade antigos não emitem eventos padronizados de Game Over, então o ArcadeOrchestrator combina três estratégias em paralelo — a primeira que disparar vence:

```
┌─────────────────────────────────────────────┐
│           CompositeDetector                  │
│                                             │
│  ① ProcessWatchStrategy  ──► mais rápida   │
│     Detecta se o emulador fechou            │
│                                             │
│  ② LogParserStrategy     ──► média         │
│     Regex configurável no log do emulador   │
│                                             │
│  ③ TimeoutStrategy       ──► fallback      │
│     Sempre dispara — o ciclo nunca trava    │
└─────────────────────────────────────────────┘
```

Cada jogo pode ter sua própria estratégia e timeout configurados individualmente:

```yaml
detection:
  default:
    strategy: timeout
    timeout_seconds: 90
  overrides:
    kof99.zip:
      strategy: composite
      timeout_seconds: 120    # Tempo extra para a tela de Continue
    garou.zip:
      strategy: log_parser
      log_pattern: "Game Over"
      fallback_to_timeout: true
      timeout_seconds: 150
```

---

## Arquitetura

```
ArcadeOrchestrator/
├── src/
│   ├── ArcadeOrchestrator.Core/           # Domínio e interfaces (sem dependências externas)
│   │   ├── Domain/
│   │   │   ├── Entities/                  # Game, Franchise, RotationSession
│   │   │   ├── Enums/                     # OrchestratorState, DetectionStrategyType
│   │   │   └── Events/                    # SessionEndedEvent
│   │   ├── Application/
│   │   │   ├── Interfaces/                # IEmulatorAdapter, IDetectionStrategy, IGameCatalog
│   │   │   ├── Services/                  # StateMachine, RotationEngine, SessionEndDetector
│   │   │   └── UseCases/                  # StartFestaModeUseCase, SkipToNextGameUseCase
│   │   └── Detection/                     # TimeoutStrategy, ProcessWatchStrategy, LogParserStrategy
│   │
│   ├── ArcadeOrchestrator.Infrastructure/ # Implementações concretas
│   │   ├── Adapters/                      # RetroArchAdapter, FBNeoAdapter
│   │   ├── Catalog/                       # YamlGameCatalogRepository
│   │   ├── Config/                        # ConfigManager + modelos tipados
│   │   ├── Logging/                       # SerilogLogger, RotationHistoryWriter
│   │   └── Win32/                         # HotkeyManager, ProcessHelper, NativeMethods (P/Invoke)
│   │
│   └── ArcadeOrchestrator.Overlay/        # Aplicação WPF (entry point)
│       ├── Views/                         # OverlayWindow.xaml, ConfigWindow.xaml
│       ├── ViewModels/                    # OverlayViewModel, ConfigViewModel
│       └── Services/                      # OverlayService
│
├── tests/
│   ├── ArcadeOrchestrator.Core.Tests/
│   └── ArcadeOrchestrator.Infrastructure.Tests/
│
├── config/
│   ├── config.yaml                        # Configuração do emulador, hotkeys, detecção
│   └── catalog.yaml                       # Lista de jogos por franquia com pesos
│
├── docs/
│   └── system-feature-flows.md            # Registro histórico das features e fluxos
│
├── logs/                                  # Gerado em runtime (ignorado pelo Git)
├── backlog.md
├── setup.sh
└── ArcadeOrchestrator.sln
```

### Dependências entre projetos

```
Core  ◄──  Infrastructure  ◄──  Overlay (entry point)
Core  ◄──  Core.Tests
Infrastructure  ◄──  Infrastructure.Tests
```

O `Core` não depende de nenhum projeto interno — apenas de abstrações do .NET e `Microsoft.Extensions.Logging.Abstractions`. Isso permite testar toda a lógica de domínio sem instanciar emuladores ou janelas WPF.

---

## Stack tecnológica

| Camada | Tecnologia | Motivo da escolha |
|---|---|---|
| Linguagem | C# 12 | Records, pattern matching, async/await nativo |
| Runtime | .NET 8 LTS | Suporte até novembro de 2026 |
| UI / Overlay | WPF | Único framework maduro com suporte real a janelas click-through e transparência no Windows |
| MVVM | CommunityToolkit.Mvvm | Elimina boilerplate de INotifyPropertyChanged sem geração de código pesada |
| Config | YamlDotNet | Deserialização tipada de YAML sem reflexão em runtime |
| Logging | Serilog | Structured logging com rolling file e sink de console |
| Testes | xUnit + FluentAssertions + NSubstitute | Combinação padrão do ecossistema .NET para testes unitários legíveis |
| Win32 | P/Invoke via CsWin32 | `RegisterHotKey`, `SetWindowPos`, `TerminateProcess` sem wrappers frágeis |

---

## Pacotes NuGet instalados

| Projeto | Pacote | Versão usada |
|---|---|---|
| Core | `Microsoft.Extensions.Logging.Abstractions` | 8.x |
| Core | `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.x |
| Infrastructure | `YamlDotNet` | latest stable |
| Infrastructure | `Serilog` + `Serilog.Sinks.File` + `Serilog.Sinks.Console` | latest stable |
| Infrastructure | `Microsoft.Extensions.Hosting` | 8.x |
| Overlay | `CommunityToolkit.Mvvm` | latest stable |
| Overlay | `Microsoft.Extensions.DependencyInjection` | 8.x |
| Tests | `FluentAssertions` | latest stable |
| Tests | `NSubstitute` | latest stable |

---

## Avisos importantes

**ROMs e BIOS:** este projeto não distribui, baixa nem referencia ROMs ou arquivos de BIOS. Assume-se que o usuário possui os arquivos legalmente. Configure os caminhos no `catalog.yaml` apontando para seus próprios arquivos.

**Overlay em Fullscreen Exclusive:** configure o emulador para rodar em **Borderless Window** (`window_mode: borderless` no `config.yaml`). Janelas DirectX em modo Exclusive Fullscreen impedem qualquer overlay externo — este é o único cenário onde o overlay não aparecerá.

**WPF é exclusivo do Windows:** o projeto `ArcadeOrchestrator.Overlay` não compila em Linux ou macOS. Os projetos `Core` e `Infrastructure` são multiplataforma, mas o sistema como um todo é direcionado ao Windows 10/11.

---

## Documentação

| Arquivo | Conteúdo |
|---|---|
| `docs/system-feature-flows.md` | Registro histórico de todas as features, fluxos internos, decisões técnicas e regras de negócio |
| `backlog.md` | Estado atual do projeto: em andamento, pendentes e concluídas |
| `config/config.yaml` | Referência de todas as opções de configuração com comentários |
| `config/catalog.yaml` | Exemplo de catálogo de ROMs com pesos e franquias |

---

## Roteiro de evolução

**Fase 1 — MVP atual:** Timeout + ProcessWatch + LogParser + Overlay + Hotkeys + RetroArch.

**Fase 2 — Leitura de memória:** plugin `MemoryReadStrategy` usando `ReadProcessMemory` (Win32) com mapas de offset por jogo em YAML. Detecta Game Over lendo diretamente variáveis internas do emulador.

**Fase 3 — Multi-emulador:** `OrchestratorPool` mantém N instâncias em paralelo, pré-aquecendo o próximo jogo enquanto o atual ainda exibe a tela de Game Over. Elimina o tempo de carregamento entre rotações.

---

## Licença

Distribuído sob a licença MIT. Veja o arquivo `LICENSE` para detalhes.
