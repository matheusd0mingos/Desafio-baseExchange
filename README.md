# Base Exchange — Order Exposure

Implementação do desafio técnico da Base Exchange para processamento de ordens e controle de exposição financeira por ativo, com validação de regras de negócio, persistência transacional e publicação de eventos.

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
                    ┌─────────────────┼─────────────────┐
                    │                 │                 │
                    ▼                 ▼                 ▼
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
                 │ Use Cases             │
                 │ Interfaces            │
                 └──────────┬───────────┘
                            │
                            ▼
                 ┌──────────────────────┐
                 │       Domain         │
                 │                      │
                 │ Entities              │
                 │ Value Objects         │
                 │ Strategies            │
                 │ Domain Events         │
                 └──────────────────────┘
                            ▲
                            │ implements
                 ┌──────────┴───────────┐
                 │    Infrastructure    │
                 │                      │
                 │ PostgreSQL / EF Core  │
                 │ Kafka                 │
                 │ Outbox                │
                 │ Repositories          │
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
4. Verificação de idempotência;
5. Execução da regra de negócio;
6. Persistência;
7. Registro na Outbox;
8. Commit da transação;
9. Notificação de eventos após o commit.

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

* Envio de ordens;
* Visualização do resultado do processamento;
* Consulta das exposições;
* Consulta das ordens processadas.

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

    verificar idempotência

    calcular nova exposição

    validar limite

    atualizar exposição

    registrar ordem aceita

    registrar evento na Outbox

COMMIT
```

O lock é liberado automaticamente quando a transação termina.

---

# Idempotência

O `OrderId` é utilizado como identificador idempotente da ordem.

Caso a mesma ordem seja enviada novamente, o sistema consulta as ordens já aceitas antes de modificar a exposição.

Exemplo:

```text
Primeiro envio:
OrderId = ABC
Exposição = R$ 500.000
→ aceita

Segundo envio:
OrderId = ABC
→ não altera novamente a exposição
→ retorna o resultado original
```

Isso evita que retries de clientes ou intermediários causem duplicidade financeira.

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

---

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
  "exposicao_atual": 31988.08,
  "msg_erro": null
}
```

Em caso de rejeição:

```json
{
  "sucesso": false,
  "exposicao_atual": 950000.00,
  "msg_erro": "Erro aconteceu porque o limite de exposição foi excedido."
}
```

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
OrderAccumulator API
        │
        ├── PostgreSQL
        └── Kafka
```

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
* Idempotência;
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

Para executar o projeto localmente:

* .NET 10 SDK
* Docker
* Docker Compose

Opcionalmente:

* Visual Studio / Rider / VS Code
* .NET Aspire workload

---

## Opção 1 — Docker Compose

Suba toda a infraestrutura:

```bash
docker compose up --build
```

Após a inicialização:

| Serviço          | Endereço               |
| ---------------- | ---------------------- |
| Frontend         | http://localhost:3000  |
| API              | http://localhost:8080  |
| Kafka UI         | http://localhost:8085  |
| Aspire Dashboard | http://localhost:18888 |
| pgAdmin          | http://localhost:5050  |

O PostgreSQL fica disponível em:

```text
localhost:5432
```

Banco:

```text
orderdb
```

Usuário:

```text
postgres
```

Senha:

```text
postgres
```

---

## Opção 2 — .NET Aspire

Na raiz da solução, execute o AppHost:

```bash
dotnet run --project src/OrderExposure.AppHost
```

O Aspire irá provisionar e conectar os recursos necessários para o ambiente de desenvolvimento.

O dashboard do Aspire disponibiliza a visualização dos recursos, logs, traces e métricas.

---

# Fluxo de processamento

Uma ordem percorre o seguinte fluxo:

```text
                    POST /ordens
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
* **Idempotência**
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

# Possíveis evoluções

Em um ambiente produtivo, algumas evoluções poderiam ser consideradas:

* Autenticação e autorização;
* Secrets fora do `docker-compose`;
* Kafka com múltiplos brokers;
* PostgreSQL com alta disponibilidade;
* Dead Letter Topic;
* Retenção e replay de eventos;
* Outbox Publisher distribuído com métricas de backlog;
* Testes de integração com containers;
* Testes de carga e concorrência;
* CI/CD;
* Versionamento formal dos eventos;
* Rate limiting;
* Resiliência e circuit breakers entre serviços.

Esses componentes não foram adicionados indiscriminadamente ao desafio para manter o escopo controlado.

---

# Execução dos testes

Execute:

```bash
dotnet test
```

Para executar com maior detalhamento:

```bash
dotnet test --verbosity normal
```

---

# .gitignore

O repositório possui `.gitignore` configurado para evitar o versionamento de arquivos gerados pelo ambiente de desenvolvimento, build e ferramentas locais.

---

## Challenge

This is a challenge by [Coodesh](https://coodesh.com/).
