# Kitchen Flow

Projeto de estudo de uma arquitetura orientada a eventos para restaurantes.
O sistema registra pedidos, persiste os dados no PostgreSQL e publica eventos
no RabbitMQ para processamento assíncrono por outros componentes.

## Tecnologias

- C# e ASP.NET Core
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- HTML, CSS e JavaScript
- Docker Compose

## Execução local

Suba PostgreSQL e RabbitMQ:

```bash
docker compose up -d
```

Inicie a API com hot reload:

```bash
dotnet watch --project backend/KitchenFlow.csproj
```

A API fica disponível em `http://localhost:5196`.

Sirva a interface de caixa em outro terminal:

```bash
python3 -m http.server 5500 --directory interface-caixa
```

Depois, acesse `http://localhost:5500`.

## Banco de dados

As migrations são aplicadas automaticamente na inicialização da API. Também
podem ser aplicadas manualmente:

```bash
dotnet ef database update --project backend/KitchenFlow.csproj
```

Para criar uma migration:

```bash
dotnet ef migrations add NomeDaMigration \
  --project backend/KitchenFlow.csproj \
  --output-dir Data/Migrations
```

## Fluxo atual

1. A interface envia um pedido com cliente, telefone e itens.
2. A API persiste o pedido no PostgreSQL.
3. A API publica o evento `OrderCreated` no RabbitMQ.

Nas proximas etapas montaremos os workers para consumir os eventos e disparar ações.
