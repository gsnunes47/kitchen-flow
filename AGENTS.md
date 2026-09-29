# Pizza Flow

## Objetivo

Projeto de estudo e portfólio para aprender e demonstrar:

- C# e ASP.NET Core
- React
- arquitetura orientada a eventos
- mensageria
- workers/consumers
- integração entre serviços

O foco principal não é construir um CRUD complexo.
O foco é compreender e demonstrar mensageria, processamento assíncrono
e fluxo de eventos entre componentes.

## Diretrizes do projeto

- Priorize o fluxo de mensageria e eventos sobre funcionalidades de CRUD.
- Prefira soluções simples e idiomáticas.
- Introduza abstrações, camadas e padrões quando resolverem uma necessidade concreta do projeto; evite adicioná-los apenas por convenção ou antecipação de necessidades futuras.
- Não introduza novas tecnologias ou bibliotecas apenas para aumentar a stack.
- Antes de mudanças arquiteturais relevantes, explique o problema, a solução proposta e os trade-offs.
- Preserve a arquitetura existente quando não houver motivo concreto para alterá-la.

## Aprendizado

Este projeto também é utilizado para aprendizado de C# e .NET.

Não evite uma solução tecnicamente apropriada apenas porque ela introduz
um conceito, padrão ou recurso que eu ainda não conheço.

Quando uma implementação introduzir um conceito novo e relevante:
- explique brevemente o que ele resolve;
- relacione-o aos conhecimentos que já possuo quando houver uma analogia útil;
- destaque particularidades importantes do ecossistema .NET;
- evite complexidade que não seja necessária para o problema.

O objetivo é aprender através da implementação de soluções reais,
não limitar o projeto apenas ao que eu já conheço.

## Validação

Após alterações, execute as validações relevantes quando possível.

Backend:

- `dotnet build`
- `dotnet test`

Frontend:

- `npm run build`
- testes relevantes, quando existirem

Não considere uma tarefa concluída se as alterações introduzirem erros de
compilação ou testes quebrados.

Antes de concluir tarefas maiores, verifique também:

- `git diff`
- `git status`