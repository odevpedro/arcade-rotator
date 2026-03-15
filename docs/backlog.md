# Backlog — ArcadeOrchestrator

## Em andamento

- [ ] `IEmulatorAdapter`: interface abstrata do adapter de emulador
- [ ] `RetroArchAdapter`: implementação concreta (launch/stop via CLI)

---

## Pendentes

### Must Have (MVP)

- [ ] ConfigManager: leitura e validação de YAML com modelos tipados
- [ ] GameCatalog: load de ROMs com metadados, tags e pesos por franquia
- [ ] `IEmulatorAdapter`: interface abstrata do adapter de emulador
- [ ] `RetroArchAdapter`: implementação concreta (launch/stop via CLI)
- [ ] `TimeoutStrategy`: detecção de fim de sessão por inatividade com RawInput hook
- [ ] `ProcessWatchStrategy`: detecção de fim de sessão por encerramento de processo
- [ ] `CompositeDetector`: orquestrador das estratégias de detecção em cascata
- [ ] `RotationEngine`: sorteio ponderado com anti-repeat e bias de franquia
- [ ] `StateMachine`: máquina de estados (IDLE → LAUNCHING → PLAYING → DETECTING_END → COOLDOWN)
- [ ] `OverlayService`: janela WPF click-through, TopMost, transparente e DPI-aware
- [ ] `HotkeyManager`: skip, restart, pause, abrir config (Win32 RegisterHotKey)
- [ ] `Logger`: Serilog com arquivo rotativo e histórico de rotações

### Should Have

- [ ] `LogParserStrategy`: FileSystemWatcher + regex configurável por jogo
- [ ] `FBNeoAdapter`: segundo adapter concreto de emulador
- [ ] Hot-reload de configuração (apenas em estado IDLE)
- [ ] Interface mínima de configuração (WPF, fora do jogo)
- [ ] Detecção de inatividade de input via RawInput como fator auxiliar do timeout
- [ ] Override de `detection_strategy` por jogo individual no YAML

### Could Have

- [ ] Tray icon com menu de contexto (minimizar para bandeja)
- [ ] Estatísticas de sessão (tempo médio por jogo, jogo mais jogado)
- [ ] Plugin interface para `MemoryReadStrategy` (arquitetura preparada, implementação futura)
- [ ] Suporte a múltiplos perfis de configuração (família, solo, festa)
- [ ] Countdown animado no overlay antes do próximo jogo
- [ ] Exportação do histórico de rotações em CSV
- [ ] `OrchestratorPool`: pré-aquecimento do próximo emulador em background (elimina loading entre rotações)

---

## Concluídas

- [x] Especificação técnica e arquitetura do MVP definidas
- [x] Modelos de configuração YAML documentados (config.yaml + catalog.yaml)
- [x] Estrutura de pastas do projeto definida
- [x] Protótipo de código das interfaces e implementações-chave gerado
- [x] Plano de evolução para Fase 2 (leitura de memória) e Fase 3 (múltiplos emuladores) documentado
- [x] Shell script `setup.sh` criado (gera solução .NET, projetos, referências, NuGet packages, YAML e Git)
