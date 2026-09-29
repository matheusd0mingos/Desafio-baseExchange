# Base Exchange — Order Exposure

Implementação do desafio técnico da Base Exchange para processamento de ordens e controle de exposição financeira por ativo, com validação de regras de negócio, persistência transacional e publicação de eventos.

## Sumário

- [Visão geral](#visão-geral)
- [Requisitos do desafio](#requisitos-do-desafio)
- [Como executar](#como-executar)
  - [Pré-requisitos](#pré-requisitos)
  - [Opção 1 — Docker Compose](#opção-1--docker-compose)
  - [Opção 2 — .NET Aspire](#opção-2--net-aspire)
- [Roteiro de demonstração](#roteiro-de-demonstração)
- [Arquitetura](#arquitetura)
  - [Mapa de dependências](#mapa-de-dependências)
  - [Estrutura da solução](#estrutura-da-solução)
  - [Fluxo de processamento](#fluxo-de-processamento)
- [Decisões arquiteturais](#decisões-arquiteturais)
  - [Clean Architecture](#clean-architecture)
  - [Domain-Driven Design](#domain-driven-design)
  - [Strategy Pattern](#strategy-pattern)
- [Concorrência e consistência](#concorrência-e-consistência)
- [Transactional Outbox](#transactional-outbox)
- [Kafka](#kafka)
- [PostgreSQL](#postgresql)
  - [Modelo de dados](#modelo-de-dados)
- [API](#api-1)
- [Blazor WebAssembly](#blazor-webassembly)
- [.NET Aspire](#net-aspire)
- [Observabilidade](#observabilidade)
- [Testes](#testes)
- [Tecnologias](#tecnologias)
- [Sobre decisões de escopo](#sobre-decisões-de-escopo)

## Visão geral

O projeto é composto por duas aplicações:

* **OrderGenerator** — aplicação web responsável pela entrada das ordens e apresentação do resultado ao usuário.
* **OrderAccumulator** — API responsável por validar, processar e persistir as ordens, mantendo a exposição financeira de cada ativo dentro do limite definido.

Para garantir consistência sob concorrência, a atualização da exposição é realizada de forma transacional no PostgreSQL utilizando bloqueio pessimista da linha (`SELECT ... FOR UPDATE`).

Os eventos resultantes do processamento são registrados em uma **Transactional Outbox** e posteriormente publicados no **Apache Kafka**, evitando inconsistências entre a transação do banco de dados e a publicação de mensagens.

---

## Requisitos do desafio

Cada ordem possui:

* **Ativo:** `PETR4`, `VALE3` ou `VIIA4`
* **Lado:** Compra (`C`) ou Venda (`V`)
* **Quantidade:** inteiro positivo menor que `100.000`
* **Preço:** positivo, menor que `1.000` e múltiplo de `0,01`

A exposição financeira de cada ativo é calculada como:

```text
Exposição = Σ(Preço × Quantidade das compras)
          - Σ(Preço × Quantidade das vendas)
```

O limite de exposição é de:

```text
R$ 1.000.000,00
```

Uma ordem somente é aceita quando a nova exposição permanecer dentro do limite absoluto:

```text
|Nova Exposição| <= R$ 1.000.000,00
```

Quando uma ordem é rejeitada, nenhuma alteração de exposição é persistida.

---

# Arquitetura

A solução utiliza uma separação em camadas inspirada em **Clean Architecture**, com o domínio isolado dos detalhes de infraestrutura.

## Visão geral

```text
                           ┌──────────────────────┐
                           │    OrderGenerator    │
                           │   Blazor WebAssembly │
                           └──────────┬───────────┘
                                      │ HTTP / JSON
                                      ▼
                           ┌──────────────────────┐
                           │  OrderAccumulator    │
                           │      ASP.NET Core    │
                           └──────────┬───────────┘
                                      │
                     ┌────────────────┼─────────────────┐
                     │                │                 │
                     ▼                ▼                 ▼
               ┌───────────┐    ┌────────────┐    ┌────────────┐
               │ Application│    │ PostgreSQL │    │   Kafka    │
               │ + Domain   │    │            │    │            │
               └───────────┘    └────────────┘    └────────────┘
                                      ▲
                                      │
                               Transactional
                                  Outbox
```

## Mapa de dependências

A arquitetura organiza as dependências de forma que o domínio permaneça independente dos detalhes de infraestrutura.

```text
                 ┌──────────────────────┐
                 │   OrderGenerator     │
                 │  Blazor WebAssembly  │
                 └──────────┬───────────┘
                            │ HTTP
                            ▼
                 ┌──────────────────────┐
                 │         API          │
                 └──────────┬───────────┘
                            │
                            ▼
                 ┌──────────────────────┐
                 │    Application       │
                 │                      │
                 │ Use Cases            │
                 │ Interfaces           │
                 └──────────┬───────────┘
                            │
                            ▼
                 ┌──────────────────────┐
                 │       Domain         │
                 │                      │
                 │ Entities             │
                 │ Value Objects        │
                 │ Strategies           │
                 │ Domain Events        │
                 └──────────────────────┘
                            ▲
                            │ implements
                 ┌──────────┴───────────┐
                 │    Infrastructure    │
                 │                      │
                 │ PostgreSQL / EF Core │
                 │ Kafka                │
                 │ Outbox               │
                 │ Repositories         │
                 └──────────────────────┘
```

**Regra de dependência:** o domínio não conhece detalhes de infraestrutura. As implementações de persistência e mensageria dependem das abstrações definidas pelas camadas internas.

## Estrutura da solução

```text
src/
├── OrderAccumulator.Api/
├── OrderAccumulator.Application/
├── OrderAccumulator.Domain/
├── OrderAccumulator.Infrastructure/
├── OrderExposure.Contracts/
├── OrderExposure.AppHost/
├── OrderExposure.ServiceDefaults/
└── OrderGenerator/

tests/
└── OrderAccumulator.Tests/
```

### Domain

Contém as regras de negócio e não possui dependência de infraestrutura.

Principais conceitos:

* `Ordem`
* `ExposicaoAtivo`
* `Ativo`
* `Lado`
* `Preco`
* `Quantidade`
* Domain Events
* Value Objects
* Strategies

O domínio é responsável por decidir se uma ordem pode ou não alterar a exposição.

### Application

Contém os casos de uso e as abstrações utilizadas pela aplicação.

O principal caso de uso é:

```text
ProcessarOrdemUseCase
```

Ele coordena:

1. Validação da ordem;
2. Abertura da transação;
3. Bloqueio da exposição do ativo;
4. Execução da regra de negócio;
5. Persistência;
6. Registro na Outbox;
7. Commit da transação;
8. Notificação de eventos após o commit.

### Infrastructure

Implementa os detalhes externos da aplicação:

* Entity Framework Core
* PostgreSQL
* Repositories
* Unit of Work
* Transactional Outbox
* Kafka Producer
* Persistência dos eventos
* Observers

### API

Camada HTTP responsável por expor os endpoints REST e traduzir as requisições para os casos de uso da aplicação.

### OrderGenerator

Frontend desenvolvido com **Blazor WebAssembly**, responsável pela interação com o usuário.

Possui telas para:

* **Nova ordem** — envio de ordens com validação imediata e exibição da resposta da requisição;
* **Exposição e ordens** — exposição por ativo com barra de uso do limite e últimas ordens aceitas;
* **Caixa de saída** — eventos da Outbox e seu status de publicação no Kafka.

As telas de consulta se atualizam a cada 3 segundos.

---

# Decisões arquiteturais

## Clean Architecture

A solução separa regras de negócio de detalhes externos.

A direção das dependências é:

```text
API
 │
 ▼
Application
 │
 ▼
Domain

Infrastructure ───────► Application / Domain
```

O domínio não conhece:

* PostgreSQL;
* Entity Framework;
* Kafka;
* HTTP;
* Blazor.

Isso permite testar as regras de negócio sem depender de infraestrutura externa.

---

## Domain-Driven Design

O núcleo do processamento foi modelado utilizando conceitos de DDD.

### Aggregate Root

`ExposicaoAtivo` representa o agregado responsável pela exposição financeira de um ativo.

A alteração da exposição acontece através de:

```csharp
exposicao.Registrar(ordem, estrategia)
```

Assim, a regra que determina se uma exposição pode ultrapassar o limite permanece dentro do domínio.

### Value Objects

Foram utilizados Value Objects para representar valores que possuem regras próprias:

```text
Quantidade
Preco
```

Por exemplo, é impossível criar um `Quantidade` inválido através da API pública do objeto.

Isso evita espalhar validações de negócio pela aplicação.

---

# Strategy Pattern

Compra e venda possuem impactos diferentes na exposição:

```text
Compra → + (preço × quantidade)

Venda  → - (preço × quantidade)
```

Essa regra foi encapsulada através de:

```text
ILadoStrategy
├── CompraStrategy
└── VendaStrategy
```

A seleção da estratégia é feita através de:

```text
ILadoStrategyFactory
└── LadoStrategyFactory
```

Dessa forma, o agregado `ExposicaoAtivo` não precisa possuir condicionais específicos para cada lado da ordem.

---

# Concorrência e consistência

Esse é um dos pontos centrais da implementação.

Considere duas requisições simultâneas:

```text
Exposição atual: R$ 900.000

Ordem A: + R$ 70.000
Ordem B: + R$ 80.000
```

Se ambas simplesmente lessem a exposição antes de atualizá-la, poderiam aceitar simultaneamente as duas ordens.

O resultado seria:

```text
900.000 + 70.000 + 80.000 = 1.050.000
```

violando o limite.

Para evitar isso, a exposição do ativo é bloqueada dentro da transação:

```sql
SELECT ativo, valor
FROM exposicoes
WHERE ativo = ...
FOR UPDATE
```

Assim, somente uma transação por vez pode modificar a exposição daquele ativo.

O fluxo é:

```text
BEGIN TRANSACTION

    SELECT ... FOR UPDATE

    calcular nova exposição

    validar limite

    atualizar exposição

    registrar ordem aceita

    registrar evento na Outbox

COMMIT
```

O lock é liberado automaticamente quando a transação termina.

---

# Transactional Outbox

A publicação de eventos no Kafka não é realizada diretamente dentro da transação de negócio.

Em vez disso, o sistema utiliza uma **Transactional Outbox**.

Durante a mesma transação que altera a exposição:

```text
PostgreSQL

┌─────────────────────────────┐
│ Exposição                   │
│ Ordem aceita                │
│ Outbox                      │
└─────────────────────────────┘
             │
             │ COMMIT
             ▼
       estado persistido
```

Posteriormente, um `BackgroundService` consulta as mensagens pendentes da Outbox e publica no Kafka.

Isso evita o problema:

```text
Banco atualizado
      +
Kafka falhou
      =
evento perdido
```

Com a Outbox:

```text
Banco atualizado
      +
evento armazenado na Outbox
      =
evento pode ser publicado posteriormente
```

Se o Kafka estiver indisponível, a mensagem permanece na Outbox e será tentada novamente.

---

# Kafka

O Kafka é utilizado como mecanismo de publicação dos eventos de processamento.

A API não depende do Kafka para decidir se uma ordem pode ser aceita.

A fonte de verdade da exposição é o PostgreSQL.

O Kafka é utilizado para distribuição assíncrona dos eventos após o processamento.

A chave das mensagens é o ativo:

```text
PETR4
VALE3
VIIA4
```

Isso permite que mensagens de um mesmo ativo sejam direcionadas para a mesma partição, preservando a ordem relativa dos eventos daquele ativo dentro da partição.

Tópicos:

| Tópico | Conteúdo |
| ------ | -------- |
| `ordens-aceitas` | Ordens aceitas |
| `ordens-rejeitadas` | Ordens rejeitadas (auditoria) |

As mensagens são **eventos de integração**, separados dos eventos internos do domínio, no mesmo vocabulário do edital e versionados:

```json
{
  "versao": 1,
  "evento": "OrdemAceita",
  "ordem_id": "d1c1fae0-d55d-4d54-b3ac-01b8fe4ecbb2",
  "ativo": "PETR4",
  "lado": "C",
  "quantidade": 584,
  "preco": 54.87,
  "sucesso": true,
  "exposicao_atual": 32044.08,
  "msg_erro": null,
  "ocorrida_em": "2026-09-29T13:44:23.4499606+00:00"
}
```

O producer utiliza:

```text
acks = all
enable.idempotence = true
```

para aumentar a segurança da publicação.

---

# PostgreSQL

O PostgreSQL é utilizado como banco de dados transacional.

Principais responsabilidades:

* Persistência das exposições;
* Persistência das ordens aceitas;
* Persistência da Outbox;
* Controle de concorrência através de locks transacionais.

O Entity Framework Core é utilizado como ORM.

## Modelo de dados

São três tabelas, criadas pelas migrations do EF Core ao subir a API.

```text
exposicoes                 ordens_aceitas                 outbox
──────────────────         ────────────────────────       ─────────────────────────
ativo      PK              ordem_id             PK        id            PK
valor                      ativo                          tipo
                           lado                           chave
                           quantidade                     conteudo      (jsonb)
                           preco                          criada_em
                           exposicao_resultante           publicada_em  (null = pendente)
                           occurred_on
```

| Tabela | Papel | Decisão |
| ------ | ----- | ------- |
| `exposicoes` | O saldo atual de cada ativo | Uma linha por ativo, criada zerada pela migration: o `SELECT ... FOR UPDATE` sempre tem uma linha para travar, inclusive na primeira ordem de um ativo |
| `ordens_aceitas` | Registro das ordens aceitas | A chave primária é o `ordem_id` |
| `outbox` | Eventos esperando publicação no Kafka | Índice parcial em `criada_em` somente das pendentes (`WHERE publicada_em IS NULL`) |

Valores monetários usam `numeric` (decimal exato), nunca ponto flutuante. `Ativo` e `Lado` são gravados como texto (`PETR4`, `Compra`), legíveis no pgAdmin e imunes a uma reordenação do enum no código.

As tabelas do banco são modelos de persistência separados do domínio: o repositório lê a linha e reconstrói o agregado `ExposicaoAtivo`, que nunca conhece o EF Core.

# API

A API utiliza ASP.NET Core e expõe endpoints REST.

A entrada de uma ordem possui o formato:

```json
{
  "ativo": "PETR4",
  "lado": "C",
  "quantidade": 584,
  "preco": 54.87
}
```

A resposta possui o formato:

```json
{
  "sucesso": true,
  "exposicao_atual": 32044.08,
  "msg_erro": null
}
```

Em caso de rejeição:

```json
{
  "sucesso": false,
  "exposicao_atual": 32044.08,
  "msg_erro": "Erro aconteceu porque a ordem levaria a exposição de PETR4 a R$ 100.030.044,09, ultrapassando o limite de R$ 1.000.000,00."
}
```

Todas as respostas, inclusive as de erro, seguem esse formato.

| Status | Quando |
| ------ | ------ |
| `200`  | Ordem aceita |
| `400`  | Dado inválido (ativo, lado, quantidade, preço ou JSON malformado) |
| `422`  | Limite de exposição ultrapassado |
| `503`  | Banco indisponível |

Endpoints de consulta:

| Endpoint | Retorna |
| -------- | ------- |
| `GET /api/exposicoes` | Exposição atual e percentual do limite usado, por ativo |
| `GET /api/ordens?limite=50` | Últimas ordens aceitas (máximo 200) |
| `GET /api/outbox?limite=50` | Mensagens da Outbox e seu status (`Pendente` / `Publicada`) |

---

# Blazor WebAssembly

O `OrderGenerator` foi implementado como uma aplicação **Blazor WebAssembly**.

O frontend realiza chamadas HTTP para o `OrderAccumulator.Api`.

A escolha permite manter o frontend dentro do ecossistema .NET/C#, compartilhando os contratos de comunicação através do projeto:

```text
OrderExposure.Contracts
```

Os contratos são compartilhados entre frontend e backend, reduzindo duplicação de modelos de request/response.

---

# .NET Aspire

O projeto utiliza **.NET Aspire** para orquestração do ambiente de desenvolvimento.

O AppHost declara os recursos necessários:

```text
Gateway (YARP) :5100
   ├── /api/*  → OrderAccumulator API ──┬── PostgreSQL (+ pgAdmin)
   │                                    └── Kafka (+ Kafka UI)
   └── /*      → OrderGenerator (Blazor)
```

O gateway YARP faz no desenvolvimento o papel que o nginx faz no Docker Compose: front e API na mesma origem, sem CORS.

Além disso, são disponibilizados:

* PostgreSQL;
* pgAdmin;
* Kafka;
* Kafka UI;
* Service Discovery;
* Health Checks;
* OpenTelemetry;
* Aspire Dashboard.

O Aspire é utilizado principalmente para simplificar a composição e observabilidade do ambiente local.

---

# Observabilidade

A aplicação utiliza:

* OpenTelemetry;
* Health Checks;
* Logging estruturado;
* Tracing HTTP;
* Métricas ASP.NET Core;
* Métricas de runtime;
* Instrumentação de HttpClient.

Endpoints de health check disponíveis em desenvolvimento:

```text
/health
/alive
```

O Aspire Dashboard pode ser utilizado para visualizar traces, logs e métricas.

---

# Testes

Os testes estão concentrados em:

```text
tests/OrderAccumulator.Tests/
```

São testados principalmente:

### Domain

* Criação de ordens;
* Validação de preço;
* Validação de quantidade;
* Validação de ativo e lado;
* Cálculo da exposição;
* Limite de exposição;
* Estratégias de compra e venda.

### Application

* Processamento de ordens;
* Aceitação;
* Rejeição;
* Comportamento da transação.

A maior parte das regras de negócio pode ser testada sem banco de dados ou Kafka.

---

# Tecnologias

| Tecnologia            | Utilização                |
| --------------------- | ------------------------- |
| C#                    | Linguagem principal       |
| .NET 10               | Plataforma                |
| ASP.NET Core          | API REST                  |
| Blazor WebAssembly    | Frontend                  |
| Entity Framework Core | ORM                       |
| PostgreSQL            | Persistência transacional |
| Apache Kafka          | Mensageria/eventos        |
| .NET Aspire           | Orquestração              |
| Docker                | Containerização           |
| OpenTelemetry         | Observabilidade           |
| xUnit                 | Testes                    |
| Bootstrap             | Estilização do frontend   |

---

# Como executar

## Pré-requisitos

| Para | Precisa de |
| ---- | ---------- |
| Rodar com Docker Compose | **Somente Docker** (não é preciso ter o .NET instalado) |
| Rodar com .NET Aspire | .NET 10 SDK, Docker e a [CLI do Aspire](https://aspire.dev) |
| Rodar os testes | .NET 10 SDK |

---

## Opção 1 — Docker Compose

Suba toda a infraestrutura:

```bash
docker compose up --build -d
```

A primeira execução demora alguns minutos (download das imagens e compilação do Blazor). Após a inicialização:

| Serviço          | Endereço               |
| ---------------- | ---------------------- |
| Frontend         | http://localhost:3000  |
| API              | http://localhost:8080  |
| Kafka UI         | http://localhost:8085  |
| Aspire Dashboard | http://localhost:18888 |
| pgAdmin           | http://localhost:5050  |

### Senhas

Não há senhas no repositório. Na primeira execução, o serviço `secrets-init` gera senhas aleatórias em um volume próprio, e PostgreSQL, pgAdmin e API as leem como arquivos (`*_FILE` e `/run/secrets`). Para ver as credenciais geradas:

```bash
docker compose logs secrets-init
```

No pgAdmin, registre o servidor com host `postgres`, usuário `postgres` e a senha exibida no log. O PostgreSQL não é exposto fora da rede do Compose.

### Comandos úteis

```bash
docker compose logs -f api   # acompanhar a API
docker compose down          # parar, mantendo os dados
docker compose down -v       # parar e ZERAR tudo (dados e senhas)
```

O banco nasce com os três ativos zerados. Os dados persistem entre reinícios de propósito: uma exposição não pode zerar porque um container reiniciou.

---

## Opção 2 — .NET Aspire

Na raiz da solução, execute o AppHost:

```bash
dotnet run --project src/OrderExposure.AppHost
```

O AppHost sobe PostgreSQL (+ pgAdmin), Kafka (+ Kafka UI), a API, o frontend e o gateway YARP.

Acesse o frontend pelo gateway em:

```text
http://localhost:5100
```

Os demais links ficam no dashboard exibido no terminal.

A API espera o PostgreSQL, mas **não** espera o Kafka: ela aceita ordens mesmo com o Kafka fora do ar.

---

# Fluxo de processamento

Uma ordem percorre o seguinte fluxo:

```text
                    POST /api/ordens
                         │
                         ▼
                   ┌─────────────┐
                   │     API     │
                   └──────┬──────┘
                          │
                          ▼
                 ┌─────────────────┐
                 │ Application     │
                 │ ProcessarOrdem  │
                 └────────┬────────┘
                          │
                          ▼
                 ┌─────────────────┐
                 │ Domain          │
                 │                 │
                 │ ExposicaoAtivo  │
                 │ + Strategy      │
                 └────────┬────────┘
                          │
                       decisão
                       /       \
                  aceita       rejeita
                     │             │
                     └──────┬──────┘
                            ▼
                 ┌─────────────────┐
                 │ PostgreSQL      │
                 │                 │
                 │ Exposição       │
                 │ Ordem           │
                 │ Outbox          │
                 └────────┬────────┘
                          │ COMMIT
                          ▼
                      ┌─────────┐
                      │  Kafka  │
                      └─────────┘
```

---

# Princípios utilizados

A implementação procura aplicar os seguintes princípios:

* **Separation of Concerns**
* **Dependency Inversion**
* **Single Responsibility**
* **Domain-Driven Design**
* **Clean Architecture**
* **Fail-safe persistence**
* **Consistência transacional**
* **Observabilidade**
* **Testabilidade**

A complexidade adicional de PostgreSQL + Kafka + Outbox foi utilizada especificamente para tratar problemas de consistência, concorrência e confiabilidade de publicação, em vez de utilizar mensageria apenas como requisito arquitetural.

---

# Estrutura de infraestrutura

O ambiente Docker possui:

```text
┌──────────────────────────────────────────────┐
│                  Docker                      │
│                                              │
│  ┌──────────┐       ┌──────────────┐        │
│  │ Frontend │──────►│      API     │        │
│  └──────────┘       └──────┬───────┘        │
│                             │                │
│                 ┌───────────┴───────────┐    │
│                 ▼                       ▼    │
│          ┌─────────────┐         ┌─────────┐ │
│          │ PostgreSQL  │         │  Kafka  │ │
│          └─────────────┘         └─────────┘ │
│                 │                       │    │
│                 ▼                       ▼    │
│              pgAdmin                 Kafka UI│
│                                              │
│              Aspire Dashboard                │
└──────────────────────────────────────────────┘
```

---

# Sobre decisões de escopo

Algumas decisões foram tomadas pensando na confiabilidade do processamento financeiro:

### PostgreSQL como fonte da verdade

A exposição não é mantida apenas em memória. O estado persistido no PostgreSQL permite recuperação após reinício da aplicação.

### Kafka desacoplado do processamento síncrono

Uma indisponibilidade do Kafka não impede que a API continue processando ordens, desde que o PostgreSQL esteja disponível.

Os eventos ficam armazenados na Outbox para publicação posterior.

### Concorrência por ativo

O lock pessimista é aplicado somente à linha correspondente ao ativo processado.

Assim, ordens concorrentes para ativos diferentes não precisam bloquear umas às outras.

Exemplo:

```text
PETR4 → lock PETR4
VALE3 → lock VALE3
VIIA4 → lock VIIA4
```

### Limite inclusivo

Uma exposição exatamente igual a:

```text
R$ 1.000.000,00
```

é válida.

Somente valores que ultrapassam o limite absoluto são rejeitados.

---

# Roteiro de demonstração

Prova ao vivo de que o Kafka fora do ar não derruba o sistema nem perde eventos:

```bash
docker compose stop kafka
```

1. Em **Nova ordem**, envie algumas ordens: todas são aceitas normalmente.
2. Em **Caixa de saída**, os eventos aparecem como ⏳ **Pendente**.

```bash
docker compose start kafka
```

3. Em alguns segundos, os eventos passam sozinhos para ✅ **Publicada**.
4. No **Kafka UI** (http://localhost:8085), as mensagens estão nos tópicos.

---

# Execução dos testes

Execute:

```bash
dotnet test tests/OrderAccumulator.Tests
```

Apontar para o projeto de testes compila apenas o necessário. Rodar `dotnet test` na raiz também compila o AppHost, que exige a CLI do Aspire.

Para executar com maior detalhamento:

```bash
dotnet test tests/OrderAccumulator.Tests --verbosity normal
```

Destaque: o teste `OrdensSimultaneas_NaoPodemFurarOLimite` dispara 20 compras de R$ 100.000 ao mesmo tempo, e exatamente 10 são aceitas.

---

# .gitignore

O repositório possui `.gitignore` configurado para evitar o versionamento de arquivos gerados pelo ambiente de desenvolvimento, build e ferramentas locais.

---

## Challenge

This is a challenge by [Coodesh](https://coodesh.com/).
